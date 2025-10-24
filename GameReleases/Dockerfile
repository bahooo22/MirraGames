# Stage 0: базовый рантайм
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 1: build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Кэшируем NuGet пакеты
COPY *.sln ./
COPY GameReleases.WebApi/GameReleases.WebApi.csproj GameReleases.WebApi/
COPY GameReleases.Core/GameReleases.Core.csproj GameReleases.Core/
COPY GameReleases.Infrastructure/GameReleases.Infrastructure.csproj GameReleases.Infrastructure/

# NuGet restore — будет кэшироваться, пока не изменятся .csproj
RUN dotnet restore GameReleases.sln

# Копируем остальной код
COPY . .

WORKDIR /src/GameReleases.WebApi

# Устанавливаем Playwright CLI (кэшируется, если версия не изменилась)
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"

# Сборка
RUN dotnet build "GameReleases.WebApi.csproj" -c Release -o /app/build

# Stage 2: publish
FROM build AS publish
RUN dotnet publish "GameReleases.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Кэшируем Playwright браузеры в отдельном volume (ускоряет пересборку)
RUN pwsh /app/publish/bin/Release/net8.0/playwright.ps1 install --with-deps || true

# Stage 3: final runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameReleases.WebApi.dll"]
