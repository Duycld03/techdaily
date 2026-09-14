# Tasks: AI-Powered Markdown Formatting & Async Ingestion Progress

## 1. Bookmark Extraction & Blacklist Filtering
- [x] 1.1 Refactor `PdfPigExtractor.cs` to preserve all outline bookmarks with distinct target pages, removing arbitrary depth/level truncation.
- [x] 1.2 Implement case-insensitive blacklist filtering for front-matter (TOC, copyright, preface, cover) and back-matter (index, bibliography, references, colophon) in `PdfPigExtractor.cs`.
- [x] 1.3 Add safety monolith splitter at `##` headings for individual bookmark slices exceeding 4,000 words.

## 2. Universal AI Markdown Formatter & Two-Tier Ingestion
- [x] 2.1 Add `IsAiFormatted` boolean property to `DocumentChunk` domain entity, Application DTOs, and EF Core configuration with database migration.
- [x] 2.2 Define `IAiMarkdownFormatter` interface and implement in `GeminiAiService.cs` using Gemini Flash Lite with prompt engineering for standardized TechInsight Markdown (header, `> [!NOTE]`, clean prose, universal language code fences, callouts, and 3 key takeaways).
- [x] 2.3 Refactor `PdfIngestionWorker.cs` to execute Tier 1 rapid ingestion: persist bookmarks, AI-format Slices 1–3, and set book status to `Ready` with `ProgressPercentage = 10%`.
- [x] 2.4 Implement Tier 2 background loop in `PdfIngestionWorker.cs` to format Slices 4..N with 1.2s rate-limiting, progressively updating `DocumentBook.ProgressPercentage` and `StatusMessage`.
- [x] 2.5 Implement On-Demand JIT AI formatting fallback in the reader slice query handler (`GetChunkByOrder` or reader endpoint) so navigating ahead immediately formats and caches uncurated slices.

## 3. Frontend Real-Time Ingestion Progress
- [x] 3.1 Update TypeScript book interfaces in `frontend/` to include `progressPercentage`, `statusMessage`, and chunk formatting state.
- [x] 3.2 Update `frontend/pages/library.vue` to render an active progress bar and dynamic status message (`AI is curating slice X/Y...`) on book cards when `progressPercentage < 100`.
- [x] 3.3 Ensure polling in `frontend/pages/library.vue` actively refreshes books that are currently undergoing Tier 2 background curation.

## 4. Local-First Automated Verification
- [x] 4.1 Update `PdfPigExtractorTests.cs` to test distinct page bookmark preservation, blacklist filtering, and disclaimer removal.
- [x] 4.2 Start fullstack locally via `./run-dev.sh` and run a test ingestion on `/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf` (first 15 slices).
- [x] 4.3 Verify DOM rendering via Playwright browser at `http://localhost:3000`: check Slice 3 reading duration (~5–10 mins), inspect `<pre><code>` blocks to ensure zero trapped prose, and capture verification screenshots.
- [x] 4.4 Execute backend test suite (`dotnet test backend/tests/TechDaily.Tests`) and frontend test suite (`npm test`).
