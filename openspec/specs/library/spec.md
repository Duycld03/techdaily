# Library Specification

## Purpose
Provides technical document ingestion capabilities including markdown series creation, memory-efficient PDF streaming extraction up to 200MB/800 pages, and web documentation URL crawling with clean markdown conversion.

## Requirements

### Requirement: PDF File Ingestion
The library system SHALL support PDF uploads up to 350 MB and multi-thousand page documents without blocking HTTP request execution or loading entire files into the Large Object Heap (LOH). The system SHALL stream uploaded files directly to temporary disk storage (`bufferSize = 80KB`), persist a `DocumentBook` record with status `Processing`, and enqueue processing into an in-memory background worker queue via `System.Threading.Channels`.
The background ingestion pipeline SHALL execute a two-tier curation strategy using Gemini Flash Lite:
1. **Tier 1 (Instant Availability):** Extract bookmarks, generate chunk shells, and immediately invoke AI formatting for the initial 3 slices. Mark `DocumentBook.Status = Ready` within 15 seconds so users can immediately start Day 1 study.
2. **Tier 2 (Paced Background Queue):** Progressively format remaining slices (Slices 4..N) with rate-limit pacing (1.2s delay between AI calls), updating `ProgressPercentage` ($10\% \to 100\%$) and `StatusMessage` (e.g. `AI is curating slice 45/230 (20%)...`) after each slice.

#### Scenario: User uploads a valid PDF document
- **WHEN** authenticated user submits `POST /api/v1/library/upload-pdf` with a valid PDF file <= 350 MB
- **THEN** server streams file to temporary storage without contiguous in-memory allocation, creates `DocumentBook` with `ProcessingStatus = Processing` and `ProgressPercentage = 0`, and returns `HTTP 202 Accepted` within 2 seconds.

#### Scenario: Background Worker processes PDF book
- **WHEN** background ingestion worker dequeues a PDF ingestion task
- **THEN** worker extracts text and chapter structure, stores sequential `DocumentChunk` entities, updates `ProgressPercentage` and `StatusMessage` periodically in the database, and transitions status to `Ready` upon completion.

#### Scenario: Uploaded PDF exceeds page limit or file size limit
- **WHEN** user uploads a PDF file exceeding 10,000 pages or 350 MB
- **THEN** system returns `400 Bad Request` with an error message detailing the safety boundary.

#### Scenario: Uploaded PDF is corrupted or encrypted
- **WHEN** user uploads a corrupted or password-protected PDF
- **THEN** system returns `400 Bad Request` or background worker records `ProcessingStatus = Failed` with descriptive `ErrorMessage`, and cleans up temporary disk files safely.

#### Scenario: Book uploaded and initial slices curated
- **WHEN** user uploads a technical PDF book
- **THEN** Tier 1 extracts outline and formats Slices 1–3 with AI, transitions book status to `Ready`, and immediately enables reading for early chapters.

#### Scenario: Background worker formats subsequent slices
- **WHEN** book enters Tier 2 background curation
- **THEN** the worker iterates through remaining slices, calling the AI markdown formatter with rate limiting, saving formatted Markdown to `DocumentChunk.OriginalTextMarkdown` and `SummaryMarkdown`, and updating `DocumentBook.ProgressPercentage`.

---

### Requirement: Web URL Document Crawler & Preview
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/crawl-url` to extract documentation content from web URLs or raw markdown links, stripping extraneous navigation, scripts, and ads into clean Markdown.

#### Scenario: User crawls documentation URL
- **WHEN** authenticated user submits `POST /api/v1/library/crawl-url` with a valid web URL
- **THEN** system fetches content, parses main article body into markdown, extracts title, and returns preview DTO with word count estimate.

#### Scenario: User crawls raw Markdown URL
- **WHEN** user provides a URL pointing to raw Markdown (e.g. GitHub raw file)
- **THEN** system retrieves raw text directly without HTML parsing overhead.

---

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and URL Crawler with content preview before ingestion confirmation.

#### Scenario: User switches import modal tabs
- **WHEN** user opens import modal on `/library` and selects "PDF Upload" tab
- **THEN** UI displays drag-and-drop dropzone with file size limit guidance and category selector.

#### Scenario: User crawls URL and previews content in modal
- **WHEN** user inputs URL in "URL Crawler" tab and clicks "Fetch Content"
- **THEN** UI displays title, category, and markdown preview before user confirms final import.

---

### Requirement: Internationalization (i18n)
All new modal tabs, dropzones, upload limits, crawl buttons, loading states, and error alerts SHALL have full `en` and `vi` translations in locale files.

#### Scenario: Vietnamese user views import modal
- **WHEN** user with Vietnamese locale opens import modal
- **THEN** all tab labels, upload instructions, and button labels render in Vietnamese.

### Requirement: Native PDF Bookmarks & Chapter-Aware Structuring
The PDF extractor SHALL preserve all outline bookmarks with distinct destination pages rather than truncating bookmarks at an arbitrary hierarchy depth level. Topics and sections with unique target pages MUST each form an independent, consumable reading slice (~5–10 pages). The extractor SHALL automatically detect and exclude front-matter (e.g. Table of Contents, Cover, Copyright, Preface) and back-matter (e.g. Index, Bibliography, References, Colophon, Contributors).

#### Scenario: Book with native PDF Bookmarks
- **WHEN** PDF contains a valid Bookmarks / Outline tree
- **THEN** extractor segments slices aligned to all bookmarks pointing to unique destination pages, naming each slice after the author's official chapter/section title.

#### Scenario: Fallback for books without native Bookmarks
- **WHEN** PDF lacks an embedded Bookmarks tree
- **THEN** extractor falls back gracefully to visual heading heuristics (font size grouping, chapter regex) capped by sensible page and word thresholds.

#### Scenario: PDF with nested outline bookmarks across multiple levels
- **WHEN** a PDF contains outline bookmarks distributed across Level 1, Level 2, and Level 3
- **THEN** the extractor preserves every bookmark that points to a unique destination page, preventing multi-thousand page sections from collapsing into single monster slices.

#### Scenario: Slices exceeding maximum word thresholds
- **WHEN** an individual bookmark section contains more than 4,000 words
- **THEN** the extractor applies a safety split only at natural `##` or `###` headings outside code fences, preserving complete code blocks and prose continuity.

#### Scenario: PDF contains introductory TOC and closing index bookmarks
- **WHEN** the PDF contains bookmarks matching blacklisted front-matter or back-matter terms
- **THEN** the extractor discards these items from slice generation, ensuring Slice 1 begins immediately with substantive chapter content.

---

### Requirement: Ingestion Progress Polling API
The library system SHALL provide a lightweight endpoint `GET /api/v1/library/books/{id}/status` returning the current `ProcessingStatus`, `ProgressPercentage` (0–100), `StatusMessage`, and `TotalChunks`.

#### Scenario: Client monitors ingestion progress
- **WHEN** client polls `GET /api/v1/library/books/{id}/status` while book is processing
- **THEN** server returns status code, percentage, and current step message without querying heavy chunk text.

### Requirement: In-Flight Slice Curation Deduplication

The library store SHALL maintain an active in-flight request map for slice curation calls indexed by `${bookId}:${chunkOrder}`. When `curateSlice(bookId, order)` is invoked while an identical request is already pending, the store SHALL return the existing active `Promise<ChunkSummary | null>` rather than dispatching a duplicate HTTP request to the backend.

#### Scenario: User navigates to a slice that is already undergoing background prefetch

- **GIVEN** slice 9 of a book is actively being prefetched by the reader lookahead service
- **WHEN** user clicks directly on slice 9 in the table of contents
- **THEN** `libraryStore.curateSlice` returns the in-flight prefetch promise
- **AND** zero additional HTTP POST requests are dispatched for slice 9.

#### Scenario: In-flight promise completes or fails

- **WHEN** an in-flight slice curation promise resolves or rejects
- **THEN** the store removes the `${bookId}:${chunkOrder}` key from its active map so subsequent calls can fetch fresh state if needed.

---

### Requirement: Keyed Concurrency Locking in Slice Curation API

The backend `CurateSliceHandler` SHALL serialize concurrent execution for the same `(BookId, ChunkOrder)` using a keyed lock. Upon acquiring the lock, the handler SHALL re-evaluate `chunk.IsAiFormatted` (double-checked locking). If another concurrent request has already completed curation and saved the result, the handler SHALL immediately return the formatted slice without invoking the external AI formatting service.

#### Scenario: Concurrent curation requests arrive at the backend

- **GIVEN** two concurrent requests arrive for `POST /api/v1/library/books/{bookId}/slices/{order}/curate`
- **WHEN** the first request acquires the lock and calls Gemini AI formatting
- **THEN** the second request waits on the lock
- **AND** once the first request commits the formatted slice to the database, the second request acquires the lock, detects `chunk.IsAiFormatted == true`, and returns the existing entity without calling Gemini AI.

#### Scenario: CancellationToken cancellation during lock wait

- **WHEN** a client disconnects while waiting for the slice lock
- **THEN** the handler releases any acquired resources and propagates `OperationCanceledException` cleanly without corrupting the lock dictionary.

### Requirement: Lean 3-Slice Initial Ingestion
The system SHALL only format the first 3 slices of an uploaded document during initial background ingestion. Slices 4..N SHALL remain stored with raw markdown until accessed by a reader.

#### Scenario: Initial document upload completes in under 10 seconds
- **GIVEN** a user uploads a PDF book with 20 chapters
- **WHEN** `PdfIngestionWorker` processes the ingestion job
- **THEN** it formats Slices 1, 2, and 3 using the all-in-one AI prompt
- **AND** it marks `DocumentBook.Status = Ready`, `ProgressPercentage = 100`, and `StatusMessage = "Ready for reading"`
- **AND** it stops processing without running an eager background loop on Slices 4..20.

### Requirement: Clean Library Card Presentation
The `/library` page SHALL display book status as `Ready` without rendering an ongoing background curation progress bar.

#### Scenario: User views library card
- **GIVEN** a book that has completed Tier 1 initial ingestion
- **WHEN** the user views the book card on `/library`
- **THEN** the card displays a `Ready` badge and the user's reading milestone (e.g. `Resumes at Slice X` or slice count)
- **AND** no pulsing background progress bar is shown.

### Requirement: Ingestion Progress Reporting & UI Status Indication
The library system SHALL expose real-time curation progress through `GET /api/v1/library/books/{id}/status`, and the Library UI (`library.vue`) SHALL render a dynamic progress bar and active step message while Tier 2 AI curation is ongoing.

#### Scenario: User views library while book is being curated by AI
- **WHEN** user views `/library` and a book is undergoing Tier 2 background formatting (`ProgressPercentage < 100`)
- **THEN** the book card displays an active progress bar with percentage indicator and descriptive status message (`AI is curating slice X/Y...`).

### Requirement: Lightweight Book Details and Single Slice Retrieval
The library API SHALL provide book details with lightweight chunk summaries for Table of Contents rendering via `GET /api/v1/library/books/{id}` (omitting full markdown from chunk lists), and provide a dedicated endpoint `GET /api/v1/library/books/{id}/slices/{chunkOrder}` to retrieve the complete markdown, takeaways, and quiz for a specific slice.

#### Scenario: Client requests book details for reader
- **WHEN** client requests `GET /api/v1/library/books/{id}`
- **THEN** response contains book metadata and an array of chunk summaries containing IDs, titles, chunk orders, and reading times, without heavy markdown content.

#### Scenario: Client requests a specific slice
- **WHEN** client requests `GET /api/v1/library/books/{id}/slices/{chunkOrder}`
- **THEN** response contains the full `originalTextMarkdown`, `summaryMarkdown`, `keyTakeaways`, and `microQuiz` for that slice.

#### Scenario: Client requests a non-existent slice
- **WHEN** client requests `GET /api/v1/library/books/{id}/slices/{chunkOrder}` with an invalid slice order or book ID
- **THEN** server returns HTTP 404 Not Found.
