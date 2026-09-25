# Proposal

## Why
Following the historical database purge and vector backfill operation executed on 2026-09-16, the command-line argument handling (`args.Contains("--cleanup-data")`) in `Program.cs`, its underlying service `DatabaseMaintenanceRunner`, its dedicated unit tests, and standalone SQL scripts remain in the codebase as dead weight. 

All tainted mock data has been permanently purged from both local development and production databases. Furthermore, normal application startup already handles PostgreSQL migrations, idempotent curriculum and insights seeding, and vector backfills. Retaining this obsolete maintenance CLI scaffolding adds unnecessary cognitive overhead, occupies over 130 lines in `Program.cs`, and directly violates the project invariant in `AGENTS.md`: *"Delete dead weight; prefer boring design to needless abstraction. Code made obsolete by cutover is in scope."*

## What Changes
- **Remove CLI Maintenance Block in `Program.cs`**: Delete the 133-line `if (args.Contains("--cleanup-data"))` execution block from `backend/src/TechDaily.Api/Program.cs`, allowing `Program.cs` to focus purely on standard middleware, dependency injection, and endpoint routing.
- **Delete `DatabaseMaintenanceRunner.cs`**: Remove `backend/src/TechDaily.Infrastructure/Maintenance/DatabaseMaintenanceRunner.cs` (383 lines of obsolete mock slug regexes, transactional purge logic, and console reporting).
- **Remove DI Registration**: Remove `services.AddScoped<DatabaseMaintenanceRunner>();` from `backend/src/TechDaily.Infrastructure/DependencyInjection.cs`.
- **Delete Maintenance Unit Tests**: Remove `backend/tests/TechDaily.Tests/Infrastructure/DatabaseMaintenanceRunnerTests.cs` (105 lines).
- **Remove Legacy One-off SQL Scripts**: Delete `scripts/cleanup-dry-run.sql` and `scripts/cleanup-execute.sql`.
- **Update Core Platform Spec**: In `openspec/specs/core-platform/spec.md`, remove the obsolete requirement `Headless Operational Maintenance CLI`.

## Capabilities

### Modified Capabilities
- `core-platform`: Removes the `Headless Operational Maintenance CLI` requirement and its associated CLI execution scenarios.

## Impact
- **Backend**: Eliminates 600+ lines of dead code. Zero regressions or breaking changes for existing Minimal API endpoints, domain models, or database schemas.
- **Database**: Zero schema migrations required. Standard startup seeding and vector backfill routines in `CurriculumSeeder` and `TechInsightsSeeder` remain untouched.
- **Testing**: Backend test suite compiles cleanly and passes all active tests without deprecated maintenance test scaffolding.
