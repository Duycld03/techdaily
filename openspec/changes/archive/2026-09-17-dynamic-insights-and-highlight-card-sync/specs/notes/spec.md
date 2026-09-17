## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying all user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`). The page SHALL NOT include a "Saved Insights" tab, delegating all insight bookmarking exclusively to `/insights` (`[Đã Lưu]`).

#### Scenario: User navigates to reading notes hub
- **WHEN** an authenticated user opens `/notes`
- **THEN** system loads all highlights via `GET /api/v1/notes/highlights`
- **AND** returns highlight DTOs containing `HasFlashcard = true` for highlights that have already been converted into an SM-2 spaced repetition card
- **AND** renders a single unified list of reading highlights where highlights with `hasFlashcard: true` automatically display the disabled "In SM-2" check badge across initial page loads and refreshes without "Saved Insights" tabs or bookmark unpinning modals.

#### Scenario: User filters notes by search query or tag
- **WHEN** user types a search string or clicks a tag pill in `/notes`
- **THEN** client instantly filters highlights matching selected text, reflection note, book title, chapter title, or tag names without making additional server round-trips.

#### Scenario: User deletes a reading highlight
- **WHEN** user clicks `Delete` on a highlight card and confirms in the deletion dialog
- **THEN** client invokes `DELETE /api/v1/notes/highlights/{id}`
- **AND** removes the highlight card from the local view and displays a confirmation toast (`notes.toast_delete_success`).

---

### Requirement: Deliberate Flashcard SM-2 Creation from Notes
Each highlight card in `/notes` SHALL feature a deliberate "Flashcard SM-2" action button to convert the excerpt and reflection into an active recall spaced repetition card (`POST /api/v1/review/cards/from-highlight`). Flashcard creation state SHALL be persistently reflected on the highlight card: upon card creation or initial page load where `HasFlashcard = true`, the action button transitions to a disabled green badge displaying `<Check />` and localized text `notes.in_sm2` ("Đã Trong SM-2" / "In SM-2"), preserving state across browser refreshes. Flashcards generated from highlights SHALL maintain independent content storage (`FrontMarkdown` and `BackMarkdown`), ensuring that subsequent modification or deletion of the parent highlight never deletes, cascades, or corrupts the flashcard in the SM-2 review deck.

#### Scenario: User creates SM-2 flashcard from highlight
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes`
- **THEN** client invokes `POST /api/v1/review/cards/from-highlight` with `highlightId` and current user `locale`
- **AND** backend creates or retrieves the `SpacedRepetitionCard` with `SourceType = CardSourceType.Highlight`, `SourceHighlightId = highlightId`, `FrontMarkdown = excerpt`, and `BackMarkdown = note / summary`
- **AND** frontend displays a success toast (`notes.toast_flashcard_success`) and immediately marks the card as created with the green `In SM-2` check badge.

#### Scenario: User reloads notes hub after creating flashcards
- **WHEN** user reloads `/notes` (F5) or revisits the page after previously converting highlights into flashcards
- **THEN** client fetches highlights via `GET /api/v1/notes/highlights`
- **AND** backend queries `SpacedRepetitionCards` for the current user and returns `HasFlashcard = true` for each linked highlight
- **AND** client populates `createdCardHighlightIds` with all highlight IDs having `hasFlashcard: true`
- **AND** all previously converted highlights immediately render the disabled green `<Check />` `In SM-2` button without reverting to the amber `⚡ Flashcard SM-2` state.

#### Scenario: Highlight deleted after flashcard generation
- **WHEN** user deletes a highlight that previously generated an SM-2 flashcard
- **THEN** backend soft-deletes the `UserHighlight` record
- **AND** the foreign key on `SpacedRepetitionCards.SourceHighlightId` is set to null via `onDelete: ReferentialAction.SetNull`
- **AND** the flashcard remains fully intact and schedulable in the user's review deck with its persisted front and back markdown.
