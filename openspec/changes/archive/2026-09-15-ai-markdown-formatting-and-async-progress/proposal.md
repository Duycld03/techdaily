# Proposal: AI-Powered Markdown Formatting & Async Ingestion Progress

## Why
Technical documentation PDFs (e.g. Microsoft Learn, O'Reilly, Manning) contain deeply heterogeneous content structures: multi-column prose, terminal commands, code declarations, razor syntax, tables, and callouts. Relying solely on deterministic Regex heuristics to reconstruct Markdown is brittle:
1. **Code Block Trapping:** Explanatory prose paragraphs starting with lowercase tool names (e.g., `dotnet watch command. Go back to the app...`) trigger CLI start patterns and get trapped inside red monospace code blocks.
2. **Boilerplate & Garbage Artifacts:** Leading brackets, punctuation, and pre-release disclaimers (e.g. `) Important This information relates to a pre-release product...`) leak into reading content.
3. **Coarse Slicing Collapse:** Heuristic bookmark filtering (`targetMaxDepth = 1`) dropped Level 2/3 bookmarks, collapsing 5,956 pages into a single 425-minute monster slice (`Get started`).
4. **Opaque Background Ingestion:** Users lack visual visibility into where the background ingestion process is when processing large literature.
5. **Slow Verification Loop:** Relying on full remote CI/CD deployments to test UI formatting changes causes slow iteration loops and developer friction.

## What Changes
1. **Bookmark-Preserving Slicing Engine (`PdfPigExtractor.cs`):**
   - Retain all outline bookmarks with distinct page ranges (no depth-based bookmark dropping).
   - Automatically blacklist front-matter (*Table of Contents, Contents, Cover, Copyright, Preface, About the Authors*) and back-matter (*Index, Bibliography, References, Colophon, Contributors*).
   - Apply a safety splitter at `##` headings only if an individual bookmark slice exceeds 4,000 words.
2. **Slice-by-Slice AI Markdown Formatter (`GeminiAiService.cs`):**
   - Use Gemini Flash Lite (`gemini-3.5-flash-lite` / `gemini-3.1-flash-lite`) to format raw extracted text slice-by-slice.
   - Standardize into a uniform TechInsight-style Markdown structure:
     - Header & Context callout (`> [!NOTE]`)
     - Clean, non-interrupted prose paragraphs
     - Universal language code fences (automatically detecting `csharp`, `python`, `typescript`, `sql`, `go`, `rust`, `bash`, `yaml`, `dockerfile`, etc.)
     - Callouts (`> [!TIP]`, `> [!IMPORTANT]`)
     - Key Takeaways bullet points.
3. **Two-Tier Ingestion & Real-Time Progress:**
   - **Tier 1 (Instant Availability):** Raw bookmark extraction + AI formatting of initial slices (1–3) in < 15 seconds. Book becomes immediately readable.
   - **Tier 2 (Background Processing):** Worker formats remaining slices sequentially with rate-limit pacing (1.2s delay), updating `ProgressPercentage` and `StatusMessage` (e.g., `AI đang tối ưu lát cắt 45/230 (20%)...`).
   - **On-Demand JIT Fallback:** If a user jumps ahead to an unformatted slice, the API formats it on-demand in ~1.5s and caches it into the database.
4. **Local-First Playwright Verification:**
   - Agent runs fullstack locally (`./run-dev.sh`) and tests UI DOM via Playwright headless browser at `http://localhost:3000`, verifying `<pre><code>` blocks, reading minutes, and capturing screenshots before any git push.

## Capabilities
- **Library (`specs/library/spec.md`):** Lossless outline bookmark preservation, front/back-matter blacklist filtering, two-tier AI formatting queue, and live progress reporting.
- **Reader (`specs/reader/spec.md`):** Standardized TechInsight Markdown schema and on-demand JIT slice AI formatting.

## Scope & Impact
- **Backend:** `PdfPigExtractor.cs`, `PdfIngestionWorker.cs`, `GeminiAiService.cs`, `DocumentChunk.cs`, `BookEndpoints.cs`.
- **Frontend:** `library.vue`, `DocReaderPane.vue`, `[bookId].vue`.
- **Database:** Support cached AI formatted content (`OriginalTextMarkdown` / `SummaryMarkdown`, `IsAiFormatted`) and live progress columns in `DocumentBooks`.
