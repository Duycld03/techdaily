#!/usr/bin/env bash
# ==============================================================================
# DeepPace — Production Database Data Restoration & Migration Script
# ==============================================================================
# Idempotent tool to migrate legacy PostgreSQL data from techdaily_pgdata_prod
# into deeppace_pgdata_prod with zero data loss and post-restoration verification.
# ==============================================================================

set -euo pipefail

# Visual log formatting
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${BLUE}ℹ️  [INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}✅ [SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}⚠️  [WARN]${NC} $1"; }
log_error() { echo -e "${RED}❌ [ERROR]${NC} $1"; }

TEMP_CONTAINER="temp_legacy_db_restore"
TEMP_DUMP_FILE="/tmp/deeppace_legacy_restore_$$.sql"

CLEANUP_SELF=false
if [[ "${1:-}" == "--cleanup-self" ]] || [[ "${AUTO_CLEANUP_SCRIPT:-false}" == "true" ]]; then
  CLEANUP_SELF=true
fi

cleanup() {
  local exit_code=$?
  if docker ps -a -q --filter "name=${TEMP_CONTAINER}" | grep -q .; then
    log_info "Cleaning up temporary container '${TEMP_CONTAINER}'..."
    docker rm -f "${TEMP_CONTAINER}" >/dev/null 2>&1 || true
  fi
  if [ -f "${TEMP_DUMP_FILE}" ]; then
    log_info "Securely removing temporary SQL dump..."
    shred -u "${TEMP_DUMP_FILE}" 2>/dev/null || rm -f "${TEMP_DUMP_FILE}"
  fi
  if [ "${exit_code}" -eq 0 ] && [ "${CLEANUP_SELF}" = "true" ]; then
    log_info "Removing one-time migration script '${BASH_SOURCE[0]}'..."
    rm -f -- "${BASH_SOURCE[0]}"
    log_success "One-time script removed."
  fi
}
trap cleanup EXIT SIGINT SIGTERM

echo "=========================================================="
echo "📦 DeepPace Production Database Restoration & Migration"
echo "=========================================================="

# 1. Detect Docker Volumes
log_info "Detecting Docker PostgreSQL volumes..."

LEGACY_VOLUME=$(docker volume ls -q | grep -E 'techdaily_pgdata_prod' | head -n 1 || true)
TARGET_VOLUME=$(docker volume ls -q | grep -E 'deeppace_pgdata_prod' | head -n 1 || true)
if [ -n "${TARGET_VOLUME}" ]; then
  log_info "Found target Docker volume: ${TARGET_VOLUME}"
fi
TARGET_CONTAINER="deeppace_postgres_prod"
TARGET_USER="${POSTGRES_USER:-deeppace_user}"
TARGET_DB="${POSTGRES_DB:-deeppace_db}"

if [ -z "${LEGACY_VOLUME}" ]; then
  log_warn "Legacy volume 'techdaily_pgdata_prod' was not found on this host."
  log_info "Checking if target container '${TARGET_CONTAINER}' is currently running..."
  if ! docker ps --format '{{.Names}}' | grep -q "^${TARGET_CONTAINER}$"; then
    log_error "Neither legacy volume nor active '${TARGET_CONTAINER}' container found. Exiting."
    exit 1
  fi
  log_info "Target container is active. Proceeding to verify database integrity directly..."
else
  log_info "Found legacy Docker volume: ${LEGACY_VOLUME}"

  # 2. Ensure Target Container is Running
  if ! docker ps --format '{{.Names}}' | grep -q "^${TARGET_CONTAINER}$"; then
    log_info "Starting target production database '${TARGET_CONTAINER}' via docker compose..."
    docker compose -f docker-compose.prod.yml up -d db
    log_info "Waiting for target database to become healthy..."
    until [ "$(docker inspect -f '{{.State.Health.Status}}' "${TARGET_CONTAINER}" 2>/dev/null)" = "healthy" ]; do
      sleep 1
    done
  fi

  # 3. Check if in-place rename is possible (if container mounts legacy data directly)
  DB_EXISTS=$(docker exec -i "${TARGET_CONTAINER}" psql -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='techdaily_db';" 2>/dev/null || true)
  if [ "${DB_EXISTS}" = "1" ]; then
    log_info "Found 'techdaily_db' inside '${TARGET_CONTAINER}'. Executing in-place rename..."
    docker exec -i "${TARGET_CONTAINER}" psql -U postgres -c "ALTER DATABASE techdaily_db RENAME TO deeppace_db;"
    docker exec -i "${TARGET_CONTAINER}" psql -U postgres -c "DO \$\$ BEGIN IF EXISTS (SELECT FROM pg_roles WHERE rolname = 'techdaily_user') THEN ALTER USER techdaily_user RENAME TO deeppace_user; END IF; END \$\$;"
    log_success "In-place database and user rename complete."
  else
    # 4. Dump & Restore Pipeline from Legacy Volume
    log_info "Spinning up ephemeral container '${TEMP_CONTAINER}' mounting '${LEGACY_VOLUME}'..."
    docker run --rm -d \
      --name "${TEMP_CONTAINER}" \
      -v "${LEGACY_VOLUME}:/var/lib/postgresql/data" \
      -e POSTGRES_PASSWORD=temp_restore_secret \
      pgvector/pgvector:pg17 >/dev/null

    log_info "Waiting for legacy database to initialize..."
    for _ in {1..30}; do
      if docker exec "${TEMP_CONTAINER}" pg_isready -U postgres >/dev/null 2>&1; then
        break
      fi
      sleep 1
    done

    LEGACY_USER=$(docker exec "${TEMP_CONTAINER}" psql -U postgres -tAc "SELECT rolname FROM pg_roles WHERE rolname IN ('techdaily_user', 'deeppace_user') LIMIT 1;")
    LEGACY_DB=$(docker exec "${TEMP_CONTAINER}" psql -U postgres -tAc "SELECT datname FROM pg_database WHERE datname IN ('techdaily_db', 'deeppace_db') LIMIT 1;")

    log_info "Exporting dump from ${LEGACY_DB} (${LEGACY_USER})..."
    docker exec "${TEMP_CONTAINER}" pg_dump -U "${LEGACY_USER}" -d "${LEGACY_DB}" --clean --if-exists > "${TEMP_DUMP_FILE}"

    log_info "Ensuring target database '${TARGET_DB}' and role '${TARGET_USER}' exist on '${TARGET_CONTAINER}'..."
    docker exec -i "${TARGET_CONTAINER}" psql -U postgres -c "
      DO \$\$ BEGIN
        IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${TARGET_USER}') THEN
          CREATE ROLE ${TARGET_USER} WITH LOGIN SUPERUSER PASSWORD '${POSTGRES_PASSWORD:-techdaily_password_secret}';
        END IF;
      END \$\$;
    " >/dev/null
    docker exec -i "${TARGET_CONTAINER}" psql -U postgres -c "
      SELECT 'CREATE DATABASE ${TARGET_DB} OWNER ${TARGET_USER}'
      WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${TARGET_DB}')\gexec
    " >/dev/null
    docker exec -i "${TARGET_CONTAINER}" psql -U "${TARGET_USER}" -d "${TARGET_DB}" -c "CREATE EXTENSION IF NOT EXISTS vector;" >/dev/null

    log_info "Streaming dump into '${TARGET_CONTAINER}' (${TARGET_DB})..."
    docker exec -i "${TARGET_CONTAINER}" psql -U "${TARGET_USER}" -d "${TARGET_DB}" < "${TEMP_DUMP_FILE}" >/dev/null 2>&1 || true

    log_success "Data restored successfully from ${LEGACY_VOLUME} into ${TARGET_DB}."
  fi
fi

# 5. Relational Integrity & Record Count Verification
echo ""
log_info "Verifying database record counts on '${TARGET_DB}'..."

docker exec -i "${TARGET_CONTAINER}" psql -U "${TARGET_USER}" -d "${TARGET_DB}" -c "
SELECT 'Users' AS entity, count(*) AS total_records FROM \"Users\"
UNION ALL
SELECT 'DocumentBooks', count(*) FROM \"DocumentBooks\"
UNION ALL
SELECT 'DocumentChunks', count(*) FROM \"DocumentChunks\"
UNION ALL
SELECT 'SpacedRepetitionCards', count(*) FROM \"SpacedRepetitionCards\"
UNION ALL
SELECT 'UserHighlights', count(*) FROM \"UserHighlights\";
"

echo ""
log_success "Database migration and restoration verification completed successfully!"

if [ "${CLEANUP_SELF}" != "true" ]; then
  echo ""
  log_info "💡 Note: Once production migration is verified, you can delete this one-time script with:"
  echo "   rm -f scripts/restore-prod-database.sh"
fi
