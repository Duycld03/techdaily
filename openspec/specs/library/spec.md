# Library Specification

## Purpose
Provides technical document ingestion capabilities including markdown series creation, memory-efficient PDF streaming extraction up to 200MB/800 pages, and web documentation URL crawling with clean markdown conversion.

## Requirements

### Requirement: PDF File Ingestion
The library system SHALL support PDF uploads up to 350 MB and multi-thousand page documents without blocking HTTP request execution or loading entire files into the Large Object Heap (LOH). The system SHALL stream uploaded files directly to backend-managed source storage (`bufferSize = 80KB`), persist a `DocumentBook` record with status `Processing`, and durably admit an ingestion attempt before dispatching background work. In-memory worker notification SHALL NOT be the sole record of accepted work.
The background ingestion pipeline SHALL use the existing lean initial-curation strategy:
1. **Tier 1 (Instant Availability):** Extract bookmarks, generate chunk shells, and invoke the existing initial formatting for at most the first 3 slices. Mark `DocumentBook.Status = Ready` under the existing personal readability criteria so users can start study without formatting every remaining slice.
2. **Tier 2 (On-Demand Curation):** Slices 4..N SHALL retain readable source content until requested through existing reader curation. The pipeline SHALL NOT introduce an eager background loop for every remaining slice.
Validated sources from failed or interrupted processing SHALL follow the document-ingestion retention and quota contract. Successful inputs and invalid uploads SHALL be deleted immediately; retained input SHALL never become an administrator download.

#### Scenario: User uploads a valid PDF document
- **WHEN** authenticated user submits `POST /api/v1/library/upload-pdf` with a valid PDF file <= 350 MB
- **THEN** server streams the file without contiguous in-memory allocation, creates `DocumentBook` with `ProcessingStatus = Processing` and `ProgressPercentage = 0`, and durably admits its attempt
- **AND** it returns `HTTP 202 Accepted` within 2 seconds after validated input receipt and admission, with the existing book response and additive attempt identity.

#### Scenario: Background Worker processes PDF book
- **WHEN** background ingestion worker claims an accepted PDF ingestion attempt
- **THEN** worker extracts text and chapter structure, stores sequential `DocumentChunk` entities, updates `ProgressPercentage` and safe status information periodically, and transitions status to `Ready` upon completion
- **AND** duplicate delivery cannot append another copy of the slices.

#### Scenario: Uploaded PDF exceeds page limit or file size limit
- **WHEN** user uploads a PDF file exceeding 10,000 pages or 350 MB
- **THEN** system returns `400 Bad Request` for a rejected request or records the background page-limit failure with the same safety boundary
- **AND** invalid unaccepted source bytes are cleaned up.

#### Scenario: Uploaded PDF is corrupted or encrypted
- **WHEN** user uploads a corrupted or password-protected PDF
- **THEN** system returns `400 Bad Request` for rejected input or the worker records `ProcessingStatus = Failed` with a safe error code
- **AND** validated failed input is retained only within the bounded recovery policy; invalid or unusable staging is cleaned up.

#### Scenario: Book uploaded and initial slices curated
- **WHEN** user uploads a technical PDF book
- **THEN** Tier 1 extracts outline and performs existing initial formatting for Slices 1 through min(3, slice count), transitions the readable book to `Ready`, and enables reading
- **AND** original text and the selected document language remain protected by existing invariants.

#### Scenario: Background worker formats subsequent slices
- **WHEN** a reader explicitly requests an unformatted slice after the initial ingestion has completed
- **THEN** existing on-demand curation handles that slice
- **AND** initial ingestion does not start an eager formatting loop or expose an administrator regeneration action.

#### Scenario: Initial import interrupted before delivery
- **WHEN** a process restarts after PDF acceptance but before worker delivery
- **THEN** persisted queued work resumes under the same book and attempt IDs
- **AND** no second book is created.
---

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
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and Web Article URL Ingestion with content preview before ingestion confirmation.

All three tabs SHALL load active category descriptors from the shared category endpoint, ordered by display order and slug, and require an explicit `categoryId` UUID selection. No category SHALL be preselected or inferred from source keywords. The accessible selector SHALL show stored English or Vietnamese names for the active UI locale. Empty or unavailable metadata SHALL block submission with a localized, retryable explanation.

A category-independent helper SHALL explain that source prose is preserved while structural headings and paragraph boundaries can be restored during curation. Changing a category SHALL NOT change this preservation policy.

1. **De-Jargonized Web Article Ingestion (Tab 3):**
   - Title SHALL read "Extract Article from Web" / "Trích Xuất Bài Viết Từ Web" (replacing legacy "Cào & Chuyển Đổi").
   - Action button SHALL read "Fetch Content" / "Lấy Nội Dung" (replacing legacy "Cào Dữ Liệu").
   - URL input placeholder SHALL provide broad lifelong learning and deliberate practice examples: `"https://... (Substack, Medium, Farnam Street, blog cá nhân hoặc tài liệu)"` / `"https://... (Substack, Medium, Farnam Street, blogs, or documentation)"`.
2. **Universal Markdown & Notes Placeholder (Tab 1):**
   - Title placeholder SHALL provide diverse examples encompassing both technical and deliberate practice books: `"Ví dụ: Atomic Habits — Chương 1 hoặc Designing Data-Intensive Applications — Chương 5"`.
   - Content placeholder SHALL invite pasting notes, excerpts, or markdown text naturally.

When the URL Crawler detects an embedded PDF (`IsPdfDetected == true`), the modal SHALL render an Embedded PDF Detection Preview Card:
- Highlighting the detected document title and resolved remote PDF URL.
- Displaying a dedicated one-click action button: "Import & Slice PDF Directly" (`$t('library.import_detected_pdf')`).
- Suppressing the empty markdown preview editor and directing the user directly toward automated streaming ingestion.

#### Scenario: User selects DeepPace category in any import tab
- **GIVEN** an authenticated user opens the document import modal on `/library`
- **WHEN** user opens the Category dropdown on the Markdown, PDF, or Web URL tab
- **THEN** the dropdown contains all active categories, including administrator-created subjects, in the current locale
- **AND** selecting a category sends its UUID as `categoryId`, never an ordinal or enum name.

#### Scenario: User inputs article URL with universal web placeholder
- **GIVEN** user views the Web URL ingestion tab in the import modal
- **WHEN** the tab is rendered
- **THEN** the input displays a friendly placeholder referencing Substack, Medium, Farnam Street, blogs, and documentation
- **AND** the action button displays "Lấy Nội Dung" (Vietnamese) or "Fetch Content" (English) without scraping jargon.

#### Scenario: Verbatim hint dynamically guides mindset and habits curation
- **GIVEN** user views the Category selector in any of the 3 import tabs
- **WHEN** viewing the callout banner
- **THEN** the callout explains preservation of author prose and faithful structural restoration for every category, without recommending a category to change formatting behavior.

#### Scenario: URL crawler preview reveals detected embedded PDF reader
- **WHEN** user crawls a URL and the backend responds with `IsPdfDetected = true` and a valid `DetectedPdfUrl`
- **THEN** the modal displays an embedded PDF alert card with the resolved PDF URL and a prominent "Import & Slice PDF Directly" button
- **AND** clicking the button invokes `POST /api/v1/library/import-remote-pdf` with the detected URL, closes the modal, and initiates ingestion progress polling on the library view.

### Requirement: Internationalization (i18n)
All new modal tabs, dropzones, upload limits, crawl buttons, loading states, error alerts, input placeholders, technical service notes, and book card badges SHALL have full `en` and `vi` translations in locale files.

The document import modal SHALL display localized placeholders and service notices across all tabs:
1. **Markdown Tab Content Placeholder:** The content textarea SHALL bind to `$t('library.content_placeholder')`.
2. **PDF Upload Tab Replace Hint:** The dropzone replace guidance SHALL bind to `$t('library.pdf_replace_hint')`.
3. **PDF Upload Tab Title Placeholder:** The optional title input SHALL bind to `$t('library.title_placeholder')`.
4. **PDF Upload Tab Architecture Notes:** Technical service badges SHALL bind to `$t('library.pdf_service_note_1')` ("300 MB Streaming • Background Service" / "Xử lý luồng 300 MB • Dịch vụ chạy ngầm") and `$t('library.pdf_service_note_2')` ("Look-Ahead Buffer Synthesis" / "Tổng hợp bộ đệm dự đoán trước").

#### Scenario: Vietnamese user views import modal
- **GIVEN** an authenticated user has selected the Vietnamese (`vi`) locale
- **WHEN** user opens the document import modal on `/library`
- **THEN** all tab labels, upload instructions, button labels, input placeholders, and technical service notes render in Vietnamese without raw English text.

#### Scenario: English user views import modal
- **GIVEN** an authenticated user has selected the English (`en`) locale
- **WHEN** user opens the document import modal on `/library`
- **THEN** all tab labels, upload instructions, button labels, input placeholders, and technical service notes render in English.

---

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
The library system SHALL provide an owner-authorized lightweight endpoint `GET /api/v1/library/books/{id}/status` returning the current `ProcessingStatus`, `ProgressPercentage` (0–100), safe `StatusMessage`, and `TotalChunks`. It SHALL add latest attempt identity/state, source availability/expiry, control version and safe recovery guidance without loading source or chunk text. Other users and unowned legacy targets SHALL return 404.

#### Scenario: Client monitors ingestion progress
- **WHEN** client polls `GET /api/v1/library/books/{id}/status` while their book is processing
- **THEN** server returns status code, percentage, safe current step and attempt/source recovery metadata without querying heavy chunk text.

#### Scenario: Owner needs to supply source again
- **WHEN** an owner loads status for a failed import with missing or expired input
- **THEN** the response identifies source availability and permitted replacement under the current control version
- **AND** it does not expose a backend path or retained source download.

#### Scenario: Other account or administrator uses personal status API
- **WHEN** a caller requests another account's book status through the learner API
- **THEN** the response is 404 regardless of administrator designation
- **AND** authorized admin metadata inspection uses the separate operations API.

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
The `/library` page SHALL serve as the technical document catalog, displaying books, bookmarks, and curation states adhering to the **Dev-Learning Studio** visual theme and standardized 2-tier catalog header architecture:

1. **Authenticated Route Guard Invariant**:
   - Access to `/library` and `/read/*` SHALL strictly require an active authenticated user session.
   - Unauthenticated visitors navigating to `/library` or `/read/[bookId]` SHALL be immediately redirected to `/login` with the original requested destination encoded in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
   - Authenticated sessions with valid, unexpired JWT tokens SHALL be granted immediate access.

2. **Standardized 2-Tier Header & Obsidian Studio Controls**:
   - The `/library` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) with `max-w-7xl mx-auto space-y-4`.
   - **Row 1 (Header Top Bar)**:
     - **Left**: Icon badge (`w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0`) containing `<BookOpen class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`, primary title (`text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`), and subtitle (`text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`).
     - **Right**: Search input (`w-72 sm:w-80 h-10 rounded-xl`) with shortcut hint (`⌘K`) alongside the "+ Thêm Tài Liệu" primary action button (`h-10 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold shadow-md shadow-brand-500/20`).
   - **Row 2 (Full-Width Category Filter Bar)**:
     - Category filter pills SHALL render on their own full-width horizontal row (`flex flex-wrap items-center gap-2 w-full py-0.5`) with glass styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`), highlighting active category pills with Iris Violet styling (`bg-slate-100 dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 border-slate-300 dark:border-white/[0.12]`).

3. **Glass Card Document Catalog**:
   - Document cards in the library grid SHALL render as `.glass-card` containers with translucent hairline borders (`border-white/[0.08]`), elevating on hover (`hover:border-white/[0.16]`).
   - In-progress reading progress bars SHALL use Iris Violet gradient fills (`bg-gradient-to-r from-brand-600 to-brand-500`) over subtle dark track backgrounds (`dark:bg-canvas-elevated`).
   - Bookmark resume badges SHALL display translucent brand accents (`bg-brand-500/10 text-brand-400 border border-brand-500/20`).
   - Ready to read status badges SHALL render as neutral studio badges (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400` with `BookOpen` icon), eliminating out-of-place emerald green styling to preserve semantic color hierarchy.
   - Action buttons on book cards (Resume, Read, Delete, Export) SHALL use glass styling without legacy hardcoded background colors.

4. **Import Modal Modernization**:
   - The 3-tab import modal (Markdown Series, PDF Upload, Web Crawler) SHALL render using `.glass-panel` elevation with a backdrop blur overlay (`bg-black/60 backdrop-blur-sm`).
   - Drag-and-drop PDF dropzone SHALL use subtle dark glass styling with active drag state highlighted in Iris Violet (`border-brand-500 bg-brand-500/5`).
   - Modal action buttons and confirmation dialogs SHALL adhere to Dev-Learning Studio tokens.

5. **Status & Curation Clarity**:
   - The `/library` page SHALL display book status as `Ready` without rendering an ongoing background curation progress bar once initial slices are curated.

#### Scenario: User views library card
- **WHEN** user views book list on `/library`
- **THEN** ready books display "Ready" status and clean metadata
- **AND** cards render as `.glass-card` elements over a dark canvas with hairline borders
- **AND** do not display the ongoing background curation progress bar once initial slices are ready.

#### Scenario: User views unread document card status badge
- **WHEN** user views a ready document card on `/library` that has no active bookmark
- **THEN** the card displays a neutral studio badge ("Ready to Read" / "Sẵn sàng đọc") with `BookOpen` icon
- **AND** the badge renders with neutral background (`bg-slate-100 dark:bg-canvas-subtle`), hairline border (`border-slate-200/80 dark:border-white/[0.08]`), and muted text (`text-slate-600 dark:text-slate-400`)
- **AND** no green or emerald styling (`bg-emerald-50`, `text-emerald-700`, `border-emerald-200`) is applied to the status badge.

#### Scenario: Unauthenticated visitor attempts to access library
- **WHEN** an unauthenticated visitor navigates to `/library`
- **THEN** the system redirects to `/login?redirect=%2Flibrary`
- **AND** the user cannot access book import, deletion, or catalog management until authenticated.

#### Scenario: User views standardized library header on desktop
- **WHEN** user opens `/library` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** Row 1 renders the icon badge (`w-10 h-10 rounded-2xl`), bold title (`text-xl sm:text-2xl font-black`), search input, and "+ Thêm Tài Liệu" button aligned in the top bar
- **AND** Row 2 renders the category filter pills spanning the full container width.

#### Scenario: Unauthenticated visitor attempts to access reader view
- **WHEN** an unauthenticated visitor navigates to `/read/book-123`
- **THEN** the system redirects to `/login?redirect=%2Fread%2Fbook-123`
- **AND** reading progress and bookmarks are protected behind authentication.

### Requirement: Ingestion Progress Reporting & UI Status Indication
The library system SHALL expose real-time curation progress through `GET /api/v1/library/books/{id}/status`, and the Library UI (`library.vue`) SHALL render a dynamic progress bar and active step message while Tier 2 AI curation is ongoing.

#### Scenario: User views library while book is being curated by AI
- **WHEN** user views `/library` and a book is undergoing Tier 2 background formatting (`ProgressPercentage < 100`)
- **THEN** the book card displays an active progress bar with percentage indicator and descriptive status message (`AI is curating slice X/Y...`).

### Requirement: Lightweight Book Details and Single Slice Retrieval
The library API SHALL provide authenticated endpoints for book details and slice reading:
1. `GET /api/v1/library/books/{id}`: Returns book metadata and an array of chunk summaries (omitting full markdown).
2. `GET /api/v1/library/books/{id}/status`: Returns ingestion progress (`ProcessingStatus`, `ProgressPercentage`, `StatusMessage`, `TotalChunks`).
3. `GET /api/v1/library/books/{id}/slices/{chunkOrder}`: Returns the complete markdown, takeaways, and quiz for a specific slice.

Each chunk summary and single-slice response SHALL include the slice's content `language` (a short code such as `en` or `vi`, defaulting to `en` only when unknown), so that language-dependent reader features — such as on-device narration voice selection — can act on the document's language without issuing a separate request.

All of the above endpoints SHALL enforce `.RequireAuthorization()` and reject unauthenticated requests with `HTTP 401 Unauthorized`.

#### Scenario: Client requests book details for reader
- **WHEN** client requests `GET /api/v1/library/books/{id}` with JWT authorization header
- **THEN** response contains book metadata and an array of chunk summaries containing IDs, titles, chunk orders, reading times, and each slice's `language`, without heavy markdown content.

#### Scenario: Client requests a specific slice
- **WHEN** client requests `GET /api/v1/library/books/{id}/slices/{chunkOrder}` with JWT authorization header
- **THEN** response contains the full `originalTextMarkdown`, `summaryMarkdown`, `keyTakeaways`, `microQuiz`, and the slice's `language` for that slice.

#### Scenario: Slice language reflects the document's ingested language
- **WHEN** a book was ingested with content language `vi` and the client requests its book details or any of its slices
- **THEN** the returned chunk summary and slice carry `language == "vi"`, and the value is not silently replaced by the `en` default.

#### Scenario: Client requests a non-existent slice
- **WHEN** client requests `GET /api/v1/library/books/{id}/slices/{chunkOrder}` with an invalid slice order or book ID
- **THEN** server returns HTTP 404 Not Found.

#### Scenario: Unauthenticated request to book details or slice is rejected
- **WHEN** client sends a request to `GET /api/v1/library/books/{id}` or `GET /api/v1/library/books/{id}/slices/{order}` without an authorization token
- **THEN** server returns HTTP 401 Unauthorized.

### Requirement: Paginated Book Catalog Browsing and State Synchronization
The library system SHALL expose an authenticated endpoint `GET /api/v1/library/books` accepting optional query parameters: `categoryId` (UUID), `search` (string), `page` (integer, default 1, minimum 1), and `pageSize` (integer, default 12, minimum 1, maximum 100). The endpoint SHALL enforce `.RequireAuthorization()` and reject unauthenticated requests with `HTTP 401 Unauthorized`.

When authorized, the endpoint SHALL return a structured envelope containing:
1. `books`: A list of `BookDto` items matching filter criteria, ordered by `CreatedAt` descending.
2. `totalCount`: Total number of books matching the query across the entire catalog.
3. `page`: The current active page number (1-based).
4. `pageSize`: The page size applied to the query.
5. `totalPages`: The total number of pages calculated as $\lceil \text{totalCount} / \text{pageSize} \rceil$, or 0 if `totalCount` is 0.

The `/library` page interface SHALL render book cards in a responsive grid aligned to multiples of 12 (1 column on mobile, 2 columns on tablet, 3 columns on desktop, 4 columns on large monitors), preventing ragged trailing rows.

The `/library` page SHALL render numbered pagination controls (`< 1 2 3 ... 8 >`) when `totalPages > 1`:
1. **Numbered Page Buttons**: Direct access buttons for available pages with an active highlight indicator on the currently viewed page.
2. **Ellipsis Compaction**: Pages beyond the visible window SHALL be truncated with non-clickable ellipsis (`...`) indicators.
3. **Previous / Next Controls**: Navigational buttons to decrement or increment the active page, automatically disabled on boundary pages (`page === 1` and `page === totalPages`).
4. **Filter Reset**: Applying a new category filter or entering a search query SHALL automatically reset the active page to 1.
5. **Two-Way URL Query Synchronization**: The active `page`, `categoryId`, and `search` query SHALL synchronize bidirectionally with browser URL query parameters (`?page=N&categoryId=UUID&search=S`). Reloading the page or sharing the URL SHALL restore the exact catalog page and filter state.

#### Scenario: User browses the first page of the technical library
- **WHEN** an authenticated user navigates to `/library`
- **THEN** client calls `GET /api/v1/library/books?page=1&pageSize=12` with JWT authorization header
- **AND** backend returns up to 12 book cards along with `totalCount`, `page = 1`, `pageSize = 12`, and `totalPages`
- **AND** client renders the books in a balanced responsive grid with page 1 highlighted in the pagination controls.

#### Scenario: Anonymous request to book catalog is rejected
- **WHEN** an unauthenticated client sends `GET /api/v1/library/books` without a valid JWT bearer token
- **THEN** backend rejects the request with `HTTP 401 Unauthorized`.
#### Scenario: User navigates to a subsequent catalog page
- **GIVEN** the library catalog has 30 books (`totalPages = 3`)
- **WHEN** the user clicks page number "2" in the pagination bar
- **THEN** client updates the URL query string to `?page=2`
- **AND** fetches books via `GET /api/v1/library/books?page=2&pageSize=12`
- **AND** smoothly replaces the grid items with books 13 through 24 and highlights button "2".

#### Scenario: User filters catalog by category or search term
- **GIVEN** the user is currently viewing page 3 of the catalog (`?page=3`)
- **WHEN** the user selects the administrator-defined Backend category or enters search term "architecture"
- **THEN** client resets the active page to 1
- **AND** updates URL query string to `?categoryId=<backend-category-id>&search=architecture&page=1`
- **AND** requests page 1 of the filtered result set from the backend.

#### Scenario: User enters direct URL with pagination and filter parameters
- **WHEN** a user navigates directly to `/library?categoryId=<database-category-id>&search=postgres&page=2` via bookmark or shared link
- **THEN** client parses `categoryId = the selected category UUID`, `search = 'postgres'`, and `page = 2` from the route query
- **AND** dispatches `GET /api/v1/library/books?categoryId=<database-category-id>&search=postgres&page=2&pageSize=12`
- **AND** renders the second page of matching database books with active filter states displayed.

#### Scenario: User navigates using browser back and forward history
- **GIVEN** the user navigated from page 1 to page 2 and then page 3 in the library
- **WHEN** the user clicks the browser "Back" button
- **THEN** client responds to the URL query change to `?page=2`
- **AND** fetches and displays page 2 items without a full page reload or layout flickering.

#### Scenario: User requests an out-of-bounds page number
- **WHEN** a user manually enters a URL with `?page=999` exceeding `totalPages`
- **THEN** backend returns an empty `books` list with the accurate `totalCount` and `totalPages`
- **AND** client displays a friendly empty state prompting the user to return to page 1.

#### Scenario: Mobile viewport renders compact responsive pagination controls
- **GIVEN** the user accesses `/library` on a narrow mobile viewport ($< 640\text{px}$)
- **WHEN** the catalog contains multiple pages
- **THEN** pagination controls condense the numbered buttons to the current page and immediate neighbors while maintaining minimum touch target dimensions ($\ge 40\text{px} \times 40\text{px}$) and touch accessibility.

#### Scenario: Retained inactive category filter
- **WHEN** a user opens a valid `categoryId` filter for an inactive category
- **THEN** the owned documents remain filterable and the category label is resolved from shared metadata
- **AND** an unknown or malformed category reference is rejected without falling back to another category.

### Requirement: Consistent Book Card Status Badge Baseline Alignment
The library catalog on `/library` SHALL align the bookmark resume badge, ready badge, and in-progress ingestion status indicator to a consistent bottom baseline across all book cards.

Alignment SHALL remain consistent within each row of the library grid, regardless of variable title lengths (1-line vs 2-line) or the presence/absence of the author or source URL subtitle.

1. **Flex-Stretched Card Content Structure:**
   - The book card upper content container SHALL flex-stretch (`flex flex-col flex-1`) to occupy all available vertical space above the card action footer.
   - The category badge, total chunks count, document title (`<h3>`), and optional subtitle (`<p>`) SHALL remain anchored at the top of the upper content area.
2. **Bottom-Anchored Status Container:**
   - The status indicator elements (bookmark resume badge, ready badge, and processing ingestion indicator) SHALL reside within a bottom-anchored container (`mt-auto pt-3`) inside the upper content wrapper.
   - The status container SHALL maintain an identical vertical distance directly above the card footer divider (`pt-4 border-t`) across adjacent cards of differing content heights.
3. **Primary Status and Progress Row:**
   - The book card status area SHALL display the primary ingestion or resume badge (`Ready to Read`, `Resumes at Slice X`, or `Processing`).
   - The card SHALL NOT render duplicate status text (`● Sẵn sàng` / `● Ready to read`) immediately above the progress bar, reserving that row strictly for numeric slice counts and reading completion percentages.

#### Scenario: Book card with 1-line title aligns status badge with 2-line title card
- **WHEN** a book with a short single-line title is displayed next to a book with a two-line title
- **THEN** both cards' bottom-anchored status elements and action rows align horizontally across the row.

#### Scenario: Book card without author or source URL maintains bottom-aligned badge
- **WHEN** a book has no author or source URL metadata
- **THEN** the upper container flexes to fill available vertical space and the status badge remains anchored to the bottom baseline.

#### Scenario: In-progress ingestion indicator aligns with completed and bookmarked cards
- **WHEN** an ingesting book is displayed alongside completed or active reading books
- **THEN** all cards maintain identical outer heights and aligned action footers.

#### Scenario: Book card rendering for unread document
- **WHEN** an unread document card is displayed in the learner library
- **THEN** it renders the top status badge and slice count without duplicating "Ready to read" or "Sẵn sàng" above the progress bar.

### Requirement: Book Card Category Badge and Ingestion Status Localization
The library catalog on `/library` SHALL display localized category badges and localized background ingestion progress indicators on each document card.

1. **Category Badge Resolution:**
   - Each book SHALL expose `categoryId` and its authoritative category descriptor.
   - The badge SHALL use `nameEn` or `nameVi` for the current locale, including inactive categories, without numeric/string enum normalization or a fixed translation map.
   - Renamed categories SHALL display current metadata after refresh without changing book identity, content, or progress.
   - A missing descriptor SHALL produce an explicit unavailable state rather than a fabricated Engineering category.
2. **Dynamic Ingestion Status Translation:**
   - When a book's processing status is `Processing` (or numeric `1`), the book card SHALL render a localized status indicator via a status message translator (`getStatusMessage`).
   - The translator SHALL detect backend progress patterns and map them to localized strings:
     - `"File uploaded, queued for processing..."` $\to$ `$t('library.status_uploaded_queued')`
     - `"Remote PDF download initiated. Processing chapters..."` $\to$ `$t('library.status_remote_download')`
     - `"Analyzing document structure and bookmarks..."` or `"Analyzing PDF structure and bookmarks..."` $\to$ `$t('library.status_analyzing_structure')`
     - `"Persisting chapters and slices..."` $\to$ `$t('library.status_persisting_slices')`
     - `"Parsing pages..."` $\to$ `$t('library.status_parsing_pages')`
     - `"Generating chunks..."` $\to$ `$t('library.status_generating_chunks')`
     - `"AI is curating initial slice {current}/{total}..."` $\to$ `$t('library.status_curating_slice', { current, total })`
     - `"Analyzing content: page {current}/{total}"` $\to$ `$t('library.status_analyzing_pages', { current, total })`
     - `"Extracting topic: {topic}"` $\to$ `$t('library.status_extracting_topic', { topic })`
     - `"Extracted {count} slices. Complete!"` $\to$ `$t('library.status_extracted_complete', { count })`
     - `"Ready for reading"` $\to$ `$t('library.status_ready')`
   - For unrecognized status strings, the translator SHALL fall back to the existing message or `$t('library.processing_pdf')`.

#### Scenario: Book card displays localized category for string enum in Vietnamese
- **GIVEN** an EngineeringCraft string reference has been migrated to a UUID descriptor with `nameVi = "Tư Duy Kỹ Sư & Năng Suất"` and the locale is Vietnamese
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Tư Duy Kỹ Sư & Năng Suất"`
- **AND** the badge does NOT render `'Engineering'`.

#### Scenario: Book card displays localized category for string enum in English
- **GIVEN** a legacy BackendDotNet reference has been migrated to a UUID descriptor with `nameEn = "Backend & Distributed"` and the locale is English
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Backend & Distributed"`.

#### Scenario: Book card displays localized category for numeric ID
- **GIVEN** a former ordinal category reference has been migrated to a UUID descriptor with `nameVi = "Cơ Sở Dữ Liệu"` and the locale is Vietnamese
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Cơ Sở Dữ Liệu"`.

#### Scenario: Book card displays localized status for background queuing
- **GIVEN** a book is in status `Processing` with `statusMessage = "File uploaded, queued for processing..."` and locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the ingestion indicator displays `"Tệp đã tải lên, đang chờ xử lý..."`.

#### Scenario: Book card displays parameterized localized status for slice curation
- **GIVEN** a book is in status `Processing` with `statusMessage = "AI is curating initial slice 3/12..."` and locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the ingestion indicator displays the interpolated string `"AI đang tối ưu hóa lát cắt mở đầu 3/12..."`.

### Requirement: Book Card Action Row Presentation
The book card footer SHALL present primary management and navigation actions without redundant branding or truncated labels.

1. **Removal of Redundant Brand Badge:**
   - The book card footer SHALL NOT render the hardcoded `<span ...>GitBook Reader</span>` element.
   - The action row SHALL cleanly partition secondary actions (Delete button, Export to Obsidian button) on the left and the primary navigation CTA ("Read Slices" / "Continue Reading") on the right.

#### Scenario: Book card footer renders cleanly without truncated brand label
- **GIVEN** any book card rendered in the `/library` grid
- **WHEN** the user inspects the card footer action row
- **THEN** the footer contains the Delete button, the Export to Obsidian button, and the primary reading navigation CTA
- **AND** the truncated text `"GitBook Rea..."` or `"GitBook Reader"` is completely absent from the DOM.

### Requirement: Remote PDF Direct Ingestion Pipeline
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/import-remote-pdf` to stream remote PDF documents directly into the server's PDF ingestion pipeline without requiring the user to download the file locally.

The endpoint handler (`ImportRemotePdfHandler`) SHALL:
1. Validate the remote URL scheme (`http` / `https`) and enforce strict SSRF validation to prevent access to private IP ranges, loopback addresses, or cloud metadata services, including pinned DNS and redirect protection.
2. Initiate a streaming HTTP GET request with `HttpCompletionOption.ResponseHeadersRead` and verify the `Content-Type` header or stream magic bytes start with `%PDF-`.
3. Stream the file directly to backend-managed source storage with an 80KB buffer size, ensuring zero Large Object Heap (LOH) contiguous byte array allocations and enforcing storage quotas.
4. Enforce a maximum file size limit of 350 MB during streaming, aborting with `HTTP 400 Bad Request` if the stream exceeds the limit.
5. Create an owner-scoped `DocumentBook` with `SourceType = PdfBook`, `Category = request.Category`, `AuthorOrSourceUrl = request.PdfUrl`, `Status = ProcessingStatus.Processing`, and `ProgressPercentage = 0` as part of tracked admission.
6. Durably record dispatch for background PDF extraction and the existing initial slice-curation policy. A retained-source retry SHALL use the accepted bytes without automatically fetching the URL again.
7. Return `HTTP 202 Accepted` with the initialized book response and additive attempt identity within 2 seconds after validated download and admission. A post-admission fetch failure SHALL return safe failure details identifying its failed target/attempt.

#### Scenario: User imports remote PDF directly from detected web viewer URL
- **WHEN** authenticated user submits `POST /api/v1/library/import-remote-pdf` with a valid remote PDF URL, title, category, and language
- **THEN** server validates the target, streams the remote PDF into backend-managed storage, registers the owned book and durable ingestion attempt, and returns `HTTP 202 Accepted` with the new book record.

#### Scenario: Remote PDF streaming ingestion enforces SSRF security boundaries
- **WHEN** user submits `POST /api/v1/library/import-remote-pdf` with a URL resolving to `127.0.0.1`, `169.254.169.254`, or a private RFC 1918 subnet
- **THEN** server rejects the request immediately with `HTTP 400 Bad Request` and error code `SSRF_DETECTED`.

#### Scenario: Remote PDF endpoint rejects oversized files or non-PDF streams
- **WHEN** remote URL streams more than 350 MB or does not contain valid PDF header signatures
- **THEN** server aborts the stream, removes rejected staging bytes, releases its reservation and returns `HTTP 400 Bad Request`.

#### Scenario: Remote PDF fails before usable input exists
- **WHEN** an admitted remote import fails before receiving validated PDF bytes
- **THEN** its owned failed document and attempt remain discoverable without exposing raw fetch errors
- **AND** recovery requires explicit validated source resupply.
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

### Requirement: Verbatim Book Text Preservation & Curation Safety
The system SHALL guarantee 100% verbatim text preservation for imported documents in every category, including administrator-created subjects.

The source reading body SHALL NEVER be replaced with an AI summary, condensed prose, or synthetic content. A successful `FormattedMarkdown` result may replace the stored body only as faithful structural restoration under the preservation invariants below.

The system SHALL enforce the following invariants during book ingestion and slice curation:
1. **Verbatim Content Invariance:** The source reading body SHALL retain the author's words, examples, and language without omission, abbreviation, condensation, or AI rewording. During an existing ingestion or explicit curation action, faithful structural restoration MAY adjust Markdown headings, paragraph boundaries, and non-content extraction artifacts as specified by `ai-curation`; raw extraction bytes need not remain identical. Category changes SHALL never trigger this operation.
2. **AI Enrichment Scope:** The AI slice formatter (`GeminiAiService.FormatSliceAsync`) SHALL populate auxiliary fields separately from the faithful source reading body:
   - `DocumentChunk.SummaryMarkdown`: Structured executive summary and core conceptual takeaways.
   - `DocumentChunk.KeyTakeaways`: Exactly 3 high-impact engineering habits or principles.
   - `DocumentChunk.EstimatedReadMinutes`: Calculated reading time.
   - `InterviewQuestions`: Senior Scenario Drill evaluating practical trade-offs.
3. **Reader Surface Fidelity:** The client reader interface (`/read/[bookId]`) SHALL render the author's authentic prose directly from `OriginalTextMarkdown`, ensuring readers engage with the author's true words, narratives, and tone.

#### Scenario: Background PDF ingestion curates slice for Engineering Craft book
- **WHEN** `PdfIngestionWorker` curates an extracted chapter slice for a book in any retained category
- **THEN** the worker invokes the common source-faithful curation flow to restore structure and generate separate summaries, takeaways, and scenario drills
- **AND** the source reading body is never replaced by condensed or synthetic AI content
- **AND** the source reading body preserves the complete extracted author prose while permitting faithful structural Markdown restoration
- **AND** `chunk.SummaryMarkdown`, `chunk.KeyTakeaways`, and `InterviewQuestions` are populated successfully.

#### Scenario: On-demand slice curation preserves original book prose
- **WHEN** an authenticated user or worker triggers `CurateSliceHandler` for a slice in an imported book
- **THEN** the handler preserves the author's prose and source language while persisting faithful structural restoration
- **AND** updates structural Markdown and the separate summary, takeaways, reading-time, and question fields without truncating or rewriting the author's narrative prose.

#### Scenario: Reader interface renders author's true words for imported book
- **WHEN** an authenticated user opens `/read/[bookId]` for an imported book in any category
- **THEN** the reader renders `chunk.OriginalTextMarkdown` containing the author's complete, uncompressed narrative prose
- **AND** the reader does NOT render an AI-condensed summary in place of the author's words.

#### Scenario: End-to-end local validation with Vietnamese Atomic Habits PDF fixture
- **WHEN** a Vietnamese narrative PDF fixture with chapter boundaries, dialogue, and examples is processed through the local ingestion pipeline
- **THEN** `PdfPigExtractor` segments the book into chunks aligned with the source chapter boundaries
- **AND** every generated `DocumentChunk.OriginalTextMarkdown` retains the complete author prose across all chapters without AI compression, allowing only faithful structural restoration.

#### Scenario: Category metadata changes do not curate historical prose
- **WHEN** an administrator renames, reorders, or deactivates a category or an owner reclassifies an existing book
- **THEN** source prose, summaries, audio, and learning progress remain unchanged and no AI formatting is scheduled.

### Requirement: Authenticated AI curation and generation endpoints
The AI-powered endpoints that invoke external generation services SHALL require JWT authentication and per-user rate limiting. Specifically, the slice curation endpoint (`/api/v1/library/books/{id}/slices/{order}/curate`), the term explanation endpoint, and the chunk challenge endpoint SHALL reject unauthenticated requests with `HTTP 401 Unauthorized`. These endpoints SHALL also enforce per-user rate limits to protect external AI provider quota from abuse by individual authenticated users.

#### Scenario: Unauthenticated request to curate a slice
- **WHEN** an unauthenticated client calls `GET /api/v1/library/books/{id}/slices/{order}/curate`
- **THEN** the system returns `HTTP 401 Unauthorized`

#### Scenario: Authenticated request to curate a slice
- **WHEN** an authenticated user calls `GET /api/v1/library/books/{id}/slices/{order}/curate`
- **THEN** the system processes the curation request normally

#### Scenario: Rate-limited user exceeds AI endpoint quota
- **WHEN** an authenticated user exceeds the per-user rate limit on AI generation endpoints
- **THEN** the system returns `HTTP 429 Too Many Requests`

### Requirement: Book deletion ownership verification
The delete book endpoint SHALL verify that the requesting user is the owner of the book (the user who created or imported it, identified by the `CreatedByUserId` field on `DocumentBook`). If the requesting user is not the owner, the system SHALL return `HTTP 403 Forbidden` with error code `LIBRARY_FORBIDDEN`. Books with no recorded owner (`CreatedByUserId` is null, from pre-migration data) SHALL also be rejected with `HTTP 403 Forbidden` — deletion of unowned legacy books requires an explicit admin mechanism not in scope for this change.

#### Scenario: Owner deletes their own book
- **WHEN** the user who created a book sends `DELETE /api/v1/library/books/{id}`
- **THEN** the system soft-deletes the book and returns `HTTP 200`

#### Scenario: Non-owner attempts to delete a book
- **WHEN** a user who did not create the book sends `DELETE /api/v1/library/books/{id}`
- **THEN** the system returns `HTTP 403 Forbidden` with `{ "code": "LIBRARY_FORBIDDEN" }`

#### Scenario: Deleting a legacy book with no owner
- **WHEN** an authenticated user sends `DELETE /api/v1/library/books/{id}` for a book where `CreatedByUserId` is null
- **THEN** the system returns `HTTP 403 Forbidden` with `{ "code": "LIBRARY_FORBIDDEN" }`

### Requirement: PDF upload content validation
The PDF upload endpoint SHALL validate uploaded files by checking that the file stream begins with the PDF magic bytes (`%PDF-`) in addition to the existing file extension check. Files that do not pass both checks SHALL be rejected with `HTTP 400 Bad Request` and error code `INVALID_PDF_FORMAT`. The existing 350 MB file size limit, request timeouts (nginx 300s proxy_read_timeout), and `CancellationToken` propagation SHALL be preserved. On validation failure after the file has been written to temporary storage, the temporary file SHALL be cleaned up.

#### Scenario: Valid PDF file uploaded
- **WHEN** an authenticated user uploads a file with `.pdf` extension whose content starts with `%PDF-`
- **THEN** the system accepts the upload and begins processing

#### Scenario: Non-PDF file with .pdf extension uploaded
- **WHEN** a user uploads a file named `malware.pdf` whose content does not start with `%PDF-`
- **THEN** the system returns `HTTP 400` with `{ "code": "INVALID_PDF_FORMAT" }` and any temporary file is cleaned up

### Requirement: SSRF validation with pinned DNS resolution and redirect protection
The URL security validator SHALL resolve the target hostname's DNS once and pin the resolved IP address through to the HTTP client connection via `ConnectCallback`. The `ConnectCallback` SHALL preserve the original hostname for TLS SNI and certificate validation while directing the TCP connection to the validated IP address.

HTTP clients performing SSRF-validated requests SHALL disable automatic redirect following (`AllowAutoRedirect = false`). If redirect following is required, each redirect destination SHALL be validated through the same SSRF validation pipeline (URL parsing, scheme validation, DNS resolution, IP validation, and connection pinning) before the redirect is followed.

The validation SHALL reject the following IP ranges: private IPv4 (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16), loopback (127.0.0.0/8), link-local (169.254.0.0/16), CGNAT/shared (100.64.0.0/10), multicast IPv4 (224.0.0.0/4), broadcast (255.255.255.255), unspecified IPv4 (0.0.0.0/8), IPv6 loopback (::1), IPv6 link-local (fe80::/10), IPv6 site-local (fec0::/10), IPv6 unique-local (fc00::/7), IPv6 multicast (ff00::/8), IPv6 unspecified (::), IPv4-mapped IPv6 addresses (::ffff:0:0/96) where the mapped IPv4 falls into any rejected range, and internal hostnames. DNS resolution results SHALL be checked against all of these ranges.

#### Scenario: Normal URL passes SSRF validation
- **WHEN** a URL resolves to a public IP address
- **THEN** the HTTP client connects to the same IP that was validated, not a separately resolved address

#### Scenario: DNS rebinding attack attempt
- **WHEN** a URL's first DNS resolution returns a public IP but subsequent resolutions would return a private IP
- **THEN** the HTTP client still connects to the original validated public IP, preventing access to internal services

#### Scenario: Redirect to private IP
- **WHEN** a validated URL returns a 3xx redirect to a URL that resolves to a private or loopback IP
- **THEN** the redirect is not followed and the request is rejected

#### Scenario: HTTPS request with IP pinning
- **WHEN** an HTTPS URL is validated and the connection is pinned to a specific IP
- **THEN** the TLS handshake uses the original hostname for SNI and certificate validation, not the pinned IP

### Requirement: Secure markdown rendering on all pages
All frontend pages rendering user-generated, AI-generated, or crawler-sourced markdown content via `v-html` SHALL configure their MarkdownIt instances with `html: false` to prevent injection of raw HTML. No MarkdownIt instance in the application SHALL enable the `html` option.

#### Scenario: AI-generated content with HTML tags
- **WHEN** AI-generated content contains `<script>alert("xss")</script>` or `<img onerror="steal()">` HTML tags
- **THEN** the markdown renderer escapes these tags as text rather than interpreting them as HTML

#### Scenario: Markdown rendering on insights, notes, and review pages
- **WHEN** markdown content is rendered on the insights, notes, or review pages
- **THEN** the MarkdownIt instance uses `html: false`, matching the reader page's configuration

### Requirement: Rate limiter real client IP partitioning
The rate limiting middleware SHALL partition requests by the real client IP address when the application runs behind a reverse proxy. The application SHALL configure forwarded headers middleware to trust `X-Forwarded-For` from the known proxy network (Docker bridge subnet, `ForwardLimit = 1`) and reject forwarded headers from untrusted sources. The middleware SHALL correctly detect HTTPS via `X-Forwarded-Proto` from the trusted proxy.

#### Scenario: Multiple clients behind nginx proxy
- **WHEN** two different clients make requests through the nginx reverse proxy
- **THEN** each client's requests are rate-limited independently based on their real IP from `X-Forwarded-For`

#### Scenario: Request without X-Forwarded-For
- **WHEN** a request arrives without an `X-Forwarded-For` header
- **THEN** the rate limiter falls back to the connection's remote IP address

#### Scenario: Client-supplied X-Forwarded-For spoofing attempt
- **WHEN** a client directly sends a request with a spoofed `X-Forwarded-For` header not from the trusted proxy network
- **THEN** the middleware ignores the untrusted forwarded header and uses the connection's remote IP address

### Requirement: DocumentBook ownership tracking
The `DocumentBook` entity SHALL include a `CreatedByUserId` field (nullable Guid FK → Users) recording the user who created or imported the book. All book creation endpoints (PDF upload, URL crawl, markdown import, remote PDF import) SHALL set `CreatedByUserId` to the authenticated user's ID. Existing books created before this migration will have `CreatedByUserId = null`.

#### Scenario: User imports a book via PDF upload
- **WHEN** an authenticated user uploads a PDF via `POST /api/v1/library/upload-pdf`
- **THEN** the created `DocumentBook` has `CreatedByUserId` set to the authenticated user's ID

#### Scenario: Querying books created before the migration
- **WHEN** the system queries a book created before the `CreatedByUserId` field was added
- **THEN** `CreatedByUserId` is null and the book is treated as having no recorded owner

### Requirement: Document Library Grid & Card Layout Density
The Document Library interface (`pages/library.vue`) SHALL present technical publications with compact, balanced card geometry and responsive grid spacing:
1. **Document Card Dimensions & Padding**: Individual book cards SHALL render with compact padding (`p-4 sm:p-5 rounded-2xl`) and balanced internal vertical spacing (`space-y-3`), eliminating excessive padding (`p-6 sm:p-7 rounded-3xl`).
2. **Grid Spacing**: The responsive book grid SHALL use a balanced gap (`gap-4 sm:gap-5`), preserving clean multi-column layouts across desktop screen sizes without unnecessary vertical stretching.
3. **Card Content Proportions**: Book title text (`text-base sm:text-lg font-bold line-clamp-2`), status indicators, and action buttons (`Continue Reading`, `Delete`, `Export`) SHALL fit compactly within each card.

#### Scenario: Library cards display balanced whitespace
- **WHEN** browsing technical publications on the `/library` route
- **THEN** book cards display compact vertical height and clean padding, allowing more cards to remain visible above the fold on desktop screens.

### Requirement: Document Ingestion Dialog Layout & Sticky Action Architecture
The document import and ingestion dialog on `/library` SHALL utilize the standardized 3-tier modal shell (`AppModal.vue`):
1. **Pinned Modal Header**: Title and close button SHALL remain pinned at the top (`shrink-0`).
2. **Scrollable Form Body**: Markdown form fields, PDF dropzone upload areas, and Web URL crawler options SHALL reside in an independently scrollable body container capped at `max-h-[60vh]`.
3. **Sticky Action Footer**: The "Cancel" (`library.cancel`) and primary action buttons ("Save Document", "Upload & Process PDF", "Crawl URL") SHALL remain permanently pinned at the bottom of the modal (`shrink-0`) and visible on all viewports without scrolling.

#### Scenario: User opens PDF import tab on 1080p desktop
- **WHEN** user opens the "Nhập Tài Liệu Mới" modal on `/library` and selects the "Tệp PDF" tab
- **THEN** the modal fits comfortably within `max-h-[85vh]`
- **AND** the Dropzone area and inputs scroll inside the body container if vertical space is constrained
- **AND** the "Hủy" and "Tải Lên & Xử Lý PDF" buttons are immediately visible in the sticky bottom footer.

### Requirement: User-Scoped Document Catalog Isolation
The library endpoint `GET /api/v1/library/books` SHALL query and return only documents owned by the authenticated user (`CreatedByUserId == currentUserId`).

1. The endpoint SHALL enforce `.RequireAuthorization()` and return `HTTP 401 Unauthorized` for unauthenticated requests.
2. The endpoint query SHALL filter `WHERE "CreatedByUserId" = currentUserId AND "IsDeleted" = false`, ensuring strict privacy and isolation between users.
3. Legacy system books or unowned books (`CreatedByUserId IS NULL`) SHALL NOT appear in the user's active library catalog.

#### Scenario: User queries their library catalog
- **WHEN** an authenticated user calls `GET /api/v1/library/books`
- **THEN** only books where `CreatedByUserId` equals the authenticated user's ID are returned
- **AND** books belonging to other users or legacy unowned records are excluded.

### Requirement: Starter Handbook Deletion Autonomy and Empty State Transition
A new user's library SHALL start empty; no starter content is provisioned on registration. The initial empty state SHALL guide users to import their own documents or explicitly choose a prepared document from Discover.

1. When the authenticated user requests deletion of a book they own via `DELETE /api/v1/library/books/{id}`, the system SHALL soft-delete the book (`IsDeleted = true`) and all associated `DocumentChunks`.
2. Any active `UserBookPacer` pointing to the deleted book SHALL be deactivated.
3. When zero active books remain in the user's library, the UI across `/library`, `/today`, and `/roadmap` SHALL display an empty state with personal import guidance (PDF upload, web crawler, or markdown series) and a link to `/library?tab=discover`.

#### Scenario: New account starts with an empty library
- **WHEN** a newly registered user opens `/library`, `/today`, or `/roadmap` before importing or adopting any document
- **THEN** the view displays the zero-book empty state prompting personal document ingestion or browsing Discover
- **AND** `GET /api/v1/library/books` returns an empty list with `totalCount = 0`.

#### Scenario: User deletes their starter handbook
- **WHEN** an authenticated user deletes the last remaining book from their library
- **THEN** the server returns `HTTP 200 OK` with `{ "success": true }`
- **AND** subsequent queries to `GET /api/v1/library/books` return an empty list with `totalCount = 0`
- **AND** the `/library` view displays the zero-book empty state offering personal import and discovery.

### Requirement: Explicit Document Language Selection at Import

The document import experience (PDF upload and remote-PDF / URL import) SHALL let the user explicitly select the document's content language (English or Vietnamese) as a first-class field. The selector SHALL default to the current interface locale but SHALL be user-overridable, and the import SHALL send the selected language as the document's language rather than silently using the interface locale. The selected language SHALL be applied to the document's slices so language-dependent downstream behavior (such as narration voice) matches the document's actual language regardless of the interface locale at import time. The selector SHALL be an accessible in-page control (not a native browser `<select>`) with localized `en`/`vi` labels.

#### Scenario: Language selector shown and defaulted to interface locale
- **WHEN** a user opens the PDF upload or URL import form
- **THEN** a document-language selector is shown, defaulting to the current interface locale.

#### Scenario: English document uploaded under a Vietnamese interface
- **WHEN** a user whose interface is Vietnamese uploads an English document and sets the language selector to English
- **THEN** the document and its slices are tagged English, not Vietnamese.

#### Scenario: Narration voice follows the chosen import language
- **WHEN** a document imported with a chosen language is later narrated
- **THEN** the narration voice matches the language chosen at import, independent of the current interface locale.

### Requirement: Document Language Fidelity in AI Slice Formatting and Callouts

The AI slice formatter (`GeminiAiService.FormatSliceAsync` and `IAiMarkdownFormatter`) SHALL adapt formatting rules based on the document's content language (`chunk.Language`, e.g. `en` or `vi`).

When formatting slices:
1. **Language Invariance Across All Structural Elements:** All synthesized additions—including the executive context note (`> [!NOTE]`), practical/architectural alerts (`> [!TIP]`, `> [!WARNING]`, `> [!IMPORTANT]`), takeaway headings and bullet points, and the scenario challenge drill—MUST strictly match the language of the source document:
   - For Vietnamese documents (`Language == "vi"`): All callout titles, callout body prose, summary overviews, key takeaways, and scenario drills SHALL be written in natural, idiomatically accurate Vietnamese. The formatter SHALL NOT emit English callouts, English bullet points, or English scenarios for Vietnamese literature.
   - For English documents (`Language == "en"`): All callout titles, callout body prose, summary overviews, key takeaways, and scenario drills SHALL be written in English.
2. **Alert Title Localization:** Alert tags (`NOTE`, `TIP`, `WARNING`, `IMPORTANT`) SHALL retain standard GitHub Alert identifiers (`> [!NOTE]`, `> [!TIP]`, `> [!WARNING]`) so CSS callout styling renders correctly in the reader, while their explanatory content is in the document language.

#### Scenario: Vietnamese document slice curates callouts and takeaways in Vietnamese
- **WHEN** AI formats a slice of a Vietnamese book (`Language == "vi"`, such as *827-thoi-quen-nguyen-tu-thuviensach.vn*)
- **THEN** the executive note (`> [!NOTE]`), practical alerts (`> [!TIP]`, `> [!WARNING]`), key takeaways, and scenario challenge are all generated in Vietnamese, with zero untranslated English narrative blocks.

#### Scenario: English document slice curates callouts and takeaways in English
- **WHEN** AI formats a slice of an English book (`Language == "en"`, such as *ASP.NET Core Architecture Guide*)
- **THEN** all callouts, takeaways, and scenario challenges are generated in English.

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide a unified, responsive search and filter toolbar layout synchronized across both the "My Library" and "Discover" tabs.

On both "My Library" and "Discover" tabs, the primary toolbar SHALL present:
1. A search input (`sm:w-72 sm:flex-initial`) with dedicated clear trigger (`X`) and search execution on Enter or clear.
2. An All pill and a bounded quick subset selected by configured category order, not hardcoded subject names. Every category SHALL remain reachable through the advanced filter even when omitted from the quick subset.
3. An Advanced Filter trigger button (`[ 🎛️ Bộ Lọc ]` / "Filter") displaying an active filter badge counter when any non-default filter is applied.

Clicking the Advanced Filter trigger button SHALL open an accessible filter dialog modal permitting users to filter documents by all nondeleted categories from shared metadata, including inactive categories for historical filtering, content language, and document format, with dedicated reset and apply actions.

Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift when switching categories.

#### Scenario: User filters library by Mental Models category
- **WHEN** the user selects the stored Mental Models category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Mental Models category UUID
- **AND** the filter button transitions active styling with zero horizontal layout shift or width jumping.

#### Scenario: User filters library by Habits & Deep Work category
- **WHEN** the user selects the stored Habits category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Habits category UUID.

#### Scenario: User switches category filter tab in library
- **WHEN** user clicks a category tab (e.g., "Tất Cả", "Frontend & Web", "Backend & Phân Tán", "Thiết Kế Mã", "Mô Hình Tư Duy")
- **THEN** the active tab updates its background, border, and text colors via `transition-colors`
- **AND** the button maintains its exact text width and baseline geometry without any border jitter, horizontal position jumping, or layout shifts across adjacent tabs.

#### Scenario: User clears or toggles category filter
- **WHEN** user clicks "Tất Cả" (All) after viewing a specific category
- **THEN** the category filter state is cleared
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.

#### Scenario: User filters My Library with quick category chips
- **WHEN** user clicks "Backend & Runtime" in My Library
- **THEN** the book list updates to show only personal books under the selected Backend category UUID
- **AND** the button styling transitions with zero layout shift or width jumping.

#### Scenario: User opens advanced filter modal in My Library
- **WHEN** user clicks the "Bộ Lọc" button in My Library
- **THEN** the advanced filter modal opens, displaying all nondeleted categories, language options, and source formats
- **AND** the trigger button displays the active filter count badge when filters are applied.

#### Scenario: User applies deep category filter from modal in My Library
- **WHEN** the user selects the Engineering Craft or Mental Models category UUID from the advanced filter and clicks Apply
- **THEN** the modal closes, the catalog updates to display the matching personal books, and the filter counter badge increments.

#### Scenario: User clears or resets filters in My Library
- **WHEN** user clicks Reset in the filter modal or clicks the "Tất Cả" quick pill
- **THEN** all category and search filters are cleared, and the book list reloads all documents.

#### Scenario: New category outside the quick subset
- **WHEN** an administrator creates a category that is outside the toolbar's bounded quick subset
- **THEN** it appears in the advanced filter with its localized stored name and filtering sends its `categoryId` UUID.

### Requirement: Book Metadata and Category Update API
The system SHALL expose an authenticated HTTP PATCH endpoint `PATCH /api/v1/library/books/{id}` allowing users to update the metadata and domain category of books they have imported into their library.

1. **Authentication & Authorization**:
   - The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and return `HTTP 401 Unauthorized` for unauthenticated requests.
   - The endpoint SHALL verify that the requesting user is the owner of the book (`CreatedByUserId == currentUserId`). If the user does not own the book or `CreatedByUserId` is null, the endpoint SHALL return `HTTP 403 Forbidden` with RFC 7807 problem details and error code `LIBRARY_FORBIDDEN`.
2. **Resource Existence & Soft Delete**:
   - If the book ID does not exist or has `IsDeleted == true`, the endpoint SHALL return `HTTP 404 Not Found`.
3. **Payload Validation**:
   - The payload SHALL accept `Title` (string, required, length 1–255), `AuthorOrSourceUrl` (string, optional, max length 500), `CategoryId` (required UUID referencing a nondeleted category), and `Language` (string, optional, allowed values: `"en"`, `"vi"`).
   - If `Title` is whitespace or exceeds 255 characters, `CategoryId` is malformed or does not identify a nondeleted category, or `Language` is specified with an unsupported locale, the system SHALL return `HTTP 400 Bad Request` with validation error details.
4. **Cascade Taxonomy and Language Alignment**:
   - Changing `CategoryId` SHALL require an active category, persist the new reference, and realign linked learning artifacts without changing the book identifier, slug, prose, progress, or source-ingestion state. Retaining the current inactive category during unrelated edits SHALL remain permitted.
   - Subsequent calls to `GET /api/v1/graph` for the user SHALL immediately reflect the new pillar connection (`pillar-{categoryId}`), moving the book and its chunks to the updated DeepPace constellation without dangling edges.
   - Updating `Language` SHALL cascade and persist `chunk.Language = request.Language` on all surviving `DocumentChunk` records belonging to the book, ensuring subsequent AI curation (`CurateSliceHandler`) and TTS narration (`GetOrSynthesizeChunkAudioHandler`) operate under the updated language code.
5. **Response Contract**:
   - Upon successful update, the endpoint SHALL return `HTTP 200 OK` with the updated `BookDto`.

#### Scenario: Owner updates book title, author, and category
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Title = "Atomic Habits (Bản Đầy Đủ)"`, `AuthorOrSourceUrl = "James Clear"`, and `CategoryId` set to the active Habits category UUID
- **THEN** the system returns `HTTP 200 OK` with updated `BookDto`
- **AND** the book's category UUID is updated to the selected category
- **AND** the book's `Title` is updated in the database.

#### Scenario: Non-owner attempts to update book
- **WHEN** an authenticated user attempts to update a book created by a different account
- **THEN** the system returns `HTTP 403 Forbidden` with error code `LIBRARY_FORBIDDEN`
- **AND** no book data is modified.

#### Scenario: Update with invalid title or category
- **WHEN** user submits `PATCH /api/v1/library/books/{id}` with an empty title or an unknown or malformed category UUID
- **THEN** the system returns `HTTP 400 Bad Request` with descriptive validation errors.

#### Scenario: Owner updates book language and cascades to document chunks
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Language = "vi"`
- **THEN** the system returns `HTTP 200 OK`
- **AND** all existing `DocumentChunk` records belonging to book `123` have their `Language` updated to `"vi"`
- **AND** subsequent TTS audio generation requests for chunks in this book select the Vietnamese neural voice.

#### Scenario: Reclassification races with deactivation
- **WHEN** the requested category is deactivated before reclassification admission commits
- **THEN** the endpoint returns HTTP 409 `Category.Inactive` and leaves the original assignment and linked learning state unchanged
- **AND** existing queued/running source-operation concurrency protections remain enforced.

### Requirement: Book Management and Edit Interface in Library
The Document Library interface (`pages/library.vue`) SHALL provide an accessible, responsive book editing modal dialog complying with the **Dev-Learning Studio** UI design system and Pillar 2 component governance.

1. **Card Action Trigger**:
   - Each book card owned by the user SHALL display an Edit button (`Pencil` icon, `library.edit_book`) alongside the Delete button.
   - The button SHALL have responsive padding and an accessible label/tooltip.
2. **Accessible Edit Modal (`EditBookModal`)**:
   - Clicking Edit SHALL open a teleported dialog adhering to `AppModal.vue` conventions (strictly prohibiting native browser `prompt` or `confirm` dialogs).
   - **Category & Language Dropdowns**: The modal MUST use custom accessible `AppSelect.vue` to render active category descriptors plus the book's current inactive category when applicable, and document language choices (`en` / `vi`), strictly prohibiting raw HTML `<select><option>` elements.
   - **Form Fields**: Accessible text inputs for Book Title and Author / Source URL.
   - **Taxonomy Callout**: A responsive informational banner explaining: *"Thay đổi chuyên mục sẽ cập nhật lại Cột trụ Đồ thị Tri thức và phân loại thẻ nhớ tương ứng"* (Vietnamese) / *"Updating the category re-aligns Knowledge Graph constellations and spaced repetition cards"* (English).
   - **Action Buttons**: "Hủy" (`library.cancel`) and "Lưu Thay Đổi" (`library.save_changes`) with `whitespace-nowrap shrink-0` and responsive gap, displaying a loading spinner during submission (`isSaving`).
3. **Optimistic / Immediate UI Synchronization**:
   - Upon successful save, the modal closes, a success toast notification appears (`toast.success`), and the book card on `/library` updates its title, category badge, and metadata immediately without requiring a full page refresh.

#### Scenario: User opens edit modal and updates category
- **WHEN** user clicks the Edit button on a book card in `/library`
- **THEN** the modal opens with current book Title, Author, and Category pre-populated in `AppSelect`
- **WHEN** user selects the active category whose stored Vietnamese name is "Thói Quen & Tập Trung Sâu" and clicks "Lưu Thay Đổi"
- **THEN** the modal shows a loading spinner
- **AND** upon API success, the modal closes, a success toast is displayed, and the book card badge updates to "Thói Quen & Tập Trung Sâu".

#### Scenario: Responsive and bilingual button layout
- **WHEN** the edit modal renders in both English and Vietnamese locales across Mobile ($390\text{px}$) and Desktop ($1440\text{px}$) viewports
- **THEN** all action buttons maintain `whitespace-nowrap shrink-0` with zero text truncation or button collision.

#### Scenario: Editing a book with an inactive category
- **WHEN** the owner opens the edit dialog for a book assigned to an inactive category
- **THEN** its current category remains visible and retaining it is permitted
- **AND** inactive categories are not offered as new assignments to other books.

### Requirement: Document Chunk Composite Slice Indexing
The system persistence configuration for `DocumentChunk` SHALL define a composite unique index on `(DocumentBookId, ChunkOrder)` filtered by `WHERE "IsDeleted" = false` (`IX_DocumentChunks_DocumentBookId_ChunkOrder`) to optimize sequential reading slice retrieval (`GET /api/v1/library/books/{id}/slices/{chunkOrder}`) to an $O(1)$ index seek.

#### Scenario: Reading slice lookup by book ID and chunk order
- **WHEN** an authenticated user navigates to reading slice $N$ of a book via `GetBookSlice`
- **THEN** the database query resolves the chunk using the composite index on `(DocumentBookId, ChunkOrder)` without sequential table scanning

---

### Requirement: Library and discovery navigation
The library SHALL expose My Library and Discover tabs with URL-synchronized tab, search, filters and page state. My Library SHALL remain the default and continue to return only the current user's books. Discovery requests SHALL use a separate catalog and SHALL NOT merge shared entries into personal results.

#### Scenario: Navigate and reload discovery
- **WHEN** a learner opens `/library?tab=discover`, applies filters and reloads or navigates back
- **THEN** the selected tab and query state are restored
- **AND** changing filters resets that tab's page to one without mixing results from the other tab.

#### Scenario: Adopt and open a document
- **WHEN** a learner adds a catalog document
- **THEN** My Library shows the owned ready copy and provides the existing reader entry point
- **AND** adoption alone does not advance reading progress or switch the active book pacer.

---

### Requirement: Owned document access for adopted books
Personal document read and mutation operations SHALL enforce the authenticated owner's identity for adopted books as for personal imports. Catalog identifiers SHALL NOT grant access through personal book, slice, export, curation, audio, quiz, pacer or graph interfaces.

#### Scenario: Request another learner's adopted book
- **WHEN** a learner uses another user's book or slice identifier through a personal document operation
- **THEN** the operation returns 404 without exposing content or changing learning state.

#### Scenario: Use an owned copy after catalog hiding
- **WHEN** a learner opens their own adopted book after its catalog source is hidden
- **THEN** reading and existing learning actions continue against the owned copy.

---

### Requirement: Owner-only failed-source replacement
An authenticated owner SHALL supply validated, source-type-matching input for their failed or interrupted initial import. Replacement SHALL preserve document ID, ownership, category and known language, require an explicit owner choice when language is unknown, enforce version/replay protection, and durably admit one new attempt. Ready, deleted, unowned and learning-active targets SHALL be rejected.

#### Scenario: Owner replaces an expired PDF
- **WHEN** the owner explicitly supplies a valid replacement PDF for an eligible failed book
- **THEN** one recovery attempt is accepted under the same book ID and chosen language
- **AND** the response exposes its attempt/status identity without requiring an administrator to impersonate the owner.

#### Scenario: Owner confirms a new web or Markdown snapshot
- **WHEN** the owner supplies accepted replacement text through the existing source-specific import experience
- **THEN** the input is validated and admitted under the failed book's original source type and owner
- **AND** preview alone neither changes the book nor queues processing.

#### Scenario: Legacy failed book has unknown language
- **WHEN** an owner replaces source for a failed legacy book without a recorded content language
- **THEN** admission requires an explicit English or Vietnamese document-language choice
- **AND** the interface locale is not treated as an already known source language.

#### Scenario: Another account attempts source replacement
- **WHEN** a learner or administrator submits source replacement for another account's personal document
- **THEN** the endpoint returns 404 without saving input, changing ownership or queuing work.

#### Scenario: Replay, stale state or invalid source
- **WHEN** an identical accepted owner command is replayed
- **THEN** it returns the original recovery attempt without duplicating source storage.
- **WHEN** the version is stale, the target is unsafe or input violates validation/quota limits
- **THEN** no replacement or runnable attempt is committed and any unaccepted staging is removed.

---

### Requirement: Source-relevant metadata concurrency
Changes to metadata affecting an ingestion attempt's category, language or source identity SHALL invalidate stale recovery forms. While ingestion is Queued or Running, conflicting metadata changes SHALL return 409 instead of changing the processing input underneath a worker. Permitted metadata edits after failure SHALL remain owner-only and preserve existing language-cascade behavior.

#### Scenario: Owner changes language before retry confirmation
- **WHEN** an owner changes the failed book's language before an administrator submits an older retry form
- **THEN** the older command is rejected as stale
- **AND** a fresh command uses the owner's current chosen language.

#### Scenario: Owner edits source-relevant fields during processing
- **WHEN** an owner requests a conflicting category, language or source change while an ingestion attempt is active
- **THEN** the update returns 409 and existing processing metadata and slices are unchanged.

---

### Requirement: Localized owner recovery guidance
Failed personal imports SHALL show safe English/Vietnamese guidance for missing, expired, invalid and quota-blocked sources. Eligible owners SHALL have an accessible explicit source-replacement action using existing import/modal controls; loading and validation failures SHALL preserve entered input. No retained-source viewer or administrator credential workaround SHALL be shown.

#### Scenario: Owner opens a failed import on mobile
- **WHEN** an owner inspects a failed book in English or Vietnamese at 390x844
- **THEN** the source-unavailable explanation and explicit replacement action remain readable and usable
- **AND** completing replacement updates the existing book card rather than adding another book.

#### Scenario: Replacement rejected or canceled
- **WHEN** validation, quota or a state conflict rejects replacement
- **THEN** safe localized feedback appears and the form remains recoverable.
- **WHEN** the owner cancels before confirmation
- **THEN** no new ingestion attempt is created.
