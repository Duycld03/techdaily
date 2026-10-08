# Proposal

## Why

Following the complete platform rebrand to **DeepPace** (renaming solution files, C# namespaces, database containers, and the domain to `deeppace.duckdns.org`), the environment template (`.env.example`) and deployment configurations still contain legacy `techdaily` references, legacy database names, and outdated CORS and JWT issuer settings. This causes configuration drift, potential runtime connection failures on VPS deployments, and ambiguity for developers configuring local or remote environments.

Establishing a canonical, audited, and comprehensive environment configuration across both local development and VPS production guarantees zero configuration mismatch and secures all platform integrations (PostgreSQL, Gemini AI, Google Cloud TTS, Google OAuth, Web Push, and SMTP).

## What Changes

- **Audited & Rebranded Environment Template (`.env.example`)**:
  - Rebrand all headers, comments, and instructions from `TechDaily` to `DeepPace`.
  - Update default database variables to `POSTGRES_DB=deeppace_db` and `POSTGRES_USER=deeppace_user`.
  - Add explicit JWT Issuer and Audience variables (`Jwt__Issuer=DeepPace`, `Jwt__Audience=DeepPaceUsers`).
  - Update default production CORS origins to `CORS_ALLOWED_ORIGINS=https://deeppace.duckdns.org,https://techdaily.duckdns.org`.
  - Update Web Push contact subject to `WebPush__Subject=mailto:support@deeppace.app`.
  - Group and document all 9 configuration categories clearly: Database, JWT Security, Google OAuth 2.0, Frontend Runtime, Gemini AI Engine, Google Cloud Neural TTS, Web Push (VAPID), Transactional Email (SMTP), and E2E Test Credentials.
- **Local Development Alignment (`run-dev.sh`)**:
  - Ensure fallback environment variables and banners align with `deeppace_db` and `deeppace_user`.
  - Verify seamless derivation of frontend and backend Google Client IDs and database connection strings.
- **VPS Production Deployment Alignment (`docker-compose.prod.yml`)**:
  - Standardize container environment injection via `env_file: .env`.
  - Align database service and connection string with `deeppace_db` and `deeppace_user`.
- **Production Database Data Restoration & Migration Script (`scripts/restore-prod-database.sh`)**:
  - Provide an automated, fail-safe migration script that detects existing data in the legacy `techdaily_pgdata_prod` volume.
  - Execute zero-loss restoration into `deeppace_db` and `deeppace_user` on `deeppace_pgdata_prod` (via automated `pg_dump` / `psql` pipe or in-place `ALTER DATABASE` rename).
  - Validate that all user accounts, imported books, spaced repetition cards, and highlights are fully preserved.
## Capabilities

### Modified Capabilities
- `core-platform`: Updates platform environment variable configuration contracts, `.env.example`, and deployment environment defaults.

## Impact

- **Affected Files**:
  - `.env.example`
  - `docker-compose.prod.yml`
  - `run-dev.sh`
  - `scripts/restore-prod-database.sh`
- **Breaking Changes**: None. Supporting fallback variables and providing migration commands ensures existing installations transition smoothly.
- **Verification Gate**:
  - Both local development (`run-dev.sh`) and production compose (`docker compose -f docker-compose.prod.yml config`) parse environment variables without errors or missing mandatory secrets.
