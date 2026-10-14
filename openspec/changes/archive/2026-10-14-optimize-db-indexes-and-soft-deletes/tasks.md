# Tasks

## 1. Infrastructure Persistence & Entity Configurations

- [x] 1.1 Configure partial unique index `WHERE "IsDeleted" = false` for `User.Email` in `UserConfiguration` (`EntityConfigurations.cs`)
- [x] 1.2 Configure partial unique index `WHERE "IsDeleted" = false` for `DocumentBook.Slug` in `DocumentBookConfiguration` (`EntityConfigurations.cs`)
- [x] 1.3 Configure composite unique index on `(DocumentBookId, ChunkOrder)` with `WHERE "IsDeleted" = false` in `DocumentChunkConfiguration` (`EntityConfigurations.cs`)
- [x] 1.4 Configure partial unique index for `UserBookPacer.(UserId, DocumentBookId)` in `UserBookPacerConfiguration` (`EntityConfigurations.cs`)
- [x] 1.5 Configure partial unique index for `DailyDrill.(UserId, ScheduledDate, QuestionId)` in `DailyDrillConfiguration` (`EntityConfigurations.cs`)
- [x] 1.6 Configure partial unique index for `TechInsight.Slug` and `UserInsightBookmark.(UserId, InsightId)` in `TechInsightConfiguration` and `UserInsightBookmarkConfiguration` (`EntityConfigurations.cs`)
- [x] 1.7 Configure partial unique index for `UserQuizProgress.(UserId, QuestionId)` and `UserPushSubscription.(UserId, Endpoint)` in `UserQuizProgressConfiguration` and `UserPushSubscriptionConfiguration` (`EntityConfigurations.cs`)

## 2. Migrations & Database Schema Updates

- [x] 2.1 Generate EF Core migration `AddSoftDeletePartialIndexesAndSliceLookup` targeting `DeepPace.Infrastructure`
- [x] 2.2 Verify migration drops old unconditional unique indexes and establishes partial unique indexes matching the global soft-delete query filter

## 3. Application Use-Case Query Optimization

- [x] 3.1 Add `.AsNoTracking()` to `_dbContext.UserBookPacers` read-only query in `GetTodayFocusHandler.cs`
- [x] 3.2 Verify `GetBookSliceHandler` query alignment with the new composite slice index

## 4. Verification & Testing

- [x] 4.1 Write regression tests proving a soft-deleted user email and soft-deleted book slug permit re-registration and re-import
- [x] 4.2 Run the full backend test suite (`dotnet test`) to verify 100% green tests and zero regressions
