# syntax=docker/dockerfile:1.5

##########################################
# --- Stage 0: Playwright base (бразуеры)
##########################################
FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS playwright


##########################################
# --- Stage 1: Build environment
##########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Кэш NuGet пакетов
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet nuget list source

COPY *.sln ./
COPY GameReleases.Core/*.csproj GameReleases.Core/
COPY GameReleases.Infrastructure/*.csproj GameReleases.Infrastructure/
COPY GameReleases.WebApi/*.csproj GameReleases.WebApi/
RUN dotnet restore GameReleases.sln

# Устанавливаем Playwright CLI
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="/root/.dotnet/tools:${PATH}"

# Копируем весь исходный код
COPY . .
WORKDIR /src/GameReleases.WebApi
RUN dotnet build -c Release -o /app/build --no-restore


##########################################
# --- Stage 2: Publish
##########################################
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

##########################################
# --- Stage 3: Runtime (API)
##########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Копируем ВСЮ папку ms-playwright из образа Playwright
COPY --from=playwright /ms-playwright /ms-playwright

# Копируем системные зависимости
COPY --from=playwright /usr/lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
COPY --from=playwright /lib/x86_64-linux-gnu /lib/x86_64-linux-gnu
COPY --from=playwright /usr/share/fonts /usr/share/fonts
COPY --from=playwright /etc/fonts /etc/fonts
COPY --from=playwright /etc/ssl /etc/ssl

# Создаем правильные симлинки для Playwright
RUN mkdir -p /root/.cache && \
    # Основной симлинк
    ln -sf /ms-playwright /root/.cache/ms-playwright

# Проверяем установку
RUN echo "=== Browser check ===" && \
    echo "Checking actual browser path..." && \
    ls -la /ms-playwright/chromium_headless_shell-1187/chrome-linux/headless_shell && \
    echo "Checking symlink..." && \
    ls -la /root/.cache/ms-playwright/chromium_headless_shell-1187/chrome-linux/headless_shell && \
    echo "✅ Browser is properly installed"

# Копируем dotnet tools из build stage
COPY --from=build /root/.dotnet /root/.dotnet
ENV PATH="/root/.dotnet/tools:${PATH}"

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]

##########################################
# --- Stage 4: Migrations (no browsers)
##########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS migrate
WORKDIR /app

# Копируем опубликованные артефакты
COPY --from=publish /app/publish ./bin_release

# Копируем ТОЛЬКО исходники (без bin/obj папок)
COPY *.sln ./
COPY GameReleases.Core/ ./GameReleases.Core/
COPY GameReleases.Infrastructure/ ./GameReleases.Infrastructure/
COPY GameReleases.WebApi/ ./GameReleases.WebApi/

# Восстанавливаем пакеты
RUN dotnet restore GameReleases.sln

# dotnet-ef CLI
RUN dotnet tool install --global dotnet-ef
ENV PATH="/root/.dotnet/tools:${PATH}"

# Устанавливаем netcat и psql
RUN apt-get update -qq && apt-get install -y --no-install-recommends netcat-openbsd postgresql-client && rm -rf /var/lib/apt/lists/*

# Миграционный скрипт
COPY migrate.sh /usr/local/bin/migrate.sh
RUN chmod +x /usr/local/bin/migrate.sh

ENTRYPOINT ["/usr/local/bin/migrate.sh"]
