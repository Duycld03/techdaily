# Spec Delta

## ADDED Requirements

### Requirement: Daily Pacer Automatic Slice Progression on Calendar Day Change
When accessing the Daily Focus studio without an explicit `chunkOrder` parameter on a subsequent calendar day (`today > LastReadDate`), the system SHALL increment the active pacer's slice order by one (`CurrentChunkOrder + 1`, bounded by `TotalChunks`) and update `LastReadDate` to today if the current slice's drill is completed. Within the same calendar day or when the prior drill is incomplete, the system SHALL preserve the current slice.

#### Scenario: User opens today studio on subsequent day after completing prior slice
- **GIVEN** an authenticated user whose active pacer is on Slice 1 of a book with 75 slices
- **AND** the scenario drill for Slice 1 was completed on 2026-09-28 (`Status == Reviewed`)
- **AND** the user opens `/today` on a subsequent date 2026-10-06 without a `chunkOrder` parameter
- **WHEN** the system resolves today's focus
- **THEN** the active pacer's `CurrentChunkOrder` advances to Slice 2
- **AND** `LastReadDate` updates to 2026-10-06
- **AND** the response provides reading content for Slice 2 and a fresh unattempted `Pending` scenario challenge drill.

#### Scenario: User visits today studio multiple times on same calendar day
- **GIVEN** an authenticated user completed the daily scenario challenge drill for Slice 2 on 2026-10-06
- **WHEN** the user reloads `/today` or revisits the page later on the same date (2026-10-06)
- **THEN** the system keeps the learner on Slice 2
- **AND** the scenario challenge pane displays the completed evaluation state (`Reviewed`), optimal answer highlighting, and architectural breakdown.

#### Scenario: User visits today studio on subsequent day with incomplete prior drill
- **GIVEN** an authenticated user's active pacer is on Slice 2 with an unattempted or pending drill
- **WHEN** the user opens `/today` on the following calendar day without completing the drill
- **THEN** the active pacer remains on Slice 2
- **AND** the system presents Slice 2 and its pending drill so the learner can complete the required study before advancing.

#### Scenario: User manually navigates slices via table of contents or pagination
- **GIVEN** an active pacer positioned at Slice 2 for today's study
- **WHEN** the user clicks pagination controls or table of contents to view Slice 5 (`?chunkOrder=5`)
- **THEN** the system serves the reading content for Slice 5
- **AND** returning to default daily focus without `chunkOrder` preserves the pacer's authentic daily progress position.

#### Scenario: User completes final slice of book
- **GIVEN** an active pacer positioned at the final slice of a book (`CurrentChunkOrder == TotalChunks`)
- **WHEN** the user completes the final slice's scenario challenge drill
- **THEN** the pacer marks `CompletedAt` with the current timestamp
- **AND** subsequent daily visits remain anchored at `TotalChunks` without out-of-bounds index errors.
