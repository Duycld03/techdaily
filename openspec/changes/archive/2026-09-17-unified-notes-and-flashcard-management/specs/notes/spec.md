## Purpose
Provides a dedicated reading notes and highlights management hub (`/notes`), enabling software engineers to curate chapter highlights, edit personal technical reflections and tags inline, filter by tags and search keywords, and deliberately generate SuperMemo SM-2 flashcards without interface clutter from saved insights.

## ADDED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying all user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, and timestamps. The page SHALL NOT include a "Saved Insights" tab, delegating all insight bookmarking exclusively to `/insights` (`[🔖 Đã lưu]`).

#### Scenario: User navigates to reading notes hub
- **WHEN** an authenticated user opens `/notes`
- **THEN** system loads all highlights via `GET /api/v1/notes/highlights`
- **AND** renders a single unified list of reading highlights without "Saved Insights" tabs or bookmark unpinning modals.

#### Scenario: User filters notes by search query or tag
- **WHEN** user types a search string or clicks a tag pill in `/notes`
- **THEN** client instantly filters highlights matching selected text, reflection note, book title, chapter title, or tag names without making additional server round-trips.

#### Scenario: User deletes a reading highlight
- **WHEN** user clicks `Delete` on a highlight card and confirms in the deletion dialog
- **THEN** client invokes `DELETE /api/v1/notes/highlights/{id}`
- **AND** removes the highlight card from the local view and displays a confirmation toast (`notes.toast_delete_success`).

---

### Requirement: Highlight Reflection and Tag Updating
The system SHALL provide an API endpoint `PUT /api/v1/notes/highlights/{id}` allowing users to update the personal reflection note and technical tags of an existing reading highlight. The frontend `/notes` interface SHALL provide an inline editing mechanism for highlights, enabling in-place editing of notes and tags without page reloads.

#### Scenario: User saves updated reflection note and tags inline
- **WHEN** user clicks "Edit Note" on a highlight card in `/notes`, modifies the note text and tag list, and clicks "Save Changes"
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}` with `note` and `tags`
- **AND** backend validates the request, updates `Note`, `Tags`, and `UpdatedAt` on the entity, and returns HTTP 200 OK with the updated highlight DTO
- **AND** frontend updates the highlight card in `useNotesStore` and displays a success toast (`notes.toast_update_success`).

#### Scenario: User clears reflection note
- **WHEN** user clears the note text and submits the inline editor
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}` with `note = null` or empty string
- **AND** backend clears the `Note` property on the highlight and persists changes.

#### Scenario: Unauthorized update attempt
- **WHEN** user attempts to update a highlight belonging to another user account
- **THEN** backend rejects the request with HTTP 404 Not Found or HTTP 403 Forbidden without modifying database records.

---

### Requirement: Deliberate Flashcard SM-2 Creation from Notes
Each highlight card in `/notes` SHALL feature a deliberate "Flashcard SM-2" action button to convert the excerpt and reflection into an active recall spaced repetition card (`POST /api/v1/review/cards/from-highlight`). Flashcards generated from highlights SHALL maintain independent content storage (`FrontMarkdown` and `BackMarkdown`), ensuring that subsequent modification or deletion of the parent highlight never deletes, cascades, or corrupts the flashcard in the SM-2 review deck.

#### Scenario: User creates SM-2 flashcard from highlight
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes`
- **THEN** client invokes `POST /api/v1/review/cards/from-highlight` with `highlightId` and current user `locale`
- **AND** backend creates or retrieves the `SpacedRepetitionCard` with `SourceType = CardSourceType.Highlight`, `SourceHighlightId = highlightId`, `FrontMarkdown = excerpt`, and `BackMarkdown = note / summary`
- **AND** frontend displays a success toast (`notes.toast_flashcard_success`) and marks the card as created.

#### Scenario: Highlight deleted after flashcard generation
- **WHEN** user deletes a highlight that previously generated an SM-2 flashcard
- **THEN** backend soft-deletes the `UserHighlight` record
- **AND** the foreign key on `SpacedRepetitionCards.SourceHighlightId` is set to null via `onDelete: ReferentialAction.SetNull`
- **AND** the flashcard remains fully intact and schedulable in the user's review deck with its persisted front and back markdown.
