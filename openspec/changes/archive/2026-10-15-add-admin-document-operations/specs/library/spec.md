# Library Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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

### Requirement: Source-relevant metadata concurrency
Changes to metadata affecting an ingestion attempt's category, language or source identity SHALL invalidate stale recovery forms. While ingestion is Queued or Running, conflicting metadata changes SHALL return 409 instead of changing the processing input underneath a worker. Permitted metadata edits after failure SHALL remain owner-only and preserve existing language-cascade behavior.

#### Scenario: Owner changes language before retry confirmation
- **WHEN** an owner changes the failed book's language before an administrator submits an older retry form
- **THEN** the older command is rejected as stale
- **AND** a fresh command uses the owner's current chosen language.

#### Scenario: Owner edits source-relevant fields during processing
- **WHEN** an owner requests a conflicting category, language or source change while an ingestion attempt is active
- **THEN** the update returns 409 and existing processing metadata and slices are unchanged.

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
