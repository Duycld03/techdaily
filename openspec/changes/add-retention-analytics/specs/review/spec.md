# Spec Delta

## ADDED Requirements

### Requirement: SM-2 Retention Analytics Aggregation API

The system SHALL expose an authenticated, read-only endpoint `GET /api/v1/review/analytics` that returns retention health metrics for the requesting user's active (non-deleted) `SpacedRepetitionCards`, derived entirely from current card state (`Status`, `EaseFactor`, `RepetitionCount`, `NextReviewDate`, `SourceType`) without persisting or mutating any data. The response SHALL contain:

1. **At-Risk Segment**:
   - `overdueCount`: cards whose `NextReviewDate` is strictly before the current UTC date (past due, not yet reviewed).
   - `leechCount`: cards whose `EaseFactor` is at or below the low-ease threshold `1.70` (repeatedly failed, drifting toward the `1.30` floor).
   - `atRiskCount`: the count of the union of overdue and leech cards (each card counted once).
2. **Maturity Distribution**: card counts by `CardStatus` (`learning`, `reviewing`, `mastered`) and `totalCards`.
3. **Difficulty Distribution**: `EaseFactor` bucket counts across the bounded `[1.30, 2.50]` range — `struggling` (`[1.30, 1.70]`), `developing` (`(1.70, 2.10)`), and `comfortable` (`[2.10, 2.50]`).
4. **Source-Channel Retention Breakdown**: one entry per `CardSourceType` present in the deck (`Highlight`, `QuizMistake`, `DocumentChunk`), each reporting `total`, the maturity split (`learning`, `reviewing`, `mastered`), and `averageEaseFactor` rounded to two decimals.

The endpoint SHALL scope every aggregate to the authenticated user and SHALL exclude soft-deleted cards.

#### Scenario: Unauthenticated request to analytics endpoint
- **WHEN** a client sends `GET /api/v1/review/analytics` without a valid JWT
- **THEN** the system responds with `HTTP 401 Unauthorized` and no analytics data.

#### Scenario: Authenticated user retrieves retention analytics
- **WHEN** an authenticated user sends `GET /api/v1/review/analytics`
- **THEN** the system returns `HTTP 200 OK` with the at-risk segment, maturity distribution, difficulty distribution, and source-channel breakdown scoped to that user's active cards.

#### Scenario: Overdue and leech cards counted in the at-risk segment
- **GIVEN** a user whose deck contains cards with `NextReviewDate` before today and cards with `EaseFactor <= 1.70`
- **WHEN** the analytics are computed
- **THEN** `overdueCount` reflects cards past their due date, `leechCount` reflects cards at or below the `1.70` ease threshold, and `atRiskCount` equals the size of their union without double-counting a card that is both overdue and a leech.

#### Scenario: Source-channel breakdown unifies the three recall sources
- **GIVEN** a deck containing cards sourced from reader highlights, quiz mistakes, and daily drills
- **WHEN** the analytics are computed
- **THEN** the response includes one breakdown entry per present `CardSourceType` with its `total`, maturity split, and `averageEaseFactor`
- **AND** a source type absent from the deck is omitted or reported with zeroed counts.

#### Scenario: Empty deck returns zeroed aggregates
- **WHEN** an authenticated user with no spaced-repetition cards requests analytics
- **THEN** the system returns `HTTP 200 OK` with all counts equal to zero and an empty source-channel breakdown, without raising an error.

### Requirement: Deck Management Retention Analytics Panel

The `/review` Deck Management tab SHALL surface the retention analytics from `GET /api/v1/review/analytics` alongside the existing `MasteryGaugeCard` and `ReviewForecastChart`, without duplicating those components elsewhere. The panel SHALL present:

1. **At-Risk & Leech Card**: displays `overdueCount`, `leechCount`, and `atRiskCount`, and provides a 1-click action that opens the Deck Management card list ordered by soonest due date so overdue and low-ease at-risk cards surface first, letting the user act on them immediately.
2. **Source-Channel Retention Card**: compares retention across the three recall sources (reader highlights, quiz mistakes, daily drills), showing per-source total, maturity split, and average ease factor so the user can see which channel retains best.

All labels, counts, and action controls SHALL be fully localized in English (`en`) and Vietnamese (`vi`), and action controls SHALL use `whitespace-nowrap shrink-0` to prevent text wrapping collisions across locales.

#### Scenario: User views retention analytics in Deck Management
- **GIVEN** an authenticated user with graded cards across multiple sources
- **WHEN** the user opens the Deck Management tab on `/review`
- **THEN** the At-Risk & Leech card and Source-Channel Retention card render with data from `GET /api/v1/review/analytics`
- **AND** the existing Mastery Gauge and Review Forecast remain the sole instances of those components.

#### Scenario: User jumps from the at-risk card to the deck list
- **GIVEN** the At-Risk & Leech card reports a non-zero `atRiskCount`
- **WHEN** the user activates its review action
- **THEN** the Deck Management card list opens ordered by soonest due date so overdue at-risk cards surface first, without a full page reload.

#### Scenario: Retention analytics render in both locales
- **WHEN** the user toggles the application locale between `en` and `vi` on the Deck Management tab
- **THEN** all retention card titles, metric labels, and action controls update reactively and render without truncation or layout overflow in either locale.
