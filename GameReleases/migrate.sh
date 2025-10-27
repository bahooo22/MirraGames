#!/bin/bash
set -euo pipefail

# -------------------------
# CONFIG
# -------------------------
DB_HOST="${DB_HOST:-postgres}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-game_releases}"
DB_USER="${DB_USER:-postgres}"
DB_PASS="${DB_PASS:-1234}"

INFRA_PROJECT="./GameReleases.Infrastructure/GameReleases.Infrastructure.csproj"
WEBAPI_PROJECT="./GameReleases.WebApi/GameReleases.WebApi.csproj"
MIGRATIONS_DIR="./GameReleases.Infrastructure/Migrations"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

trap 'echo -e "${RED}❌ ERROR at line $LINENO. Last command: $BASH_COMMAND${NC}"; exit 1' ERR

log() { echo -e "$(date +"%Y-%m-%d %H:%M:%S") | $1"; }

# -------------------------
# WAIT FOR POSTGRES
# -------------------------
log "${YELLOW}⏳ Waiting for PostgreSQL...${NC}"
until PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "postgres" -c '\q' >/dev/null 2>&1; do
    sleep 1
done
log "${GREEN}✅ PostgreSQL is ready.${NC}"

# -------------------------
# CREATE DATABASE IF NOT EXISTS
# -------------------------
if ! PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -lqt | cut -d \| -f 1 | grep -qw "$DB_NAME"; then
    log "${YELLOW}⚠ Database '$DB_NAME' does not exist. Creating...${NC}"
    PGPASSWORD="$DB_PASS" createdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME"
    log "${GREEN}✅ Database created.${NC}"
else
    log "${GREEN}✅ Database '$DB_NAME' exists.${NC}"
fi

# -------------------------
# BUILD PROJECTS
# -------------------------
log "${YELLOW}🛠 Building projects...${NC}"
dotnet build "$INFRA_PROJECT" -c Release
dotnet build "$WEBAPI_PROJECT" -c Release
log "${GREEN}✅ Build succeeded.${NC}"

export ConnectionStrings__DefaultConnection="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASS"

# -------------------------
# CHECK EXISTING TABLES AND EF HISTORY
# -------------------------
EXISTING_TABLES=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';")

MIGRATIONS_TABLE=$(PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc \
    "SELECT to_regclass('__EFMigrationsHistory');")

# -------------------------
# CREATE BASELINE IF NEEDED
# -------------------------
if [[ "$EXISTING_TABLES" -gt 0 && "$MIGRATIONS_TABLE" == "" ]]; then
    log "${YELLOW}⚠ Existing tables detected but __EFMigrationsHistory is missing.${NC}"
    log "${YELLOW}✅ Creating baseline migration...${NC}"

    if [ ! -d "$MIGRATIONS_DIR" ]; then
        dotnet ef migrations add Baseline --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" --output-dir Migrations
        MIG_FILE=$(ls "$MIGRATIONS_DIR"/*_Baseline.cs | head -n1)
        sed -i '/migrationBuilder.CreateTable/d' "$MIG_FILE"
        log "${GREEN}✅ Baseline migration created.${NC}"
    fi

    log "${YELLOW}Marking all migrations as applied...${NC}"
    dotnet ef migrations list --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" | while read -r mig; do
        if [[ ! -z "$mig" ]]; then
            log "Marking migration $mig as applied"
            PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
                -c "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\",\"ProductVersion\") VALUES ('$mig','8.0.0');"
        fi
    done
fi

# -------------------------
# CREATE NEW MIGRATION IF MODEL CHANGED
# -------------------------
log "${YELLOW}Checking for model changes...${NC}"
set +e
timestamp=$(date +%Y%m%d%H%M%S)
migName="Auto${timestamp}"
output=$(dotnet ef migrations add "$migName" --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" --output-dir Migrations 2>&1)
rc=$?
set -e
echo "$output"
if [ $rc -ne 0 ]; then
    if echo "$output" | grep -qi "No changes"; then
        log "${GREEN}✅ No model changes detected.${NC}"
    else
        log "${RED}❌ Failed to add migration '${migName}'. Exiting.${NC}"
        exit 1
    fi
else
    log "${GREEN}✅ Migration '${migName}' created.${NC}"
    
    # Полностью переписываем миграцию для безопасного изменения типа
    MIG_FILE=$(ls "$MIGRATIONS_DIR"/*_"${migName}".cs | head -n1)
    if [ -f "$MIG_FILE" ]; then
        log "${YELLOW}🔧 Creating custom migration script...${NC}"
        
        # Генерируем SQL для миграции
        SQL_FILE="${MIGRATIONS_DIR}/${migName}.sql"
        dotnet ef migrations script --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" --output "$SQL_FILE"
        
        # Исправляем SQL файл
        sed -i 's/ALTER TABLE "Games" ALTER COLUMN "AppId" TYPE numeric(20,0)/ALTER TABLE "Games" ALTER COLUMN "AppId" TYPE numeric(20,0) USING "AppId"::numeric(20,0)/' "$SQL_FILE"
        
        # Применяем исправленный SQL
        log "${YELLOW}Applying fixed migration SQL...${NC}"
        PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$SQL_FILE"
        
        log "${GREEN}✅ Custom migration applied successfully.${NC}"
        
        # Помечаем миграцию как примененную в EF History
        PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
            -c "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\",\"ProductVersion\") VALUES ('$migName','8.0.0');"
        
        log "${GREEN}✅ Migration marked as applied.${NC}"
        exit 0
    fi
fi

# -------------------------
# APPLY PENDING MIGRATIONS (только если не было создано новой миграции с изменением типа)
# -------------------------
log "${YELLOW}Applying EF Core migrations...${NC}"
set +e
apply_output=$(dotnet ef database update --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" 2>&1)
apply_rc=$?
set -e
echo "$apply_output"
if [ $apply_rc -ne 0 ]; then
    log "${RED}❌ dotnet ef database update failed (rc=$apply_rc).${NC}"
    
    # Если ошибка связана с преобразованием типа, создаем кастомный скрипт
    if echo "$apply_output" | grep -qi "cannot be cast automatically"; then
        log "${YELLOW}🛠 Creating custom migration script for type conversion...${NC}"
        
        # Получаем список ожидающих миграций
        PENDING_MIGRATIONS=$(dotnet ef migrations list --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" | grep -v "No migrations were found")
        
        # Создаем кастомный SQL скрипт для каждой ожидающей миграции
        for mig in $PENDING_MIGRATIONS; do
            if [[ ! -z "$mig" ]]; then
                log "${YELLOW}Processing migration: $mig${NC}"
                
                # Генерируем SQL для конкретной миграции
                SQL_FILE="${MIGRATIONS_DIR}/${mig}_custom.sql"
                dotnet ef migrations script "$mig" --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT" --output "$SQL_FILE"
                
                # Исправляем проблемные строки
                sed -i 's/ALTER TABLE "Games" ALTER COLUMN "AppId" TYPE numeric(20,0)/ALTER TABLE "Games" ALTER COLUMN "AppId" TYPE numeric(20,0) USING "AppId"::numeric(20,0)/' "$SQL_FILE"
                
                # Применяем исправленный SQL
                PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$SQL_FILE"
                
                log "${GREEN}✅ Migration '$mig' applied via custom script.${NC}"
            fi
        done
    else
        log "${YELLOW}🔍 Checking PostgreSQL connection...${NC}"
        if ! command -v psql >/dev/null 2>&1; then
            apt-get update -qq && apt-get install -y --no-install-recommends postgresql-client
        fi
        if ! PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -U "$DB_USER" -c "SELECT 1;" >/dev/null 2>&1; then
            log "${RED}❌ Cannot connect to PostgreSQL via psql. Exiting.${NC}"
            exit 1
        fi
        log "${YELLOW}Retrying EF migrations...${NC}"
        dotnet ef database update --project "$INFRA_PROJECT" --startup-project "$WEBAPI_PROJECT"
    fi
fi

log "${GREEN}✅ All migrations applied successfully.${NC}"
log "${GREEN}🎉 Database migration process completed successfully.${NC}"
exit 0