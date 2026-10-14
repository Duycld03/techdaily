# Library Delta

## MODIFIED Requirements

### Requirement: Starter Handbook Deletion Autonomy and Empty State Transition
A new user's library SHALL start empty; no starter content is provisioned on registration. The initial empty state SHALL guide users to import their own documents or explicitly choose a prepared document from Discover.

1. When the authenticated user requests deletion of a book they own via `DELETE /api/v1/library/books/{id}`, the system SHALL soft-delete the book (`IsDeleted = true`) and all associated `DocumentChunks`.
2. Any active `UserBookPacer` pointing to the deleted book SHALL be deactivated.
3. When zero active books remain in the user's library, the UI across `/library`, `/today`, and `/roadmap` SHALL display an empty state with personal import guidance (PDF upload, web crawler, or markdown series) and a link to `/library?tab=discover`.

#### Scenario: New account starts with an empty library
- **WHEN** a newly registered user opens `/library`, `/today`, or `/roadmap` before importing or adopting any document
- **THEN** the view displays the zero-book empty state prompting personal document ingestion or browsing Discover
- **AND** `GET /api/v1/library/books` returns an empty list with `totalCount = 0`.

#### Scenario: User deletes their starter handbook
- **WHEN** an authenticated user deletes the last remaining book from their library
- **THEN** the server returns `HTTP 200 OK` with `{ "success": true }`
- **AND** subsequent queries to `GET /api/v1/library/books` return an empty list with `totalCount = 0`
- **AND** the `/library` view displays the zero-book empty state offering personal import and discovery.

## ADDED Requirements

### Requirement: Library and discovery navigation
The library SHALL expose My Library and Discover tabs with URL-synchronized tab, search, filters and page state. My Library SHALL remain the default and continue to return only the current user's books. Discovery requests SHALL use a separate catalog and SHALL NOT merge shared entries into personal results.

#### Scenario: Navigate and reload discovery
- **WHEN** a learner opens `/library?tab=discover`, applies filters and reloads or navigates back
- **THEN** the selected tab and query state are restored
- **AND** changing filters resets that tab's page to one without mixing results from the other tab.

#### Scenario: Adopt and open a document
- **WHEN** a learner adds a catalog document
- **THEN** My Library shows the owned ready copy and provides the existing reader entry point
- **AND** adoption alone does not advance reading progress or switch the active book pacer.

### Requirement: Owned document access for adopted books
Personal document read and mutation operations SHALL enforce the authenticated owner's identity for adopted books as for personal imports. Catalog identifiers SHALL NOT grant access through personal book, slice, export, curation, audio, quiz, pacer or graph interfaces.

#### Scenario: Request another learner's adopted book
- **WHEN** a learner uses another user's book or slice identifier through a personal document operation
- **THEN** the operation returns 404 without exposing content or changing learning state.

#### Scenario: Use an owned copy after catalog hiding
- **WHEN** a learner opens their own adopted book after its catalog source is hidden
- **THEN** reading and existing learning actions continue against the owned copy.
