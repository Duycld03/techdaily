# Tasks: Fix Term Explainer i18n, Layout, and Context

- [x] 1. Frontend TermExplainerModal Layout & i18n
  - [x] 1.1 Add translation keys to `frontend/i18n/locales/en.json` under `reader`: `instant_cache`, `term_explainer_loading`, `term_explainer_powered_by`, `term_explainer_copy`, and `term_explainer_copied`.
  - [x] 1.2 Add corresponding Vietnamese translation keys to `frontend/i18n/locales/vi.json` under `reader`.
  - [x] 1.3 Refactor header layout in `frontend/components/today/TermExplainerModal.vue`:
    - Add `min-w-0 flex-1` container boundaries to title and category wrapper.
    - Apply `truncate max-w-[180px] sm:max-w-xs` to category title element.
    - Apply `whitespace-nowrap shrink-0` to the `⚡ Instant Cache` badge to eliminate two-line wrapping.
    - Add `shrink-0` to modal dismiss button.
  - [x] 1.4 Replace all hardcoded English strings in `TermExplainerModal.vue` with dynamic `$t(...)` bindings.

- [x] 2. Frontend Reader Surrounding Context Extraction
  - [x] 2.1 Implement `extractSurroundingContext(selection, maxChars)` helper function in `frontend/pages/read/[bookId].vue` (or utility composable) to traverse the DOM tree from the active selection range to the enclosing block node (`p`, `blockquote`, `li`, `pre`, `div`).
  - [x] 2.2 Extract up to 500 characters of surrounding text centered on the selection, collapsing redundant whitespace.
  - [x] 2.3 Update `handleExplainSelection` in `frontend/pages/read/[bookId].vue` to assign extracted context to `currentContext`, gracefully falling back to `currentChunk.value?.chapterTitle || ""` if DOM text is unavailable.
  - [x] 2.4 Verify `TermExplainerModal` receives rich `currentContext` and forwards it via `focusStore.explainTerm()`.

- [x] 3. Backend Cache Hygiene & Poisoned Cache Purge
  - [x] 3.1 Refactor `ExplainTermAsync` in `backend/src/TechDaily.Infrastructure/Services/TermExplanationService.cs`:
    - Add a persistence gate flag (`isLlmGenerated = true` only upon valid LLM HTTP 200 response with candidate text).
    - Ensure `GetFallbackExplanation` returns fallback text directly with `isFromCache = false` without writing any record to `_dbContext.TermExplanationCaches`.
    - Restrict vector generation and `TermExplanationCaches.AddAsync` strictly to verified LLM outputs.
  - [x] 3.2 Create a database migration or startup cleanup command to purge poisoned legacy cache entries from PostgreSQL `TermExplanationCaches` (`ExplanationText` matching fallback placeholder signatures).
  - [x] 3.3 Add diagnostic logging for LLM failures indicating transient fallback was returned without database caching.

- [x] 4. Automated Verification & Testing
  - [x] 4.1 Add or update unit tests in `frontend/tests/components/TermExplainerModal.spec.ts` verifying responsive layout classes, badge wrapping resistance, and i18n localized text output in both English and Vietnamese.
  - [x] 4.2 Add unit tests for `extractSurroundingContext` covering paragraph selection, multiline block selection, and missing selection fallback.
  - [x] 4.3 Add or update backend unit tests in `TechDaily.Tests` for `TermExplanationService`:
    - Verify successful Gemini responses persist to `TermExplanationCaches` with embeddings.
    - Verify failed Gemini responses return fallback text and DO NOT write records to `TermExplanationCaches`.
  - [x] 4.4 Run backend test suite (`dotnet test`) and frontend test suite (`npm test`) to confirm all tests pass cleanly.
