# Spec Delta

## ADDED Requirements

### Requirement: Read-Time Streak Decay Evaluation
When querying user focus statistics or user profile metrics, the system SHALL evaluate the learner's effective streak status based on elapsed calendar days since `LastActiveDate`. If the gap exceeds the consecutive day window and remaining freeze credits cannot cover the absence, the system SHALL report an effective active streak of `0` in read-time responses without mutating database persistence prior to drill submission.

#### Scenario: User queries focus stats after multi-day absence exceeding freeze credits
- **GIVEN** a user with stored `CurrentStreak = 2`, `FreezeCreditsRemaining = 1`, and `LastActiveDate = 2026-09-30`
- **WHEN** the user calls `GET /api/v1/daily/today` or `GET /api/v1/user/profile` on 2026-10-06 (6 days later)
- **THEN** the response reports `CurrentStreak = 0`
- **AND** the best record `LongestStreak = 4` is preserved.

#### Scenario: User queries focus stats on same day as completed activity
- **GIVEN** a user who completed a drill on 2026-10-06 with resulting `CurrentStreak = 1`
- **WHEN** the user queries focus stats on 2026-10-06
- **THEN** the response reports `CurrentStreak = 1`.

#### Scenario: User queries focus stats next day with consecutive continuity
- **GIVEN** a user with `LastActiveDate` equal to yesterday and `CurrentStreak = 3`
- **WHEN** the user queries focus stats today prior to completing today's drill
- **THEN** the response reports `CurrentStreak = 3` as the streak remains active within the daily grace window.

---

### Requirement: Daily Drill Materialization and Lifecycle Isolation
When an active pacer advances or a daily focus slice is served for a specific calendar date, the system SHALL isolate new daily sessions from historical submissions. Newly advanced slices with unattempted challenges SHALL be served in `Pending` state with correct answers masked. When a user explicitly navigates to review a previously completed slice, the system SHALL display that slice's historical evaluation without resetting today's active drill.

#### Scenario: New daily slice receives fresh pending drill
- **GIVEN** an active pacer advances from Slice 1 to Slice 2 on 2026-10-06
- **WHEN** the system resolves the scenario challenge for Slice 2
- **THEN** the drill is served with `Status = "Pending"`
- **AND** `SelectedOptionIndex` is null
- **AND** correct answer index and architectural explanation are masked.

#### Scenario: Learner reviews previously completed slice without resetting today's drill
- **GIVEN** today's active pacer is on Slice 2 with a pending drill
- **WHEN** the user explicitly navigates to review completed Slice 1 (`?chunkOrder=1`)
- **THEN** Slice 1 displays its reviewed state and explanation
- **AND** navigating back to Slice 2 restores today's pending challenge.
