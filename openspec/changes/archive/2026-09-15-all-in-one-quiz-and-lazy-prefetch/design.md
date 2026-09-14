# Design: All-in-One Quiz Generation, Lazy Curation & Lookahead Prefetch

## 1. System Architecture & Data Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Frontend as Nuxt 4 (Reader/Today)
    participant Api as ASP.NET Core 10 Minimal API
    participant Worker as PdfIngestionWorker
    participant Gemini as Gemini Flash Lite
    participant DB as PostgreSQL (techdaily_db)

    Note over User,DB: Phase 1: Upload & 3-Slice Ingestion (<10s)
    User->>Api: POST /api/v1/library/upload
    Api->>DB: Save DocumentBook + Raw DocumentChunks (1..N)
    Api-->>Worker: Enqueue Ingestion Job
    Worker->>Gemini: Format Slices 1, 2, 3 (All-in-One: Markdown + Quiz)
    Gemini-->>Worker: JSON { markdown, summary, takeaways, drill }
    Worker->>DB: Update Chunks 1..3 (IsAiFormatted=true) + Create InterviewQuestions
    Worker->>DB: Set DocumentBook Status = Ready, Progress = 100%
    Note over Worker: STOP! (No eager processing of slices 4..N)

    Note over User,DB: Phase 2: Reading & 1-Step Lookahead Prefetch
    User->>Frontend: View Slice 3
    Frontend->>DB: Fetch Slice 3 (already formatted)
    Frontend->>Api: POST /books/{id}/slices/4/curate (Background Prefetch)
    Api->>Gemini: All-in-One Format Slice 4
    Gemini-->>Api: Slice 4 Formatted + Quiz
    Api->>DB: Save Slice 4 + InterviewQuestion
    User->>Frontend: Click "Next" -> View Slice 4 (Instant, 0s delay)

    Note over User,DB: Phase 3: Unprefetched JIT & Error Recovery
    User->>Frontend: Jump to Slice 10 (unformatted)
    Frontend->>Frontend: Show "AI is curating this chapter..." spinner
    Frontend->>Api: POST /books/{id}/slices/10/curate
    alt API Failure / Rate Limit
        Api-->>Frontend: 500 / 429 Error
        Frontend->>User: Display Retry Card ("Retry with AI" | "View raw text temporarily")
        User->>Frontend: Click "View raw text temporarily"
        Frontend->>Frontend: Set isViewingRawTemporarily = true (RAM only)
        Note over Frontend,DB: DB IsAiFormatted remains FALSE
        User->>Frontend: F5 / Refresh Page
        Frontend->>Api: Re-attempt POST /books/{id}/slices/10/curate
    end
```

---

## 2. Interface & DTO Contracts

### `IAiMarkdownFormatter.cs`
```csharp
public record AiScenarioDrillVo(
    string QuestionText,
    List<string> Options,
    int CorrectOptionIndex,
    string ExplanationMarkdown,
    List<string> ExpectedKeyPoints);

public record AiFormattedSliceResult(
    string FormattedMarkdown,
    string SummaryMarkdown,
    List<string> KeyTakeaways,
    int EstimatedReadMinutes,
    AiScenarioDrillVo? ScenarioDrill);

public interface IAiMarkdownFormatter
{
    Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
        string rawMarkdown,
        string chapterTitle,
        string? targetLocale = "en",
        CancellationToken cancellationToken = default);
}
```

### Combined Gemini Prompt
```text
You are a Principal Technical Writer and Software Architect.
Transform raw extracted book text into a publication-ready Technical Insight module AND create a Senior-level Architectural Scenario Drill.

Respond strictly with valid JSON:
{
  "formattedMarkdown": "Clean markdown with # Title, > [!NOTE], clean prose, code fences, > [!TIP], and ### Key Takeaways",
  "summaryMarkdown": "2-3 sentence executive architectural summary",
  "keyTakeaways": ["Point 1", "Point 2", "Point 3"],
  "scenarioDrill": {
    "questionText": "Realistic production trade-off problem testing these key takeaways",
    "options": ["A", "B", "C", "D"],
    "correctOptionIndex": 1,
    "explanationMarkdown": "Deep dive into why B is optimal and A/C/D incur technical debt",
    "expectedKeyPoints": ["Trade-off 1", "Trade-off 2"]
  }
}
```

---

## 3. Worker & Ingestion Refactoring (`PdfIngestionWorker.cs`)
- Ingestion extracts all slices from native bookmarks.
- Formats only `Math.Min(3, chunks.Count)` slices with Gemini Flash Lite.
- For each slice, saves the formatted content and persists the corresponding `InterviewQuestion` entity linked via `DocumentChunkId`.
- Updates `DocumentBook.Status = ProcessingStatus.Ready`, `ProgressPercentage = 100`, `StatusMessage = "Ready for reading"`.
- Deletes the infinite eager background loop for slices 4..N.

---

## 4. Frontend State & UX Specifications

### Lookahead Prefetching
- In `read/[bookId].vue` and `today.vue`:
  ```typescript
  function triggerLookaheadPrefetch(nextChunkOrder: number) {
    const nextChunk = book.value?.chunks?.find(c => c.chunkOrder === nextChunkOrder)
    if (nextChunk && !nextChunk.isAiFormatted && !prefetchedSet.has(nextChunkOrder)) {
      prefetchedSet.add(nextChunkOrder)
      libraryStore.curateSlice(bookId.value, nextChunkOrder).catch(() => {})
    }
  }
  ```

### Ephemeral Raw View
- `isViewingRawTemporarily = ref(false)`
- Never written to `localStorage` or backend.
- Banner atop reader:
  ```html
  <div v-if="isViewingRawTemporarily" class="bg-amber-500/10 border border-amber-500/30 text-amber-300 px-4 py-2 rounded-lg flex items-center justify-between text-sm">
    <span>Viewing unformatted raw text.</span>
    <button @click="handleRetryCurate" class="font-semibold underline hover:text-amber-200">Format with AI</button>
  </div>
  ```
