# Tasks: Today Quiz State Deserialization, Highlight/Card Deduplication, and Explainer Resilience

## Phase 1: Today Page Quiz Status & Natural Layout Flow
- [x] 1.1 Support dual-type drill status checks in `frontend/components/today/InterviewChallengePane.vue`:
  - Update `isReviewed = computed(() => props.drill?.status === 2 || props.drill?.status === 'Reviewed' || props.drill?.status === 'reviewed')`.
  - Ensure option selection styling and submit button visibility react correctly to both integer and string enum values.
- [x] 1.2 Update header completed badge in `frontend/pages/today.vue`:
  - Update `v-if="focusStore.data?.drill?.status === 2 || focusStore.data?.drill?.status === 'Reviewed' || focusStore.data?.drill?.status === 'reviewed'"`.
  - Ensure the green completed badge persists across hard page refreshes when a drill has been submitted.
- [x] 1.3 Normalize `drill.status` in `frontend/stores/useDailyFocusStore.ts`:
  - In `fetchTodayData()`, inspect `res.drill.status` and normalize string enum values (`"Reviewed" -> 2`, `"Submitted" -> 1`, `"Pending" -> 0`).
  - Ensure `submitOption()` maintains consistent status representations in local store state.
- [x] 1.4 Refactor layout container in `frontend/components/today/InterviewChallengePane.vue`:
  - Replace `space-y-5 sm:space-y-6 flex-1 flex flex-col justify-between` with `space-y-6 flex-1 flex flex-col justify-start`.
  - Eliminate the 300–400px empty vertical gap between the options list and submit/explanation controls on desktop screens.
- [x] 1.5 Manual UX sanity check:
  - Complete a scenario drill on `/today`, refresh the page, and verify the reviewed state, selected option, feedback banner, score, and explanation remain intact.

## Phase 2: Highlight & Flashcard Deduplication
- [x] 2.1 Implement highlight deduplication in `backend/src/TechDaily.Application/Features/Notes/CreateHighlight/CreateHighlightHandler.cs`:
  - Query `UserHighlights` for existing match by `(UserId, DocumentChunkId, SelectedText.Trim())` before calling `AddAsync`.
  - If match exists, update `Note` (if new note provided) and merge `Tags` (if new tags provided).
  - Return the existing highlight's DTO with its stable `Id` without inserting duplicate rows.
- [x] 2.2 Verify card idempotency in `backend/src/TechDaily.Application/Features/Review/CreateCardFromHighlight/CreateCardFromHighlightHandler.cs`:
  - Confirm that querying by `SourceHighlightId == request.HighlightId` successfully detects previously created cards when `CreateHighlightHandler` returns the existing `HighlightId`.
- [x] 2.3 Update highlight and flashcard creation in `frontend/pages/read/[bookId].vue`:
  - In `handleCreateFlashcardFromSelection()`, trim selected text and utilize the returned highlight ID from `notesStore.createHighlight()`.
  - Prevent creating redundant local highlight cards in `notesStore.highlights`.
- [x] 2.4 Update highlight creation in `frontend/components/today/DocReaderPane.vue`:
  - In `handleHighlightSelection()`, ensure trimmed text is sent and handle deduplicated response smoothly.
- [x] 2.5 Update `frontend/stores/useNotesStore.ts`:
  - When `createHighlight()` returns a highlight with an ID already present in `highlights.value`, update the existing highlight in place rather than unshifting a duplicate.

## Phase 3: Term Explainer Length Clamping, Resilient Caching & i18n
- [x] 3.1 Implement safe term length clamping in `backend/src/TechDaily.Infrastructure/Services/TermExplanationService.cs`:
  - Truncate `normalizedTerm` to maximum 200 characters: `var safeTerm = normalizedTerm.Length > 200 ? normalizedTerm[..200] : normalizedTerm`.
  - Use `safeTerm` for exact cache queries, embedding generation, and `TermExplanationCache.Term` persistence, preventing Postgres `22001` character varying overflow.
- [x] 3.2 Implement non-blocking auxiliary caching in `TermExplanationService.cs`:
  - Wrap `_dbContext.TermExplanationCaches.AddAsync` and `SaveChangesAsync` inside an isolated `try-catch` block.
  - On database exception, log a structured warning and continue returning the generated LLM explanation with `IsFromCache = false`.
- [x] 3.3 Update client error resolution in `frontend/composables/useApiError.ts`:
  - Detect HTTP 500 status codes (`statusCode === 500` or `status === 500`).
  - Map HTTP 500 to `fallbackKey && te(fallbackKey) ? t(fallbackKey) : t('api_errors.SERVER_ERROR')` rather than leaking raw English `responseData.detail`.
- [x] 3.4 Separate error state from content in `frontend/components/today/TermExplainerModal.vue`:
  - Add `const errorMessage = ref<string | null>(null)` alongside `explanation`.
  - In `loadExplanation()`, reset `errorMessage.value = null` on start; in `catch`, set `errorMessage.value = formatError(err, 'today.explain_error')`.
- [x] 3.5 Add dedicated error banner and retry action in `TermExplainerModal.vue`:
  - When `errorMessage` is non-null, render a styled error alert card (`bg-rose-50 dark:bg-rose-950/40`) with an `AlertCircle` icon, localized message, and a `Retry` button calling `loadExplanation()`.
  - Suppress rendering of the prose Markdown box during error state.
- [x] 3.6 Update modal footer in `TermExplainerModal.vue`:
  - Disable or hide the "Copy" action button when `errorMessage` is active or `explanation` is empty.

## Phase 4: Verification & Automated Tests
- [x] 4.1 Create backend unit tests in `backend/tests/TechDaily.Tests/Application/CreateHighlightHandlerTests.cs`:
  - Test highlight deduplication: returns existing highlight when same `(UserId, ChunkId, SelectedText)` submitted.
  - Test note and tags update on existing highlight.
- [x] 4.2 Create backend unit tests in `backend/tests/TechDaily.Tests/Application/CreateCardFromHighlightHandlerTests.cs`:
  - Test flashcard creation idempotency: repeated calls with same `HighlightId` return existing card without database duplicates.
- [x] 4.3 Create backend unit tests in `backend/tests/TechDaily.Tests/Infrastructure/TermExplanationServiceTests.cs`:
  - Test term truncation: strings > 200 characters are safely clamped without throwing exceptions.
  - Test cache write resilience: secondary database exception during cache persistence still returns valid LLM explanation.
- [x] 4.4 Create frontend unit tests in `frontend/tests/components/InterviewChallengePane.spec.ts`:
  - Verify component renders reviewed state when `drill.status` is string `"Reviewed"`.
  - Verify component renders reviewed state when `drill.status` is integer `2`.
  - Verify selected option index and feedback card remain rendered.
- [x] 4.5 Create frontend unit tests in `frontend/tests/composables/useApiError.spec.ts`:
  - Verify HTTP 500 ProblemDetails payload maps to `api_errors.SERVER_ERROR` in English and Vietnamese.
  - Verify HTTP 500 ProblemDetails payload maps to caller `fallbackKey` when provided.
- [x] 4.6 Create frontend unit tests in `frontend/tests/components/TermExplainerModal.spec.ts`:
  - Verify dedicated error banner with retry button renders on API failure.
  - Verify clicking retry calls `loadExplanation()`.
  - Verify copy button is disabled or hidden during error state.
