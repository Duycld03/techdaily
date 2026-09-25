# Tasks

## 1. Api (Program.cs Cleanup)

- [x] 1.1 Remove `args.Contains("--cleanup-data")` evaluation and maintenance execution block from `backend/src/TechDaily.Api/Program.cs`.

## 2. Infrastructure (Maintenance Class & DI Removal)

- [x] 2.1 Remove `services.AddScoped<DatabaseMaintenanceRunner>();` from `backend/src/TechDaily.Infrastructure/DependencyInjection.cs`.
- [x] 2.2 Delete `backend/src/TechDaily.Infrastructure/Maintenance/DatabaseMaintenanceRunner.cs`.
- [x] 2.3 Delete standalone SQL scripts `scripts/cleanup-dry-run.sql` and `scripts/cleanup-execute.sql`.

## 3. Tests & Verification

- [x] 3.1 Delete `backend/tests/TechDaily.Tests/Infrastructure/DatabaseMaintenanceRunnerTests.cs`.
- [x] 3.2 Verify backend compiles cleanly with `dotnet build backend/TechDaily.sln`.
- [x] 3.3 Verify all remaining backend tests pass with `dotnet test backend/TechDaily.sln`.
