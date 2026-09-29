# Tasks

## 1. Application: Retention Analytics Use-Case

- [x] 1.1 Add `GetReviewAnalyticsRequest`/`GetReviewAnalyticsResponse` DTOs under `TechDaily.Application/Features/Review/GetReviewAnalytics` covering the at-risk segment (`overdueCount`, `leechCount`, `atRiskCount`), maturity distribution (`totalCards`, `learning`, `reviewing`, `mastered`), difficulty buckets (`struggling`, `developing`, `comfortable`), and a source-channel breakdown list (`sourceType`, `total`, `learning`, `reviewing`, `mastered`, `averageEaseFactor`). Verify: `dotnet build` succeeds with the new types.
- [x] 1.2 Implement `GetReviewAnalyticsHandler` (Pure DI, one class) querying `SpacedRepetitionCards` filtered by `UserId && !IsDeleted`: overdue = `NextReviewDate < today`, leech = `EaseFactor <= 1.70` (named `LeechEaseFactorThreshold` constant), `atRiskCount` = deduplicated union; status counts; ease-factor buckets over `[1.30, 2.50]`; per-source counts and `Average(EaseFactor)` rounded to 2 decimals. Register the handler in DI. Verify: unit test over a seeded card set asserts overdue/leech counts, union dedup for a card that is both, bucket boundary placement (`1.70` and `2.10`), and per-source totals plus average ease.
- [x] 1.3 Add a unit test for the empty-deck case asserting all counts are zero and the source-channel breakdown is empty without throwing. Verify: the test passes.

## 2. Api: Analytics Endpoint

- [x] 2.1 Add `GET /api/v1/review/analytics` to `ReviewEndpoints.cs` under the group's `.RequireAuthorization()`, resolving `UserId` from claims and delegating to `GetReviewAnalyticsHandler`; declare `.Produces<GetReviewAnalyticsResponse>(200)` and `.ProducesProblem(401)`. Verify: an authenticated request returns `200` with populated aggregates and a request without a JWT returns `401` (exercise both against the running backend).

## 3. Frontend: Deck Management Retention Panel

- [x] 3.1 Add a typed analytics fetch action to the review Pinia store (no `any`; model the response shape) that calls `GET /api/v1/review/analytics` and stores the parsed payload. Verify: a Vitest test asserts the action requests the analytics endpoint and exposes the parsed aggregates in store state.
- [x] 3.2 Create `AtRiskLeechCard.vue` and `SourceChannelRetentionCard.vue` under `components/review/`, and wire them into the Deck Management tab of `pages/review.vue` beside the reused `MasteryGaugeCard` and `ReviewForecastChart` (no duplicate instances). The at-risk card's action switches to the Deck Management card list (already ordered overdue-first). Verify: a Vitest test asserts activating the at-risk action performs the deck-list/tab-switch state change (behavior, not CSS classes).
- [x] 3.3 Add `en` and `vi` i18n keys for every new title, metric label, and action control; apply `whitespace-nowrap shrink-0` on action controls. Verify: a Vitest/locale test asserts the new keys resolve in both `en` and `vi` (no raw key strings rendered).
- [x] 3.4 Visual gate: drive headless Chromium to `/review` Deck Management with an authenticated session and capture Desktop (1440x900) and Mobile (390x844) screenshots in both `en` and `vi`. Verify: both new cards render with correct data and no text overflow/wrapping collisions; present the screenshots.

## 4. Integration Verification

- [x] 4.1 Run the full backend (`dotnet test`) and frontend (`npm test`) suites — Gate 1. Verify: all suites pass 100%.
- [x] 4.2 Exercise the end-to-end flow on the running stack: seed graded cards across highlight/quiz/drill sources, open `/review` Deck Management, and confirm the analytics endpoint response matches what the At-Risk & Source-Channel cards display — Gate 2. Verify: presented screenshots and the endpoint payload agree.
