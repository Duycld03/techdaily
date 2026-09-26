# Spec Delta

## MODIFIED Requirements

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
