#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

INFRA_PROJECT="./GameReleases.Infrastructure/GameReleases.Infrastructure.csproj"
STARTUP_PROJECT="./GameReleases.WebApi/GameReleases.WebApi.csproj"
MIGRATIONS_DIR="./GameReleases.Infrastructure/Migrations"
CONNECTION_STRING="Host=postgres;Port=5432;Database=game_releases;Username=postgres;Password=1234"

echo -e "${YELLOW} Waiting for PostgreSQL to become available...${NC}"
until nc -z postgres 5432; do
  sleep 1
done
echo -e "${GREEN} PostgreSQL is ready.${NC}"

echo -e "${YELLOW} Building solution to ensure models are compiled...${NC}"
dotnet build -c Release

# Helper: try to run a command but don't exit script on non-zero
run_no_exit() {
  set +e
  output=$("$@" 2>&1)
  rc=$?
  set -e
  echo "$output"
  return $rc
}

# 1) Если нет папки миграций — создаём InitialCreate
if [ ! -d "$MIGRATIONS_DIR" ]; then
  echo -e "${YELLOW} Migrations folder not found. Creating initial migration 'InitialCreate'...${NC}"

  # пытаемся добавить InitialCreate; если нет изменений — это нормально
  set +e
  output=$(dotnet ef migrations add InitialCreate --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" --output-dir Migrations -- --connection "$CONNECTION_STRING" 2>&1)
  rc=$?
  set -e

  echo "$output"
  if [ $rc -ne 0 ]; then
    if echo "$output" | grep -qi "No changes"; then
      echo -e "${YELLOW} No model changes detected when creating InitialCreate — skipping migration creation.${NC}"
    else
      echo -e "${RED} Failed to create initial migration (rc=$rc). Output:${NC}"
      echo "$output"
      exit 1
    fi
  else
    echo -e "${GREEN} Initial migration 'InitialCreate' created.${NC}"
  fi
else
  # 2) Есть папка миграций — пробуем добавить автоматическую миграцию с таймстампом
  timestamp=$(date +%Y%m%d%H%M%S)
  migName="Auto${timestamp}"
  echo -e "${YELLOW} Migrations folder exists. Attempting to add migration '${migName}' if model changed...${NC}"

  set +e
  output=$(dotnet ef migrations add "$migName" --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" --output-dir Migrations -- --connection "$CONNECTION_STRING" 2>&1)
  rc=$?
  set -e

  echo "$output"
  if [ $rc -ne 0 ]; then
    if echo "$output" | grep -qi "No changes"; then
      echo -e "${YELLOW} No model changes detected — no new migration created.${NC}"
    else
      echo -e "${RED} Failed to add migration '${migName}' (rc=$rc). Output:${NC}"
      echo "$output"
      # не пытаемся автоматически исправлять — выходим с ошибкой
      exit 1
    fi
  else
    echo -e "${GREEN} Migration '${migName}' created.${NC}"
  fi
fi

# 3) Применяем миграции
echo -e "${YELLOW} Applying EF Core migrations...${NC}"

# Явно указываем строку подключения для EF Core
export ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=game_releases;Username=postgres;Password=1234"

set +e
apply_output=$(dotnet ef database update --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" -- --connection "$CONNECTION_STRING" 2>&1)
apply_rc=$?
set -e

echo "$apply_output"
if [ $apply_rc -ne 0 ]; then
  echo -e "${RED} dotnet ef database update failed with rc=$apply_rc.${NC}"
  
  # Проверяем конкретную ошибку подключения
  if echo "$apply_output" | grep -qi "Failed to connect"; then
    echo -e "${YELLOW}! Connection problem detected. Checking if we can connect with psql...${NC}"
    
    # Устанавливаем psql если нет
    if ! command -v psql >/dev/null 2>&1; then
      echo -e "${YELLOW}Installing psql...${NC}"
      apt-get update && apt-get install -y postgresql-client
    fi
    
    # Проверяем подключение к PostgreSQL
    if PGPASSWORD="1234" psql -h postgres -U postgres -c "SELECT 1;" >/dev/null 2>&1; then
      echo -e "${GREEN} Can connect to PostgreSQL with psql${NC}"
      
      # Проверяем существование базы
      if ! PGPASSWORD="1234" psql -h postgres -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'game_releases'" | grep -q 1; then
        echo -e "${YELLOW}Creating database 'game_releases'...${NC}"
        PGPASSWORD="${POSTGRES_PASSWORD:-1234}" psql -h postgres -U "${POSTGRES_USER:-postgres}" -c "CREATE DATABASE game_releases;"
	echo -e "${GREEN} Database created${NC}"
      else
        echo -e "${YELLOW}Database 'game_releases' already exists.${NC}"
      fi
      
      echo -e "${YELLOW} Retrying EF migrations...${NC}"
      dotnet ef database update --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" -- --connection "$CONNECTION_STRING"
      echo -e "${GREEN} Migrations applied successfully${NC}"
    else
      echo -e "${RED} psql is not available in the image — cannot create DB automatically. Please run migrations manually.${NC}"
      exit 1
    fi
  else
    # иная ошибка — выходим
    exit 1
  fi
else
  echo -e "${GREEN}✅ All migrations applied successfully.${NC}"
fi

# Конец скрипта — успешное завершение
exit 0
