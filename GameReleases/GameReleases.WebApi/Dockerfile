FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GameReleases.WebApi/GameReleases.WebApi.csproj", "GameReleases.WebApi/"]
COPY ["GameReleases.Core/GameReleases.Core.csproj", "GameReleases.Core/"]
COPY ["GameReleases.Infrastructure/GameReleases.Infrastructure.csproj", "GameReleases.Infrastructure/"]
RUN dotnet restore "GameReleases.WebApi/GameReleases.WebApi.csproj"
COPY . .
WORKDIR "/src/GameReleases.WebApi"
RUN dotnet build "GameReleases.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameReleases.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameReleases.WebApi.dll"]