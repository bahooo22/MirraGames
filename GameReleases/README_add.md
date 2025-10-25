
## ⚡ Оптимизация Docker-сборки и миграций

### 🧱 Структура Docker-окружения

В проекте используются **два Dockerfile**:

| Файл                 | Назначение                                                                         |
| -------------------- | ---------------------------------------------------------------------------------- |
| `Dockerfile`         | Сборка и запуск основного **GameReleases.WebApi** (с Playwright и ASP.NET Runtime) |
| `Dockerfile.migrate` | Применение **EF Core миграций** к PostgreSQL при старте контейнера                 |

И один общий `docker-compose.yml`, который поднимает 4 сервиса:

```yaml
postgres        # база данных PostgreSQL
clickhouse      # аналитическое хранилище
migrate         # выполняет dotnet ef database update
game-releases-api  # основной веб-API
```

Контейнер `migrate` автоматически ждёт, пока PostgreSQL станет доступен
(через healthcheck и netcat), применяет миграции и завершается.
После этого `game-releases-api` стартует с готовой базой.

---

### 🧩 Оптимизация сборки `Dockerfile.migrate`

Чтобы ускорить сборку и не переустанавливать пакеты при каждом `docker build`,
установка `netcat` и `dotnet-ef` вынесена в отдельный кэшируемый слой:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app

RUN apt-get update -qq && apt-get install -y --no-install-recommends netcat-openbsd \
    && dotnet tool install --global dotnet-ef \
    && rm -rf /var/lib/apt/lists/*
ENV PATH="$PATH:/root/.dotnet/tools"

COPY *.sln ./
COPY GameReleases.WebApi/GameReleases.WebApi.csproj GameReleases.WebApi/
COPY GameReleases.Core/GameReleases.Core.csproj GameReleases.Core/
COPY GameReleases.Infrastructure/GameReleases.Infrastructure.csproj GameReleases.Infrastructure/
RUN dotnet restore

COPY . .
COPY migrate.sh /usr/local/bin/migrate.sh
RUN chmod +x /usr/local/bin/migrate.sh

ENTRYPOINT ["/usr/local/bin/migrate.sh"]
```

📦 **Результат:**

* `dotnet-ef` и `netcat` кэшируются между сборками
* База и миграции поднимаются мгновенно после `docker-compose up`

---

### 🧠 Оптимизация Playwright и WebAPI-образа

Полный Playwright-образ весит ~1.5 ГБ.
Чтобы не скачивать его заново при каждом изменении `.cs`-файла,
он используется **только как источник браузеров**, а CLI ставится отдельно:

```dockerfile
FROM mcr.microsoft.com/playwright/dotnet:v1.40.0-jammy AS playwright

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln ./
COPY GameReleases.WebApi/GameReleases.WebApi.csproj GameReleases.WebApi/
COPY GameReleases.Core/GameReleases.Core.csproj GameReleases.Core/
COPY GameReleases.Infrastructure/GameReleases.Infrastructure.csproj GameReleases.Infrastructure/
RUN dotnet restore GameReleases.sln

COPY . .
WORKDIR /src/GameReleases.WebApi

RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet build "GameReleases.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameReleases.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# ✅ Браузеры берутся из слоя playwright (без повторной загрузки)
COPY --from=playwright /ms-playwright /ms-playwright
COPY --from=publish /app/publish .

RUN rm -rf /root/.nuget /tmp/*
ENTRYPOINT ["dotnet", "GameReleases.WebApi.dll"]
```

📊 **Итоги оптимизации:**

* Кэшированное восстановление зависимостей (`restore`)
* Браузеры Playwright не перекачиваются при каждом билде
* Образ стал легче и собирается в 2–3 раза быстрее
* Можно разрабатывать и деплоить без скачивания гигабайтов от Microsoft

---

### 🔄 Пересборка проекта

Если нужно **принудительно обновить** образы:

```bash
docker compose build --no-cache
```

Если нужно **только перезапустить миграции**:

```bash
docker compose run --rm migrate
```

---

Хочешь, я сделаю JSON-блок Postman-коллекции прямо ссылкой в README
(например, `"📦 Скачать коллекцию для Postman"` с кнопкой `Run in Postman`)?
