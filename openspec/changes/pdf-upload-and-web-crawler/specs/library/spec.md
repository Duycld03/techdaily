# Delta Spec: PDF Upload & Web Document Crawler Capability

## Requirements

### REQ-1: PDF File Ingestion
- The system SHALL provide an API endpoint `POST /api/v1/library/upload-pdf` requiring authentication.
- The request SHALL accept a `multipart/form-data` payload containing:
  - `file`: PDF file (`application/pdf`), maximum file size up to **500 MB** (configurable via Kestrel request body limit).
  - `title`: string (optional, defaults to PDF metadata title or filename).
  - `category`: integer (`Category` enum: FrontendWeb, BackendDotNet, DatabaseStorage, CloudDevOps, SystemDesign).
  - `language`: string (defaults to `"en"`).
- The system SHALL extract text page by page using streaming extraction (`IFormFile.OpenReadStream()`), group pages into logical reading slices of 400–800 words (~3–5 minutes reading time), and create `DocumentBook` and `DocumentChunk` records.
- The system SHALL process large PDF files without buffering the entire file as a single contiguous array in RAM, preventing Large Object Heap (LOH) fragmentation.
- If the PDF is corrupted or encrypted with a password, the system SHALL return `400 Bad Request` with an appropriate error message.

### REQ-2: Web URL Document Crawler & Preview
- The system SHALL provide an API endpoint `POST /api/v1/library/crawl-url` requiring authentication.
- The request SHALL accept a JSON payload containing:
  - `url`: string (valid HTTP/HTTPS URL).
- The system SHALL:
  - If the URL targets raw Markdown (e.g. `raw.githubusercontent.com` or GitHub `.md` file), fetch raw text directly.
  - If the URL targets an HTML page, parse the main content container (`<article>`, `<main>`, `.markdown-body`), strip extraneous navigation/ads/scripts, and convert to clean Markdown.
  - Extract the page `<title>` or OpenGraph title.
- The endpoint SHALL return:
  - `title`: string
  - `sourceUrl`: string
  - `markdownContent`: string
  - `estimatedWordCount`: integer

### REQ-3: 3-Tab Import Modal Interface
- The Import Modal on `/library` SHALL provide 3 selectable tabs:
  - **Tab 1: Markdown (`MarkdownSeries`):** Manual title, category, source URL, and textarea.
  - **Tab 2: PDF Upload (`PdfDocument`):** Drag-and-drop file upload zone, file validation, category selection, and upload progress animation.
  - **Tab 3: URL Crawler (`WebDocUrl`):** URL input field with a "Fetch Content" button. Upon fetching, the title, category, and markdown content preview are automatically filled for user review before confirming chunking.

### REQ-4: Internationalization (i18n)
- All new modal tabs, drag-and-drop dropzones, file size limits, crawl buttons, loading states, and error alerts SHALL have full `en` and `vi` translations in `en.json` and `vi.json`.
