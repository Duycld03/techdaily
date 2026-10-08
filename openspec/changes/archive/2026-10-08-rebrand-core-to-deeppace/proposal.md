# Proposal

## Why

TechDaily has evolved from a developer-focused interview preparation tool into **DeepPace**, an open deliberate practice and active recall platform for technical documentation and mental craft. While user-facing UI copy and prompts were previously rebranded, the internal core—including the .NET solution (`TechDaily.sln`), C# project namespaces (`TechDaily.*`), DbContext (`TechDailyDbContext`), database containers (`techdaily_db`), client-side token storage keys (`techdaily_token`), CI/CD workflows, and root documentation—remains coupled to the legacy identity. Completing a clean-slate, end-to-end rebranding now establishes unified identity and architectural coherence across the entire stack while the codebase size is manageable (~340 C# files) and before further scaling.

## What Changes

- **Project Documentation & Governance Synchronization**:
  - Rebrand root `README.md`, developer conventions `AGENTS.md`, and `openspec/config.yaml` to DeepPace.
  - Update local development script `run-dev.sh` banner and service labels.
  - Update GitHub Actions CI/CD (`.github/workflows/ci.yml`) workflow name, solution paths, test runners, and GHCR container image names (`deeppace-backend`, `deeppace-frontend`).
- **Backend .NET Solution, Projects & Namespaces**:
  - Rename solution file `backend/TechDaily.sln` to `backend/DeepPace.sln`.
  - Rename project directories and `.csproj` files:
    - `backend/src/TechDaily.Domain/` $\rightarrow$ `backend/src/DeepPace.Domain/DeepPace.Domain.csproj`
    - `backend/src/TechDaily.Application/` $\rightarrow$ `backend/src/DeepPace.Application/DeepPace.Application.csproj`
    - `backend/src/TechDaily.Infrastructure/` $\rightarrow$ `backend/src/DeepPace.Infrastructure/DeepPace.Infrastructure.csproj`
    - `backend/src/TechDaily.Api/` $\rightarrow$ `backend/src/DeepPace.Api/DeepPace.Api.csproj`
    - `backend/tests/TechDaily.Tests/` $\rightarrow$ `backend/tests/DeepPace.Tests/DeepPace.Tests.csproj`
  - Refactor all C# namespaces from `namespace TechDaily.*` to `namespace DeepPace.*`.
  - Rename `TechDailyDbContext` to `DeepPaceDbContext`.
  - Update `backend/Dockerfile` restore paths, build targets, and entrypoint assembly (`DeepPace.Api.dll`).
- **Database & Container Orchestration**:
  - Update `docker-compose.yml` and `docker-compose.prod.yml`:
    - Container names: `deeppace_postgres`, `deeppace_backend_prod`, `deeppace_frontend_prod`, `deeppace_nginx_prod`.
    - Database name & user: `POSTGRES_DB: deeppace_db`, `POSTGRES_USER: deeppace_user`.
    - Connection strings in `appsettings.json`, `appsettings.Development.json`, and `.env.example`.
  - Include zero-data-loss database migration commands (`pg_dump` and `ALTER DATABASE` procedures) for existing VPS volumes.
- **Client Storage, JWT & Frontend Package**:
  - Update `frontend/package.json` package name to `"deeppace-frontend"`.
  - Transition client session keys (`techdaily_token` $\rightarrow$ `deeppace_token`, `techdaily_user` $\rightarrow$ `deeppace_user`, `techdaily_reader_typography` $\rightarrow$ `deeppace_reader_typography`) with backward-compatible fallback parsing to prevent session loss.
  - Update default JWT Issuer and Audience to `"DeepPace"` and `"DeepPaceUsers"` while accepting legacy tokens during migration window.

## Capabilities

### New Capabilities

*(None)*

### Modified Capabilities

- `core-platform`: Updates platform solution name, project files, C# namespaces, DbContext, container names, and database default identifiers.
- `auth`: Updates JWT Issuer/Audience contracts, client-side session cookie/localStorage identifiers (`deeppace_token`, `deeppace_user`), and session migration fallbacks.

## Impact

- **Affected Systems**: Entire repository (Backend C# solution/projects, Frontend storage/package, Docker Compose, CI/CD, Documentation).
- **Breaking Changes**:
  - C# assembly and namespace renaming requires rebuilding dependencies (`dotnet restore` / `dotnet build`).
  - Production deployment requires database rename procedure on PostgreSQL.
- **Verification Gate**:
  - 100% of backend tests (`dotnet test backend/DeepPace.sln`, 351 tests) must pass.
  - 100% of frontend tests (`npm test`, 74 test suites, 664 tests) must pass.
  - Production build (`npm run build` and `dotnet publish`) must succeed with zero errors.
