# 🎮 Game Releases API

**Game Releases API** — backend‑сервис для сбора и аналитики будущих релизов игр из Steam.  
Сервис синхронизирует данные о релизах и метаданных, сохраняет их в PostgreSQL, формирует агрегаты в ClickHouse и предоставляет REST API для аналитики, фильтрации и мониторинга интереса к играм.

---

## 🚀 Запуск проекта (настоящие настройки)

> В проекте не используется HTTPS в dev. Все примеры ниже используют HTTP на порту 8080.

### PowerShell (Windows, рекомендуемый)
```powershell
docker compose --profile prod up --build
```
или
### PowerShell (Windows)
```powershell
docker compose --profile dev up --build
```


### Bash (Linux / macOS)
```bash
docker compose --profile prod up --build
```
или
```bash
docker compose --profile dev up --build
```

После успешного запуска:
- API доступен по: http://localhost:8080  
- Swagger UI (dev): http://localhost:8080/swagger — только если в окружении включён ENABLE_SWAGGER=true

---

## 📘 Основные эндпоинты API

### 🎮 Игры
| Метод | Маршрут | Описание |
| --- | --- | --- |
| GET | /api/v1/games | Список игр с пагинацией и фильтрами |
| GET | /api/v1/games/{id} | Получить игру по ID |
| GET | /api/v1/games/app/{appId} | Получить игру по AppId |
| POST | /api/v1/games | Создать игру (требует JWT) |
| PUT | /api/v1/games/{id} | Обновить игру (требует JWT) |
| DELETE | /api/v1/games/{id} | Удалить игру (требует JWT) |
| GET | /api/v1/games/releases?month=yyyy-MM&platform=&genre= | Релизы за месяц |
| GET | /api/v1/games/calendar?month=yyyy-MM | Календарь релизов (агрегация по дням) |

### 📊 Аналитика
| Метод | Маршрут | Описание |
| --- | --- | --- |
| GET | /api/v1/analytics/top-genres?month=yyyy-MM | Топ‑5 жанров + средний фолловеров |
| GET | /api/v1/analytics/dynamics?months=yyyy-MM,yyyy-MM,... | Динамика по жанрам за несколько месяцев |

### 🔐 Аутентификация
| Метод | Маршрут | Описание |
| --- | --- | --- |
| POST | /api/v1/auth/login | Вход, получение JWT (тест: admin/admin123) |

---

## 🛠️ Используемые технологии

* ASP.NET Core 8  
* Entity Framework Core + PostgreSQL  
* ClickHouse — аналитическое хранилище  
* Swagger / OpenAPI  
* JWT-аутентификация  
* Docker + docker-compose (profiles: dev, prod)  
* Playwright (парсинг Steam, браузеры вынесены в отдельный слой/том)

---

## 🧱 Архитектура репозитория (важные файлы)
- GameReleases.Core — DTO, сущности, интерфейсы, бизнес‑логика.  
- GameReleases.Infrastructure — EF Core, репозитории, миграции.  
- GameReleases.WebApi — контроллеры, Program.cs, DI, конфигурация.  
- Dockerfile — multi‑stage сборка webapi; копирование Playwright браузеров из stage.  
- Dockerfile.migrate — образ для применения EF Core миграций (migrate.sh).  
- docker-compose.yml — сервисы: postgres, clickhouse, migrate, game-releases-api, game-releases-api-dev, playwright-install.  
- migrate.sh — ожидание Postgres, генерация/применение миграций.  
- entrypoint.sh — runtime wrapper, гарантирует наличие браузеров и запускает приложение.  
- .dockerignore — исключения из контекста сборки.

---

## ⚡ Оптимизация сборки и Playwright

- Playwright‑браузеры берутся из отдельного stage (или через сервис playwright-install), чтобы не перекачивать гигабайты при каждом изменении кода.  
- Используется BuildKit cache: docker‑compose настроен на `cache_from` / `cache_to` (локальная папка .buildx-cache). Для корректной работы включайте BuildKit и инициализируйте buildx builder (см. команды выше).  
- Dev‑режим (game-releases-api-dev) использует dotnet watch и монтирует исходники + host NuGet cache (~/.nuget/packages) для быстрой итерации.

---

## 🔄 Миграции и скрипты

- `migrate` сервис выполняет:
  1. Ожидание доступности Postgres (healthcheck + nc).  
  2. Сборку проекта и попытку создать миграцию (InitialCreate / Auto<TIMESTAMP>) при отсутствии/изменениях.  
  3. Применение миграций через `dotnet ef database update`.  
- При ошибках подключения `migrate.sh` пытается использовать psql для создания БД и повторного применения миграций. Настройки подключения в compose: `Host=postgres;Port=5432;Database=game_releases;Username=postgres;Password=1234` — замените значениями для prod/CI.

---

## 🧰 Полезные команды и сценарии

- Подготовить buildx (один раз):
```bash
docker buildx use mybuilder 2>/dev/null || docker buildx create --name mybuilder --driver docker-container --use
docker buildx inspect --bootstrap
```

- Быстрый запуск dev:
```bash
# PowerShell
$env:DOCKER_BUILDKIT="1"; docker compose --profile dev up --build --remove-orphans --detach

# Bash
DOCKER_BUILDKIT=1 docker compose --profile dev up --build --remove-orphans -d
```

- Сборка без кэша:
```bash
docker compose build --no-cache
```

- Выполнить только миграции:
```bash
docker compose run --rm migrate
```

- Очистка buildx кэша:
```bash
docker buildx prune --all --force
rm -rf .buildx-cache
```

- Полная очистка ресурсов:
```bash
docker system prune --volumes --all --force
```

- Логи и статус:
```bash
docker compose logs -f migrate
docker compose logs -f playwright-install
docker compose logs -f game-releases-api-dev
docker compose ps
```

---

## 🛠 Частые проблемы и решения

- restore падает с ошибкой "Could not find file ... /root/.nuget/packages/...": кэш NuGet повреждён. Решения:
  - удалить проблемный пакет в `.buildx-cache/root/.nuget/packages/...` или в хостовом `~/.nuget/packages/...` и пересобрать;  
  - пересобрать без кэша: `docker compose build --no-cache`;  
  - очистить весь buildx кэш: `docker buildx prune --all --force` + удалить `.buildx-cache`.
- BuildKit pipe error на Windows: Docker Desktop не запущен или некорректный контекст — запустите Docker Desktop или используйте WSL2.
- Playwright не установил браузеры: проверьте `docker compose logs playwright-install`, перезапустите `playwright-install`, убедитесь в корректном монтировании тома `playwright-cache`.
- Если builder занят/ошибки имени: `docker buildx use mybuilder` или `docker buildx rm mybuilder` и затем создать заново.

---

## 📦 Postman коллекция (локальные URL)

Включён JSON‑блок коллекции в репо. Перед импортом замените в коллекции все URL на http://localhost:8080 (protocol = http, port = 8080). Примеры запросов:
- POST http://localhost:8080/api/v1/auth/login  
- GET http://localhost:8080/api/v1/games?page=1&pageSize=10  
- GET http://localhost:8080/api/v1/analytics/top-genres?month=2025-11

---

## 🔐 Рекомендации по безопасности и CI

- Не храните секреты в репозитории. Секреты (JWT secret, DB credentials) передавайте через CI secrets или env‑файлы вне репо.  
- В production отключите Swagger по умолчанию; включайте только через переменную `ENABLE_SWAGGER=true` и/или защитите доступ.  
- На Windows для стабильной работы с большим кэшем рекомендуется WSL2 или отключение антивируса для папки кэша.

---

## 📄 Лицензия

Проект распространяется под лицензией **MIT**.

--- 

© 2025 — Game Releases API Team 🎮 BaHooo
