# Proposal

## Why

The SM-2 deck already unifies every recall source (reader highlights, quiz mistakes, daily drills) into one `SpacedRepetitionCards` table, but the only retention analytics exposed today are a status-based mastery gauge (`MasteryGaugeCard`) and a 7-day due-forecast (`ReviewForecastChart`). Users cannot see which cards are slipping (overdue or repeatedly failed) or which learning channel actually retains best, so the active-recall loop has no health signal to act on.

## What Changes

- Add a read-only SM-2 retention analytics aggregation endpoint `GET /api/v1/review/analytics` that derives, strictly from existing card fields (`Status`, `EaseFactor`, `RepetitionCount`, `NextReviewDate`, `SourceType`):
  - **At-risk segment**: overdue backlog (`NextReviewDate < today`) and leech cards (low `EaseFactor` approaching the `1.30` floor).
  - **Maturity distribution**: card counts by `CardStatus` (Learning / Reviewing / Mastered).
  - **Difficulty distribution**: `EaseFactor` histogram buckets across the `[1.30, 2.50]` range.
  - **Source-channel retention breakdown**: per `CardSourceType` (Highlight / QuizMistake / DocumentChunk) totals plus maturity split and average `EaseFactor`, unifying the three inflow sources into one comparison.
- Extend the `/review` Deck Management tab with retention analytics cards: an At-Risk & Leech panel and a Source-Channel Retention breakdown card, reusing the existing `MasteryGaugeCard` and `ReviewForecastChart` rather than duplicating them.
- Non-goals (explicitly out of scope for this change):
  - No review-history log and no true forgetting curve / historical retention rate — metrics are derived only from current SM-2 card state (no migration, no write-path change).
  - No reverse-sync from graded drill-sourced cards back to `DailyDrills`/`DrillStatus`.
  - No cross-source content deduplication between daily drills and book-grounded quiz questions.
  - No rebuild of the `/quiz` topic/seniority mastery dashboard or the `/profile` streak metrics.

## Capabilities

### New Capabilities

<!-- None: retention analytics already lives in the review capability. -->

### Modified Capabilities

- `review`: Add requirements for a read-only SM-2 retention analytics aggregation API and the `/review` Deck Management retention analytics panel (at-risk/leech segmentation and source-channel retention breakdown).

## Impact

- **Backend**: New `GetReviewAnalytics` use-case handler + response DTO (`TechDaily.Application/Features/Review/GetReviewAnalytics`), registered in DI; new `GET /api/v1/review/analytics` route in `ReviewEndpoints.cs` under `.RequireAuthorization()`. Read-only queries over `SpacedRepetitionCards`; no domain, entity, or migration changes.
- **Frontend**: New `components/review/` cards (at-risk/leech panel, source-channel retention breakdown) wired into the Deck Management tab of `pages/review.vue`; review store gains an analytics fetch action; `en`/`vi` i18n keys added. Existing `MasteryGaugeCard` and `ReviewForecastChart` reused unchanged.
- **No changes** to the grade/write path, quiz/drill progress models, or database schema.
