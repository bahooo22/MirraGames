#!/bin/bash
set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Waiting for postgres...${NC}"
while ! nc -z postgres 5432; do sleep 1; done
echo -e "${GREEN}Postgres is ready. Starting migrations.${NC}"

if dotnet ef database update \
  --no-build \
  --project ./GameReleases.Infrastructure/GameReleases.Infrastructure.csproj \
  --startup-project ./GameReleases.WebApi/GameReleases.WebApi.csproj \
  --connection "Host=postgres;Port=5432;Database=game_releases;Username=postgres;Password=1234"
then
  echo -e "${GREEN}Migrations applied successfully!${NC}"
else
  echo -e "${RED}Migration failed!${NC}"
  exit 1
fi
