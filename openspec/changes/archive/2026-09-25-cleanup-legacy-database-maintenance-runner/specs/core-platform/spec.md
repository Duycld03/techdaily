# Spec Delta: core-platform

## REMOVED Requirements

### Requirement: Database Maintenance CLI & Tooling Contract
The platform application `TechDaily.Api` SHALL provide command-line arguments for headless operational maintenance:
- `--cleanup-data`: Activates maintenance mode.
- `--dry-run`: Analyzes and reports tainted records within an aborted transaction.
- `--execute`: Executes atomic data purge within a committed transaction.
- `--backfill-embeddings`: Iteratively backfills unvectorized document chunks using `IEmbeddingService`.
- `--batch-size=<N>`: Controls the batch size for vectorization (default 25, clamped to 5–50).
- `--reseed-catalog`: Seeds curated technical insights from `tech-insights.json`.

#### Scenario: Headless execution in container environment
- **WHEN** the maintenance CLI is executed inside a container via `dotnet TechDaily.Api.dll --cleanup-data --execute --backfill-embeddings --reseed-catalog`
- **THEN** the application executes the purge, re-seeds curated items, completes the vector backfill, and exits with code `0`.

#### Scenario: Maintenance CLI dry-run reports non-zero tainted records
- **WHEN** the maintenance CLI is executed with `--cleanup-data --dry-run` against a database containing legacy fallback records
- **THEN** the CLI outputs the exact count of tainted records per table and exits with code `0` without altering database state.
