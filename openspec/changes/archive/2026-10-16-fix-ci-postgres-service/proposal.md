# Proposal

## Why

The backend test suite in `.github/workflows/ci.yml` failed in GitHub Actions run `38859746944` because 15 PostgreSQL infrastructure and concurrency integration tests require an active PostgreSQL instance with the `pgvector` extension and the `TEST_POSTGRES_CONNECTION` environment variable. The runner environment lacked a database container, causing `InvalidOperationException` and static initialization failures during `dotnet test`. Adding a dedicated PostgreSQL service container to the CI workflow ensures that all 842+ backend unit and integration tests run, verify real database constraints (row locks, unique slug indexes, and transaction rollbacks), and pass reliably prior to deployment.

## What Changes

- Add a `postgres` service container running `pgvector/pgvector:pg17` to the `backend-ci` job in `.github/workflows/ci.yml` with health checks (`pg_isready`).
- Configure the `TEST_POSTGRES_CONNECTION` environment variable in the `Run Backend Tests (DeepPace.Tests)` step pointing to `localhost:5432`.
- Ensure test database schema readiness by allowing integration tests querying the shared test database (`CatalogAdoptionPerfAndRollbackVerificationTests`, `PostgresCatalogAdoptionTests`, `PostgresRoleSchemaAndBackfillTests`) to automatically apply EF Core migrations via `db.Database.Migrate()` on connection check.
- Harden `CatalogAdoptionPerfAndRollbackVerificationTests` against static constructor initialization failures (`TypeInitializationException`) when running in environments without `TEST_POSTGRES_CONNECTION`.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Add requirement for automated CI backend integration test harness with containerized PostgreSQL and pgvector service.

## Impact

- **CI/CD Pipeline**: Adds ~15-20 seconds for pulling the `pgvector:pg17` image and starting the service container in GitHub Actions runner.
- **Backend Tests**: 100% of integration and concurrency tests (including `PostgresCategoryConcurrencyTests`, `PostgresCategoryAssignmentConcurrencyTests`, and `CatalogAdoptionPerfAndRollbackVerificationTests`) will be fully exercised and validated on every push and pull request.
- **Production Safety**: Guarantees zero regression on database schema migrations, row locking semantics, and PostgreSQL-specific features before deploying container images.
