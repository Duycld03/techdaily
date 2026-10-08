# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Canonical DeepPace Environment Variable Standard
The platform SHALL maintain a canonical environment variable dictionary in `.env.example` adhering to the DeepPace architecture and .NET configuration binding conventions:

1. **Naming & Section Hierarchy**:
   - Environment variables overriding hierarchical configuration SHALL use .NET-native double-underscore notation (`Section__Key`), binding directly to `IConfiguration` without custom parsers.
   - Database credentials SHALL default to `deeppace_db` and `deeppace_user`.
   - JWT tokens SHALL default to Issuer `"DeepPace"` and Audience `"DeepPaceUsers"`.
   - Allowed CORS origins SHALL default to `https://deeppace.duckdns.org`.
2. **Environment Parity**:
   - The same variable names in `.env` SHALL be used across both local development (sourced by `run-dev.sh`) and VPS production (injected by `docker-compose.prod.yml` via `env_file`).
   - Secrets SHALL NOT be hardcoded in committed application source files or configuration templates.

#### Scenario: Local development script boots with standard .env
- **WHEN** a developer executes `./run-dev.sh` with a valid `.env`
- **THEN** the script loads all variables
- **AND** the backend connects to `deeppace_db` with `deeppace_user` on `localhost:5432`
- **AND** the frontend boots with `NUXT_PUBLIC_API_BASE_URL=http://localhost:5000`.

#### Scenario: Production compose deploys with DeepPace environment variables
- **WHEN** the production stack is booted via `docker compose -f docker-compose.prod.yml up`
- **THEN** `deeppace_backend_prod` reads secrets from `.env`
- **AND** ASP.NET Core accepts JWT tokens with Issuer `"DeepPace"` and enforces CORS for `https://deeppace.duckdns.org`.

### Requirement: Production Database Migration & Restoration Tooling
The platform SHALL provide an idempotent, automated data restoration script (`scripts/restore-prod-database.sh`) to migrate legacy PostgreSQL database data from `techdaily_pgdata_prod` into `deeppace_pgdata_prod`:

1. **Volume & Container Detection**:
   - The script SHALL automatically detect if the legacy volume `techdaily_pgdata_prod` exists on the host.
   - If legacy data is detected, the script SHALL execute a zero-data-loss transfer into `deeppace_db` under user `deeppace_user`.
2. **Data Integrity & Relational Verification**:
   - The restoration process SHALL preserve all relational entities, vector embeddings, users, document books, chunks, highlights, and spaced repetition cards without corruption.

#### Scenario: Production restoration script migrates legacy volume data
- **WHEN** an administrator runs `./scripts/restore-prod-database.sh` on the VPS
- **THEN** the script extracts database contents from legacy `techdaily_pgdata_prod`
- **AND** loads them into `deeppace_postgres_prod` (`deeppace_db`)
- **AND** verifies that existing user accounts and learning artifacts are fully queryable.
