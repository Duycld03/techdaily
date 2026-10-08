# Tasks

## 1. Domain & Application Refactoring

- [x] 1.1 Rename `TechDaily.Domain` directory and project file to `DeepPace.Domain/DeepPace.Domain.csproj`, updating namespaces across all entities, value objects, and enums.
- [x] 1.2 Rename `TechDaily.Application` directory and project file to `DeepPace.Application/DeepPace.Application.csproj`, updating project references and use-case namespaces.

## 2. Infrastructure & Api Refactoring

- [x] 2.1 Rename `TechDaily.Infrastructure` directory and project file to `DeepPace.Infrastructure/DeepPace.Infrastructure.csproj`, rename `TechDailyDbContext` to `DeepPaceDbContext`, and update all infrastructure service namespaces.
- [x] 2.2 Rename `TechDaily.Api` directory and project file to `DeepPace.Api/DeepPace.Api.csproj`, update Minimal API endpoint namespaces, and configure default JWT Issuer and Audience to `"DeepPace"` and `"DeepPaceUsers"`.
- [x] 2.3 Rename `TechDaily.Tests` directory and project file to `DeepPace.Tests/DeepPace.Tests.csproj` and update test file namespaces, mocks, and fixtures.
- [x] 2.4 Rename solution file `backend/TechDaily.sln` to `backend/DeepPace.sln` and update internal project GUID bindings.

## 3. Container & DevOps Orchestration

- [x] 3.1 Update `backend/Dockerfile` restore, build, and publish commands, setting entrypoint to `["dotnet", "DeepPace.Api.dll"]`.
- [x] 3.2 Update `docker-compose.yml` and `docker-compose.prod.yml` container names, database defaults (`POSTGRES_DB: deeppace_db`, `POSTGRES_USER: deeppace_user`), and connection strings.
- [x] 3.3 Update `.github/workflows/ci.yml` workflow title, solution build steps, and GHCR container image names (`deeppace-backend`, `deeppace-frontend`).

## 4. Frontend Storage & Configuration

- [x] 4.1 Update `frontend/package.json` package name to `"deeppace-frontend"`.
- [x] 4.2 Update `frontend/app/stores/useAuthStore.ts`, `useApiClient.ts`, and `auth.global.ts` to use `deeppace_token` and `deeppace_user` with backward-compatible migration fallback for legacy keys.
- [x] 4.3 Update reader and graph local storage preference keys to `deeppace_*` prefix across frontend components and composables.

## 5. Documentation & Project Governance

- [x] 5.1 Rebrand `README.md` title, badges, project overview, and architectural breakdown to DeepPace.
- [x] 5.2 Update `AGENTS.md` and `openspec/config.yaml` project descriptions and conventions to DeepPace.
- [x] 5.3 Update `run-dev.sh` console banner and service labels.

## 6. Verification & Dual-Gate Testing

- [x] 6.1 Execute `dotnet test backend/DeepPace.sln` to verify all 351 backend tests pass 100%.
- [x] 6.2 Execute `npm test` in `frontend/` to verify all 74 test suites pass 100%.
- [x] 6.3 Execute `npm run build` in `frontend/` and `dotnet publish` in `backend/` to verify production builds compile cleanly.
- [x] 6.4 Conduct headless browser visual inspection on Desktop and Mobile viewports verifying authenticated app hydration.
