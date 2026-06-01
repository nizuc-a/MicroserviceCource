# EventService API

Сервис для управления событиями и бронированиями.  
Реализован на **ASP.NET Core 8** с использованием **PostgreSQL**, **Entity Framework Core**, **чистой архитектуры** и **конкурентной обработки**.

---

## Оглавление

- [Основные возможности](#основные-возможности)
- [Технологии](#технологии)
- [Архитектура](#архитектура)
- [Запуск проекта](#запуск-проекта)
- [Миграции](#миграции)
- [Тестирование](#тестирование)
- [API Endpoints](#api-endpoints)
- [Обработка конкурентности](#обработка-конкурентности)
- [Фоновый сервис](#фоновый-сервис)

---

## Основные возможности

- **CRUD мероприятий** (создание, чтение, обновление, удаление)
- **Поля `TotalSeats` / `AvailableSeats`** – контроль количества мест
- **Бронирование мест** с защитой от овербукинга (статический `SemaphoreSlim`)
- **Асинхронная фоновая обработка** бронирований (статусы `Pending` → `Confirmed` / `Rejected`)
- **Фильтрация и пагинация** событий по названию, дате начала и окончания
- **Каскадное удаление** событий (удаляются связанные брони)
- **PostgreSQL** через Entity Framework Core
- **Миграции EF Core** для управления схемой
- **Репозитории** (`IEventRepository`, `IBookingRepository`)
- **Чистая архитектура** (4 проекта)
- **Интеграционные тесты** с реальной БД (Testcontainers)
- **Swagger** документация

---

## Технологии

| Компонент            | Технология                              |
|----------------------|------------------------------------------|
| .NET                 | 10.0                                      |
| Веб-фреймворк        | ASP.NET Core Web API                     |
| ORM                  | Entity Framework Core 10                  |
| Database             | PostgreSQL 16                            |
| Контейнеризация      | Docker + Testcontainers                  |
| Тесты                | xUnit, Moq, FluentAssertions            |
| Документация API     | Swagger / Swashbuckle                    |
| Архитектура          | Clean Architecture (Domain, Application, Infrastructure, Presentation) |

---

## Архитектура

Проект разделён на **4 сборки**, зависимости направлены **строго внутрь**:

```
EventService.Domain # Сущности, перечисления, доменные исключения
↑
EventService.Application # Use cases, DTO, интерфейсы портов (репозитории)
↑
EventService.Infrastructure # Реализации репозиториев, DbContext, миграции
↑
EventService.Api (Presentation) # Контроллеры, Middleware, DI, BackgroundService
```


- **Domain** не зависит от внешних фреймворков.
- **Application** зависит только от Domain, определяет **порты** (интерфейсы репозиториев и внешних сервисов).
- **Infrastructure** реализует порты, содержит EF Core и миграции.
- **Presentation** — Composition Root, регистрирует зависимости и вызывает методы расширения из Infrastructure и Application.

---

## Запуск проекта

### 1. Клонирование и переключение на ветку `sprint-7`

```bash
git clone https://github.com/nizuc-a/MicroserviceCource.git
cd MicroserviceCource
git checkout sprint-7
```

### 2. Запуск PostgreSQL через Docker
```bash
docker-compose up -d
```

### 3. Применение миграций и запуск API
```bash
cd EventService.Api
dotnet run
```

При старте автоматически выполняется `db.Database.Migrate()`.
Swagger будет доступен по адресу: `https://localhost:5000/swagger` (порт может отличаться).

---

## Миграции

Миграции создаются и хранятся в проекте `EventService.Infrastructure`.
Пример команды (из папки `EventService.Api`):

```bash
dotnet ef migrations add InitialCreate --context AppDbContext
```

После изменения модели создавайте новую миграцию и применяйте её:

```bash
dotnet ef database update
```

Все миграции автоматически применяются при запуске приложения.

---

## Тестирование

### Юнит-тесты (`EventService.UnitTests`)

- Используют InMemory-провайдер EF Core.

- Проверяют логику сервисов и репозиториев изолированно.

- Тестируют конкурентные сценарии (овербукинг, уникальность ID).

### Интеграционные тесты (`EventService.IntegrationTests`)


- Поднимают реальный контейнер PostgreSQL через Testcontainers.

- Перед каждым тестом база удаляется (`EnsureDeleted()`) и создаётся заново (`Migrate()`).

- Проверяют:

   - создание таблиц и связей (foreign key, check constraints)

   - каскадное удаление

   - работу репозиториев с реальной БД

   - конкурентную защиту (овербукинг)

Запуск всех тестов:

```bash
dotnet test
```

Для интеграционных тестов требуется запущенный Docker.

---

## API Endpoints


| Метод   | Эндпоинт   |    Описание|
|---------|------------|------------|
| GET | `/api/events` | Получить список событий (пагинация, фильтры) |
| GET | `/api/events/{id}` | Получить событие по ID |
| POST | `/api/events` | Создать событие (TotalSeats обязателен) |
| PUT | `/api/events/{id}` | Обновить событие |
| DELETE | `/api/events/{id}` | Удалить событие (и все его брони) |
| POST | `/api/events/{id}/book` | Создать бронь на событие |
| GET | `/api/bookings/{id}` | Получить информацию о брони |

Подробная спецификация доступна в Swagger: `/swagger`.

#### GET `/events`

Получить список событий с пагинацией и фильтрацией.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `title` | string | No | Фильтр по названию (частичное совпадение) | `?title=conference` |
| `from` | datetime (ISO 8601) | No | Фильтр по дате начала (включительно) | `?from=2024-03-01` |
| `to` | datetime (ISO 8601) | No | Фильтр по дате окончания (включительно) | `?to=2024-03-31` |
| `page` | int | Yes | Номер страницы (default: 1, min: 1) | `?page=2` |
| `pageSize` | int | Yes | Количество элементов на странице (default: 10, min: 1) | `?pageSize=20` |

#### Пример запроса

```http
GET /api/events?title=tech&from=2024-03-01&to=2024-03-31&page=1&pageSize=10
Host: localhost:5000
Accept: application/json
```

#### Примеры запросов с разными параметрами

```http
# Без фильтров (все события)
GET /events?page=1&pageSize=10

# Фильтр по названию
GET /events?title=webinar&page=1&pageSize=10

# Фильтр по дате начала (события с 1 марта 2024)
GET /events?from=2024-03-01&page=1&pageSize=10

# Фильтр по диапазону дат
GET /events?from=2024-03-01&to=2024-03-31&page=1&pageSize=10

# Комбинированный фильтр
GET /events?title=tech&from=2024-03-01&to=2024-03-31&page=1&pageSize=10
```

#### Пример успешного ответа (200 OK)

```json
{
  "allElementCount": 25,
  "page": 1,
  "events": [
    {
      "id": 1,
      "title": "Tech Conference 2024",
      "description": "Annual technology conference",
      "startAt": "2024-03-15T09:00:00",
      "endAt": "2024-03-15T18:00:00"
    },
    {
      "id": 2,
      "title": "Tech Workshop",
      "description": "Hands-on workshop",
      "startAt": "2024-03-20T10:00:00",
      "endAt": "2024-03-20T17:00:00"
    }
  ],
  "currentPageElementCount": 2
}
```
#### Пример ответа с ошибкой (404 Not Found)                                 

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
  "title": "An unhandled exception occurred",
  "status": 404,
  "detail": "Event with Id 1 not found",
  "instance": "/events/1",
}
```

#### POST  `/events`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `title` | string | No | Название (макс. 256 символов) |
| `description` | string | No | Описание (макс. 2000 символов)|
| `startAt` | datetime (ISO 8601) | Yes | Дата и время начала |
| `endAt` | datetime (ISO 8601) | Yes | Дата и время окончания (должно быть позже startAt) |
| `totalSeats` | int | Yes | Общее количество мест (больше 0) |

#### Пример запроса

```http
POST /api/events
Host: localhost:5000
Content-Type: application/json
```

```json

{
  "title": "Tech Conference 2024",
  "description": "Annual technology conference",
  "startAt": "2024-03-15T09:00:00Z",
  "endAt": "2024-03-15T18:00:00Z",
  "totalSeats": 100
}
```

#### Пример успешного ответа (201 Created)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Tech Conference 2024",
  "description": "Annual technology conference",
  "startAt": "2024-03-15T09:00:00Z",
  "endAt": "2024-03-15T18:00:00Z",
  "totalSeats": 100,
  "availableSeats": 100
}
```

---

## Обработка конкурентности
### Проблема

Одновременные запросы на бронирование могут привести к овербукингу (броней больше, чем мест).

### Решение

- В `BookingService` используется статический SemaphoreSlim (вместо lock, так как внутри нужны await-вызовы к БД).

- Весь критический участок (чтение события, проверка мест, уменьшение AvailableSeats, создание брони, SaveChangesAsync) защищён семафором.

- При превышении лимита выбрасывается `NoAvailableSeatsException` → HTTP 409 Conflict.

 ```cs
private static readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        // атомарная операция: проверка + изменение
    }
    finally { _semaphore.Release(); }
}
```

---

## Фоновый сервис

- Запускается каждые 5 секунд.

- Получает все брони со статусом `Pending`.

- Обрабатывает их параллельно через `Task.WhenAll`.

- Перед обработкой каждой брони создаётся отдельный scope через `IServiceScopeFactory`, чтобы получить scoped-репозитории и `DbContext`.

- Использует `SemaphoreSlim` для сериализации записи в БД при финальном обновлении статуса (чтобы не было race condition при изменении одного ресурса).

- Если событие удалено к моменту обработки – бронь отклоняется (`Rejected`) и место освобождается.
- При любой ошибке бронь также переводится в `Rejected`, а место возвращается.
  
#### Статусы бронирования:

- `Pending` – бронь создана, ожидает обработки (устанавливается сразу при вызове POST `/book`).

- `Confirmed` – бронь подтверждена (фоновый сервис через 5 секунд переводит в этот статус, если событие существует).

- `Rejected` – бронь отклонена (событие не найдено, нет мест или другая ошибка).
