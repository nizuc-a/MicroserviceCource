# MicroserviceCource

### Описание проекта 
Проект представляет собой первый шаг к созданию сервиса для управления мероприятиями на ASP.NET Core Web API. В рамках этого спринта реализована базовая функциональность CRUD (создание, просмотр, обновление и удаление мероприятий) через REST API.

### Требования
Язык программирования: C# - Платформа: .NET 8 или выше - Веб-фреймворк: ASP.NET Core Web API

### 1. Установка и запуск. 

1) Клонируйте репозиторий:

```bash
   git clone https://github.com/nizuc-a/MicroserviceCource.git

```

2) Переключитесь на ветку `sprint-2`:

```bash
   git checkout sprint-2
```

Для запуска тестов 
```bash
   dotnet test
```

3) Соберите проект:

```bash
   cd MicroserviceCource
   dotnet build
```
4) Запустите проект:

```bash
   dotnet run
```

### 2. Структура проекта
Проект имеет простую и понятную структуру с базовым разделением кода по назначению. Основные компоненты:
  * Controllers — контроллеры для обработки HTTP-запросов;
  * Data — файлы, связанные с доступом к данным и контекстом базы данных;
  * Extension — расширения для дополнительного функционала;
  * Interfaces > Services — интерфейсы для сервисов;
  * Model
    * DTO > Event — объекты передачи данных (DTO) для событий;
    * Entity — сущности данных;
  * Services — сервисы для реализации бизнес-логики.

### 3. Используемые технологии и библиотеки 
ASP.NET Core Web API; Swagger для тестирования API; Dependency Injection (DI) для управления зависимостями.

### 4. Нововведения.
Добавлена фильтрация для получения всех событий. Фильтр вклюает в себя фильтрацию по названию, дате начала и окончания события. Есть возможность использования пагинации.

#### GET /events

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
#### Пример ответа с ошибкой 

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
  "title": "An unhandled exception occurred",
  "status": 404,
  "detail": "Event with Id 1 not found",
  "instance": "/events/1",
}
```

