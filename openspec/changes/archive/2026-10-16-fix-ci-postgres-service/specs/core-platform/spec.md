# Spec Delta

## ADDED Requirements

### Requirement: Automated CI Backend Integration & PostgreSQL Service Test Harness
The continuous integration workflow for backend services (`.github/workflows/ci.yml`) SHALL provision a dedicated PostgreSQL service container with the `pgvector` extension (`pgvector/pgvector:pg17`), binding port 5432 and injecting `TEST_POSTGRES_CONNECTION` into the test execution environment to validate all unit, integration, and database concurrency suites prior to artifact deployment.

#### Scenario: Backend CI Test Execution with PostgreSQL Service
- **WHEN** a push or pull request triggers the `DeepPace CI` workflow
- **THEN** the `backend-ci` runner launches a healthy `pgvector/pgvector:pg17` container on port 5432, passes `TEST_POSTGRES_CONNECTION` to `dotnet test`, and completes all backend test suites with 100% pass rate.

#### Scenario: Schema Migration and Concurrency Isolation in Test Database
- **WHEN** database integration and concurrency tests execute against the test PostgreSQL instance
- **THEN** the required relational schema is migrated on demand, dynamic isolated test databases are created and disposed without leaking connections or state, and row-level locks and slug unique constraint conflicts are verified under concurrency.

#### Scenario: Test Suite Resilience Outside CI Environment
- **WHEN** backend tests are executed locally without an active PostgreSQL test instance or without `TEST_POSTGRES_CONNECTION`
- **THEN** tests requiring a live PostgreSQL instance report skipped status gracefully without throwing unhandled `TypeInitializationException` or aborting the test runner process.
