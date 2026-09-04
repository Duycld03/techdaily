# Library Specification

## Purpose
Provides technical document ingestion capabilities including markdown series creation, memory-efficient PDF streaming extraction up to 200MB/800 pages, and web documentation URL crawling with clean markdown conversion.

## Requirements

### Requirement: PDF File Ingestion
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/upload-pdf` accepting `multipart/form-data` with maximum file size up to 200 MB and up to 800 pages. The system SHALL extract text page-by-page using streaming extraction (`IFormFile.OpenReadStream()`), group pages into reading slices of 400–800 words without buffering the entire file into contiguous RAM, and create `DocumentBook` and `DocumentChunk` records.

#### Scenario: User uploads a valid PDF document
- **WHEN** authenticated user submits `POST /api/v1/library/upload-pdf` with a valid PDF file <= 200MB and <= 800 pages
- **THEN** system streams page text, slices into logical chunks, saves `DocumentBook` with `Format = DocumentFormat.Pdf`, and returns `201 Created`.

#### Scenario: Uploaded PDF exceeds page limit or file size limit
- **WHEN** user uploads a PDF file exceeding 800 pages or 200 MB
- **THEN** system returns `400 Bad Request` with an error message detailing the safety boundary.

#### Scenario: Uploaded PDF is corrupted or encrypted
- **WHEN** user uploads a corrupted or password-protected PDF
- **THEN** system returns `400 Bad Request` with an error indicating file extraction failure.

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
