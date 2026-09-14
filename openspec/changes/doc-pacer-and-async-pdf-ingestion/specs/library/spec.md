# Delta Spec: Library — Asynchronous Large PDF Ingestion & Bookmark Extraction

## MODIFIED Requirements

### Requirement: Large PDF Streaming & Background Ingestion Pipeline
The library system SHALL support PDF uploads up to 300 MB and multi-thousand page documents without blocking HTTP request execution or loading entire files into the Large Object Heap (LOH). The system SHALL stream uploaded files directly to temporary disk storage (`bufferSize = 80KB`), persist a `DocumentBook` record with status `Processing`, and enqueue processing into an in-memory background worker queue via `System.Threading.Channels`.

#### Scenario: User uploads large technical document (e.g. 240MB, 8,351 pages)
- **WHEN** user uploads a valid `.pdf` file up to 300 MB via `POST /api/v1/library/upload-pdf`
- **THEN** server streams file to temporary storage without contiguous in-memory allocation, creates `DocumentBook` with `ProcessingStatus = Processing` and `ProgressPercentage = 0`, and returns `HTTP 202 Accepted` within 2 seconds.

#### Scenario: Background Worker processes PDF book
- **WHEN** background ingestion worker dequeues a PDF ingestion task
- **THEN** worker extracts text and chapter structure, stores sequential `DocumentChunk` entities, updates `ProgressPercentage` and `StatusMessage` periodically in the database, and transitions status to `Ready` upon completion.

#### Scenario: Document processing error handling
- **WHEN** PDF is corrupted or exceeds unrecoverable memory thresholds
- **THEN** worker records `ProcessingStatus = Failed` with descriptive `ErrorMessage`, and cleans up temporary disk files safely.

---

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
