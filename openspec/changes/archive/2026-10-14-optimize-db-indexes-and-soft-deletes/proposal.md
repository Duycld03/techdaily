# Proposal: Optimize DB Indexes & Enforce Soft-Delete Invariants

## Why
Database inspection using `postgres-patterns` and `dotnet-patterns` identified critical database efficiency and data-integrity issues in the DeepPace PostgreSQL storage layer:
1. Unique constraints across soft-deletable entities (`User`, `DocumentBook`, `UserBookPacer`, `DailyDrill`, `TechInsight`, `UserInsightBookmark`, `UserQuizProgress`, `UserPushSubscription`) lack `WHERE "IsDeleted" = false` partial index filters, directly violating the `AGENTS.md` Rule 36 core invariant. This causes hard constraint violations when a user re-registers with an email previously soft-deleted or re-imports a deleted book slug.
2. The core reading slice query (`GetBookSliceHandler`) executes on every page turn (`WHERE "DocumentBookId" = @bookId AND "ChunkOrder" = @chunkOrder`), but `DocumentChunk` lacks a composite index on `(DocumentBookId, ChunkOrder)`, causing sequential chunk scanning during active reader navigation.
3. Read-only queries in `GetTodayFocusHandler` omit `.AsNoTracking()`, consuming unnecessary EF Core change-tracker memory.

## What Changes
- Enforce partial unique indexes with `WHERE "IsDeleted" = false` on all soft-deletable entities having unique constraints in `EntityConfigurations.cs`:
  - `User.Email`
  - `DocumentBook.Slug`
  - `UserBookPacer.(UserId, DocumentBookId)`
  - `DailyDrill.(UserId, ScheduledDate, QuestionId)`
  - `TechInsight.Slug`
  - `UserInsightBookmark.(UserId, InsightId)`
  - `UserQuizProgress.(UserId, QuestionId)`
  - `UserPushSubscription.(UserId, Endpoint)`
- Add composite unique index on `DocumentChunk.(DocumentBookId, ChunkOrder)` with `WHERE "IsDeleted" = false` to guarantee sub-millisecond $O(1)$ slice seek during reading.
- Generate and verify a new EF Core migration (`AddSoftDeletePartialIndexesAndSliceLookup`) updating index definitions in PostgreSQL.
- Add `.AsNoTracking()` to read-only queries in `GetTodayFocusHandler.cs`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Updates relational database invariants so that all soft-deletable entities with unique business keys enforce partial unique indexes (`WHERE "IsDeleted" = false`).
- `library`: Specifies that `DocumentChunk` must maintain a composite index on `(DocumentBookId, ChunkOrder)` for $O(1)$ slice seek performance during reading.

## Impact
- **Database**: Updates PostgreSQL indexes via EF Core migration. Safe schema change with backward compatibility.
- **Backend**: Eliminates allocation/tracking overhead in read-only focus queries and makes slice lookup instantaneous.
- **Breaking Changes**: None. Existing soft-deleted data will no longer collide with new active records.
