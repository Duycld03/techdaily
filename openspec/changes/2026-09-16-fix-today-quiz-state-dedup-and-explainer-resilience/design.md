# Technical Design: Today Quiz State Deserialization, Highlight/Card Deduplication, and Explainer Resilience

## Architecture Overview

This design addresses three production defects identified during the live platform audit:
1. **Today Quiz Refresh State Loss & Layout Stretching:** Resolving the string-vs-integer deserialization mismatch for `DrillStatus` and replacing unnatural flexbox vertical stretching with natural content flow.
2. **Highlight & Flashcard Redundancy:** Introducing an idempotent highlight upsert mechanism and eliminating runaway duplicate SM-2 flashcard creation.
3. **Term Explainer Character Overflow & i18n Failure:** Safeguarding database boundaries by clamping term lengths to 200 characters, introducing non-blocking auxiliary caching, mapping HTTP 500 errors to localized strings, and replacing inline error prose with a dedicated error banner and retry workflow.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                               COMPONENT ARCHITECTURE                                  │
│                                                                                        │
│  [Backend: Daily Drills]                                                               │
│     ├── DomainEnums.DrillStatus { Pending=0, Submitted=1, Reviewed=2, Skipped=3 }       │
│     └── JsonStringEnumConverter serializes status as "Reviewed"                        │
│                           │                                                            │
│                           ▼ (HTTP GET /api/v1/daily/today)                             │
│  [Frontend: Today Page]                                                                │
│     ├── useDailyFocusStore.ts: Normalizes drill.status on receipt                      │
│     ├── InterviewChallengePane.vue: isReviewed accepts 2 | "Reviewed" | "reviewed"     │
│     ├── today.vue: Header completed badge accepts 2 | "Reviewed"                       │
│     └── CSS Refactor: space-y-6 flex-col justify-start (natural layout)                │
│                                                                                        │
│  [Backend: Notes & Review]                                                             │
│     ├── CreateHighlightHandler.cs: Deduplicates by (UserId, ChunkId, SelectedText)     │
│     │     └── Updates Note & Tags on existing record; returns stable Id                │
│     ├── CreateCardFromHighlightHandler.cs: Idempotently returns existing card          │
│     └── Frontend ([bookId].vue, DocReaderPane.vue): Reuses highlight ID                │
│                                                                                        │
│  [Backend: AI Term Explainer]                                                          │
│     ├── TermExplanationService.cs:                                                     │
│     │     ├── normalizedTerm clamped to <= 200 chars: normalizedTerm[..200]            │
│     │     └── Isolated try-catch around TermExplanationCaches persistence             │
│     │                                                                                  │
│  [Frontend: Error Handling & Modal UI]                                                 │
│     ├── useApiError.ts: Maps HTTP 500 & unhandled errors to api_errors.SERVER_ERROR    │
│     └── TermExplainerModal.vue:                                                        │
│           ├── errorMessage ref separated from markdown explanation ref                 │
│           ├── Dedicated error banner with AlertCircle & Retry button                   │
│           └── Disabled Copy action during error state                                  │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Resilient Drill Status Serialization & Natural Layout Flow

### 1.1 Root Cause & Serialization Analysis
In `backend/src/TechDaily.Api/Program.cs:42`, the global JSON serialization configuration registers:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
```
When `DrillStatus.Reviewed` is serialized to JSON, it produces:
```json
{
  "drill": {
    "id": "c1f7b8e2-...",
    "status": "Reviewed",
    "selectedOptionIndex": 1,
    "isCorrect": true,
    "score": 100
  }
}
```
In `frontend/components/today/InterviewChallengePane.vue:56`:
```typescript
const isReviewed = computed(() => props.drill?.status === 2);
```
And in `frontend/pages/today.vue:352`:
```html
<span v-if="focusStore.data?.drill?.status === 2" ...>
```
When the user submits an answer during an active session, `useDailyFocusStore.ts:195` assigns:
```typescript
data.value.drill.status = 2 // Reviewed
```
In that immediate in-memory session, `status === 2` evaluates to `true`.
However, when the user refreshes the page, Nuxt executes `fetchTodayData()`:
1. `GET /api/v1/daily/today` returns `drill.status = "Reviewed"`.
2. `"Reviewed" === 2` evaluates to `false`.
3. `isReviewed` evaluates to `false`.
4. The completed badge disappears from the page header.
5. `selectedOption` is populated from `props.drill.selectedOptionIndex` (e.g. `1`), but the UI shows the unsubmitted state with an active "Submit Solution" button.

### 1.2 Technical Specification: Resilient Status Handling
We implement a dual-type resilience pattern ensuring that both string representations (`"Reviewed"`, `"reviewed"`, `"Submitted"`) and legacy/in-memory numeric representations (`2`, `1`) evaluate correctly:

1. **`InterviewChallengePane.vue` Status Evaluation:**
   ```typescript
   const isReviewed = computed(() => {
     const s = props.drill?.status;
     return s === 2 || s === "Reviewed" || s === "reviewed";
   });
   ```
2. **`today.vue` Header Completed Badge:**
   ```html
   <span
     v-if="focusStore.data?.drill?.status === 2 || focusStore.data?.drill?.status === 'Reviewed' || focusStore.data?.drill?.status === 'reviewed'"
     class="hidden md:inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-emerald-100 dark:bg-emerald-950 border border-emerald-300 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 text-xs font-bold"
   >
     <CheckCircle2 class="w-3.5 h-3.5" />
     <span>{{ $t("today.completed") }}</span>
   </span>
   ```
3. **`useDailyFocusStore.ts` Normalization:**
   In `fetchTodayData()`, normalize `res.drill.status`:
   ```typescript
   if (res.drill) {
     if (typeof res.drill.status === "string") {
       const statusStr = res.drill.status.toLowerCase();
       if (statusStr === "reviewed") res.drill.status = 2;
       else if (statusStr === "submitted") res.drill.status = 1;
       else if (statusStr === "skipped") res.drill.status = 3;
       else res.drill.status = 0;
     }
   }
   ```
   This ensures that regardless of whether downstream code relies on the integer value or string value, the store and components operate consistently.

### 1.3 Natural Vertical Content Flow in `InterviewChallengePane.vue`
Currently, line 175 of `InterviewChallengePane.vue` uses:
```html
<div class="space-y-5 sm:space-y-6 flex-1 flex flex-col justify-between">
```
When the parent column stretches (such as on desktop screens with a tall reading pane on the left), `justify-between` forces the options container to the top and the action bar/explanation to the bottom, leaving a 300–400px empty chasm in between.

**Remedy:**
Replace `justify-between` with `justify-start` and consistent spacing:
```html
<div class="space-y-5 sm:space-y-6 flex-1 flex flex-col justify-start">
```
Elements stack naturally in logical reading order:
1. Scenario Question & Description
2. Options List
3. Guest Sign-in Prompt Banner (if unauthenticated)
4. Submit Action Bar (if unreviewed) OR Post-Submission Feedback Banner & Architectural Deep-Dive Card (if reviewed)

---

## Part 2: Highlight & Card Deduplication Engine

### 2.1 Database & API Flow
Currently, `CreateHighlightHandler.cs` performs an unconditional insert:
```csharp
var highlight = new UserHighlight
{
    UserId = request.UserId,
    DocumentChunkId = request.DocumentChunkId,
    SelectedText = request.SelectedText,
    Note = request.Note,
    Tags = request.Tags ?? new()
};
await _dbContext.UserHighlights.AddAsync(highlight, cancellationToken);
await _dbContext.SaveChangesAsync(cancellationToken);
```
Every text selection produces a new `Guid` `HighlightId`.
When the frontend immediately invokes `reviewStore.createCardFromHighlight(highlight.id)`, `CreateCardFromHighlightHandler.cs` evaluates:
```csharp
var existingCard = await _dbContext.SpacedRepetitionCards
    .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceHighlightId == request.HighlightId, cancellationToken);
```
Because `request.HighlightId` is freshly generated on every click, `existingCard` is always `null`. Gemini is invoked again, generating another identical card in `SpacedRepetitionCards`.

### 2.2 Deduplication Engine Specification

#### Backend: `CreateHighlightHandler.cs`
Update `CreateHighlightHandler` to query for an existing highlight before creating a new entity:
```csharp
var normalizedText = request.SelectedText.Trim();

var existingHighlight = await _dbContext.UserHighlights
    .FirstOrDefaultAsync(h =>
        h.UserId == request.UserId &&
        h.DocumentChunkId == request.DocumentChunkId &&
        h.SelectedText == normalizedText,
        cancellationToken);

if (existingHighlight != null)
{
    bool updated = false;

    // Update note if new note provided
    if (!string.IsNullOrWhiteSpace(request.Note) && existingHighlight.Note != request.Note)
    {
        existingHighlight.Note = request.Note;
        updated = true;
    }

    // Merge or update tags if new tags provided
    if (request.Tags != null && request.Tags.Count > 0)
    {
        var mergedTags = existingHighlight.Tags
            .Union(request.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mergedTags.Count != existingHighlight.Tags.Count)
        {
            existingHighlight.Tags = mergedTags;
            updated = true;
        }
    }

    if (updated)
    {
        existingHighlight.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    return new CreateHighlightResponse
    {
        Highlight = new HighlightDto
        {
            Id = existingHighlight.Id,
            DocumentChunkId = existingHighlight.DocumentChunkId,
            SelectedText = existingHighlight.SelectedText,
            Note = existingHighlight.Note,
            Tags = existingHighlight.Tags,
            CreatedAt = existingHighlight.CreatedAt
        }
    };
}
```

#### Backend: `CreateCardFromHighlightHandler.cs`
In `CreateCardFromHighlightHandler`, retain the existing idempotency check:
```csharp
var existingCard = await _dbContext.SpacedRepetitionCards
    .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceHighlightId == request.HighlightId, cancellationToken);

if (existingCard != null)
{
    return new CreateCardFromHighlightResponse(
        existingCard.Id,
        existingCard.FrontMarkdown ?? string.Empty,
        existingCard.BackMarkdown ?? string.Empty);
}
```
Because `CreateHighlightHandler` returns the existing `HighlightId`, `request.HighlightId` is now stable across multiple clicks, guaranteeing that `existingCard != null` fires as designed.

#### Frontend: Reading Interfaces
In `frontend/pages/read/[bookId].vue` and `frontend/components/today/DocReaderPane.vue`:
1. When the user selects text, check if a highlight matching `(chunkId, selectedText.trim())` already exists in `notesStore.highlights`.
2. When creating a flashcard:
   ```typescript
   async function handleCreateFlashcardFromSelection() {
     if (!floatingToolbar.value.selectedText || !currentChunk.value?.id) return;
     isCreatingFlashcard.value = true;
     try {
       const highlight = await notesStore.createHighlight({
         documentChunkId: currentChunk.value.id,
         selectedText: floatingToolbar.value.selectedText.trim(),
       });
       const localeVal = (locale.value as string) || "en";
       await reviewStore.createCardFromHighlight(highlight.id, localeVal);
       toast.success(t("reader.toast_flashcard_success"));
     } catch (err: any) {
       toast.error(formatError(err, "reader.toast_flashcard_error"));
     } finally {
       isCreatingFlashcard.value = false;
       floatingToolbar.value.visible = false;
       isNotePopoverOpen.value = false;
     }
   }
   ```
3. In `useNotesStore.ts`:
   When `createHighlight` returns a highlight whose ID is already in `highlights.value`, update the existing entry in place rather than unshifting a duplicate.

---

## Part 3: Resilient Term Explanation, Length Clamping & i18n

### 3.1 PostgreSQL Constraint & Truncation Guard
In `TechDailyDbContext`:
- `TermExplanationCaches.Term` has `builder.Property(t => t.Term).HasMaxLength(200).IsRequired();`
- `ExplainTermValidator.cs` allows up to 500 characters: `RuleFor(x => x.Term).NotEmpty().MaximumLength(500);`

When an engineer selects a detailed excerpt, `term.Length` can exceed 200 characters. `TermExplanationService` attempts to persist `normalizedTerm` directly into `TermExplanationCaches`, causing PostgreSQL error `22001 (value too long for type character varying(200))`.

**Technical Specification:**
In `TermExplanationService.cs`:
```csharp
var normalizedTerm = term.Trim().ToLowerInvariant();
var safeTerm = normalizedTerm.Length > 200 ? normalizedTerm[..200] : normalizedTerm;
var safeCategory = category.Length > 200 ? category[..200] : category;
```
Use `safeTerm` consistently:
- Exact Cache Lookup:
  ```csharp
  var cached = await _dbContext.TermExplanationCaches
      .FirstOrDefaultAsync(t => t.Term == safeTerm && t.Locale == locale, cancellationToken);
  ```
- Semantic Vector Cache Embedding:
  ```csharp
  var embeddingResult = await _embeddingService.GenerateEmbeddingAsync($"[{safeCategory}] {safeTerm}", cancellationToken);
  ```
- Cache Entity Population:
  ```csharp
  var newCache = new TermExplanationCache
  {
      Term = safeTerm,
      Category = safeCategory,
      Locale = locale,
      ExplanationText = explanation,
      Embedding = termVector,
      HitCount = 1
  };
  ```

### 3.2 Non-Blocking Auxiliary Cache Write
The primary objective of `ExplainTermAsync` is delivering a high-quality explanation to the user. Caching is a performance optimization and must never be a point of catastrophic failure.

**Technical Specification:**
```csharp
// 4. Save to DB Cache with Vector Embedding (Non-blocking resilience)
try
{
    if (termVector == null)
    {
        var embRes = await _embeddingService.GenerateEmbeddingAsync($"[{safeCategory}] {safeTerm}", cancellationToken);
        if (embRes.IsSuccess)
        {
            termVector = embRes.Value;
        }
    }

    var newCache = new TermExplanationCache
    {
        Term = safeTerm,
        Category = safeCategory,
        Locale = locale,
        ExplanationText = explanation,
        Embedding = termVector,
        HitCount = 1
    };

    await _dbContext.TermExplanationCaches.AddAsync(newCache, cancellationToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogWarning(ex, "Failed to persist term explanation cache for term '{Term}'. Proceeding with generated explanation.", safeTerm);
}

return new TermExplanationResult(explanation, false);
```

### 3.3 Frontend `useApiError.ts` 500 Mapping
Currently, lines 68-75 of `frontend/composables/useApiError.ts` intercept ProblemDetails:
```typescript
if (typeof responseData.title === 'string' && typeof responseData.detail === 'string') {
  return `${responseData.title}: ${responseData.detail}`
}
if (typeof responseData.detail === 'string') {
  return responseData.detail
}
```
When ASP.NET Core encounters an unhandled exception (or returns 500), it returns:
`{ title: "Server Error", status: 500, detail: "An unexpected error occurred..." }`.
This bypasses localized translation dictionaries and displays raw English text to all users.

**Technical Specification:**
Update `useApiError.ts` to inspect the HTTP status code. If `status === 500` or the error indicates an internal server fault:
```typescript
const statusCode =
  (typeof responseData?.status === 'number' ? responseData.status : undefined) ||
  (typeof errorObj?.status === 'number' ? errorObj.status : undefined) ||
  (typeof errorObj?.statusCode === 'number' ? errorObj.statusCode : undefined);

if (statusCode === 500) {
  if (fallbackKey && te(fallbackKey)) {
    return t(fallbackKey);
  }
  if (te('api_errors.SERVER_ERROR')) {
    return t('api_errors.SERVER_ERROR');
  }
  return 'An unexpected server error occurred. Please try again.';
}
```
This guarantees that 500 responses resolve to:
- English: `"An unexpected server error occurred. Please try again."`
- Vietnamese: `"Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau."`

### 3.4 `TermExplainerModal.vue` Error UI & Retry Action
Currently, errors caught in `loadExplanation()` are assigned to `explanation.value`, rendering the error string inside the Markdown prose box.

**Technical Specification:**
1. **State Segregation:**
   ```typescript
   const explanation = ref<string | null>(null);
   const errorMessage = ref<string | null>(null);
   const isLoading = ref(false);
   ```
2. **Execution Flow in `loadExplanation`:**
   ```typescript
   async function loadExplanation() {
     isLoading.value = true;
     errorMessage.value = null;
     explanation.value = null;
     try {
       const res = await focusStore.explainTerm(
         props.term,
         props.category || "Software Architecture",
         props.context || "",
         locale.value,
       );
       explanation.value = res.explanation;
       isFromCache.value = !!res.isFromCache;
     } catch (err: any) {
       errorMessage.value = formatError(err, "today.explain_error");
     } finally {
       isLoading.value = false;
     }
   }
   ```
3. **Template Rendering:**
   - **Loading State:** Render spinner and "Explaining term..." label.
   - **Error State (`v-else-if="errorMessage"`):**
     ```html
     <div class="p-5 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800/60 flex flex-col items-start gap-3.5 text-rose-900 dark:text-rose-200">
       <div class="flex items-center gap-2.5 font-bold text-sm">
         <AlertCircle class="w-5 h-5 text-rose-600 dark:text-rose-400 shrink-0" />
         <span>{{ $t("today.explain_error_title") || $t("common.error") }}</span>
       </div>
       <p class="text-xs sm:text-sm leading-relaxed text-rose-800 dark:text-rose-300">
         {{ errorMessage }}
       </p>
       <button
         type="button"
         @click="loadExplanation"
         class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-semibold text-xs transition-colors shadow-sm active:scale-95"
       >
         <RotateCcw class="w-3.5 h-3.5" />
         <span>{{ $t("common.retry") || "Retry" }}</span>
       </button>
     </div>
     ```
   - **Success State (`v-else-if="explanation"`):**
     Render Markdown prose box.
   - **Footer Action:**
     Disable or hide the "Copy" button when `!explanation` or `errorMessage` is active.

---

## Verification & Automated Testing Plan

### Automated Backend Tests
1. **`CreateHighlightHandlerTests`:**
   - Test: `ExecuteAsync_WhenHighlightExists_ReturnsExistingHighlightAndUpdatesNote`
   - Test: `ExecuteAsync_WhenNewHighlight_InsertsNewRecord`
2. **`CreateCardFromHighlightHandlerTests`:**
   - Test: `ExecuteAsync_WhenDuplicateHighlightProvided_ReturnsExistingCard`
3. **`TermExplanationServiceTests`:**
   - Test: `ExplainTermAsync_WhenTermExceeds200Chars_ClampsSafelyAndSucceeds`
   - Test: `ExplainTermAsync_WhenDbCacheThrows_StillReturnsLlmExplanation`

### Automated Frontend Tests
1. **`InterviewChallengePane.spec.ts`:**
   - Test: `renders reviewed state when drill.status is "Reviewed" (string)`
   - Test: `renders reviewed state when drill.status is 2 (number)`
   - Test: `preserves selected option index and explanation feedback`
2. **`useApiError.spec.ts`:**
   - Test: `maps status 500 ProblemDetails to api_errors.SERVER_ERROR in Vietnamese locale`
   - Test: `maps status 500 ProblemDetails to fallbackKey when provided`
3. **`TermExplainerModal.spec.ts`:**
   - Test: `renders dedicated error banner and retry button on API failure`
   - Test: `retrying invokes loadExplanation and clears error banner on success`
