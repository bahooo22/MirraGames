#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

INFRA_PROJECT="./GameReleases.Infrastructure/GameReleases.Infrastructure.csproj"
STARTUP_PROJECT="./GameReleases.WebApi/GameReleases.WebApi.csproj"
MIGRATIONS_DIR="./GameReleases.Infrastructure/Migrations"
CONNECTION_STRING="Host=postgres;Port=5432;Database=game_releases;Username=postgres;Password=1234"

echo -e "${YELLOW}⏳ Waiting for PostgreSQL to become available...${NC}"
until nc -z postgres 5432; do
  sleep 1
done
echo -e "${GREEN}✅ PostgreSQL is ready.${NC}"

# === ВСЕГДА выполняем быструю сборку ===
echo -e "${YELLOW}🛠️  Building projects for EF Core tools...${NC}"
dotnet build -c Release --no-restore

export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"

# 1) Создание миграций
if [ ! -d "$MIGRATIONS_DIR" ]; then
  echo -e "${YELLOW}No migrations folder found. Creating InitialCreate...${NC}"
  set +e
  output=$(dotnet ef migrations add InitialCreate \
    --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" \
    --output-dir Migrations 2>&1)
  rc=$?
  set -e
  echo "$output"
  if [ $rc -ne 0 ]; then
    if echo "$output" | grep -qi "No changes"; then
      echo -e "${YELLOW}No model changes detected — skipping initial migration.${NC}"
    else
      echo -e "${RED}Failed to create initial migration.${NC}"
      exit 1
    fi
  else
    echo -e "${GREEN}✅ Initial migration created.${NC}"
  fi
else
  timestamp=$(date +%Y%m%d%H%M%S)
  migName="Auto${timestamp}"
  echo -e "${YELLOW}Checking for model changes...${NC}"
  set +e
  output=$(dotnet ef migrations add "$migName" \
    --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" \
    --output-dir Migrations 2>&1)
  rc=$?
  set -e
  echo "$output"
  if [ $rc -ne 0 ]; then
    if echo "$output" | grep -qi "No changes"; then
      echo -e "${YELLOW}No model changes detected.${NC}"
    else
      echo -e "${RED}Failed to add migration '${migName}'.${NC}"
      exit 1
    fi
  else
    echo -e "${GREEN}✅ Migration '${migName}' created.${NC}"
  fi
fi

# 2) Применение миграций
echo -e "${YELLOW}Applying EF Core migrations...${NC}"
set +e
apply_output=$(dotnet ef database update \
  --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT" 2>&1)
apply_rc=$?
set -e

echo "$apply_output"
if [ $apply_rc -ne 0 ]; then
  echo -e "${RED}❌ dotnet ef database update failed (rc=$apply_rc).${NC}"
  
  if echo "$apply_output" | grep -qi "Failed to connect\|connection"; then
    echo -e "${YELLOW}🔍 Connection problem detected. Checking with psql...${NC}"
    
    if ! command -v psql >/dev/null 2>&1; then
      echo -e "${YELLOW}Installing psql client...${NC}"
      apt-get update -qq && apt-get install -y --no-install-recommends postgresql-client
    fi
    
    if PGPASSWORD="1234" psql -h postgres -U postgres -c "SELECT 1;" >/dev/null 2>&1; then
      echo -e "${GREEN}✅ Can connect to PostgreSQL with psql${NC}"
      
      if ! PGPASSWORD="1234" psql -h postgres -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'game_releases'" | grep -q 1; then
        echo -e "${YELLOW}Creating database 'game_releases'...${NC}"
        PGPASSWORD="1234" psql -h postgres -U postgres -c "CREATE DATABASE game_releases;"
        echo -e "${GREEN}✅ Database created${NC}"
      else
        echo -e "${YELLOW}Database 'game_releases' already exists.${NC}"
      fi
      
      echo -e "${YELLOW}Retrying EF migrations...${NC}"
      set +e
      dotnet ef database update \
        --project "$INFRA_PROJECT" --startup-project "$STARTUP_PROJECT"
      retry_rc=$?
      set -e
      
      if [ $retry_rc -eq 0 ]; then
        echo -e "${GREEN}✅ Migrations applied successfully${NC}"
      else
        echo -e "${RED}❌ Migration failed again — exiting.${NC}"
        exit $retry_rc
      fi
    else
      echo -e "${RED}❌ Cannot connect to PostgreSQL even with psql. Exiting.${NC}"
      exit 1
    fi
  else
    echo -e "${RED}❌ Unknown error during migration. Exiting.${NC}"
    exit $apply_rc
  fi
else
  echo -e "${GREEN}✅ All migrations applied successfully.${NC}"
fi

exit 0
