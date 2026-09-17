# Tasks: Dynamic Insights and Highlight Card Sync

## 1. Filter Bar Pure-Text Standardization & i18n Localization

- [x] 1.1 Remove the `<Bookmark>` component icon import and usage from the saved filter chip in `frontend/pages/insights.vue`, standardizing the filter row on pure-text styling.
- [x] 1.2 Remove the `🔖` emoji prefix from `insights.saved_tab` in `frontend/i18n/locales/vi.json` (`"saved_tab": "Đã Lưu"`) and `frontend/i18n/locales/en.json` (`"saved_tab": "Saved"`).
- [x] 1.3 Add the `notes.in_sm2` key in `frontend/i18n/locales/vi.json` (`"in_sm2": "Đã Trong SM-2"`) and `frontend/i18n/locales/en.json` (`"in_sm2": "In SM-2"`), verifying bilingual parity with `grep`.

## 2. Dynamic Categories & AI Topic Suggestions Backend

- [x] 2.1 Define `InsightCategoryMetaDto`, `GetInsightsMetaRequest`, and `GetInsightsMetaResponse` records in `backend/src/TechDaily.Application/Features/Insights/DTOs/InsightDtos.cs`.
- [x] 2.2 Implement `GetInsightsMetaHandler` in `backend/src/TechDaily.Application/Features/Insights/GetInsightsMeta/GetInsightsMetaHandler.cs` querying published `TechInsights` counts and curated `Topics` from PostgreSQL with fallback defaults.
- [x] 2.3 Map route `GET /api/v1/insights/meta` in `backend/src/TechDaily.Api/Endpoints/InsightsEndpoints.cs` and register `GetInsightsMetaHandler` in `backend/src/TechDaily.Application/DependencyInjection.cs`.

## 3. Dynamic Categories & AI Topic Suggestions Frontend

- [x] 3.1 Update `frontend/stores/useInsightsStore.ts` to add `CategoryMeta` interface, `categoryMetadata` and `suggestedTopics` state refs, and the `fetchMetadata()` action.
- [x] 3.2 Refactor `frontend/pages/insights.vue` to remove the hardcoded `categories` array and `suggestedTopicPool` object, dynamically computing filter chips and AI inspiration chips from `useInsightsStore`.
- [x] 3.3 Ensure `frontend/pages/insights.vue` invokes `insightsStore.fetchMetadata()` on mount to hydrate dynamic categories and suggestions.

## 4. Persistent Flashcard SM-2 State on Notes (F5 Bug Fix)

- [x] 4.1 Add `public bool HasFlashcard { get; set; }` to `backend/src/TechDaily.Application/Features/Notes/DTOs/HighlightDto.cs`.
- [x] 4.2 Update `backend/src/TechDaily.Application/Features/Notes/GetHighlights/GetHighlightsHandler.cs` to batch-query `SpacedRepetitionCards` for the current user's `SourceHighlightId`s and map `HasFlashcard = cardHighlightIds.Contains(h.Id)`.
- [x] 4.3 Add `hasFlashcard?: boolean` to the `Highlight` interface in `frontend/stores/useNotesStore.ts`.
- [x] 4.4 In `frontend/pages/notes.vue`, synchronize `createdCardHighlightIds` with all highlight IDs having `hasFlashcard: true` during `fetchHighlights` / `onMounted`.
- [x] 4.5 Update the flashcard button in `frontend/pages/notes.vue` to render `<Check class="w-3.5 h-3.5 text-emerald-500" />` with `$t('notes.in_sm2')` and disabled styling when `createdCardHighlightIds.has(item.id)`.

## 5. Verification & Testing

- [x] 5.1 Implement unit tests in `backend/tests/TechDaily.Tests/Application/GetHighlightsHandlerTests.cs` verifying `HasFlashcard` evaluates to `false` without a card, flips to `true` when a linked `SpacedRepetitionCard` exists, and enforces user isolation.
- [x] 5.2 Implement Vitest unit test in `frontend/tests/pages/notes.spec.ts` asserting that a highlight loaded with `hasFlashcard: true` renders in the disabled `In SM-2` state with `<Check />` icon upon mount/reload.
- [x] 5.3 Update `frontend/tests/stores/insights.spec.ts` to verify that `useInsightsStore.fetchMetadata()` populates dynamic categories and suggested topics.
- [x] 5.4 Run `dotnet test` and `npm run test` on modified test suites to verify zero regressions.
