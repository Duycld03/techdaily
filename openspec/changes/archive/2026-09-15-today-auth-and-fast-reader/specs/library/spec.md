## ADDED Requirements

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
