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
The `/library` page SHALL serve as the technical document catalog, displaying books, bookmarks, and curation states adhering to the **Dev-Learning Studio** visual theme.

The `/library` page and its associated reader subroutes (`/read/[bookId]`) SHALL be protected by mandatory client-side and server-side route authentication middleware:
1. **Authenticated Route Guard Invariant**:
   - Access to `/library` and `/read/*` SHALL strictly require an active authenticated user session.
   - Unauthenticated visitors navigating to `/library` or `/read/[bookId]` SHALL be immediately redirected to `/login` with the original requested destination encoded in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
   - Authenticated sessions with valid, unexpired JWT tokens SHALL be granted immediate access.
2. **Obsidian Studio Canvas & Glass Controls**:
   - The `/library` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate backgrounds.
   - Category filter pills and the search bar SHALL render using glass styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`), highlighting active category pills with Iris Violet styling (`bg-slate-100 dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 border-slate-300 dark:border-white/[0.12]`).
   - The "Import Document" action button SHALL feature Iris Violet styling (`bg-brand-600 hover:bg-brand-500 text-white shadow-brand-500/20`).
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

### Requirement: Paginated Book Catalog Browsing and State Synchronization
The library system SHALL expose an endpoint `GET /api/v1/library/books` accepting optional query parameters: `category` (enum/integer), `search` (string), `page` (integer, default 1, minimum 1), and `pageSize` (integer, default 12, minimum 1, maximum 100). The endpoint SHALL return a structured envelope containing:
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
5. **Two-Way URL Query Synchronization**: The active `page`, `category`, and `search` query SHALL synchronize bidirectionally with browser URL query parameters (`?page=N&category=C&search=S`). Reloading the page or sharing the URL SHALL restore the exact catalog page and filter state.

#### Scenario: User browses the first page of the technical library
- **WHEN** an authenticated or anonymous user navigates to `/library`
- **THEN** client calls `GET /api/v1/library/books?page=1&pageSize=12`
- **AND** backend returns up to 12 book cards along with `totalCount`, `page = 1`, `pageSize = 12`, and `totalPages`
- **AND** client renders the books in a balanced responsive grid with page 1 highlighted in the pagination controls.

#### Scenario: User navigates to a subsequent catalog page
- **GIVEN** the library catalog has 30 books (`totalPages = 3`)
- **WHEN** the user clicks page number "2" in the pagination bar
- **THEN** client updates the URL query string to `?page=2`
- **AND** fetches books via `GET /api/v1/library/books?page=2&pageSize=12`
- **AND** smoothly replaces the grid items with books 13 through 24 and highlights button "2".

#### Scenario: User filters catalog by category or search term
- **GIVEN** the user is currently viewing page 3 of the catalog (`?page=3`)
- **WHEN** the user selects category "Backend .NET" or enters search term "architecture"
- **THEN** client resets the active page to 1
- **AND** updates URL query string to `?category=1&search=architecture&page=1`
- **AND** requests page 1 of the filtered result set from the backend.

#### Scenario: User enters direct URL with pagination and filter parameters
- **WHEN** a user navigates directly to `/library?category=2&search=postgres&page=2` via bookmark or shared link
- **THEN** client parses `category = 2`, `search = 'postgres'`, and `page = 2` from the route query
- **AND** dispatches `GET /api/v1/library/books?category=2&search=postgres&page=2&pageSize=12`
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

### Requirement: Consistent Book Card Status Badge Baseline Alignment
The library catalog on `/library` SHALL align the bookmark resume badge, ready badge, and in-progress ingestion status indicator to a consistent bottom baseline across all book cards within each row of the library grid, regardless of variable title lengths (1-line vs 2-line) or the presence/absence of the author or source URL subtitle.

1. **Flex-Stretched Card Content Structure:**
   - The book card upper content container SHALL flex-stretch (`flex flex-col flex-1`) to occupy all available vertical space above the card action footer.
   - The category badge, total chunks count, document title (`<h3>`), and optional subtitle (`<p>`) SHALL remain anchored at the top of the upper content area.
2. **Bottom-Anchored Status Container:**
   - The status indicator elements (bookmark resume badge, ready badge, and processing ingestion indicator) SHALL reside within a bottom-anchored container (`mt-auto pt-3`) inside the upper content wrapper.
   - The status container SHALL maintain an identical vertical distance directly above the card footer divider (`pt-4 border-t`) across adjacent cards of differing content heights.

#### Scenario: Book card with 1-line title aligns status badge with 2-line title card
- **WHEN** multiple book cards with differing title line counts (e.g. a 1-line title alongside 2-line titles) are rendered in the `/library` grid
- **THEN** both cards' status badges are pinned to the bottom of the content container via `mt-auto`
- **AND** the top edges and baselines of the badges align horizontally across the grid row directly above the card footer action divider.

#### Scenario: Book card without author or source URL maintains bottom-aligned badge
- **WHEN** a book card without an author or source URL subtitle is rendered alongside cards with subtitle text
- **THEN** the status badge container uses `mt-auto` to anchor directly above the card footer divider
- **AND** the absence of the subtitle does not cause the status badge to float higher up in the card body.

#### Scenario: In-progress ingestion indicator aligns with completed and bookmarked cards
- **WHEN** a book with processing status `Processing` is rendered in a grid row alongside a `Ready` book or a bookmarked book
- **THEN** the processing ingestion indicator is bottom-anchored within the upper content container via `mt-auto pt-3`
- **AND** its bottom edge sits immediately above the card footer action divider consistent with adjacent cards.

### Requirement: Book Card Category Badge and Ingestion Status Localization
The library catalog on `/library` SHALL display localized category badges and localized background ingestion progress indicators on each document card.

1. **Category Badge Resolution:**
   - The UI SHALL implement a category label resolver (`getCategoryLabel`) that normalizes both backend string enum identifiers (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`) and numeric identifiers (`0..4`).
   - The resolver SHALL map each normalized category to its corresponding translation key under `library.categories`:
     - `FrontendWeb` / `0` $\to$ `$t('library.categories.frontend')` ("Frontend & Web")
     - `BackendDotNet` / `1` $\to$ `$t('library.categories.backend')` ("Backend & Phân Tán" / "Backend & Distributed")
     - `DatabaseStorage` / `2` $\to$ `$t('library.categories.database')` ("Cơ Sở Dữ Liệu" / "Database & Storage")
     - `SystemDesign` / `3` $\to$ `$t('library.categories.system_design')` ("Thiết Kế Hệ Thống" / "System Design")
     - `EngineeringCraft` / `4` $\to$ `$t('library.categories.craft')` ("Tư Duy Kỹ Sư & Năng Suất" / "Engineering Craft & Mindset")
   - The UI SHALL NOT default to the hardcoded English string `'Engineering'`.

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
- **GIVEN** a book entity returned from the API has `category = "EngineeringCraft"` and the user's active locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Tư Duy Kỹ Sư & Năng Suất"`
- **AND** the badge does NOT render `'Engineering'`.

#### Scenario: Book card displays localized category for string enum in English
- **GIVEN** a book entity returned from the API has `category = "BackendDotNet"` and the user's active locale is English (`en`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Backend & Distributed"`.

#### Scenario: Book card displays localized category for numeric ID
- **GIVEN** a book entity in the store has numeric `category = 2` and the user's active locale is Vietnamese (`vi`)
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

---

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
