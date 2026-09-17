## MODIFIED Requirements

### Requirement: Web URL Document Crawler & Preview
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/crawl-url` to extract documentation content from web URLs or raw markdown links, stripping extraneous navigation, scripts, and ads into clean Markdown.

The crawler (`WebArticleCrawler`) SHALL implement an embedded PDF sniffer capable of detecting online PDF documents wrapped in JavaScript or iframe viewer web shells. The sniffer SHALL evaluate:
1. **PDF.js scripts:** Regex matching `DEFAULT_URL\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`, `pdfDoc\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`, or `file:\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`.
2. **Iframe elements:** `<iframe[^>]+src=["']([^"']+\.pdf[^"']*)["']`.
3. **Embed and Object elements:** `<embed[^>]+src=["']([^"']+\.pdf[^"']*)["']` or `<object[^>]+data=["']([^"']+\.pdf[^"']*)["']`.
4. **Google Docs/Drive Viewers:** URL query patterns matching `docs.google.com/viewer\?.*url=([^&"']+)` or `drive.google.com/file/d/([^/]+)/view`.
5. **Direct PDF MIME Responses:** Responses with `Content-Type: application/pdf` or `application/x-pdf`.

When an embedded or direct PDF is detected:
- The crawler SHALL resolve relative PDF URLs against the source document base URI to construct an absolute URL.
- The crawler SHALL enforce SSRF validation policies against the resolved PDF URL.
- The crawler SHALL return `CrawlUrlResponse` with `IsPdfDetected = true`, populated `DetectedPdfUrl`, and an informative title extracted from the page or PDF filename, without throwing an error for missing body text.

#### Scenario: User crawls documentation URL
- **WHEN** authenticated user submits `POST /api/v1/library/crawl-url` with a valid web URL
- **THEN** system fetches content, parses main article body into markdown, extracts title, and returns preview DTO with word count estimate and `IsPdfDetected = false`.

#### Scenario: User crawls raw Markdown URL
- **WHEN** user provides a URL pointing to raw Markdown (e.g. GitHub raw file)
- **THEN** system retrieves raw text directly without HTML parsing overhead.

#### Scenario: User crawls page containing embedded PDF.js viewer
- **WHEN** user submits `POST /api/v1/library/crawl-url` with a URL whose HTML embeds a PDF.js reader setting `DEFAULT_URL = "books/distributed-systems.pdf"`
- **THEN** the crawler detects the embedded PDF script pattern, resolves the absolute PDF URL against the base URL, and returns `IsPdfDetected = true` with `DetectedPdfUrl = "https://example.com/books/distributed-systems.pdf"`.

#### Scenario: User crawls page containing iframe or embed PDF tag
- **WHEN** user submits `POST /api/v1/library/crawl-url` with a page containing `<iframe src="/static/specs/raft-consensus.pdf"></iframe>`
- **THEN** the crawler detects the iframe source, resolves the absolute URL, and returns `IsPdfDetected = true` with `DetectedPdfUrl` pointing to the target PDF file.

#### Scenario: User crawls Google Docs or Drive PDF viewer link
- **WHEN** user submits `POST /api/v1/library/crawl-url` with a Google Docs viewer link `https://docs.google.com/viewer?url=https%3A%2F%2Fexample.com%2Fwhitepaper.pdf`
- **THEN** the crawler decodes the embedded target URL, verifies SSRF safety, and returns `IsPdfDetected = true` with `DetectedPdfUrl = "https://example.com/whitepaper.pdf"`.

---

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and URL Crawler with content preview before ingestion confirmation.

Under the Category selector in all 3 modal tabs (Markdown Series, PDF Upload, and URL Crawler), the modal SHALL render a dedicated in-context verbatim category helper callout banner:
1. **Notice Content & Guidance:**
   - Clearly explains that selecting Category 4 ("Engineering Craft & Mindset" / "Tư Duy Kỹ Sư & Năng Suất") preserves 100% of the book's original text while AI automatically restores run-in headings and paragraph boundaries.
   - **Vietnamese (`vi`):** `"Mẹo: Chọn chuyên mục \"Tư Duy Kỹ Sư & Năng Suất\" để giữ nguyên văn 100% nội dung sách (AI khôi phục tiêu đề & ngắt dòng)."`
   - **English (`en`):** `"Tip: Select \"Engineering Craft & Mindset\" to preserve 100% verbatim book text (AI restores headings & paragraphs)."`
2. **Visual Presentation:**
   - Styled as an ambient, non-intrusive alert box with rounded corners (`rounded-xl`), soft amber background (`bg-amber-50/80 dark:bg-amber-950/30`), amber border (`border-amber-200/80 dark:border-amber-900/50`), and readable amber text (`text-xs text-amber-800 dark:text-amber-300`).
   - Accompanied by a leading lightbulb icon (`Lightbulb`, `text-amber-500`) to highlight helpful guidance.

When the URL Crawler detects an embedded PDF (`IsPdfDetected == true`), the modal SHALL render an Embedded PDF Detection Preview Card:
- Highlighting the detected document title and resolved remote PDF URL.
- Displaying a dedicated one-click action button: "Import & Slice PDF Directly" (`$t('library.import_detected_pdf')`).
- Suppressing the empty markdown preview editor and directing the user directly toward automated streaming ingestion.

#### Scenario: User switches import modal tabs
- **GIVEN** an authenticated user on the `/library` page
- **WHEN** user opens the import modal and selects the "PDF Upload" tab
- **THEN** UI displays the drag-and-drop dropzone with file size limit guidance, category selector, and the streamlined verbatim category helper notice.

#### Scenario: User crawls URL and previews content in modal
- **GIVEN** user is in the "URL Crawler" tab of the import modal
- **WHEN** user inputs a documentation URL and clicks "Fetch Content"
- **THEN** UI displays the article title, category selector with the streamlined verbatim category helper notice, and markdown preview before final import confirmation.

#### Scenario: URL crawler preview reveals detected embedded PDF reader
- **WHEN** user crawls a URL and the backend responds with `IsPdfDetected = true` and a valid `DetectedPdfUrl`
- **THEN** the modal displays an embedded PDF alert card with the resolved PDF URL and a prominent "Import & Slice PDF Directly" button
- **AND** clicking the button invokes `POST /api/v1/library/import-remote-pdf` with the detected URL, closes the modal, and initiates ingestion progress polling on the library view.

#### Scenario: Streamlined verbatim category hint displayed in Markdown Series tab
- **GIVEN** user opens the import modal on `/library`
- **WHEN** user views the "Markdown Series" tab
- **THEN** the streamlined helper notice appears directly below the Category selector
- **AND** displays the localized tip explaining that Category "Engineering Craft & Mindset" preserves 100% verbatim text with AI-restored headings and paragraphs.

#### Scenario: Streamlined verbatim category hint displayed in PDF Upload tab
- **GIVEN** user opens the import modal on `/library` and selects the "PDF Upload" tab
- **WHEN** user views the upload form fields
- **THEN** the streamlined verbatim category helper notice appears directly beneath the Category selector and above the file dropzone.

#### Scenario: Streamlined verbatim category hint displayed in URL Crawler tab
- **GIVEN** user opens the import modal on `/library` and selects the "URL Crawler" tab
- **WHEN** user views the URL crawl parameters
- **THEN** the streamlined verbatim category helper notice appears directly beneath the Category selector.

#### Scenario: Bilingual rendering of streamlined verbatim category hint
- **GIVEN** user changes locale between Vietnamese (`vi`) and English (`en`)
- **WHEN** viewing the Category selector in any import modal tab
- **THEN** the helper notice dynamically renders the corresponding localized text without truncation or container overflow.
---

## ADDED Requirements

### Requirement: Remote PDF Direct Ingestion Pipeline
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/import-remote-pdf` to stream remote PDF documents directly into the server's PDF ingestion pipeline without requiring the user to download the file locally.

The endpoint handler (`ImportRemotePdfHandler`) SHALL:
1. Validate the remote URL scheme (`http` / `https`) and enforce strict SSRF validation to prevent access to private IP ranges, loopback addresses, or cloud metadata services.
2. Initiate a streaming HTTP GET request with `HttpCompletionOption.ResponseHeadersRead` and verify the `Content-Type` header or stream magic bytes start with `%PDF-`.
3. Stream the file directly to temporary disk storage (`Path.Combine(Path.GetTempPath(), "techdaily-uploads", $"{bookId}.pdf")`) with an 80KB buffer size, ensuring zero Large Object Heap (LOH) contiguous byte array allocations.
4. Enforce a maximum file size limit of 350 MB during streaming, aborting with `HTTP 400 Bad Request` if the stream exceeds the limit.
5. Create a `DocumentBook` entity with `SourceType = PdfBook`, `Category = request.Category`, `AuthorOrSourceUrl = request.PdfUrl`, `Status = ProcessingStatus.Processing`, and `ProgressPercentage = 0`.
6. Enqueue a `PdfIngestJob` into `IPdfIngestionQueue` for background processing by `PdfIngestionWorker` using `PdfPigExtractor` and Gemini Flash Lite slice curation.
7. Return `HTTP 202 Accepted` with the initialized `BookDto` within 2 seconds.

#### Scenario: User imports remote PDF directly from detected web viewer URL
- **WHEN** authenticated user submits `POST /api/v1/library/import-remote-pdf` with a valid remote PDF URL, title, category, and language
- **THEN** server streams the remote PDF into temporary storage, registers the `DocumentBook`, queues the background ingestion task, and returns `HTTP 202 Accepted` with the new book record.

#### Scenario: Remote PDF streaming ingestion enforces SSRF security boundaries
- **WHEN** user submits `POST /api/v1/library/import-remote-pdf` with a URL resolving to `127.0.0.1`, `169.254.169.254`, or a private RFC 1918 subnet
- **THEN** server rejects the request immediately with `HTTP 400 Bad Request` and error code `SSRF_DETECTED`.

#### Scenario: Remote PDF endpoint rejects oversized files or non-PDF streams
- **WHEN** remote URL streams more than 350 MB or does not contain valid PDF header signatures
- **THEN** server aborts the stream, removes the temporary file, and returns `HTTP 400 Bad Request`.

---

### Requirement: Smart Word-Count Slicing for Web Articles
The document ingestion pipeline (`ImportDocumentHandler`) SHALL enforce a paragraph-aware soft split (~800–1,500 words per slice) when imported web articles or Markdown documents have few or no `#`, `##`, or `###` headings, ensuring all created slices are consumable within 3–5 minutes.

The slicing algorithm SHALL execute two passes:
1. **Pass 1 (Heading Split):** Divides content along markdown headings (`#`, `##`, `###`) that occur outside code blocks (` ``` ` fences).
2. **Pass 2 (Paragraph-Aware Soft Split):** For each chunk produced in Pass 1:
   - If word count $\le 1,500$ words, retains the chunk as a single slice.
   - If word count $> 1,500$ words, subdivides the chunk along double-newline paragraph boundaries (`\n\n`) outside code fences and blockquotes, targeting ~800–1,200 words per slice (maximum 1,500 words).
   - Sequentially titles subdivided slices with part designations: `{OriginalHeading} (Part 1)`, `{OriginalHeading} (Part 2)`, etc.
   - Guarantees that code blocks, terminal sessions, and tables are never fractured across slice boundaries.

#### Scenario: Importing monolithic web article without markdown headings
- **WHEN** an authenticated user imports a 4,500-word crawled article containing only paragraph prose and zero `#` or `##` markdown headings
- **THEN** `ImportDocumentHandler` applies paragraph-aware soft splitting
- **AND** produces 3 to 4 sequential reading slices of approximately 1,100–1,500 words each
- **AND** assigns sequential titles (e.g. `Section 1 (Part 1)`, `Section 1 (Part 2)`, `Section 1 (Part 3)`).

#### Scenario: Preserving code blocks and tables across soft paragraph splits
- **WHEN** a 2,500-word technical section contains a long code snippet or markdown table
- **THEN** the soft paragraph splitter preserves the code snippet or table intact within a single slice without splitting lines mid-block.

---

### Requirement: Engineering Mindset & Productivity Category Support
The system SHALL support Category 4: `EngineeringCraft` ("Tư Duy Kỹ Sư & Năng Suất" / "Engineering Craft & Mindset") across the `Category` enum, document ingestion, library catalog filtering, and AI slice curation.

The AI slice formatter (`GeminiAiService.FormatSliceAsync` and `IAiMarkdownFormatter`) SHALL adapt formatting rules when `category == Category.EngineeringCraft`:
1. **System Prompt Adaptation:** Evaluates core principles, cognitive habits, mental models, time-blocking workflows, leadership trade-offs, and deliberate practice frameworks from books such as *Atomic Habits*, *Deep Work*, *The Staff Engineer's Path*, and *The Pragmatic Programmer*.
2. **Relaxed Code Block Constraint:** Syntax-tagged code blocks SHALL be optional and included only when the source material explicitly contains code or pseudocode, rather than forcing artificial code examples into mindset literature.
3. **Scenario Drill Focus:** The generated Senior Scenario Challenge SHALL evaluate high-impact engineering leadership, focus preservation under on-call pressure, habit loops for code quality, or navigating staff-level technical ambiguity.

#### Scenario: User imports document under Engineering Craft category
- **WHEN** user uploads a PDF or imports markdown selecting category `EngineeringCraft` (value `4`)
- **THEN** `DocumentBook.Category` is persisted as `Category.EngineeringCraft`
- **AND** the library catalog filter for "Engineering Craft & Mindset" correctly displays the book.

#### Scenario: AI formats slice for Engineering Craft book with adapted mindset prompts
- **WHEN** background ingestion worker curates a slice for a book with `Category == Category.EngineeringCraft`
- **THEN** `GeminiAiService.FormatSliceAsync` applies the mindset-adapted prompt
- **AND** generates a structured article with context callout, core principles, key takeaways, and a scenario drill evaluating engineering habits and productivity trade-offs without requiring synthetic code blocks.

---

### Requirement: Verbatim Book Text Preservation & Curation Safety
The system SHALL guarantee 100% verbatim text preservation for all imported books and literature under `Category.EngineeringCraft` (such as *Thói quen nguyên tử* / *Atomic Habits*, *Deep Work*, *The Pragmatic Programmer*).

The background ingestion worker (`PdfIngestionWorker`) and the slice curation handler (`CurateSliceHandler`) SHALL NEVER overwrite `DocumentChunk.OriginalTextMarkdown` with AI-condensed markdown (`aiResult.Value.FormattedMarkdown`).

The system SHALL enforce the following invariants during book ingestion and slice curation:
1. **Verbatim Content Invariance:** `DocumentChunk.OriginalTextMarkdown` SHALL retain the exact raw markdown and prose extracted from `PdfPigExtractor` or the web crawler without omission, abbreviation, or AI rewording.
2. **AI Enrichment Scope:** The AI slice formatter (`GeminiAiService.FormatSliceAsync`) SHALL strictly populate auxiliary fields:
   - `DocumentChunk.SummaryMarkdown`: Structured executive summary and core conceptual takeaways.
   - `DocumentChunk.KeyTakeaways`: Exactly 3 high-impact engineering habits or principles.
   - `DocumentChunk.EstimatedReadMinutes`: Calculated reading time.
   - `InterviewQuestions`: Senior Scenario Drill evaluating practical trade-offs.
3. **Reader Surface Fidelity:** The client reader interface (`/read/[bookId]`) SHALL render the author's authentic prose directly from `OriginalTextMarkdown`, ensuring readers engage with the author's true words, narratives, and tone.

#### Scenario: Background PDF ingestion curates slice for Engineering Craft book
- **WHEN** `PdfIngestionWorker` curates an extracted chapter slice for a book with `Category == Category.EngineeringCraft`
- **THEN** the worker invokes `aiFormatter.FormatSliceAsync` to generate summaries, takeaways, and scenario drills
- **AND** `chunk.OriginalTextMarkdown` is NOT overwritten by `aiResult.Value.FormattedMarkdown`
- **AND** `chunk.OriginalTextMarkdown` remains 100% identical to the verbatim text extracted by `PdfPigExtractor`
- **AND** `chunk.SummaryMarkdown`, `chunk.KeyTakeaways`, and `InterviewQuestions` are populated successfully.

#### Scenario: On-demand slice curation preserves original book prose
- **WHEN** an authenticated user or worker triggers `CurateSliceHandler` for a slice in an imported book
- **THEN** the handler preserves `chunk.OriginalTextMarkdown` verbatim
- **AND** updates only `chunk.SummaryMarkdown`, `chunk.KeyTakeaways`, `chunk.EstimatedReadMinutes`, and `InterviewQuestions` without truncating or rewriting the author's narrative prose.

#### Scenario: Reader interface renders author's true words for imported book
- **WHEN** an authenticated user opens `/read/[bookId]` for an Engineering Craft book (e.g. *Thói quen nguyên tử*)
- **THEN** the reader renders `chunk.OriginalTextMarkdown` containing the author's complete, uncompressed narrative prose
- **AND** the reader does NOT render an AI-condensed summary in place of the author's words.

#### Scenario: End-to-end local validation with Vietnamese Atomic Habits PDF fixture
- **WHEN** the 112-page Vietnamese *Atomic Habits* PDF fixture (`/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf`) is processed through the local ingestion pipeline
- **THEN** `PdfPigExtractor` segments the book into chapter chunks (including introduction, personal story, and habit laws)
- **AND** every generated `DocumentChunk.OriginalTextMarkdown` retains 100% verbatim author text across all chapters without AI compression.
