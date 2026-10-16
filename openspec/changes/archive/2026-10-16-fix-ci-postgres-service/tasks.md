# Tasks

## 1. CI Workflow Configuration

- [x] 1.1 Add `postgres` service container with `pgvector/pgvector:pg17` and `pg_isready` health check to the `backend-ci` job in `.github/workflows/ci.yml`
- [x] 1.2 Configure `TEST_POSTGRES_CONNECTION` environment variable in the `Run Backend Tests (DeepPace.Tests)` step in `.github/workflows/ci.yml`

## 2. Infrastructure & Test Resilience

- [x] 2.1 Refactor `CatalogAdoptionPerfAndRollbackVerificationTests` to safely initialize `NpgsqlDataSource` without throwing `TypeInitializationException` when `TEST_POSTGRES_CONNECTION` is absent
- [x] 2.2 Ensure shared PostgreSQL test fixtures (`CatalogAdoptionPerfAndRollbackVerificationTests`, `PostgresCatalogAdoptionTests`, `PostgresRoleSchemaAndBackfillTests`) execute `db.Database.Migrate()` when connecting to a fresh database

## 3. Verification & CI Validation

- [x] 3.1 Run full backend test suite (`dotnet test backend/DeepPace.sln`) with `TEST_POSTGRES_CONNECTION` pointing to local PostgreSQL to verify all 842+ tests pass
- [x] 3.2 Run backend tests without `TEST_POSTGRES_CONNECTION` to verify tests skip cleanly without unhandled runtime exceptions
