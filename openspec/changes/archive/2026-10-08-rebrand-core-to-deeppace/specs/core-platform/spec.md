# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: DeepPace Unified Solution & Clean Architecture Identity
The platform backend solution, C# projects, and executable assemblies SHALL operate under the canonical `DeepPace` naming hierarchy across all repository layers:

1. **Solution & Project Structure**:
   - The primary .NET solution file SHALL be `backend/DeepPace.sln`.
   - Project directories and assemblies SHALL be:
     - `DeepPace.Domain` (`backend/src/DeepPace.Domain/DeepPace.Domain.csproj`)
     - `DeepPace.Application` (`backend/src/DeepPace.Application/DeepPace.Application.csproj`)
     - `DeepPace.Infrastructure` (`backend/src/DeepPace.Infrastructure/DeepPace.Infrastructure.csproj`)
     - `DeepPace.Api` (`backend/src/DeepPace.Api/DeepPace.Api.csproj`)
     - `DeepPace.Tests` (`backend/tests/DeepPace.Tests/DeepPace.Tests.csproj`)
2. **Root Namespaces**:
   - All C# types, records, domain invariants, application use cases, and Minimal API endpoint groups SHALL use root namespace `DeepPace.*` (`DeepPace.Domain.*`, `DeepPace.Application.*`, `DeepPace.Infrastructure.*`, `DeepPace.Api.*`).
3. **Database Context & Persistence**:
   - The primary EF Core DbContext SHALL be `DeepPaceDbContext`.
   - Default container and database configurations in Docker compose and development scripts SHALL standardize on `POSTGRES_DB: deeppace_db` and `POSTGRES_USER: deeppace_user`.
4. **Container & CI/CD Pipeline Artifacts**:
   - The backend `Dockerfile` SHALL build and publish `DeepPace.Api.csproj` and invoke `ENTRYPOINT ["dotnet", "DeepPace.Api.dll"]`.
   - GitHub Actions workflow (`ci.yml`) and GHCR container registries SHALL standardize on `deeppace-backend` and `deeppace-frontend`.

#### Scenario: Backend test suite builds and executes under DeepPace solution
- **WHEN** an engineer or CI runner executes `dotnet test backend/DeepPace.sln`
- **THEN** all projects compile cleanly with zero namespace resolution errors
- **AND** 100% of unit and integration tests pass successfully.

#### Scenario: Production backend Docker container boots with DeepPace assembly
- **WHEN** the backend container launches from `ghcr.io/duycld03/deeppace-backend:latest`
- **THEN** the Kestrel server starts using `DeepPace.Api.dll`
- **AND** probes to `/health` respond with `HTTP 200 OK` and `status: "healthy"`.
