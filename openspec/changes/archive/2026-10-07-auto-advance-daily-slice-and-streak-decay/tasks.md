# Tasks

## 1. Domain Layer

- [x] 1.1 Add pure read-time `CalculateEffectiveStreak(DateOnly today)` method to `StreakRecord` in `backend/src/TechDaily.Domain/Entities/StreakRecord.cs` accounting for elapsed days and `FreezeCreditsRemaining`.
- [x] 1.2 Add unit tests for `StreakRecord.CalculateEffectiveStreak` covering same-day, consecutive-day, freeze-protected, and lapsed streak scenarios in `backend/tests/TechDaily.Tests/Domain/StreakRecordTests.cs`.

## 2. Application Layer

- [x] 2.1 Implement calendar day pacer auto-advancement logic in `GetTodayFocusHandler.cs` when `request.ChunkOrder` is null, comparing `today > activePacer.LastReadDate` and verifying the prior slice's drill is reviewed (`Status == DrillStatus.Reviewed`).
- [x] 2.2 Isolate daily drill query in `GetTodayFocusHandler.cs` to filter active daily drill by `ScheduledDate == today`, creating a fresh `Pending` drill for newly advanced slices while supporting historical inspection on manual slice navigation.
- [x] 2.3 Use `streak.CalculateEffectiveStreak(today)` when mapping `CurrentStreak` in `GetTodayFocusHandler.cs` response.
- [x] 2.4 Add application unit tests in `backend/tests/TechDaily.Tests/Features/DailyFocus/GetTodayFocusHandlerTests.cs` verifying auto-advancement on new day, preservation within same day, incomplete drill retention, and effective streak decay.

## 3. Api Layer

- [x] 3.1 Update `UserEndpoints.cs` profile endpoint to map effective streak via `user.StreakRecord.CalculateEffectiveStreak(DateOnly.FromDateTime(DateTime.UtcNow))` in `ProfileStatsDto`.
- [x] 3.2 Verify API endpoints compile cleanly and respond with accurate effective streak and advanced slice data.

## 4. Frontend Layer

- [x] 4.1 Verify `today.vue` and `InterviewChallengePane.vue` receive fresh `Pending` drill when pacer advances on new day and display reviewed state on completed slices.
- [x] 4.2 Verify `AppHeader.vue` and `StreakBadge.vue` render accurate active streak (or 0 when lapsed).

## 5. Verification & Regression Testing

- [x] 5.1 Run all backend unit and integration tests (`dotnet test`) to verify 100% test pass rate across domain and application layers.
- [x] 5.2 Run frontend Vitest test suites (`npm test`) to verify data contracts, stores, and layout components.
- [x] 5.3 Conduct end-to-end smoke verification of `/today` endpoint with automated browser validation.
