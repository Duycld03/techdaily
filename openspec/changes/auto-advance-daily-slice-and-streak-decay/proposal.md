# Proposal

## Why

In TechDaily, learners following the Daily Focus studio (`/today`) currently remain stuck on the same document slice across calendar days, even after successfully answering the scenario drill for that slice. Because the system does not auto-advance the user's pacer on new days, returning users are repeatedly presented with previously completed slices and already-graded drills from days or weeks earlier. Furthermore, streak counters in the header and profile display stale values indefinitely because streak calculation and decay only execute during drill submissions rather than being evaluated on read.

## What Changes

- **Calendar Day Pacer Auto-Advancement**: When an authenticated user opens `/today` in daily focus mode (without specifying an explicit `chunkOrder` query parameter):
  - On the same calendar day (`LastReadDate == today`), the learner remains on the current slice (`CurrentChunkOrder`) with their completed drill and explanation preserved for review.
  - On a subsequent calendar day (`LastReadDate < today`), if the previous slice's scenario drill was completed (`Status == Reviewed`), the pacer automatically advances to the next slice (`CurrentChunkOrder + 1`, clamped to `TotalChunks`) and updates `LastReadDate = today`.
  - If the previous slice's drill was incomplete or skipped, the pacer remains on that slice so the learner can finish it before progressing.
  - Manual navigation (clicking `< Prev / Next >` or selecting slices from the table of contents) remains an optional, non-destructive reading feature that allows learners to explore arbitrary slices at will.
- **Daily Drill Scoping & Isolation**: Ensure `DailyDrill` lookups and materialization for today's focus create a fresh `Pending` drill for new daily slices rather than retrieving stale historical drill responses across slices.
- **Read-Time Streak Evaluation & Decay**: In `GetTodayFocusHandler` and user profile endpoints, calculate the effective active streak on read. If the learner has been inactive beyond the consecutive day window and remaining freeze credits cannot cover the absence (`today.DayNumber - LastActiveDate.DayNumber > 1 + FreezeCreditsRemaining`), the displayed active streak evaluates to `0` instead of projecting a stale positive streak from past weeks.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `today`: Add requirement for calendar-day pacer auto-advancement when previous daily slice is completed, while preserving current day slice stability and optional manual navigation.
- `drills`: Add requirement for read-time streak decay calculation and proper daily drill lifecycle isolation on new calendar days.

## Impact

- **Backend**:
  - `GetTodayFocusHandler.cs`: Auto-advance `activePacer.CurrentChunkOrder` when crossing into a new day if the prior slice drill is completed; evaluate active streak taking into account elapsed days and freeze credits.
  - `StreakRecord.cs`: Add read-time helper method (e.g., `GetEffectiveStreak(DateOnly today)`) to compute live streak status without requiring write mutations.
  - `UserEndpoints.cs`: Reflect effective streak in profile stats.
- **Frontend**:
  - Display accurate live streak badge and automatically receive the fresh slice upon daily visit.
- **Database**:
  - Existing schema in `UserBookPacers`, `DailyDrills`, and `StreakRecords` already has all required columns (`CurrentChunkOrder`, `LastReadDate`, `LastActiveDate`, `FreezeCreditsRemaining`). No database migration is required.
