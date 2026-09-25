# Spec Delta

## ADDED Requirements

### Requirement: User-Scoped Document Catalog Isolation
The library endpoint `GET /api/v1/library/books` SHALL query and return only documents owned by the authenticated user (`CreatedByUserId == currentUserId`).

1. The endpoint SHALL enforce `.RequireAuthorization()` and return `HTTP 401 Unauthorized` for unauthenticated requests.
2. The endpoint query SHALL filter `WHERE "CreatedByUserId" = currentUserId AND "IsDeleted" = false`, ensuring strict privacy and isolation between users.
3. Legacy system books or unowned books (`CreatedByUserId IS NULL`) SHALL NOT appear in the user's active library catalog.

#### Scenario: User queries their library catalog
- **WHEN** an authenticated user calls `GET /api/v1/library/books`
- **THEN** only books where `CreatedByUserId` equals the authenticated user's ID are returned
- **AND** books belonging to other users or legacy unowned records are excluded.

### Requirement: Starter Handbook Deletion Autonomy and Empty State Transition
A user SHALL have full authorization to delete their provisioned starter handbook via `DELETE /api/v1/library/books/{id}`.

1. When the authenticated user requests deletion of a book they own (including the starter handbook), the system SHALL soft-delete the book (`IsDeleted = true`) and all associated `DocumentChunks`.
2. Any active `UserBookPacer` pointing to the deleted book SHALL be deactivated.
3. When zero active books remain in the user's library, the UI across `/library`, `/today`, and `/roadmap` SHALL display an empty state that clearly guides the user to import their own technical documents (PDF upload, web crawler, or markdown series).

#### Scenario: User deletes their starter handbook
- **WHEN** an authenticated user deletes their "Senior Engineering Craft Handbook" from the library
- **THEN** the server returns `HTTP 200 OK` with `{ "success": true }`
- **AND** subsequent queries to `GET /api/v1/library/books` return an empty list with `totalCount = 0`
- **AND** the `/library` view displays the zero-book empty state prompting document ingestion.
