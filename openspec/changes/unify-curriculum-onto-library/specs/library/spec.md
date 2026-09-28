# Library Specification

## MODIFIED Requirements

### Requirement: Starter Handbook Deletion Autonomy and Empty State Transition
A new user's library SHALL start empty; no starter content is provisioned on registration. The empty state guiding the user to import their own technical documents is therefore the INITIAL state of every new account.

1. When the authenticated user requests deletion of a book they own via `DELETE /api/v1/library/books/{id}`, the system SHALL soft-delete the book (`IsDeleted = true`) and all associated `DocumentChunks`.
2. Any active `UserBookPacer` pointing to the deleted book SHALL be deactivated.
3. When zero active books remain in the user's library — including a brand-new account that has imported nothing yet — the UI across `/library`, `/today`, and `/roadmap` SHALL display an empty state that clearly guides the user to import their own technical documents (PDF upload, web crawler, or markdown series).

#### Scenario: New account starts with an empty library
- **WHEN** a newly registered user opens `/library`, `/today`, or `/roadmap` before importing any document
- **THEN** the view displays the zero-book empty state prompting document ingestion (PDF upload, web crawler, or markdown series)
- **AND** `GET /api/v1/library/books` returns an empty list with `totalCount = 0`.

#### Scenario: User deletes their starter handbook
- **WHEN** an authenticated user deletes the last remaining book from their library
- **THEN** the server returns `HTTP 200 OK` with `{ "success": true }`
- **AND** subsequent queries to `GET /api/v1/library/books` return an empty list with `totalCount = 0`
- **AND** the `/library` view displays the zero-book empty state prompting document ingestion.
