# Library Spec Delta

## MODIFIED Requirements

### Requirement: Remote PDF Direct Ingestion Pipeline
The system SHALL provide an authenticated API endpoint `POST /api/v1/library/import-remote-pdf` to stream remote PDF documents directly into the server's PDF ingestion pipeline without requiring the user to download the file locally.

The endpoint handler (`ImportRemotePdfHandler`) SHALL:
1. Validate the remote URL scheme (`http` / `https`) and enforce strict SSRF validation to prevent access to private IP ranges, loopback addresses, or cloud metadata services.
2. Initiate a streaming HTTP GET request with `HttpCompletionOption.ResponseHeadersRead` and verify the `Content-Type` header or stream magic bytes start with `%PDF-`.
3. Stream the file directly to temporary disk storage (`Path.Combine(Path.GetTempPath(), "deeppace-uploads", $"{bookId}.pdf")`) with an 80KB buffer size, ensuring zero Large Object Heap (LOH) contiguous byte array allocations.
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

