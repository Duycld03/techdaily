# Design

## Context

TechDaily organizes document reading and interview challenges around a daily pacer (`UserBookPacer`), which tracks `CurrentChunkOrder`, `DailyPaceChunks`, `LastReadDate`, and `CompletedAt`. Users complete a multiple-choice scenario challenge (`DailyDrill`) sourced from the day's active slice (`DocumentChunk`).

Currently:
1. `GetTodayFocusHandler.cs` accepts an optional `ChunkOrder` from the query string. When omitted (default `/today` visit), it falls back to `activePacer.CurrentChunkOrder`. However, it unconditionally stamps `activePacer.LastReadDate = today` without checking whether a calendar day transition occurred (`today > LastReadDate`) or whether the previous day's drill was completed.
2. `DailyDrill` queries in `GetTodayFocusHandler.cs` match only on `UserId` and `QuestionId` without checking `ScheduledDate == today`. Revisiting a slice returns historical drill submissions from previous weeks.
3. `StreakRecord` updates `CurrentStreak` exclusively on write in `RecordCompletion(DateOnly today, int? drillScore)`. No read-time decay calculation exists, meaning a user who has not submitted a drill for several days still sees their previous positive streak number until their next submission.

See `proposal.md` for motivation and `specs/today/spec.md` & `specs/drills/spec.md` for requirements.

## Goals / Non-Goals

**Goals:**
- Automatically advance `activePacer.CurrentChunkOrder` by 1 on subsequent calendar days when the prior slice's drill has been completed (`Status == Reviewed`).
- Keep the user on the current slice within the same calendar day (`today == LastReadDate`) or when the previous slice's drill is incomplete.
- Prevent historical completed drills from masking unattempted daily drills for newly advanced slices.
- Compute effective streak decay at read time across all focus and profile queries.
- Retain existing optional manual slice navigation (`?chunkOrder=X`) for non-destructive reading.

**Non-Goals:**
- Creating a scheduled background cron worker to mutate database rows for inactive users (evaluated on read instead).
- Altering the SM-2 algorithm parameters or card review scheduling logic.
- Auto-advancing slices immediately upon drill submission (users must remain on their daily slice during the day of completion).

## Decisions

### Decision 1: Pure Handler Logic for Pacer Calendar Day Transition

When an authenticated user invokes `GET /api/v1/daily/today` without an explicit `chunkOrder`:
1. Compare `today` with `activePacer.LastReadDate`.
2. If `activePacer.LastReadDate.HasValue && today > activePacer.LastReadDate.Value`:
   - Inspect whether the drill for `activePacer.CurrentChunkOrder` has been completed (`Status == DrillStatus.Reviewed`).
   - If completed:
     - Check boundary: if `activePacer.CurrentChunkOrder < targetBook.TotalChunks`, increment `activePacer.CurrentChunkOrder += 1`.
     - If `activePacer.CurrentChunkOrder >= targetBook.TotalChunks`, set `activePacer.CompletedAt = DateTimeOffset.UtcNow`.
     - Update `activePacer.LastReadDate = today`.
   - If incomplete:
     - Keep `activePacer.CurrentChunkOrder` unchanged so the user can complete their study.
3. If `activePacer.LastReadDate == today`:
   - Maintain `activePacer.CurrentChunkOrder` without modification.

*Alternative Considered*: Auto-advancing immediately in `SubmitDailyDrillHandler`. Rejected because learners want to review the source text, listen to audio narration, and study explanations on the day of completion without being abruptly pushed to the next chapter.

### Decision 2: Read-Time Effective Streak Calculation in Domain Entity

Add a pure calculation method on `StreakRecord`:
```csharp
public int CalculateEffectiveStreak(DateOnly today)
{
    if (LastActiveDate == null || CurrentStreak == 0)
        return 0;

    var dayDifference = today.DayNumber - LastActiveDate.Value.DayNumber;

    if (dayDifference <= 1)
        return CurrentStreak;

    if (dayDifference == 2 && FreezeCreditsRemaining > 0)
        return CurrentStreak;

    return 0;
}
```

Both `GetTodayFocusHandler.cs` and `UserEndpoints.cs` will invoke `streak.CalculateEffectiveStreak(today)` when mapping `CurrentStreak` in DTO responses (`GetTodayFocusResponse` and `UserProfileResponse`).

*Alternative Considered*: Running a nightly background job to reset `CurrentStreak = 0` in the database. Rejected because read-time evaluation is zero-maintenance, immune to server downtime or timezone drift, and requires no background worker polling overhead.

### Decision 3: Daily Drill Query Isolation

In `GetTodayFocusHandler.cs`:
1. Distinguish between **today's active daily focus** (`request.ChunkOrder == null`) and **manual slice browsing** (`request.ChunkOrder != null`).
2. For today's active daily focus:
   - Query for today's drill matching `UserId == userId && QuestionId == question.Id && ScheduledDate == today`.
   - If not found:
     - If the user previously completed this question on an earlier date, instantiate a fresh daily drill session for `today` or mark completed if reviewing. For a freshly advanced slice, create a new `DailyDrill` with `Status = DrillStatus.Pending` and `ScheduledDate = today`.
3. For manual slice browsing:
   - Lookup any existing drill for `UserId == userId && QuestionId == question.Id` to display previous answers and explanations as educational reference.

## Risks / Trade-offs

- **[Risk: User visits across midnight in local timezone]** → *Mitigation*: Client passes local date/timezone in focus requests; server respects `request.TargetDate` if provided or defaults to UTC `DateOnly.FromDateTime(DateTime.UtcNow)`.
- **[Risk: Multiple tab refreshes on day transition]** → *Mitigation*: Once `activePacer.LastReadDate = today` is persisted on the first read of the day, subsequent requests on that day see `today == LastReadDate` and do not double-advance.
- **[Risk: Single-slice books or final chunk edge case]** → *Mitigation*: Advance logic clamps strictly to `targetBook.TotalChunks` and marks `CompletedAt`.

## Migration Plan

- Pure application and domain logic change.
- Existing database tables (`UserBookPacers`, `DailyDrills`, `StreakRecords`) have all necessary fields. Zero schema migrations required.
- Can be safely deployed via rolling update.
