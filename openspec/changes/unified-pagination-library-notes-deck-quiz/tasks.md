# Tasks: Unified Pagination for Library, Notes, Deck & Quiz

## 1. Backend Core Pagination & Use-Case Refactoring (.NET 10)

- [x] 1.1 Refactor `GetBooksRequest` and `GetBooksResponse` in `TechDaily.Application.Features.Library.GetBooks` to accept `Page = 1` and `PageSize = 12`, and return `TotalCount`, `Page`, `PageSize`, and `TotalPages`, verifying with `dotnet build backend/TechDaily.sln`.
- [x] 1.2 Refactor `GetBooksHandler` to execute `.CountAsync()` followed by `.Skip((page - 1) * pageSize).Take(pageSize)` with `AsNoTracking()`, verifying that unit tests pass in `TechDaily.Tests`.
- [x] 1.3 Refactor `GetHighlightsRequest`, `TagCountDto`, and `GetHighlightsResponse` in `TechDaily.Application.Features.Notes.GetHighlights` to accept `Page = 1`, `PageSize = 15`, and return `TotalCount`, `Page`, `PageSize`, `TotalPages`, and global `TagCounts`, verifying with `dotnet build backend/TechDaily.sln`.
- [x] 1.4 Refactor `GetHighlightsHandler` to aggregate global tag counts across all user highlights and slice the requested page of highlights with `AsNoTracking()`, verifying with updated unit tests in `GetHighlightsHandlerTests.cs`.
- [x] 1.5 Update `LibraryEndpoints.cs` and `NotesEndpoints.cs` to bind `[FromQuery] int page = 1` and `[FromQuery] int pageSize = ...` to use-case requests, verifying with endpoint compilation and swagger schema inspection.
- [x] 1.6 Verify and ensure `GetReviewCardsResponse` and `GetQuizReviewQueueResponse` correctly expose `TotalPages` calculation, verifying existing review and quiz tests pass via `dotnet test backend/TechDaily.sln`.

## 2. Frontend Reusable Pagination Component & Localization

- [x] 2.1 Add comprehensive pagination, notes streaming, and quiz batch practice localization strings to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, verifying that all keys are symmetric and valid JSON.
- [x] 2.2 Create `frontend/components/common/BasePagination.vue` with props (`currentPage`, `totalPages`, `totalCount`, `pageSize`), events (`update:currentPage`, `change`), smart window calculation with ellipses, and WAI-ARIA accessibility attributes (`role="navigation"`, `aria-current="page"`), verifying component mounting and pagination events in Vitest.
- [x] 2.3 Implement mobile-responsive styling in `BasePagination.vue` ensuring touch targets satisfy $\ge 40\text{px} \times 40\text{px}$ on viewports $< 640\text{px}$, verifying visually across desktop and mobile views.

## 3. Technical Library (`/library`) Paginated Integration

- [x] 3.1 Update `useLibraryStore` (`frontend/stores/useLibraryStore.ts`) to manage `currentPage`, `pageSize = 12`, `totalCount`, and `totalPages`, and update `fetchBooks` to accept pagination parameters, verifying store tests in `frontend/tests/stores/library.spec.ts`.
- [x] 3.2 Integrate `BasePagination.vue` into `frontend/pages/library.vue` below the book cards grid, verifying pagination controls appear when `totalPages > 1`.
- [x] 3.3 Implement two-way URL query synchronization in `frontend/pages/library.vue` for `?page=N&category=C&search=S` using `useRoute()` and `useRouter().replace()`, verifying that changing filters resets page to 1 and refreshing preserves active page.

## 4. Reading Notes & Highlights (`/notes`) Paginated & Streaming Integration

- [x] 4.1 Update `useNotesStore` (`frontend/stores/useNotesStore.ts`) to manage `currentPage`, `pageSize = 15`, `totalCount`, `totalPages`, and global `tagCounts`, and update `fetchHighlights` with `append: boolean` option for stream loading, verifying store tests in `frontend/tests/stores/notes.spec.ts`.
- [x] 4.2 Integrate `BasePagination.vue` and "Load More Notes" action button into `frontend/pages/notes.vue`, connecting global tag counts to the horizontal scrollable tag chip bar, verifying tag counts reflect the entire collection.
- [x] 4.3 Implement two-way URL query synchronization in `frontend/pages/notes.vue` for `?page=N&tag=T&search=S`, verifying that inline reflection editing and the green "In SM-2" check badge persist across paginated batches.

## 5. Flashcard Deck Management (`/review`) Complete Pagination Integration

- [x] 5.1 Align `useReviewStore` (`frontend/stores/useReviewStore.ts`) pagination state (`deckCurrentPage`, `deckPageSize`, `deckTotalCount`, `deckTotalPages`) and verify `fetchDeckCards` returns complete pagination metadata.
- [x] 5.2 Replace bare Previous/Next buttons in `frontend/pages/review.vue` with `BasePagination.vue`, adding smooth scroll-to-top on page change, verifying numbered buttons and ellipsis windowing for large decks.
- [x] 5.3 Implement two-way URL query synchronization in `frontend/pages/review.vue` for `?page=N&tab=deck&search=S&status=X&sourceType=Y`, verifying that browser back/forward buttons correctly restore the previous deck page.

## 6. Quiz Mistake Review Queue (`/quiz`) Interactive Pagination Integration

- [x] 6.1 Update `useInterviewQuizStore` (`frontend/stores/useInterviewQuizStore.ts`) to manage `reviewPage`, `reviewPageSize = 10`, `reviewTotalCount`, and `reviewTotalPages`, verifying store tests in `frontend/tests/stores/quiz.spec.ts`.
- [x] 6.2 Integrate `BasePagination.vue` into the Review Queue tab in `frontend/pages/quiz.vue` beneath the mistake question cards, verifying numbered pagination appears when `reviewTotalPages > 1`.
- [x] 6.3 Implement dual-mode review session triggers in `frontend/pages/quiz.vue`: "Practice Current Batch (N)" for active page questions and "Practice All Mistakes (Total N)" for full queue eager practice, verifying arena session transitions.
- [x] 6.4 Implement two-way URL query synchronization in `frontend/pages/quiz.vue` for `?tab=review&page=N`, verifying deep linking directly to mistake review queue pages.

## 7. Verification, End-to-End Testing & Validation

- [x] 7.1 Execute full backend unit and integration test suite via `dotnet test backend/TechDaily.sln` to confirm 100% pass rate with zero regression on library, notes, review, and quiz handlers.
- [x] 7.2 Execute frontend test suite via `npm run test` (or `npx vitest run`) to confirm store and pagination component assertions pass.
- [x] 7.3 Run `openspec validate --strict unified-pagination-library-notes-deck-quiz` to verify complete OpenSpec compliance with zero errors.
