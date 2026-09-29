# Design

## Context

See proposal.md (Why). The SM-2 deck (`SpacedRepetitionCards`) already unifies three recall sources via `CardSourceType` (`Highlight`, `QuizMistake`, `DocumentChunk`) and stores per-card SM-2 state: `Status` (`Learning`/`Reviewing`/`Mastered`), `EaseFactor` (bounded `[1.30, 2.50]`, default `2.50`), `RepetitionCount` (reset to `0` on a lapse), `IntervalDays`, `NextReviewDate`, `LastReviewDate`. There is no per-review history table and grading (`GradeReviewCardHandler`) only mutates current card state. Existing analytics are `GET /api/v1/review/cards` returning `DeckStatisticsDto` (status counts) consumed by `MasteryGaugeCard`, plus `ReviewForecastChart` (7-day due forecast).

## Goals / Non-Goals

**Goals:**
- Derive retention health entirely from current card state, so the change is read-only and needs no migration.
- Keep the paginated deck query untouched; add analytics as a separate concern.
- Surface a cross-source comparison so the user sees which recall channel retains best.

**Non-Goals (design-level):**
- No new `SpacedRepetitionCard` fields, no `CardReviewLog`, no changes to `ApplyReview`/`GradeReviewCardHandler` (confirmed with the user: read-only, derive from existing fields).
- No new filter parameters on `GET /api/v1/review/cards`.

## Decisions

### 1. Dedicated `GET /api/v1/review/analytics` endpoint, not an extension of `/review/cards`
The cards endpoint is a paginated list with lightweight per-status counts. Retention analytics needs deck-wide grouped aggregations (per-source maturity split, ease-factor buckets) that are independent of pagination. A dedicated read-only endpoint keeps each query single-purpose and avoids recomputing heavy aggregates on every page fetch. Mirrors the existing one-handler-per-use-case Pure DI pattern (`GetReviewAnalyticsHandler` + response DTO, registered in DI).
- Alternative: fold aggregates into `DeckStatisticsDto`. Rejected — couples list pagination with deck-wide aggregation and inflates every page request.

### 2. `EaseFactor` as the struggle/leech proxy; threshold `1.70`
With no lapse history, `EaseFactor` is the only cumulative struggle signal: it starts at `2.50` and only drops below default after `grade < 3` lapses. `<= 1.70` marks cards that have failed multiple times (well below default, drifting toward the `1.30` floor). `RepetitionCount` is unusable as a struggle signal because it resets to `0` on every lapse.
- Alternative: derive leech from `RepetitionCount`. Rejected — resets on lapse, so a chronically failing card can read `RepetitionCount = 0` identical to a brand-new card.
- Trade-off: the threshold is a heuristic, not a measured retention rate (see Risks).

### 3. At-risk = union of overdue and leech, deduplicated
`overdueCount` = `NextReviewDate < today`; `leechCount` = `EaseFactor <= 1.70`; `atRiskCount` = count of the union so a card that is both is counted once. These are three independent `CountAsync` predicates plus one union count (or a single scan projecting the two booleans), matching the existing multi-count style in `GetReviewCardsHandler`.

### 4. Single lightweight projection, aggregated in-memory (provider-portable)
The handler fetches one minimal projection (`Status`, `EaseFactor`, `NextReviewDate`, `SourceType`) of the user's active cards (filtered by `UserId && !IsDeleted`, same base query as `GetReviewCardsHandler`) and computes every aggregate in C#: at-risk/maturity/difficulty counts and a `GroupBy(SourceType)` with per-status counts and `decimal.Round(Average(EaseFactor), 2)`. This keeps the computation identical across SQLite (unit tests) and Npgsql (production), where server-side `AVG`/`SUM` over `decimal` translate inconsistently. Personal SM-2 decks are per-user and small, so the single-column-set read is cheap.
- Alternative: server-side grouped aggregation (`GroupBy(_ => 1)` + `Count(predicate)` + SQL `Average`). Rejected — decimal aggregate translation differs by provider, and the count-only parts are not worth splitting from the average into multiple round trips.

### 5. At-risk navigation reuses existing overdue-first ordering
`GET /api/v1/review/cards` already orders by `NextReviewDate` ascending, and `ReviewCardDto` already exposes `EaseFactor` and `NextReviewDate`. The at-risk card's action switches to the Deck Management list (already overdue-first) and the frontend flags at-risk rows from those DTO fields. This satisfies "overdue at-risk cards surface first" with zero backend filter change.
- Alternative: add an `atRisk=true` filter to `/review/cards` for exact cross-page isolation. Deferred — the ordering-based surfacing meets the requirement without modifying the existing querying contract; revisit only if UX needs strict isolation across large paginated decks.

### 6. Frontend placement: extend the existing Deck Management tab
Two new `components/review/` cards (at-risk/leech, source-channel retention) render in the Deck Management tab next to the reused `MasteryGaugeCard` and `ReviewForecastChart`. The review Pinia store gains one analytics fetch action. `en`/`vi` i18n keys added; action controls use `whitespace-nowrap shrink-0` per the bilingual layout invariant.

## Risks / Trade-offs

- [No true retention rate / forgetting curve] → Accepted per user decision; `EaseFactor`-based leech detection is a proxy, not a measured recall-success rate. If a real retention rate is later required, it needs a review-history log (separate change).
- [Leech threshold `1.70` is a heuristic] → Define it as a single named constant so it can be tuned without touching query logic; document it in the spec so the value is an explicit contract.
- [Analytics reads a per-card projection into memory] → The projection is limited to four columns over an already user+soft-delete filtered set (one query), and personal SM-2 decks are small, so cost is bounded. If decks grow large, the counts can move server-side (integer `Count(predicate)` translates on both providers) while keeping the average in-memory.
- [At-risk surfacing is ordering-based, not a hard filter] → For very large decks non-at-risk cards still appear after overdue ones; mitigated by the explicit at-risk counts on the analytics card and deferred `atRisk` filter option in Decision 5.
