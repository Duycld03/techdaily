# Design Document: AI-Powered Markdown Formatting & Async Ingestion Progress

## 1. Architectural Architecture

```
[ PDF Upload ]
      │
      ▼
[ PdfPigExtractor.cs ]
      ├─ 1. Bookmark extraction (all unique page targets)
      ├─ 2. Blacklist filter (TOC, Cover, Copyright, Index, Contributors)
      ├─ 3. Safety split if slice > 4,000 words
      └─ Output: N raw slices with initial RawText
      │
      ▼
[ PdfIngestionWorker.cs ]
      │
      ├─ Phase 1: Rapid Persistence & Initial AI Formatting (Slices 1..3)
      │     ├─ GeminiAiService.FormatSliceAsync() for Slices 1, 2, 3
      │     ├─ Mark Book Status = Ready (Can read immediately)
      │     └─ Update Progress = 10%, StatusMessage = "Ready for reading (3 initial slices curated)"
      │
      └─ Phase 2: Async Background AI Queue (Slices 4..N)
            ├─ Rate-limited loop (1.2s delay between calls)
            ├─ Format slice into Insight-quality Markdown
            ├─ Update DocumentChunk.OriginalTextMarkdown & SummaryMarkdown
            └─ Update DocumentBook.ProgressPercentage & StatusMessage:
                 "AI is curating slice {Order}/{Total} ({Pct}%)..."
```

---

## 2. Component Specifications

### A. Bookmark Extraction & Blacklist Filter (`PdfPigExtractor.cs`)
- **Blacklist Filter:**
  Bookmarks matching any of the following (case-insensitive) are completely excluded:
  `table of contents`, `contents`, `mục lục`, `cover`, `copyright`, `bản quyền`, `preface`, `about the author`, `about the reviewers`, `index`, `chỉ mục`, `contributors`, `credits`, `bibliography`, `references`, `colophon`.
- **Bookmark Preservation:**
  Do not filter by arbitrary `Level` cutoff. Group bookmarks by distinct `PageNumber` to retain every legitimate topic (~8–10 pages per slice).
- **Safety Monolith Splitter:**
  If an individual bookmark page range spans > 4,000 words, split at natural Markdown headings (`## `) outside code blocks to prevent oversized slices.

---

### B. Universal AI Markdown Formatter (`GeminiAiService.cs`)
- **Service Interface:**
  ```csharp
  public interface IAiMarkdownFormatter
  {
      Task<Result<AiFormattedSlice>> FormatSliceAsync(
          string rawText,
          string chapterTitle,
          string language,
          CancellationToken cancellationToken = default);
  }
  ```
- **Standardized Insight-Style Markdown Schema:**
  - `# [Title]`
  - `> [!NOTE]` with 2–3 sentence executive context summary
  - Clean narrative paragraphs (resolving broken line-wraps from PDF extraction)
  - Universal code fences with syntax tag (`python`, `csharp`, `typescript`, `sql`, `go`, `bash`, `yaml`, `dockerfile`, etc.)
  - Callout alerts (`> [!TIP]`, `> [!IMPORTANT]`)
  - Three bulleted Key Takeaways
- **Sanitization:** Removes publication dates, print headers, and pre-release disclaimers.

---

### C. Ingestion Worker & Progress Broadcasting (`PdfIngestionWorker.cs`)
1. **Tier 1 (Instant Availability):**
   - Saves book and initial chunk records.
   - Immediately invokes `FormatSliceAsync` for Slices 1, 2, and 3.
   - Flags book as `Status = ProcessingStatus.Ready` so users can begin Day 1/2 study immediately.
2. **Tier 2 (Background AI Loop):**
   - Iterates through Slices 4 to Total.
   - Paces requests to conform to Gemini Flash Lite limits (delay 1,200ms).
   - Updates `DocumentBook.ProgressPercentage` ($10\% \to 100\%$) and `DocumentBook.StatusMessage` after each slice.
3. **On-Demand Just-In-Time (JIT) Formatting:**
   - Reader endpoint `/books/{id}/chunks/{order}` checks if the requested slice is already AI-formatted.
   - If unformatted (e.g. user jumped to Slice 50 before background worker reached it), the API executes an on-demand format call, updates the DB chunk, and returns pristine Markdown immediately.

---

### D. Frontend Visual Progress (`library.vue`)
- Displays an active processing badge and progress bar on books currently being curated by AI:
  ```vue
  <div v-if="book.progressPercentage < 100" class="mt-2 space-y-1">
    <div class="flex justify-between text-xs text-slate-500">
      <span>{{ book.statusMessage }}</span>
      <span>{{ book.progressPercentage }}%</span>
    </div>
    <div class="w-full bg-slate-200 dark:bg-slate-800 h-1.5 rounded-full overflow-hidden">
      <div class="bg-brand-500 h-full transition-all duration-300" :style="{ width: `${book.progressPercentage}%` }"></div>
    </div>
  </div>
  ```

---

## 3. Local-First Automated Verification
- Agent runs `./run-dev.sh`.
- Test script executes extraction on `/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf` for the first 15 slices.
- Agent uses Playwright headless browser at `http://localhost:3000/today` to verify:
  - Slice 3 (`Get started`) is ~7–10 pages, not 425 minutes.
  - `<pre><code>` blocks contain zero prose instructions (`Go back to the app`, `Click me`).
  - GitHub alert boxes (`> [!NOTE]`, `> [!TIP]`) render cleanly.
