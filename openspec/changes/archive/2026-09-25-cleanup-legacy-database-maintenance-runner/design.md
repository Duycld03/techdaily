# Design: Clean Up Legacy Database Maintenance Runner

## Context
See `proposal.md` for historical motivation and context.
`TechDaily.Api/Program.cs` currently intercepts execution before standard middleware configuration to evaluate `args.Contains("--cleanup-data")`. If present, it resolves `DatabaseMaintenanceRunner`, executes either a dry-run or a destructive purge/backfill against PostgreSQL, outputs ASCII tables to stdout, and calls `return;`.

Now that the fallback mock data issue is permanently resolved and regular startup seeding handles database readiness, this maintenance runner is obsolete.

## Goals / Non-Goals

**Goals:**
- Purge all CLI maintenance logic from `backend/src/TechDaily.Api/Program.cs` (lines 187–319).
- Delete `backend/src/TechDaily.Infrastructure/Maintenance/DatabaseMaintenanceRunner.cs`.
- Remove DI registration in `backend/src/TechDaily.Infrastructure/DependencyInjection.cs`.
- Delete `backend/tests/TechDaily.Tests/Infrastructure/DatabaseMaintenanceRunnerTests.cs`.
- Delete standalone SQL scripts `scripts/cleanup-dry-run.sql` and `scripts/cleanup-execute.sql`.

**Non-Goals:**
- Touching `CurriculumSeeder.SeedAsync`, `TechInsightsSeeder.SeedAsync`, or `CurriculumSeeder.BackfillEmbeddingsAsync` (those handle normal startup and will be addressed in the subsequent user-centric starter handbook change).
- Modifying database migrations, API routes, or frontend code.

## Decisions

### 1. Complete Code Elimination (No Shims or Flags)
Per the project invariant *"Delete dead weight; prefer boring design to needless abstraction. Code made obsolete by cutover is in scope"*, we delete `DatabaseMaintenanceRunner` and the `args.Contains("--cleanup-data")` block entirely rather than retaining empty stubs or warnings.

### 2. Preserve Startup Health & Seeding Pipeline
Normal development startup in `Program.cs` (lines 348–393) continues to invoke:
```csharp
await context.Database.MigrateAsync();
await CurriculumSeeder.SeedAsync(context);
await TechInsightsSeeder.SeedAsync(context);
await CurriculumSeeder.BackfillEmbeddingsAsync(context, embeddingService, logger);
```
This isolates the scope of this proposal strictly to removing the dead maintenance runner without affecting runtime behavior.

## Risks / Trade-offs

- **Risk:** An operator or script attempting to run `dotnet TechDaily.Api.dll --cleanup-data` will now boot the standard Kestrel API server rather than executing a maintenance purge.
  - *Mitigation:* The purge was a one-time operation on 2026-09-16. No automated Cron jobs or deployment scripts depend on this flag. README and specs are updated accordingly.
