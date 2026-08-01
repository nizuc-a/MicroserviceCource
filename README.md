# MicroserviceCourse — система управления мероприятиями

Микросервисное приложение для управления событиями и бронированиями.  
Реализовано на **ASP.NET Core 10**, **PostgreSQL**, **Apache Kafka**, **Redis** и **чистой архитектуре**.

---

## Оглавление

- [Состав системы](#состав-системы)
- [Архитектура](#архитектура)
- [Поток данных через Kafka](#поток-данных-через-kafka)
- [Стратегия кеширования](#стратегия-кеширования)
- [Наблюдаемость](#наблюдаемость)
- [Запуск проекта](#запуск-проекта)
- [API Endpoints](#api-endpoints)
- [Аутентификация и авторизация](#аутентификация-и-авторизация)
- [Тестирование](#тестирование)

---

## Состав системы

| Компонент | Назначение | Порт (Docker / локально) |
|-----------|------------|--------------------------|
| **UserService** | Регистрация, вход, выдача JWT | 5134 / 5134 |
| **EventService** | CRUD событий, учёт мест | 5191 / 5191 |
| **BookingService** | Создание и отмена броней | 5099 / 5099 |
| **users-db** | PostgreSQL для UserService | 5433 |
| **events-db** | PostgreSQL для EventService | 5434 |
| **bookings-db** | PostgreSQL для BookingService | 5435 |
| **Kafka + Zookeeper** | Асинхронный обмен сообщениями | 9092 |
| **Redis** | Кеш EventService (событие по id, топ-10) | 6379 |
| **Prometheus** | Сбор метрик с `/metrics` | 9090 |
| **Jaeger** | UI и приём OTLP-трейсов | 16686 / 4317 |
| **Grafana** | Дашборды по метрикам | 3000 |

Общие проекты:

- `Shared.Domain` — контракты событий (`BookingConfirmed`, `BookingCreated` и др.), константы топиков Kafka (`KafkaTopics`), общие сущности outbox/inbox
- `Shared.Api` — JWT-аутентификация, Swagger, настройки

---

## Архитектура

Каждый микросервис построен по принципам **Clean Architecture** (4 слоя):

```
{Service}.Domain          # Сущности, исключения, доменные правила
        ↑
{Service}.Application     # Use cases, DTO, интерфейсы портов
        ↑
{Service}.Infrastructure # EF Core, репозитории, Kafka producer
        ↑
{Service}.Api             # Контроллеры, DI, BackgroundService
```

- У каждого сервиса **своя база данных** и **свои миграции EF Core**
- Связь между сервисами — только по идентификаторам (`UserId`, `EventId`, `BookingId`) через Kafka
- Прямых HTTP-вызовов между сервисами нет

---

## Поток данных через Kafka

### Создание и подтверждение брони

```mermaid
sequenceDiagram
    participant Client
    participant BookingService
    participant Kafka
    participant EventService

    Client->>BookingService: POST /events/{id}/book
    BookingService->>BookingService: save Pending booking + outbox
    BookingService->>Kafka: BookingCreated (topic bookings)
    Kafka->>EventService: consume BookingCreated
    EventService->>EventService: reserve seat, save + outbox
    EventService->>Kafka: BookingConfirmed (topic bookings)
    Kafka->>BookingService: consume BookingConfirmed
    BookingService->>BookingService: status Confirmed
```

### Контракт BookingCreated

Определён в [`Shared/Shared.Domain/Contracts/Booking/BookingCreated.cs`](Shared/Shared.Domain/Contracts/Booking/BookingCreated.cs):

| Поле | Тип | Описание |
|------|-----|----------|
| `BookingId` | Guid | Идентификатор брони |
| `EventId` | Guid | Идентификатор события |
| `UserId` | Guid | Идентификатор пользователя |
| `SeatCount` | int | Количество мест (по умолчанию 1) |
| `CreatedAt` | DateTime | Момент создания брони (UTC) |

При создании брони клиент передаёт количество мест в теле запроса:

```json
POST /events/{eventId}/book
{
  "seatCount": 2
}
```

Имена топиков вынесены в [`Shared/Shared.Domain/Kafka/KafkaTopics.cs`](Shared/Shared.Domain/Kafka/KafkaTopics.cs):

- `KafkaTopics.Bookings` — `"bookings"` (события бронирования)
- `KafkaTopics.Events` — `"events"` (события домена Events, например `EventDeleted`)

### Outbox / Inbox

Каждый сервис использует паттерн **Transactional Outbox** для надёжной публикации и **Inbox** для идемпотентной обработки входящих сообщений.

---

## Стратегия кеширования

EventService использует **Redis** и паттерн **Cache-Aside** для снижения нагрузки на PostgreSQL на горячих чтениях.

### Что кешируется и почему

| Сценарий | Ключ | TTL (по умолчанию) | Зачем |
|----------|------|--------------------|-------|
| Событие по идентификатору (`GET /events/{id}`) | `event:{id}` | 1 мин (`Redis:EventTtlMinutes`) | Частый точечный доступ; короткая свежесть |
| Топ-10 по проценту продаж (`GET /events/top`) | `events:top10` | 5 мин (`Redis:TopTtlMinutes`) | Виджет главной без авторизации; небольшой лаг допустим |

Процент продаж: `(total_seats - available_seats) / total_seats`.

Слои:

- абстракция `ICacheRepository` — в Application;
- реализация `RedisRepository` (StackExchange.Redis) — в Infrastructure;
- `IConnectionMultiplexer` регистрируется как **singleton** в DI.

### Поведение при чтении

1. Сначала запрос в Redis.
2. При **попадании** в кеш база данных не вызывается.
3. При **промахе** данные читаются из PostgreSQL и записываются в кеш с TTL.

### Обновление кеша при изменении данных

Для отдельного события выбрана **инвалидация при записи** (а не write-through):

1. Сначала изменения сохраняются в БД.
2. Затем удаляется ключ `event:{id}`.
3. Следующий `GET` прогревает кеш заново.

Инвалидация выполняется при:

- `PUT /events/{id}` и `DELETE /events/{id}`;
- обработке Kafka-сообщений, меняющих места: `BookingCreated` → `BookEvent`, `BookingCancelled` → `ReleaseBookingAsync`.

Кеш топ-10 **не инвалидируется** на каждое бронирование: рейтинг — агрегат, для него достаточно TTL. Явная инвалидация при каждой броне была бы избыточной.

Если процесс оборвётся между записью в БД и удалением ключа, база останется актуальной — кеш устареет максимум до истечения TTL, после чего обновится.

### Недоступный Redis

- `AbortOnConnectFail: false` — сервис стартует даже если Redis ещё не готов.
- Ошибки соединения и десериализации в `RedisRepository` логируются и **не пробрасываются** клиенту: запрос идёт в базу как при промахе.

### Конфигурация

Секция `Redis` в [`EventService/EventService.Api/appsettings.json`](EventService/EventService.Api/appsettings.json):

- локально: `Host: localhost:6379`;
- в Docker Compose: переменная `Redis__Host=redis:6379` у `events-service`.

---

## Наблюдаемость

Во все три сервиса подключены **OpenTelemetry SDK**, **Serilog** (JSON-логи) и экспорт в стек мониторинга.

| Инструмент | Назначение |
|------------|------------|
| **OpenTelemetry** | Трейсы (HTTP + EF Core) и метрики ASP.NET Core / .NET Runtime |
| **Prometheus** | Скрейп эндпоинтов `/metrics` (конфиг [`prometheus.yml`](prometheus.yml)) |
| **Jaeger** | Хранение и просмотр трейсов (OTLP gRPC `:4317`) |
| **Grafana** | Дашборд latency / throughput / error rate ([`grafana/dashboards/aspnet-metrics.json`](grafana/dashboards/aspnet-metrics.json)) |
| **Serilog** | Структурированные логи в Compact JSON в stdout |

### UI мониторинга

| Сервис | URL | Доступ |
|--------|-----|--------|
| Prometheus | http://localhost:9090 | — |
| Jaeger | http://localhost:16686 | — |
| Grafana | http://localhost:3000 | `admin` / `admin` |

### Запуск стека мониторинга

Вместе со всей системой:

```bash
docker compose up --build
```

Только инфраструктура + мониторинг (без сборки API):

```bash
docker compose up -d zookeeper kafka kafka-init redis users-db events-db bookings-db prometheus jaeger grafana
```

В Docker у сервисов задано `Otlp__Endpoint=http://jaeger:4317`. Локально в `appsettings.json` используется `http://localhost:4317`. Имя сервиса для трейсов/метрик берётся из `Otlp:ServiceName`.

В Grafana добавьте datasource Prometheus с URL `http://prometheus:9090` (из контейнера) или `http://localhost:9090` (если Grafana запущена отдельно), затем импортируйте [`grafana/dashboards/aspnet-metrics.json`](grafana/dashboards/aspnet-metrics.json).

Проверка:

1. `http://localhost:5134/metrics` (и порты 5191, 5099) — формат Prometheus
2. Jaeger UI — сервисы `UserService` / `EventService` / `BookingService`, спаны HTTP и SQL
3. Prometheus → Status → Targets — все три job'а в состоянии UP

---

## Запуск проекта

### Вариант 1: Вся система в Docker (рекомендуется)

```bash
git clone https://github.com/nizuc-a/MicroserviceCource.git
cd MicroserviceCource
git checkout sprint-11
docker compose up --build
```

Поднимаются Zookeeper, Kafka, Redis, три базы данных, три API-сервиса, Prometheus, Jaeger и Grafana. Миграции применяются автоматически при старте.

| Сервис | Swagger |
|--------|---------|
| UserService | http://localhost:5134/swagger |
| EventService | http://localhost:5191/swagger |
| BookingService | http://localhost:5099/swagger |

Остановка:

```bash
docker compose down
```

### Вариант 2: Локальный запуск API

Поднять только инфраструктуру:

```bash
docker compose up -d zookeeper kafka kafka-init redis users-db events-db bookings-db prometheus jaeger grafana
```

Запустить каждый API в отдельном терминале:

```bash
cd UserService/UserService.Api && dotnet run
cd EventService/EventService.Api && dotnet run
cd BookingService/BookingService.Api && dotnet run
```

При локальном запуске используются порты и строки подключения из `appsettings.json` каждого сервиса.

---

## API Endpoints

### UserService (http://localhost:5134)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| POST | `/auth/register` | Регистрация пользователя | Без токена |
| POST | `/auth/login` | Получение JWT-токена | Без токена |

### EventService (http://localhost:5191)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| GET | `/events` | Список событий (пагинация, фильтры) | Admin, User |
| GET | `/events/top` | Топ-10 событий по проценту продаж (кеш Redis) | Без токена |
| GET | `/events/{id}` | Событие по ID (кеш Redis) | Admin, User |
| POST | `/events` | Создать событие | Admin |
| PUT | `/events/{id}` | Обновить событие | Admin |
| DELETE | `/events/{id}` | Удалить событие | Admin |

### BookingService (http://localhost:5099)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| POST | `/events/{eventId}/book` | Создать бронь | Admin, User |
| GET | `/bookings` | Брони текущего пользователя | Admin, User |
| GET | `/bookings/{id}` | Бронь по ID | Admin, User |
| DELETE | `/bookings/{id}` | Отменить бронь | Admin, User |

---

## Аутентификация и авторизация

JWT-токен выдаёт **UserService** (`POST /auth/login`). **EventService** и **BookingService** проверяют тот же токен.

Параметры JWT одинаковы во всех сервисах (секция `"Jwt"` в `appsettings.json`):

- [`UserService/UserService.Api/appsettings.json`](UserService/UserService.Api/appsettings.json)
- [`EventService/EventService.Api/appsettings.json`](EventService/EventService.Api/appsettings.json)
- [`BookingService/BookingService.Api/appsettings.json`](BookingService/BookingService.Api/appsettings.json)

Общая конфигурация: [`Shared/Shared.Api/JwtAuthenticationExtensions.cs`](Shared/Shared.Api/JwtAuthenticationExtensions.cs).

### Получение токена через Swagger

1. Откройте Swagger UserService: http://localhost:5134/swagger
2. Зарегистрируйте пользователя через `POST /auth/register`:
   ```json
   {
     "login": "admin",
     "password": "admin123",
     "role": "Admin"
   }
   ```
3. Получите токен через `POST /auth/login`
4. Нажмите **Authorize** в Swagger EventService или BookingService и введите: `Bearer {ваш_токен}`

### Ролевая модель

| Роль | Права |
|------|-------|
| `User` | Бронирование, просмотр событий и своих броней, отмена своих броней |
| `Admin` | Все права User + CRUD событий, отмена любых броней |

---

## Тестирование

```bash
dotnet test
```

| Проект | Описание |
|--------|----------|
| `UserService.UnitTests` | Юнит-тесты AuthService |
| `EventService.UnitTests` | Юнит-тесты EventService, BookEvent, кеш (hit / miss / инвалидация) |
| `BookingService.UnitTests` | Юнит-тесты BookingService |
| `*.IntegrationTests` | Интеграционные тесты с PostgreSQL (Testcontainers) |

Для интеграционных тестов требуется запущенный Docker.

---

## Сценарий проверки end-to-end

1. Зарегистрируйте пользователя и получите JWT в UserService
2. Создайте событие (Admin) в EventService, запомните `totalSeats` / `availableSeats`
3. Создайте бронь в BookingService (`POST /events/{id}/book`)
4. Дождитесь подтверждения (статус `Confirmed`) — сообщение проходит через Kafka
5. Проверьте в EventService, что `availableSeats` уменьшилось на значение `seatCount` из запроса
