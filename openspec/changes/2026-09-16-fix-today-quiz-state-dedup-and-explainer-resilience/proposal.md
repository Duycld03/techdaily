# Proposal: Fix Today Quiz State Deserialization, Eliminate Highlight/Card Duplication, and Harden AI Explainer Resilience

## Why

TechDaily provides software engineers with an integrated daily learning cycle: curated technical documentation slices on `/today`, senior scenario interview challenges, in-context AI term explanations, and SuperMemo SM-2 spaced repetition flashcards generated from reading highlights.

A recent live architectural and user journey audit surfaced three critical defects spanning state synchronization, data integrity, database constraints, and localized error presentation:

### 1. Today Quiz Refresh State Defect & Unnatural Layout Gap
- **State Deserialization Defect:**
  In the backend (`Program.cs:42`), the global JSON options register `System.Text.Json.Serialization.JsonStringEnumConverter()`. As a result, the backend serializes `DrillStatus.Reviewed` as the string `"Reviewed"`.
  However, both `InterviewChallengePane.vue:56` and `today.vue:352` perform strict integer equality checks:
  ```typescript
  const isReviewed = computed(() => props.drill?.status === 2);
  ```
  While `useDailyFocusStore.ts:195` locally assigns `drill.status = 2` upon immediate submission within the active session, a hard reload or page refresh fetches the persisted record from `GET /api/v1/daily/today`. The deserialized payload contains `status: "Reviewed"`. The comparison `"Reviewed" === 2` strictly evaluates to `false`.
  Consequently, upon page refresh:
  1. The quiz UI silently reverts to the unsubmitted state.
  2. The completed badge on the `/today` header disappears.
  3. `selectedOptionIndex` is restored from the database, leaving the user's previously selected option visibly highlighted, but displaying the "Submit Solution" action button instead of the post-submission architectural explanation and score banner.
- **Unnatural Layout Gap:**
  In `InterviewChallengePane.vue:175`, the question and options container uses:
  ```html
  <div class="space-y-5 sm:space-y-6 flex-1 flex flex-col justify-between">
  ```
  Because of `justify-between` combined with a tall parent flex container, the multiple-choice options are pinned to the top, while the submit button or deep-dive explanation card is forced to the bottom edge. On desktop screens, this introduces a jarring, empty 300–400px chasm between the options list and the submission controls.

### 2. Highlight, Flashcard & Note Duplication
- **Unconditional Database Insert:**
  In `CreateHighlightHandler.cs:54`, the handler performs an unconditional `AddAsync` and `SaveChangesAsync` without verifying whether a highlight for `(UserId, DocumentChunkId, SelectedText)` already exists in PostgreSQL.
- **Card Idempotency Defeat:**
  When a user highlights text in the reader (`read/[bookId].vue:568-573` or `DocReaderPane.vue`) and triggers "Turn into Flashcard" / "Flashcard SM-2", the frontend creates a new highlight record before requesting card synthesis:
  ```typescript
  const highlight = await notesStore.createHighlight({ ... });
  await reviewStore.createCardFromHighlight(highlight.id, localeVal);
  ```
  Because `CreateHighlightHandler` unconditionally generates a new `Guid` `HighlightId` every single time, `CreateCardFromHighlightHandler.cs:53-54` checks:
  ```csharp
  var existingCard = await _dbContext.SpacedRepetitionCards
      .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceHighlightId == request.HighlightId, cancellationToken);
  ```
  Since `request.HighlightId` is brand new on each call, `existingCard` is never found. This completely defeats card idempotency, resulting in unbounded duplicate flashcards in the SM-2 review deck for the exact same text excerpt.
- **Note Fragmentation:**
  Personal reflection notes are stored directly on `UserHighlights.Note`. Unconditional inserts fragment engineer notes across multiple disconnected rows for the same paragraph.

### 3. Term Explainer Database Overflow, Unhandled 500, and Missing i18n
- **Character Length Mismatch & Postgres Exception 22001:**
  `ExplainTermValidator.cs:25` validates `RuleFor(x => x.Term).NotEmpty().MaximumLength(500)`. When an engineer highlights a multi-line technical sentence (such as 237 characters: *"Furthermore, authorization metadata allows middleware to inspect endpoint-specific requirements before routing..."*), validation passes and Google Gemini successfully generates a 200 OK architectural explanation.
  However, in PostgreSQL, `TermExplanationCaches.Term` is configured as `varchar(200)` via `EntityConfigurations.cs:307` (`builder.Property(t => t.Term).HasMaxLength(200)`).
  At line 186 of `TermExplanationService.cs`, `SaveChangesAsync()` crashes with `PostgresException 22001 (value too long for type character varying(200))`.
- **Secondary Cache Failure Destroys Primary User Value:**
  The cache write in `TermExplanationService.cs` lacks a `try-catch` boundary. A failure to write to the secondary caching layer propagates as an unhandled HTTP 500 Internal Server Error, throwing away the successful, costly Gemini LLM explanation and leaving the user with a broken modal.
- **Unlocalized Error String Pass-Through:**
  In `useApiError.ts:68-71`, the composable intercepts RFC 7807 problem details:
  ```typescript
  if (typeof responseData.title === 'string' && typeof responseData.detail === 'string') {
    return `${responseData.title}: ${responseData.detail}`
  }
  ```
  When the backend throws an unhandled 500 exception, ASP.NET Core returns `{ title: "Server Error", detail: "An unexpected error occurred...", status: 500 }`. `useApiError` passes this raw English string directly to the UI, completely ignoring active Vietnamese (`vi`) locale settings and the existing `api_errors.SERVER_ERROR` translation keys.
- **Broken Modal Error State:**
  In `TermExplainerModal.vue:53`, errors are caught and assigned directly to `explanation.value = formatError(err, 'today.explain_error')`. The raw error text is rendered inside the Markdown prose explanation box as if it were the AI's explanation. The modal lacks a dedicated error alert banner, lacks a "Retry" button, and still provides a "Copy" button that copies the error message.

---

## What Changes

We propose a cohesive, three-part architectural hardening across frontend reactivity, application services, and database persistence:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           SYSTEM ARCHITECTURE & DEFECT RESOLUTION                       │
│                                                                                         │
│  PART 1: Resilient Drill Status Serialization & Natural Layout                          │
│  [Backend: Program.cs] ──(JsonStringEnumConverter)──> Status: "Reviewed" (or 2)         │
│                                                              │                          │
│  [Frontend: InterviewChallengePane.vue & today.vue]          ▼                          │
│     ├── isReviewed = computed(() => status === 2 || status === 'Reviewed')              │
│     ├── Store normalization: Normalizes drill status on fetch & submit                  │
│     └── CSS: space-y-6 flex-col justify-start (Eliminates 400px empty gap)              │
│                                                                                         │
│  PART 2: Highlight & Card Deduplication Engine                                          │
│  [CreateHighlightHandler.cs]                                                            │
│     ├── Query: (UserId, DocumentChunkId, SelectedText.Trim())                           │
│     ├── Exists? ──> Update Note/Tags if supplied, return existing HighlightId           │
│     └── Not Exists? ──> Insert new UserHighlight                                        │
│  [CreateCardFromHighlightHandler.cs]                                                    │
│     └── Idempotency check: Reuse existing card by SourceHighlightId or matching text    │
│  [Frontend: [bookId].vue & DocReaderPane.vue]                                           │
│     └── Local store lookup & reuse of existing highlight before creating card           │
│                                                                                         │
│  PART 3: Hardened Term Explainer Resilience & i18n                                      │
│  [TermExplanationService.cs]                                                            │
│     ├── Clamp normalizedTerm to 200 chars: normalizedTerm[..200]                        │
│     └── Non-blocking DB cache write in try-catch (LLM result returned on cache error)   │
│  [useApiError.ts]                                                                       │
│     └── HTTP 500 status mapped to t('api_errors.SERVER_ERROR') or t(fallbackKey)        │
│  [TermExplainerModal.vue]                                                               │
│     ├── Separate errorMessage ref (Distinct rose alert banner with AlertCircle)         │
│     └── Interactive Retry button + Disabled copy button during error state              │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### Part 1: Resilient Drill Status Serialization & Natural Layout Flow
1. **Status Deserialization Flexibility:**
   - In `InterviewChallengePane.vue`, define `isReviewed` as:
     ```typescript
     const isReviewed = computed(
       () => props.drill?.status === 2 ||
             props.drill?.status === "Reviewed" ||
             props.drill?.status === "reviewed"
     );
     ```
   - In `today.vue:352`, update the completed badge condition:
     ```html
     v-if="focusStore.data?.drill?.status === 2 || focusStore.data?.drill?.status === 'Reviewed'"
     ```
   - In `useDailyFocusStore.ts`, ensure that when data is loaded from `GET /api/v1/daily/today`, `drill.status` is normalized and accurately reflected across state transitions.
2. **Natural Content Layout Flow:**
   - In `InterviewChallengePane.vue`, replace `flex-1 flex flex-col justify-between` with `space-y-6 flex-1 flex flex-col justify-start`.
   - Ensure the options list, sign-in prompt (if guest), submit button, and post-submission explanation card stack with natural vertical spacing without artificial stretching.

### Part 2: Highlight & Flashcard Deduplication Engine
1. **Idempotent Highlight Upsert:**
   - In `CreateHighlightHandler.cs`, before executing `AddAsync`, query `UserHighlights` for an existing record matching:
     `h.UserId == request.UserId && h.DocumentChunkId == request.DocumentChunkId && h.SelectedText == request.SelectedText.Trim()`
   - If an existing highlight is found:
     - Update `Note` if `request.Note` is non-empty.
     - Append or merge `Tags` if `request.Tags` are provided.
     - Persist changes and return the existing highlight's DTO (retaining its stable `Id`).
2. **Flashcard Creation Idempotency:**
   - In `CreateCardFromHighlightHandler.cs`, continue enforcing `SourceHighlightId == request.HighlightId`. Because `CreateHighlightHandler` returns a stable `HighlightId` for duplicate selections, the idempotency check succeeds, preventing duplicate flashcards.
   - In frontend reading interfaces (`[bookId].vue` and `DocReaderPane.vue`), check for an existing highlight in `useNotesStore` matching the active selection. If found, reuse it directly; otherwise, rely on the deduplicating endpoint response.

### Part 3: Resilient Term Explanation, Length Clamping & i18n
1. **Safe Database Term Length Clamping:**
   - In `TermExplanationService.cs`, enforce:
     ```csharp
     var normalizedTerm = term.Trim().ToLowerInvariant();
     var safeTerm = normalizedTerm.Length > 200 ? normalizedTerm[..200] : normalizedTerm;
     var safeCategory = category.Length > 200 ? category[..200] : category;
     ```
   - Use `safeTerm` for all `TermExplanationCaches` lookups, vector generation, and cache insertions, preventing Postgres `22001` character varying overflow.
2. **Non-Blocking Auxiliary Caching:**
   - Wrap `_dbContext.TermExplanationCaches.AddAsync` and `_dbContext.SaveChangesAsync` in an isolated `try-catch` block logging warnings on failure.
   - If caching fails for any reason (transient database error, concurrency lock, constraint violation), log the warning and return the primary `TermExplanationResult` generated by Gemini.
3. **Localized HTTP 500 Error Mapping in `useApiError.ts`:**
   - When the HTTP status is 500 or the error represents an unhandled internal server error, resolve to `t('api_errors.SERVER_ERROR')` or `t(fallbackKey)` rather than returning the raw English `responseData.detail`.
4. **Dedicated Explainer Error Banner & Retry Action:**
   - In `TermExplainerModal.vue`, separate `explanation` (Markdown content) from `errorMessage` (string).
   - If `errorMessage` is set, render a dedicated error banner with an error icon, localized error message, and a "Retry" button that calls `loadExplanation()`.
   - Disable or hide the "Copy" button when an error is present.

---

## Capabilities

### Modified Capabilities
- `quiz`:
  - Resilient deserialization of `DrillStatus` (handling string `"Reviewed"` and integer `2`).
  - Elimination of vertical 400px empty layout gap in scenario interview pane.
  - Flashcard creation idempotency for quiz mistakes and highlights.
- `reader`:
  - Reading highlight deduplication and note upserting by `(UserId, DocumentChunkId, SelectedText)`.
  - Elimination of duplicate SM-2 flashcard creation from identical text excerpts.
  - Term explanation term length clamping to 200 characters to prevent DB overflow.
  - Non-blocking cache persistence in `TermExplanationService`.
  - Dedicated error UI with Retry action in `TermExplainerModal.vue`.
- `core-platform`:
  - Localized client error resolution for HTTP 500 / ProblemDetails in `useApiError.ts`.
  - Prevention of raw English internal server error strings leaking to users.

---

## Impact

| Layer | Component | Problem Solved | Tangible User Impact |
|---|---|---|---|
| **Frontend / Today** | `InterviewChallengePane.vue` & `today.vue` | `"Reviewed"` vs `2` type mismatch upon refresh; vertical 400px gap. | Refreshing `/today` correctly displays reviewed state, score, and explanation; natural layout flow. |
| **Backend / Notes** | `CreateHighlightHandler.cs` | Unconditional highlight insert creating infinite duplicate highlights. | Identical selections reuse existing highlight, updating notes/tags without duplicate records. |
| **Backend / Review** | `CreateCardFromHighlightHandler.cs` | Cascade of duplicate flashcards for the same text selection. | Strict flashcard idempotency; SM-2 deck is clean and free of redundant cards. |
| **Backend / Explainer** | `TermExplanationService.cs` | Selection > 200 chars causing Postgres 22001 500 crash; cache failure destroying LLM response. | Safe truncation to 200 chars; non-blocking cache persistence guarantees users receive LLM explanations. |
| **Frontend / Error Handling** | `useApiError.ts` | Raw English ProblemDetails ("Server Error: An unexpected error occurred") shown to users. | HTTP 500 errors map to localized `api_errors.SERVER_ERROR` in EN and VI. |
| **Frontend / Reader Modal** | `TermExplainerModal.vue` | Raw error text rendered inside Markdown explanation box without retry. | Distinct error alert banner with interactive Retry button; clean separation of content and errors. |
