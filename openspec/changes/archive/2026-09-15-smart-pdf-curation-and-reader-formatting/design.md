# Design Document: Smart PDF Curation & Reader Formatting

## Architectural Overview

This design addresses document quality, slicing granularity, and visual presentation across three layers:
1. **Extraction Engine (`PdfPigExtractor.cs`):** Replaces flat outline scraping with a depth-aware hierarchical bookmark aggregator and a robust code block detection state machine.
2. **Persistence Worker (`PdfIngestionWorker.cs`):** Guarantees clean summary markdown extraction and disclaimer stripping before database persistence.
3. **Roadmap & Reader Presentation (`roadmap.vue`, `DocReaderPane.vue`, `[bookId].vue`):** Restores sequential milestone grouping and prevents prose text from leaking into code styling.

---

## 1. Backend Architecture & Heuristics

### A. Hierarchical Bookmark Tree Aggregator (`PdfPigExtractor.cs`)
- **Depth-Aware Curation:**
  - When walking the PDF catalog's `/Outlines` dictionary, record `(Title, PageNumber, Depth, ParentTitle)`.
  - In technical documentation PDFs (such as Microsoft Learn or O'Reilly books):
    - `Depth 0`: Root Volume / Book Title (e.g. *ASP.NET Core documentation*).
    - `Depth 1`: Major Modules (e.g. *Fundamentals*, *Blazor Web Apps*, *Security*).
    - `Depth 2`: Standalone Topics / Articles (e.g. *Dependency Injection*, *Configuration*, *Overview*).
    - `Depth 3+`: Minor subsections (e.g. *Service Lifetimes*, *Transient*, *Scoped*).
  - **Curation Rule:** Slices are generated strictly at the **Topic level** (`Depth 1` or `Depth 2` depending on root structure). Minor subsections (`Depth 3+`) are aggregated into the parent topic's page range rather than cut into separate slices.
  - **Contextual Title Formatting:** If a topic has a generic title (e.g. `"Overview"`), its slice title is qualified with the parent module name: `"{ParentModule}: Overview"`.
- **Elimination of Arbitrary 700-word Cuts:**
  - Complete topics (typically 1,500–3,500 words) remain intact as single slices for holistic learning.
  - Hard cuts with `(Part 1) ... (Part 10)` are deprecated. If an exceptional monolithic chapter exceeds 5,000 words without sub-bookmarks, it is partitioned only at natural Markdown headings (`## ` or `### `) outside code fences.

### B. Resilient Code Block Detection State Machine
The previous single-line heuristic was prone to latching into `inCodeBlock = true` and never escaping. The new design enforces clear entry and exit invariants:

```
[ Prose Line ] ---> Matches Strong Code Pattern? ---> Open Fence (```csharp) ---> [ In Code Block ]
      ^                                                                                 |
      |                                                                                 |
      +------------ Normal English Sentence / Blank Line / Heading <--------------------+
```

1. **Strong Code Open Signals:**
   - Keywords: `class `, `interface `, `struct `, `namespace `, `public `, `private `, `protected `, `async Task`, `using System...;`, `static void`.
   - Structural code lines: Lines ending in `{`, `}`, `=>`, or method signatures `(...);`.
   - Indentation: Lines indented with 4+ spaces or tabs with code tokens.
2. **Aggressive Prose Exit Signals:**
   - A line starting with an uppercase letter followed by natural prose words (e.g. "Using the app's sidebar navigation...", "Leave the browser open...").
   - A line with no code punctuation (no semicolons, braces, arrows, or variable assignments).
   - Any Markdown heading (`# `, `## `, `### `) or bullet point (`- `, `* `).
   - Two consecutive blank lines.
   - Upon encountering any prose signal, the code fence is immediately closed (`sb.AppendLine("```")`) before the text is emitted.

### C. Boilerplate & Disclaimer Sanitization
- Remove publication date strings (`### 07/30/2025`).
- Remove pre-release notices:
  `Regex.Replace(text, @"(?i)(Important\s+)?This information relates to a pre-release product[^.\n]*\.[^.\n]*\.", "")`
- Ensure `KeyTakeaways` sentence extractor discards legal disclaimers and requires technical substance.

---

## 2. Frontend Roadmap Architecture (`roadmap.vue`)

### A. Sequential Milestone Grouping
Instead of grouping by `chunk.chapterTitle` into an unordered `Map<string, chunk[]>`, the grouping algorithm processes chunks **sequentially**:
1. Iterate through chunks ordered by `chunkOrder` (1, 2, 3...).
2. Group contiguous chunks belonging to the same module (determined by the prefix in `ChapterTitle`, e.g. `Fundamentals: ...` or sequential batches of 5–8 chunks if uncurated).
3. Ensure every milestone contains slices in strict sequential order (`#1, #2, #3...`) without jumps or duplicate name collapsing.

### B. Clean Summary Formatting
- Add a utility `sanitizeSummary(text)` in `roadmap.vue`:
  - Strips leading Markdown headings (`/^\s*#{1,6}\s+/`).
  - Strips leftover dates or disclaimer prefixes.
  - Truncates cleanly to 160 characters without breaking words.

---

## 3. Database Schema Compatibility
- No EF Core schema changes or database migrations are required.
- Existing columns in `DocumentChunks` (`ChapterTitle`, `OriginalTextMarkdown`, `SummaryMarkdown`, `KeyTakeaways`) fully support the enhanced curated text.
