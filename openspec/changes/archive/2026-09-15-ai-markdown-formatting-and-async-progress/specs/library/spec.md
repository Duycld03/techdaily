# Delta Specification: Library (AI Markdown Formatting & Async Ingestion Progress)

## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Ingestion Progress Reporting & UI Status Indication
The library system SHALL expose real-time curation progress through `GET /api/v1/library/books/{id}/status`, and the Library UI (`library.vue`) SHALL render a dynamic progress bar and active step message while Tier 2 AI curation is ongoing.

#### Scenario: User views library while book is being curated by AI
- **WHEN** user views `/library` and a book is undergoing Tier 2 background formatting (`ProgressPercentage < 100`)
- **THEN** the book card displays an active progress bar with percentage indicator and descriptive status message (`AI is curating slice X/Y...`).
