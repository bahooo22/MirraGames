
````markdown
# 🎮 Game Releases API

**Game Releases API** — backend-сервис для сбора и аналитики будущих релизов игр из **Steam**.  
Сервис автоматически синхронизирует данные о грядущих релизах, сохраняет их в базу,  
и предоставляет REST API для аналитики, фильтрации и мониторинга интереса к играм.

---

## 🚀 Запуск проекта

### 1️⃣ Генерация dev-сертификата для HTTPS

```bash
dotnet dev-certs https -ep $HOME/.aspnet/https/GameReleases.WebApi.pfx -p yourpassword
dotnet dev-certs https --trust
````

> 💡 На **Windows** путь к сертификату:
>
> ```
> %USERPROFILE%\.aspnet\https
> ```

---

### 2️⃣ Запуск через Docker Compose

```bash
docker-compose up --build
```

После сборки и запуска сервисов:

* API доступен на `http://localhost:8080`
* Swagger UI — по адресу [`http://localhost:8080/swagger`](http://localhost:8080/swagger)
* При HTTPS доступе — [`https://localhost:443`](https://localhost:443)

---

## 📘 Основные эндпоинты API

### 🎮 Игры

| Метод    | Маршрут                                                 | Описание                              |
| -------- | ------------------------------------------------------- | ------------------------------------- |
| `GET`    | `/api/v1/games`                                         | Список игр с пагинацией и фильтрами   |
| `GET`    | `/api/v1/games/{id}`                                    | Получить игру по ID                   |
| `GET`    | `/api/v1/games/app/{appId}`                             | Получить игру по AppId                |
| `POST`   | `/api/v1/games`                                         | Создать игру *(требует JWT)*          |
| `PUT`    | `/api/v1/games/{id}`                                    | Обновить игру *(требует JWT)*         |
| `DELETE` | `/api/v1/games/{id}`                                    | Удалить игру *(требует JWT)*          |
| `GET`    | `/api/v1/games/releases?month=yyyy-MM&platform=&genre=` | Релизы за месяц                       |
| `GET`    | `/api/v1/games/calendar?month=yyyy-MM`                  | Календарь релизов (агрегация по дням) |

---

### 📊 Аналитика

| Метод | Маршрут                                                 | Описание                                |
| ----- | ------------------------------------------------------- | --------------------------------------- |
| `GET` | `/api/v1/analytics/top-genres?month=yyyy-MM`            | Топ-5 жанров + средний фолловеров       |
| `GET` | `/api/v1/analytics/dynamics?months=yyyy-MM,yyyy-MM,...` | Динамика по жанрам за несколько месяцев |

---

### 🔐 Аутентификация

| Метод  | Маршрут              | Описание                   |
| ------ | -------------------- | -------------------------- |
| `POST` | `/api/v1/auth/login` | Вход, получение JWT токена |

---

## 🛠️ Используемые технологии

* **ASP.NET Core 8**
* **Entity Framework Core + PostgreSQL**
* **ClickHouse** — аналитическое хранилище
* **Swagger / OpenAPI**
* **JWT-аутентификация**
* **Docker + docker-compose**
* **Playwright** *(для парсинга Steam Community)*

---

## 🧱 Архитектура проекта

```
GameReleases/
│
├── GameReleases.Core/
│   ├── DTO / Entities / Interfaces
│   ├── Services (SteamCollector, SteamService, AnalyticsService)
│   └── Кэширование, логика, парсинг
│
├── GameReleases.Infrastructure/
│   ├── Data (EF Core, PostgreSQL)
│   ├── Repositories
│   ├── ClickHouse / Analytics
│   └── Migrations
│
├── GameReleases.WebApi/
│   ├── Controllers
│   ├── DI / Configurations / Swagger
│   └── Auth / JWT / Middleware
│
└── docker-compose.yml
```

---

## 🧩 Background Services

Автоматическая синхронизация данных со Steam (через API и Playwright):

* **SteamCollector** — получает список игр и новые релизы
* **SteamSyncService** — периодически обновляет фолловеров, даты и метаданные
* **AnalyticsJob** — формирует ClickHouse-агрегации

---

## 💡 Полезные команды

### Обновление базы

```bash
dotnet ef database update --project GameReleases.Infrastructure --startup-project GameReleases.WebApi
```

### Установка Playwright браузеров

```bash
dotnet add GameReleases.Core package Microsoft.Playwright
dotnet build
pwsh bin\Debug\net8.0\playwright.ps1 install
```

---

## 📄 Лицензия

Проект распространяется под лицензией **MIT**.
Свободно используй и модифицируй в своих целях.

---

© 2025 — Game Releases API Team 🎮 BaHooo

```


{
  "info": {
    "name": "GameReleases API",
    "_postman_id": "12345678-abcd-efgh-ijkl-1234567890ab",
    "description": "Коллекция для тестирования GameReleases API (игры, аналитика, аутентификация)",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Auth - Login",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Content-Type", "value": "application/json" }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"username\": \"admin\",\n  \"password\": \"admin123\"\n}"
        },
        "url": {
          "raw": "https://localhost:443/api/v1/auth/login",
          "protocol": "https",
          "host": ["localhost"],
          "port": "443",
          "path": ["api", "v1", "auth", "login"]
        }
      }
    },
    {
      "name": "Games - Get Paged",
      "request": {
        "method": "GET",
        "url": {
          "raw": "https://localhost:443/api/v1/games?page=1&pageSize=10",
          "protocol": "https",
          "host": ["localhost"],
          "port": "443",
          "path": ["api", "v1", "games"],
          "query": [
            { "key": "page", "value": "1" },
            { "key": "pageSize", "value": "10" }
          ]
        }
      }
    },
    {
      "name": "Games - Create",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Content-Type", "value": "application/json" },
          { "key": "Authorization", "value": "Bearer {{jwt_token}}" }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"appId\": \"12345\",\n  \"name\": \"Test Game\",\n  \"releaseDate\": \"2025-11-01\",\n  \"genres\": [\"RPG\", \"Adventure\"],\n  \"followers\": 1000,\n  \"storeUrl\": \"https://store.steampowered.com/app/12345\",\n  \"posterUrl\": \"https://cdn.example.com/poster.jpg\",\n  \"shortDescription\": \"A test game\",\n  \"platforms\": [\"PC\"]\n}"
        },
        "url": {
          "raw": "https://localhost:443/api/v1/games",
          "protocol": "https",
          "host": ["localhost"],
          "port": "443",
          "path": ["api", "v1", "games"]
        }
      }
    },
    {
      "name": "Analytics - Top Genres",
      "request": {
        "method": "GET",
        "url": {
          "raw": "https://localhost:443/api/v1/analytics/top-genres?month=2025-11",
          "protocol": "https",
          "host": ["localhost"],
          "port": "443",
          "path": ["api", "v1", "analytics", "top-genres"],
          "query": [
            { "key": "month", "value": "2025-11" }
          ]
        }
      }
    },
    {
      "name": "Analytics - Dynamics",
      "request": {
        "method": "GET",
        "url": {
          "raw": "https://localhost:443/api/v1/analytics/dynamics?months=2025-09,2025-10,2025-11",
          "protocol": "https",
          "host": ["localhost"],
          "port": "443",
          "path": ["api", "v1", "analytics", "dynamics"],
          "query": [
            { "key": "months", "value": "2025-09,2025-10,2025-11" }
          ]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "jwt_token",
      "value": ""
    }
  ]
}
