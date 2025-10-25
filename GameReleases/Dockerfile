# syntax=docker/dockerfile:1.5

# --- Stage 0: Playwright runtime с браузерами ---
FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS playwright

# --- Stage 1: Build environment ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Кэш NuGet пакетов
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet nuget list source

COPY *.sln ./

# Копируем только csproj-файлы для restore-кеша
COPY GameReleases.Core/GameReleases.Core.csproj GameReleases.Core/
COPY GameReleases.Infrastructure/GameReleases.Infrastructure.csproj GameReleases.Infrastructure/
COPY GameReleases.WebApi/GameReleases.WebApi.csproj GameReleases.WebApi/

# Восстанавливаем зависимости
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore GameReleases.sln

# Копируем остальной код (с link для ускорения)
COPY --link . .

# Устанавливаем Playwright CLI (быстро)
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src/GameReleases.WebApi
RUN dotnet build -c Release -o /app/build

# --- Stage 2: Publish ---
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

## --- Stage 3: Runtime ---
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 443
#
## Копируем приложение
#COPY --from=publish /app/publish .
#
## Копируем браузеры из Playwright образа
#COPY --from=playwright /ms-playwright /ms-playwright
#
## Устанавливаем зависимости для Chromium/WebKit/Firefox
#RUN apt-get update && \
#    apt-get install -y --no-install-recommends \
#      libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 \
#      libcups2 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
#      libxrandr2 libgbm1 libasound2 libxshmfence1 libx11-6 \
#      libxext6 libxfixes3 libexpat1 libdbus-1-3 libatspi2.0-0 \
#      libxcb1 libpangocairo-1.0-0 libpango-1.0-0 libgtk-3-0 \
#      fonts-liberation libxss1 libxtst6 && \
#    rm -rf /var/lib/apt/lists/*
#
## Симлинк для Playwright
#RUN mkdir -p /root/.cache && ln -sf /ms-playwright /root/.cache/ms-playwright
#
#ENTRYPOINT ["dotnet", "GameReleases.WebApi.dll"]
# --- Stage 3: Runtime ---
FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Копируем приложение
COPY --from=publish /app/publish .

# Копируем браузеры из Playwright образа
COPY --from=playwright /ms-playwright /ms-playwright

# Симлинк для Playwright
RUN mkdir -p /root/.cache && ln -sf /ms-playwright /root/.cache/ms-playwright

ENTRYPOINT ["dotnet", "GameReleases.WebApi.dll"]