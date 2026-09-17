# Proposal: Universal Engineering Pillars and Smart Web PDF Crawler

## Why

TechDaily empowers software engineers to advance their careers through curated technical reading, spaced repetition retention, and scenario challenges. However, user feedback, real-world document ingestion workflows, and developer review have exposed four critical architectural and usability bottlenecks across `/profile` and `/library`:

1. **Framework-Pinned Curriculum Pillars on `/profile`:**
   - The Domain Mastery Goal Tracker (`DomainGoalTracker.vue`) on `/profile` currently pins progress bars to specific frameworks and databases: `.NET 10 & C# 13 Runtime`, `PostgreSQL 17 Storage Engine`, `System Design & Distributed`, and `Frontend & Browser Performance`.
   - This framework coupling alienates senior engineers building across diverse modern tech stacks (Node.js/NestJS, Go, Java/Spring, Python, MongoDB, Redis, MySQL).
   - Furthermore, `matchCategory` in `DomainGoalTracker.vue` only matches a narrow set of keywords (`.net`, `c#`, `csharp`, `clr`, `postgres`, `sql`, `b-tree`, `mvcc`). As a result, books, drills, or quiz attempts in other ecosystems fail to map to any pillar and disappear from progress visualization.

2. **Crawler Failure on Embedded Web PDF Viewers (`/library`):**
   - Users frequently paste technical book links hosted on web viewers such as `thuviensach.vn/pdf/viewer.php?id=...`, Mozilla PDF.js viewer shells, pages embedding `<iframe src="...pdf">` or `<embed src="...pdf">`, and Google Drive/Docs viewer links.
   - The current `WebArticleCrawler` strips `<script>`, `<iframe>`, and `<embed>` tags, treating the viewer container as an empty HTML document or failing with `"Extracted article content was empty or unreadable."`
   - To ingest such books today, users are forced to manually inspect developer tools, find the direct PDF stream, download multi-hundred megabyte files to their local machines, and manually re-upload them via the PDF Upload tab.

3. **Monolithic Slicing for Web Articles Without Heading Hierarchies:**
   - In `ImportDocumentHandler.cs`, the chunking algorithm (`SplitIntoChunks`) relies exclusively on regex matching of top-level and second-level Markdown headings (`^#{1,3}\s+`).
   - When users crawl or import long-form web articles, technical essays, or documentation that contain few or no `#` headings, the entire 4,000–10,000 word article is lumped into a single monolithic chunk.
   - This directly violates TechDaily's foundational 3–5 minute reading slice guarantee (~800–1,500 words per day), overwhelming readers and degrading spaced repetition granularity.

4. **Lack of Category and AI Evaluation Support for Engineering Craft & Productivity:**
   - The `Category` enum is restricted to four technical silos: `FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, and `SystemDesign`.
   - Essential high-impact engineering literature—such as *Atomic Habits* (James Clear), *Deep Work* (Cal Newport), *The Staff Engineer's Path* (Tanya Reilly), *A Philosophy of Software Design* (John Ousterhout), and *The Pragmatic Programmer*—has no home in the taxonomy.
   - Furthermore, `GeminiAiService.FormatSliceAsync` enforces strict code block rules (mandating triple-backtick language blocks and code-level syntax errors in quiz drills). When applied to mindset or leadership books, the AI either hallucinates synthetic code or fails to evaluate the core behavioral and cognitive principles of the chapter.

5. **Destructive Overwriting of Author's Prose with AI Summaries:**
   - In existing ingestion and curation code (`PdfIngestionWorker.cs` line 138 and `CurateSliceHandler.cs` line 77), the pipeline executes `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;`.
   - This destructive assignment overwrites the author's rich, narrative prose with an AI-condensed summary. For technical books and engineering craft literature (e.g., *Thói quen nguyên tử* / *Atomic Habits*, *Deep Work*, *The Pragmatic Programmer*), readers opening the reading view (`/read/[bookId]`) expect the author's authentic words and stories, not an AI summary bullet list.
   - The platform must enforce strict verbatim text preservation: `chunk.OriginalTextMarkdown` MUST remain 100% verbatim as extracted from `PdfPigExtractor` or the crawler. AI must only be used to populate `SummaryMarkdown`, `KeyTakeaways`, and the scenario drill (`InterviewQuestions`), without altering or compressing `OriginalTextMarkdown`.

Solving these five challenges transforms TechDaily into a truly framework-agnostic platform, enables zero-friction 1-click ingestion of embedded web PDFs, guarantees bite-sized reading slices for all web articles, preserves author prose with 100% verbatim fidelity for book reading, and embraces holistic engineering mindset and leadership literature.
---

## What Changes

We propose a four-pillar upgrade bridging frontend visualization, web crawling, document slicing, and AI curation:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│          UNIVERSAL PILLARS & SMART WEB PDF CRAWLER ARCHITECTURE             │
│                                                                             │
│  Pillar 1: Universal Engineering Layers on /profile                         │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ 1. Backend Runtime & Concurrency (.NET, Node, Go, Java, Python)       │  │
│  │ 2. Data Storage & Persistence (Postgres, Mongo, Redis, MySQL, ACID)   │  │
│  │ 3. Distributed Systems & Architecture (Microservices, Kafka, Outbox)  │  │
│  │ 4. Frontend & Browser Engineering (DOM, Rendering, Vue, React, TS)    │  │
│  │ • Multi-stack keyword matcher in DomainGoalTracker.vue                │  │
│  │ • Localized labels in vi.json and en.json                             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 2: Embedded PDF Sniffer & Direct Streaming Ingestion (/library)     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • WebArticleCrawler sniffs:                                           │  │
│  │   - PDF.js: DEFAULT_URL / pdfDoc / file regex                         │  │
│  │   - HTML tags: <iframe src="*.pdf">, <embed src="*.pdf">              │  │
│  │   - Google Docs / Drive viewer URLs                                   │  │
│  │   - Direct application/pdf MIME responses                             │  │
│  │ • CrawlUrlResponse exposes IsPdfDetected and DetectedPdfUrl           │  │
│  │ • POST /api/v1/library/import-remote-pdf streams directly into        │  │
│  │   temp storage (80KB buffer, zero-LOH) -> PdfIngestionWorker          │  │
│  │ • UI renders 1-click "Import & Slice PDF Directly" preview card       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 3: Smart Paragraph-Aware Word-Count Slicing (ImportDocumentHandler) │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Two-pass chunker: Heading split -> Paragraph soft-split             │  │
│  │ • Monolithic sections > 1,500 words subdivided into ~800–1,200 words  │  │
│  │ • Preserves code fences, blockquotes, and tables without splitting    │  │
│  │ • Sequential titling: "{Section} (Part 1)", "{Section} (Part 2)"      │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 4: Engineering Craft & Mindset Category (Category.EngineeringCraft) │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Enum value 4: EngineeringCraft ("Tư Duy Kỹ Sư & Năng Suất")         │  │
│  │ • GeminiAiService.FormatSliceAsync prompt adaptation:                 │  │
│  │   - Evaluates cognitive habits, deep work systems, staff leadership   │  │
│  │   - Code blocks optional (no synthetic hallucinated code)             │  │
│  │   - Scenario drills evaluate real-world engineering habit trade-offs  │  │
│  │ • UI catalog filters and dropdown options in library.vue              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 5: Verbatim Text Preservation for Books & Local Testing Strategy       │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • PdfIngestionWorker & CurateSliceHandler NEVER overwrite             │  │
│  │   chunk.OriginalTextMarkdown with AI formatted markdown               │  │
│  │ • chunk.OriginalTextMarkdown stays 100% verbatim as extracted         │  │
│  │ • /read/[bookId] renders author's true words                          │  │
│  │ • AI only populates SummaryMarkdown, KeyTakeaways, InterviewQuestions │  │
│  │ • Local testing with real fixtures (zero remote token wastage):       │  │
│  │   - 112-page Vietnamese Atomic Habits PDF fixture                     │  │
│  │   - thuviensach.vn embedded PDF.js web viewer link                    │  │
│  │   - Confirms 100% verbatim text across all sliced chapters           │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pillar 1: Framework-Agnostic Core Engineering Pillars on `/profile`
- Replace framework-pinned titles with universal engineering layers:
  * **Pillar 1:** Backend Runtime & Concurrency (`profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency") - category 1 (`Category.BackendDotNet` internally). Embraces .NET, Node.js, NestJS, Express, Go, Golang, Goroutines, Java, Spring, JVM, Python, Memory Allocation, Concurrency, Threads, Async I/O, Garbage Collection.
  * **Pillar 2:** Data Storage & Persistence (`profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence") - category 2 (`Category.DatabaseStorage`). Embraces PostgreSQL, MongoDB, Redis, MySQL, SQLite, Cassandra, ACID, B-Trees, LSM-Trees, Indexing, Query Optimization, Replication, WAL.
  * **Pillar 3:** Distributed Systems & Architecture (`profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture") - category 3 (`Category.SystemDesign`). Embraces Microservices, Event-Driven, Kafka, RabbitMQ, CAP Theorem, Outbox Pattern, Consensus, Load Balancing, Idempotency, Rate Limiting, Observability.
  * **Pillar 4:** Frontend & Browser Engineering (`profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering") - category 0 (`Category.FrontendWeb`). Embraces Browser Rendering Pipeline, Critical Rendering Path, DOM, Virtual DOM, Vue, React, TypeScript, JavaScript, Web Vitals, SSR, Hydration, Web Workers.
- In `frontend/components/profile/DomainGoalTracker.vue`, significantly expand `matchCategory(keyOrTopic: string)` to map multi-stack keywords into these 4 pillars.
- Update `frontend/i18n/locales/vi.json` and `en.json` with bilingual definitions and maintain backward-compatible alias lookups for existing tests.

### Pillar 2: Smart Embedded Web PDF Sniffer & Direct Ingestion (`/library`)
- Enhance `WebArticleCrawler.cs`:
  * Detect embedded PDF reader configurations:
    1. PDF.js viewer scripts: Regex matching `DEFAULT_URL\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`, `pdfDoc\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`, or `file:\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`.
    2. Embedded tags: `<iframe[^>]+src=["']([^"']+\.pdf[^"']*)["']>`, `<embed[^>]+src=["']([^"']+\.pdf[^"']*)["']>`, or `<object[^>]+data=["']([^"']+\.pdf[^"']*)["']>`.
    3. Google Docs/Drive viewer parameters: `docs.google.com/viewer\?.*url=([^&"']+)` or `drive.google.com/file/d/([^/]+)/view`.
    4. Direct HTTP MIME types: `Content-Type: application/pdf` or `application/x-pdf`.
  * When detected, resolve relative PDF links against the source base URI, run SSRF validation, and populate `IsPdfDetected = true` and `DetectedPdfUrl = ...` in `CrawlArticleResult`.
- Update `CrawlUrlResponse`:
  * Expose `bool IsPdfDetected` and `string? DetectedPdfUrl`.
- Add endpoint `POST /api/v1/library/import-remote-pdf`:
  * Implemented via `ImportRemotePdfHandler` (`IUseCase<ImportRemotePdfRequest, ImportRemotePdfResponse>`).
  * Validates URL, enforces SSRF defenses, streams remote PDF with 80KB buffer into temporary disk storage (`techdaily-uploads/{bookId}.pdf`) with zero LOH allocation.
  * Creates `DocumentBook` with `SourceType = PdfBook`, `AuthorOrSourceUrl = request.PdfUrl`, `Status = Processing`, and enqueues into `IPdfIngestionQueue` (`PdfIngestionWorker` + `PdfPigExtractor`).
  * Returns `HTTP 202 Accepted` with initialized `BookDto` within 2 seconds.
- Update `/library` Import Modal:
  * When `crawlUrl` returns `isPdfDetected: true`, display an "Embedded PDF Detected" preview banner showing the document title and resolved remote PDF URL.
  * Provide a 1-click action: `"Import & Slice PDF Directly"` (`$t('library.import_detected_pdf')`), triggering remote PDF ingestion and real-time polling without manual download.

### Pillar 3: Smart Word-Count Slicing for Web Articles
- In `ImportDocumentHandler.cs`, replace single-pass heading-only chunking with a two-pass chunking strategy:
  * **Pass 1:** Split by Markdown headings (`#`, `##`, `###`) outside code blocks.
  * **Pass 2:** For each chunk exceeding 1,500 words, apply a paragraph-aware soft split:
    - Subdivide along double newline paragraph boundaries (`\n\n`) outside of code fences (```...```) and blockquotes.
    - Group paragraphs into digestible slices targeting ~800–1,200 words (hard limit 1,500 words).
    - Append sequential subtitles: `{Title} (Part 1)`, `{Title} (Part 2)`.
    - Never split inside code blocks, terminal sessions, or markdown tables.

### Pillar 4: Engineering Mindset & Productivity Category Support
- In `backend/src/TechDaily.Domain/Enums/DomainEnums.cs`:
  * Add `EngineeringCraft = 4` to `Category` enum.
- In `backend/src/TechDaily.Application/Interfaces/IAiMarkdownFormatter.cs` and `TechDaily.Infrastructure/Services/GeminiAiService.cs`:
  * Pass `Category? category = null` to `FormatSliceAsync`.
  * When `category == Category.EngineeringCraft`, adapt the system instruction to evaluate cognitive focus, deep work habits, mental models, and engineering leadership principles rather than mandating programming language syntax-tagged code blocks.
  * Adapt scenario drills to evaluate engineering trade-offs, time management, and habit formation.
- Frontend & i18n:
  * Add category option `EngineeringCraft` (id: 4) across `library.vue` filter pills and import modals.
  * Localize `library.categories.craft`: `"Engineering Craft & Mindset"` (EN) / `"Tư Duy Kỹ Sư & Năng Suất"` (VI).


### Pillar 5: Verbatim Text Preservation for Books & Local Testing Strategy
- **Verbatim Text Preservation:**
  * In `backend/src/TechDaily.Infrastructure/Workers/PdfIngestionWorker.cs` (line 138) and `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs` (line 77), eliminate the destructive assignment `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;`.
  * For books in `Category.EngineeringCraft` (such as *Thói quen nguyên tử*, *Deep Work*, *The Pragmatic Programmer*) and book imports:
    - `chunk.OriginalTextMarkdown` MUST remain 100% verbatim as extracted from `PdfPigExtractor` or the crawler.
    - The reading surface (`/read/[bookId]`) renders the author's true words.
    - AI is strictly restricted to populating `SummaryMarkdown`, `KeyTakeaways`, and the scenario drill (`InterviewQuestions`), without modifying or compressing `OriginalTextMarkdown`.
- **Local Testing Strategy with Real-World Fixtures:**
  * Establish explicit local testing workflows using the local dev environment and local database, preventing remote token wastage:
    1. **PDF Fixture:** `/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf` (112-page Vietnamese *Atomic Habits* PDF). Verify chapter extraction, slice creation, and 100% verbatim text preservation across all chapters without AI compression.
    2. **Embedded Web PDF Fixture:** `https://thuviensach.vn/pdf/viewer.php?id=1c342b` (PDF.js web viewer sniffing). Verify crawler detection, relative link resolution, SSRF validation, direct streaming ingestion, and slice extraction fidelity.
---

## Capabilities

### Modified Capabilities
- `core-platform`: Refactor Domain Mastery Goal Tracker from framework-pinned titles (.NET, Postgres) to universal engineering layers (Backend Runtime & Concurrency, Data Storage & Persistence, Distributed Systems & Architecture, Frontend & Browser Engineering) with multi-stack keyword matching and bilingual translations.
- `library`:
  - Enhance `WebArticleCrawler` to sniff embedded PDF viewer links (PDF.js, iframes, embeds, Google Drive/Docs) and return `IsPdfDetected` and `DetectedPdfUrl`.
  - Add `POST /api/v1/library/import-remote-pdf` to stream remote PDFs directly into disk spooling and the `PdfPigExtractor` background queue.
  - Implement two-pass paragraph-aware soft splitting in `ImportDocumentHandler` for web articles lacking heading anchors (~800–1,500 words per slice).
  - Add `Category.EngineeringCraft` (value 4) and adapt `GeminiAiService.FormatSliceAsync` to evaluate core book principles and engineering habits.
  - Enforce verbatim text preservation in `PdfIngestionWorker` and `CurateSliceHandler`, guaranteeing `chunk.OriginalTextMarkdown` is never overwritten by AI summaries for books and Engineering Craft literature.

---

## Impact

- **Domain Models & Enums:**
  - `backend/src/TechDaily.Domain/Enums/DomainEnums.cs`: Adds `EngineeringCraft = 4` to `Category`.
- **Application Layer:**
  - `backend/src/TechDaily.Application/Features/Library/CrawlUrl/CrawlUrlHandler.cs`: Updates `CrawlUrlResponse` with `IsPdfDetected` and `DetectedPdfUrl`.
  - `backend/src/TechDaily.Application/Interfaces/IWebArticleCrawler.cs`: Updates `CrawlArticleResult` with `IsPdfDetected` and `DetectedPdfUrl`.
  - `backend/src/TechDaily.Application/Features/Library/ImportRemotePdf/ImportRemotePdfHandler.cs`: New use-case handler for streaming remote PDFs to background queue.
  - `backend/src/TechDaily.Application/Features/Library/ImportDocument/ImportDocumentHandler.cs`: Implements paragraph-aware soft splitting.
  - `backend/src/TechDaily.Application/Interfaces/IAiMarkdownFormatter.cs`: Accepts optional `Category? category`.
  - `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs`: Preserves `chunk.OriginalTextMarkdown` verbatim, restricting AI output to `SummaryMarkdown`, `KeyTakeaways`, and `InterviewQuestions`.
- **Infrastructure Layer:**
  - `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`: Implements PDF.js regex, iframe, embed, and Google Drive sniffing heuristics.
  - `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs`: Adapts `FormatSliceAsync` prompt when category is `EngineeringCraft`.
  - `backend/src/TechDaily.Infrastructure/Workers/PdfIngestionWorker.cs`: Stops overwriting `chunk.OriginalTextMarkdown` with AI formatted markdown for `Category.EngineeringCraft` and book imports; populates `SummaryMarkdown`, `KeyTakeaways`, and `InterviewQuestions`.
  - `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`: Maps `POST /api/v1/library/import-remote-pdf`.
- **Frontend Components & Locales:**
  - `frontend/components/profile/DomainGoalTracker.vue`: Updates pillar definitions to universal layers and expands `matchCategory` multi-stack keywords.
  - `frontend/pages/library.vue`: Adds detected PDF preview card with 1-click import button; adds Category 4 filter chip and select option.
  - `frontend/stores/useLibraryStore.ts`: Adds `importRemotePdf(payload)` action.
  - `frontend/i18n/locales/en.json` and `vi.json`: Updates `profile.domain_*` keys and adds `library.categories.craft` and `library.import_detected_pdf`.
- **Automated Tests:**
  - `backend/tests/TechDaily.Tests/Infrastructure/WebArticleCrawlerTests.cs`: Unit tests for PDF.js, iframe, embed, and Google Docs sniffing.
  - `backend/tests/TechDaily.Tests/Application/ImportDocumentHandlerTests.cs`: Unit tests for paragraph-aware soft word-count splitting.
  - `backend/tests/TechDaily.Tests/Application/ImportRemotePdfHandlerTests.cs`: Unit tests for remote PDF streaming and SSRF safety.
  - `backend/tests/TechDaily.Tests/Infrastructure/PdfIngestionWorkerTests.cs`: Unit tests verifying `OriginalTextMarkdown` remains 100% verbatim without AI summary overwrite for Engineering Craft books.
  - `frontend/tests/components/profile.spec.ts`: Updates tests for universal engineering pillars.
