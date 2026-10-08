# Design

## Context

TechDaily is undergoing an end-to-end core rebrand to **DeepPace**. While user-facing UI copy and prompts were previously refreshed, the underlying backend solution (`TechDaily.sln`), C# projects (`TechDaily.*.csproj`), DbContext (`TechDailyDbContext`), Docker configurations, CI/CD pipelines, client storage keys (`techdaily_token`), and project documentation remain coupled to the legacy identity. See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**
- Rename backend solution to `backend/DeepPace.sln` and all 5 C# projects to `DeepPace.*`.
- Refactor all namespaces across all 339+ C# files to `namespace DeepPace.*`.
- Rename EF Core DbContext to `DeepPaceDbContext` and update DI registrations.
- Update `backend/Dockerfile`, `docker-compose.yml`, `docker-compose.prod.yml`, and `.github/workflows/ci.yml`.
- Standardize client-side storage keys on `deeppace_*` with zero-downtime session migration fallbacks.
- Update project documentation (`README.md`, `AGENTS.md`, `openspec/config.yaml`, `run-dev.sh`).
- Verify 100% test pass rate across backend (351 tests) and frontend (664 tests).

**Non-Goals:**
- Altering business logic, SM-2 algorithm intervals, or domain entity schemas.
- Changing REST API route URLs (`/api/v1/...` routes remain unchanged).
- Renaming the remote GitHub repository URL (handled separately in GitHub repo settings).

## Decisions

### 1. Project Directory & File Renaming Sequence
- **Choice**: Rename project directories and `.csproj` files using `git mv` where possible:
  1. `backend/src/TechDaily.Domain` $\rightarrow$ `backend/src/DeepPace.Domain` (`DeepPace.Domain.csproj`)
  2. `backend/src/TechDaily.Application` $\rightarrow$ `backend/src/DeepPace.Application` (`DeepPace.Application.csproj`)
  3. `backend/src/TechDaily.Infrastructure` $\rightarrow$ `backend/src/DeepPace.Infrastructure` (`DeepPace.Infrastructure.csproj`)
  4. `backend/src/TechDaily.Api` $\rightarrow$ `backend/src/DeepPace.Api` (`DeepPace.Api.csproj`)
  5. `backend/tests/TechDaily.Tests` $\rightarrow$ `backend/tests/DeepPace.Tests` (`DeepPace.Tests.csproj`)
  6. `backend/TechDaily.sln` $\rightarrow$ `backend/DeepPace.sln`
- **Rationale**: Preserves Git history while establishing clean canonical folder naming.

### 2. Dependency-Ordered Namespace Refactoring
- **Choice**: Refactor C# files in inward-to-outward dependency order:
  `Domain` $\rightarrow$ `Application` $\rightarrow$ `Infrastructure` $\rightarrow$ `Api` $\rightarrow$ `Tests`.
- **Global Replacements**:
  - `namespace TechDaily.` $\rightarrow$ `namespace DeepPace.`
  - `using TechDaily.` $\rightarrow$ `using DeepPace.`
  - `TechDailyDbContext` $\rightarrow$ `DeepPaceDbContext`
- **Rationale**: Prevents cascading compilation errors by stabilizing the lowest architectural layer (Domain) first.

### 3. Production Database Migration & Zero Data Loss Strategy
- **Choice**: For local development, update defaults in `docker-compose.yml` (`deeppace_db`, `deeppace_user`).
  For existing production environments on VPS:
  - Provide a safe migration procedure:
    ```sql
    ALTER DATABASE techdaily_db RENAME TO deeppace_db;
    ALTER USER techdaily_user RENAME TO deeppace_user;
    ```
  - Alternatively, keep `ConnectionStrings__DefaultConnection` configurable via environment variables in `.env` so local and remote can cut over independently.

### 4. Client Session Migration Fallback
- **Choice**: In `frontend/app/stores/useAuthStore.ts`, `useApiClient.ts`, and `auth.global.ts`:
  - When reading session credentials:
    ```ts
    const token = useCookie('deeppace_token').value || useCookie('techdaily_token').value
    ```
  - When saving or refreshing:
    - Save to `deeppace_token` and `deeppace_user`.
    - Delete legacy `techdaily_token` and `techdaily_user`.
- **Rationale**: Prevents active users on production from being forcefully logged out when the code is deployed.

## Risks / Trade-offs

- **[Risk] Large Diff / Git Merge Conflicts**:
  - *Mitigation*: Complete and merge existing in-flight changes (`manage-library-books`) before executing the rename, or execute the rename on a dedicated branch with fast integration.
- **[Risk] Docker Build / CI Failure due to Path Mismatches**:
  - *Mitigation*: Update `Dockerfile` and `ci.yml` simultaneously with the solution rename, and execute `dotnet test backend/DeepPace.sln` and `npm run build` locally before pushing.
- **[Risk] Production Deployment Outage**:
  - *Mitigation*: Ensure database credentials and connection strings in `.env` match between Nginx/compose before restarting production containers.
