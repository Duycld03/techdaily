# Library Specification

## Purpose
Provides technical document ingestion capabilities including markdown series creation, memory-efficient PDF streaming extraction up to 200MB/800 pages, and web documentation URL crawling with clean markdown conversion.

## Requirements

### Requirement: PDF File Ingestion
The library system SHALL support PDF uploads up to 350 MB and multi-thousand page documents without blocking HTTP request execution or loading entire files into the Large Object Heap (LOH). The system SHALL stream uploaded files directly to temporary disk storage (`bufferSize = 80KB`), persist a `DocumentBook` record with status `Processing`, and enqueue processing into an in-memory background worker queue via `System.Threading.Channels`.

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
The PDF extractor SHALL parse native document bookmarks (Outline Tree) to determine authoritative chapter boundaries, section names, and page ranges. The extractor SHALL automatically detect and exclude front-matter (prefaces, dedications, title pages) and back-matter (indexes, bibliographies).

#### Scenario: Book with native PDF Bookmarks
- **WHEN** PDF contains a valid Bookmarks / Outline tree
- **THEN** extractor segments slices aligned to top-level and second-level chapter bookmarks, naming each slice after the author's official chapter/section title.

#### Scenario: Fallback for books without native Bookmarks
- **WHEN** PDF lacks an embedded Bookmarks tree
- **THEN** extractor falls back gracefully to visual heading heuristics (font size grouping, chapter regex) capped by sensible page and word thresholds.

---

### Requirement: Ingestion Progress Polling API
The library system SHALL provide a lightweight endpoint `GET /api/v1/library/books/{id}/status` returning the current `ProcessingStatus`, `ProgressPercentage` (0–100), `StatusMessage`, and `TotalChunks`.

#### Scenario: Client monitors ingestion progress
- **WHEN** client polls `GET /api/v1/library/books/{id}/status` while book is processing
- **THEN** server returns status code, percentage, and current step message without querying heavy chunk text.
