# Design

## Context

The backend continuous integration job `backend-ci` in `.github/workflows/ci.yml` runs `dotnet test` on an uncontainerized Ubuntu runner. Tests introduced for admin category management (`PostgresCategoryConcurrencyTests`, `PostgresCategoryAssignmentConcurrencyTests`) and catalog adoption performance (`CatalogAdoptionPerfAndRollbackVerificationTests`) directly communicate with PostgreSQL to verify `FOR SHARE` locks, unique constraint violations (code 23505), and transaction rollbacks. Without a running PostgreSQL instance and `TEST_POSTGRES_CONNECTION` environment variable, 15 tests fail in CI.

## Goals / Non-Goals

**Goals:**
- Provide a healthy, production-identical PostgreSQL 17 + pgvector service in `backend-ci`.
- Pass `TEST_POSTGRES_CONNECTION` to `dotnet test` so all integration and concurrency tests execute against a real database.
- Ensure the shared test database is migrated with all EF Core migrations before tests query tables.
- Prevent unhandled `TypeInitializationException` in `CatalogAdoptionPerfAndRollbackVerificationTests` when executed without environment variables.

**Non-Goals:**
- Replacing real database integration tests with in-memory or mocked alternatives (real concurrency row locks and vector operations require PostgreSQL).
- Changing production database deployment or migration procedures on the Google Cloud VPS.
- Altering frontend CI or docker build-push jobs.

## Decisions

### 1. GitHub Actions Native Service Container
We configure a `postgres` service container directly within the `backend-ci` job in `.github/workflows/ci.yml`:
- **Image:** `pgvector/pgvector:pg17` (identical to production and local development).
- **Environment:** `POSTGRES_DB: postgres`, `POSTGRES_USER: postgres`, `POSTGRES_PASSWORD: postgres_test_password`.
- **Ports:** `5432:5432`.
- **Health Check:** `pg_isready` with 5s interval, 5s timeout, 5 retries.
- **Connection String:** `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres_test_password`.

*Rationale:* GitHub Actions runs services in Docker on the same bridge network as the host, allowing the runner steps to connect directly via `localhost:5432`.

### 2. Dual Database Strategy for Integration Tests
- **Dynamic Isolated Databases:** `PostgresCategoryConcurrencyTests` and `PostgresCategoryAssignmentConcurrencyTests` connect to the admin database (`Database = "postgres"`), execute `CREATE DATABASE "..."`, run `db.Database.MigrateAsync()`, and clean up with `DROP DATABASE ... WITH (FORCE)`. Using superuser `postgres` allows seamless database creation and deletion.
- **Shared Test Database:** Tests like `CatalogAdoptionPerfAndRollbackVerificationTests`, `PostgresCatalogAdoptionTests`, and `PostgresRoleSchemaAndBackfillTests` use the primary database specified in the connection string. In `IsPostgresAvailable()`, when `canConnect` is true, the test calls `db.Database.Migrate()` (matching the precedent in `PostgresAccountConcurrencyTests`), guaranteeing that all tables and seed data are ready.

### 3. Guarded / Lazy DataSource Initialization
`CatalogAdoptionPerfAndRollbackVerificationTests` previously accessed `PostgresConnStr` in a static constructor:
```csharp
static CatalogAdoptionPerfAndRollbackVerificationTests()
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(PostgresConnStr);
    ...
}
```
If `TEST_POSTGRES_CONNECTION` is absent, accessing `PostgresConnStr` throws `InvalidOperationException`, wrapping into `TypeInitializationException` and failing all tests in the class before test methods execute.
We refactor `DataSource` to initialize safely only when `IsPostgresAvailable()` succeeds, or initialize with a safe fallback when connection is absent, enabling graceful skipping outside CI.

## Risks / Trade-offs

- **CI Execution Duration:** Pulling `pgvector/pgvector:pg17` and waiting for the health check adds ~15 to 25 seconds to the `backend-ci` job. This is an acceptable tradeoff for 100% confidence in database concurrency, locking, and migration safety.
- **Port Conflict in Local Act Runners:** If someone runs GitHub Actions locally with `act`, port 5432 may clash if local PostgreSQL is already bound. In actual GitHub Actions cloud runners (`ubuntu-latest`), port 5432 is free.
