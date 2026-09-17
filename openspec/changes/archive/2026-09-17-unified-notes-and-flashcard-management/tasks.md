# Tasks: Unified Notes and Flashcard Management

## 1. Backend Domain & Application

- [x] 1.1 Add domain methods `UpdateContent(string frontMarkdown, string backMarkdown)` and `ResetProgression(DateOnly? resetDate)` to `backend/src/TechDaily.Domain/Entities/SpacedRepetitionCard.cs`.
- [x] 1.2 Add domain method `Update(string? note, List<string>? tags)` to `backend/src/TechDaily.Domain/Entities/UserHighlight.cs`.
- [x] 1.3 Create `UpdateHighlightRequest`, `UpdateHighlightResponse`, `UpdateHighlightValidator`, and `UpdateHighlightHandler` in `backend/src/TechDaily.Application/Features/Notes/UpdateHighlight/`.
- [x] 1.4 Create `GetReviewCardsRequest`, `GetReviewCardsResponse`, `DeckStatisticsDto`, and `GetReviewCardsHandler` in `backend/src/TechDaily.Application/Features/Review/GetReviewCards/` supporting pagination, search, status, and source type filtering.
- [x] 1.5 Create `UpdateReviewCardRequest`, `UpdateReviewCardResponse`, `UpdateReviewCardValidator`, and `UpdateReviewCardHandler` in `backend/src/TechDaily.Application/Features/Review/UpdateReviewCard/`.
- [x] 1.6 Create `DeleteReviewCardRequest`, `DeleteReviewCardResponse`, and `DeleteReviewCardHandler` in `backend/src/TechDaily.Application/Features/Review/DeleteReviewCard/` performing soft-delete via `card.SoftDelete()`.
- [x] 1.7 Create `ResetReviewCardProgressRequest`, `ResetReviewCardProgressResponse`, and `ResetReviewCardProgressHandler` in `backend/src/TechDaily.Application/Features/Review/ResetReviewCardProgress/`.
- [x] 1.8 Register new endpoint routes in `backend/src/TechDaily.Api/Endpoints/NotesEndpoints.cs` (`PUT /api/v1/notes/highlights/{id:guid}`) and `backend/src/TechDaily.Api/Endpoints/ReviewEndpoints.cs` (`GET /cards`, `PUT /cards/{id:guid}`, `DELETE /cards/{id:guid}`, `POST /cards/{id:guid}/reset`).
- [x] 1.9 Write unit tests in `backend/tests/TechDaily.Tests/Application/` covering `UpdateHighlightHandler`, `GetReviewCardsHandler`, `UpdateReviewCardHandler`, `DeleteReviewCardHandler`, and `ResetReviewCardProgressHandler`.

## 2. Frontend Reader & Notes

- [x] 2.1 Refactor reader selection floating toolbar in `frontend/pages/read/[bookId].vue` to display exactly 3 buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`.
- [x] 2.2 Remove the direct flashcard creation button (`⚡ Flashcard`) and its click handler (`handleCreateFlashcardFromSelection`) from `frontend/pages/read/[bookId].vue`.
- [x] 2.3 Unify highlight and note interactions in `frontend/pages/read/[bookId].vue`: clicking `Highlight/Note` creates the highlight immediately via `notesStore.createHighlight()` and smoothly opens the attached reflection popover.
- [x] 2.4 Add `updateHighlight(id, { note, tags })` action to `frontend/stores/useNotesStore.ts` calling `PUT /api/v1/notes/highlights/{id}`.
- [x] 2.5 Remove redundant "Saved Insights" tab, bookmark unpinning modal, and `useInsightsStore` dependency from `frontend/pages/notes.vue`.
- [x] 2.6 Implement inline reflection note and tag editor on highlight cards in `frontend/pages/notes.vue` with toggleable edit mode, textarea, tag editor, and Save/Cancel actions.
- [x] 2.7 Verify deliberate "Flashcard SM-2" creation button remains functional on saved highlight cards in `frontend/pages/notes.vue`.

## 3. Frontend Review Deck Management

- [x] 3.1 Expand `frontend/stores/useReviewStore.ts` with deck management state (`deckCards`, `deckStatistics`, `deckTotalCount`, `deckCurrentPage`, `isDeckLoading`) and actions (`fetchDeckCards`, `updateCard`, `deleteCard`, `resetCardProgress`).
- [x] 3.2 Add dual-mode tab switcher in `frontend/pages/review.vue`: Tab 1 ("Review Session" / "Ôn tập hôm nay") and Tab 2 ("Deck Management" / "Kho thẻ của tôi").
- [x] 3.3 Implement Deck Management statistics overview cards in `frontend/pages/review.vue` displaying Total Cards, Learning, Reviewing, and Mastered counts.
- [x] 3.4 Implement search bar with debounce and filter chips for `CardStatus` (`All`, `Learning`, `Reviewing`, `Mastered`) and `CardSourceType` (`All`, `Topic`, `Highlight`, `QuizMistake`).
- [x] 3.5 Build responsive card list / table in `frontend/pages/review.vue` with front/back markdown formatting, SM-2 metric badges, pagination controls, and action buttons.
- [x] 3.6 Implement Flashcard Edit Modal with live Markdown preview (split or tabbed view), input validation, and save handling.
- [x] 3.7 Implement Reset SM-2 Progression confirmation dialog explaining that the card restarts at interval 1 day and becomes due today.
- [x] 3.8 Implement Delete Flashcard confirmation modal confirming soft-deletion from the user's active deck.

## 4. Localization & Verification

- [x] 4.1 Update `frontend/i18n/locales/en.json` with all new reader, notes, and review deck management translation keys.
- [x] 4.2 Update `frontend/i18n/locales/vi.json` with natural Vietnamese translations conforming strictly to the invariant of no hardcoded English in parentheses.
- [x] 4.3 Verify data independence invariant: deleting a highlight does not delete or corrupt its associated flashcard (`onDelete: ReferentialAction.SetNull`).
- [x] 4.4 Run backend unit tests (`dotnet test`) and frontend store tests (`bun test`) to ensure zero regressions.
- [x] 4.5 Validate OpenSpec change specification via `openspec validate unified-notes-and-flashcard-management`.
