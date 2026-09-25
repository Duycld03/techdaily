# notes Specification

## Purpose
Provides a dedicated reading notes and highlights management hub (`/notes`), enabling software engineers to curate chapter highlights, edit personal technical reflections and tags inline, filter by tags and search keywords, and deliberately generate SuperMemo SM-2 flashcards without interface clutter from saved insights.

## Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Showcase Design System** note card archetype with uniform row geometry:

1. **Obsidian Studio Canvas & Glass Controls**:
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base).
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents and inactive chips with dark glass styling.

2. **Showcase-Archetype Uniform Highlight Cards**:
   - Highlight cards SHALL render as clean elevation containers with uniform vertical geometry (~175px–185px) to eliminate row height disparity and empty bottom gaps in grid rows:
     - **Header**: Primary tag badge pill on the left, document source context on the right, and a responsive hover delete trigger (`Trash2`).
     - **Balanced Clamped Body**:
       - When a personal reflection note exists, the note SHALL render with prominent weight clamped to at most 2 lines (`line-clamp-2`), followed by the source excerpt quote clamped to at most 1 line (`line-clamp-1`).
       - When no personal reflection note exists, the source excerpt quote SHALL render clamped to at most 3 lines (`line-clamp-3`).
     - **Footer**: Technical tag chips on the left, subtle ghost/outline action triggers on the right (`Edit Note` / `Details` and `Flashcard SM-2` / `✓ In SM-2`).
   - Clicking on the note card body SHALL open the Note Details modal in the default "Xem chi tiết / Details" view.

#### Scenario: User views uniform height cards across grid rows
- **WHEN** an authenticated user navigates to `/notes`
- **THEN** all highlight cards in any given grid row render with uniform vertical height (~175px–185px)
- **AND** cards with personal notes clamp the note to 2 lines and excerpt to 1 line
- **AND** cards with quotes only clamp the excerpt to 3 lines
- **AND** zero jagged bottom gaps appear between adjacent cards in the same row track.

#### Scenario: User clicks card to open details view
- **WHEN** a user clicks on the body of a highlight card
- **THEN** system opens the Note Modal with the "Xem chi tiết / Details" tab selected by default
- **AND** displays the complete, un-truncated excerpt quote and formatted personal reflection note.
#### Scenario: User navigates to reading notes hub
- **WHEN** an authenticated user opens `/notes`
- **THEN** system loads the first page of highlights via `GET /api/v1/notes/highlights?page=1&pageSize=15`
- **AND** returns highlight DTOs containing `HasFlashcard = true` for highlights that have already been converted into an SM-2 spaced repetition card
- **AND** returns global `tagCounts` reflecting all active tags across the user's library
- **AND** renders a single unified list of reading highlights where highlights with `hasFlashcard: true` automatically display the disabled "In SM-2" check badge across initial page loads and refreshes without "Saved Insights" tabs or bookmark unpinning modals.

#### Scenario: User filters notes by search query or tag
- **WHEN** user types a search string or clicks a tag pill in `/notes`
- **THEN** client queries `GET /api/v1/notes/highlights?tag={tag}&search={query}&page=1&pageSize=15`
- **AND** instantly updates the active highlight list matching selected text, reflection note, book title, chapter title, or tag names with active page reset to 1.

#### Scenario: User deletes a reading highlight
- **WHEN** user clicks `Delete` on a highlight card and confirms in the deletion dialog
- **THEN** client invokes `DELETE /api/v1/notes/highlights/{id}`
- **AND** removes the highlight card from the local view, decrements `totalCount`, and displays a confirmation toast (`notes.toast_delete_success`).

#### Scenario: User loads paginated reading notes with global tag counts
- **GIVEN** a user has 45 saved highlights across 8 different technical tags
- **WHEN** the user navigates to `/notes`
- **THEN** the client receives 15 highlights for page 1 (`totalPages = 3`, `totalCount = 45`)
- **AND** the dynamic tag chip bar renders chips for all 8 tags with their full global frequencies (e.g. `#dotnet (18)`, `#postgres (12)`), not merely frequencies from the 15 items on page 1.

#### Scenario: User streams additional highlights using Load More button
- **GIVEN** the user is viewing page 1 of 3 on `/notes`
- **WHEN** the user clicks the "Load More Notes" button at the bottom of the list
- **THEN** client requests `GET /api/v1/notes/highlights?page=2&pageSize=15`
- **AND** appends the 15 new highlights to the bottom of the existing list (displaying 30 total cards) without jumping the user's scroll position
- **AND** updates the "Load More" button to indicate 15 remaining notes.

#### Scenario: User navigates reading notes via numbered pagination bar
- **GIVEN** the user prefers paginated browsing with page numbers
- **WHEN** the user clicks page number "2" in the pagination controls
- **THEN** client smoothly scrolls to the top of the notes container
- **AND** replaces the active list with highlights 16 through 30
- **AND** highlights page button "2" as active.

#### Scenario: Active tag filter resets pagination to page 1
- **GIVEN** the user is currently viewing page 3 of all notes
- **WHEN** the user clicks the `#kubernetes` tag chip
- **THEN** client resets the active page to 1
- **AND** updates the URL query string to `?tag=kubernetes&page=1`
- **AND** fetches the first page of Kubernetes-specific highlights.

#### Scenario: URL query synchronization preserves active note page and tag
- **WHEN** the user reloads the browser at `/notes?tag=architecture&page=2`
- **THEN** client parses `tag = 'architecture'` and `page = 2` from route query
- **AND** requests `GET /api/v1/notes/highlights?tag=architecture&page=2&pageSize=15`
- **AND** renders page 2 of architecture highlights with the `#architecture` tag chip highlighted.

#### Scenario: Inline editing and SM-2 badge persistence across paginated pages
- **GIVEN** a highlight on page 2 was previously converted to an SM-2 flashcard (`hasFlashcard: true`)
- **WHEN** the user navigates to page 2, edits the reflection note inline, and clicks "Save Changes"
- **THEN** client invokes `PUT /api/v1/notes/highlights/{id}`
- **AND** backend saves the updated note while retaining `HasFlashcard = true`
- **AND** the card updates its note content in place without reverting the green `<Check />` "In SM-2" badge.

#### Scenario: User views notes page with dynamic tag chips
- **GIVEN** an authenticated user with reading highlights tagged `#dotnet`, `#architecture`, and `#performance`
- **WHEN** the user opens `/notes`
- **THEN** the client renders a horizontal scrollable tag chip bar at the top of the hub
- **AND** the first chip displays "Tất cả" with the total highlight count and active styling
- **AND** subsequent chips display each unique tag with its occurrence count (e.g. "#dotnet (4)", "#architecture (3)").

#### Scenario: User filters highlights by clicking a tag chip
- **GIVEN** the user is viewing the `/notes` page with dynamic tag chips
- **WHEN** the user clicks the `#architecture (3)` chip
- **THEN** the active chip styles update to highlight `#architecture`
- **AND** the highlights list immediately updates to display only the 3 highlights containing the `#architecture` tag without making a server request.

#### Scenario: User combines dynamic tag filter with search input query
- **GIVEN** the user has active tag filter `#dotnet` selected
- **WHEN** the user types "memory" into the search input
- **THEN** the highlights list filters conjunctively, displaying only highlights that both contain the `#dotnet` tag AND include "memory" in the excerpt, reflection note, book title, or chapter title.

#### Scenario: User clears active tag filter by clicking "Tất cả"
- **GIVEN** the user has an active tag filter selected
- **WHEN** the user clicks the "Tất cả" chip or re-clicks the active tag chip
- **THEN** the active tag filter is reset to null
- **AND** the highlights list restores all highlights matching any active search query.

#### Scenario: Horizontal scrolling of tag chips on narrow mobile viewports
- **GIVEN** the user has a diverse library with over 10 distinct tags
- **WHEN** viewed on a mobile viewport ($375\text{px}\text{--}390\text{px}$)
- **THEN** the tag chip bar renders as a single, horizontally scrollable row (`overflow-x-auto no-scrollbar`)
- **AND** allows fluid touch swiping without vertical stacking or breaking page layout bounds.

#### Scenario: Highlight card header displays legible book and chapter titles
- **WHEN** an authenticated user views a note card on `/notes`
- **THEN** the card header displays the book title and chapter title with full horizontal space
- **AND** text is not squished or truncated into unreadable dots (`: • \`) by adjacent multi-button clusters
- **AND** hovering or inspecting displays the full document titles.

#### Scenario: Interacting with highlight actions in the card footer
- **WHEN** user views the bottom of a note card on `/notes`
- **THEN** the tag badges appear on the left side of the footer divider
- **AND** the "Edit Note" and "Flashcard SM-2" action buttons appear on the right side of the footer divider with comfortable click targets.

### Requirement: Highlight Reflection and Tag Updating
The system SHALL provide an accessible, dual-mode modal interface for inspecting and editing reading highlight notes and tags using `AppModal.vue`, featuring a mode switcher between "Xem chi tiết / Details" (default) and "Chỉnh sửa / Edit":

1. **Dual-Mode Modal Architecture**:
   - **Mode Switcher**: The modal header SHALL contain an interactive tab switcher:
     - `Xem chi tiết` (Details / Preview) — active by default upon opening a card.
     - `Chỉnh sửa` (Edit) — active when clicking the explicit "Edit Note" button or switching from Details mode.
   - **Details Mode (Default)**:
     - Displays the source document title and chapter breadcrumb.
     - Displays the complete, un-clamped source excerpt quote in a dedicated reference panel.
     - Displays the personal reflection note rendered as formatted Markdown (supporting bold, italics, bullet lists, and code spans via `markdown-it`).
     - Displays technical tags and an action to generate an SM-2 flashcard or switch directly to Edit mode.
   - **Edit Mode**:
     - Displays the source excerpt reference panel.
     - Displays a multi-line textarea (`rows="4"`) for drafting reflections.
     - Displays an interactive comma-separated tag input field.
     - Displays Cancel and Save buttons with active loading spinner feedback.
   - Pressing `Escape` or clicking outside the modal backdrop SHALL close the modal.

2. **Persistence and Mode Transition**:
   - Successfully saving in Edit mode SHALL invoke `PUT /api/v1/notes/highlights/{id}`, update the highlight in `useNotesStore`, display a success toast (`notes.toast_update_success`), and smoothly transition back to Details mode or close the modal.

#### Scenario: User inspects highlight details in default preview mode
- **WHEN** user clicks on a note card or clicks "Xem chi tiết"
- **THEN** system opens `AppModal` with the Details tab active
- **AND** renders the full original excerpt quote and formatted Markdown reflection note without truncation.

#### Scenario: User switches to edit mode, updates note, and saves
- **WHEN** user is in Details mode and clicks "Chỉnh sửa" (or clicks the Edit button directly from the card)
- **THEN** modal switches to Edit mode displaying the note textarea and tag input
- **WHEN** user modifies note text and clicks "Lưu"
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}`
- **AND** backend saves changes
- **AND** modal updates to reflect the new note in Details mode and presents a success toast.

#### Scenario: User clears reflection note
- **WHEN** user clears the note text and submits the modal editor
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}` with `note = null` or empty string
- **AND** backend clears the `Note` property on the highlight and persists changes.

#### Scenario: Unauthorized update attempt
- **WHEN** user attempts to update a highlight belonging to another user account
- **THEN** backend rejects the request with HTTP 404 Not Found or HTTP 403 Forbidden without modifying database records.
---

### Requirement: Deliberate Flashcard SM-2 Creation from Notes
Each highlight card in `/notes` SHALL feature a deliberate "Flashcard SM-2" action button to convert the excerpt and reflection into an active recall spaced repetition card (`POST /api/v1/review/cards/from-highlight`). Flashcard synthesis SHALL be strictly fail-fast: the backend SHALL invoke AI active recall synthesis and, if the external AI service fails (e.g. network timeout, rate limits, unconfigured key, or provider errors), the system SHALL immediately return an error result without persisting any synthetic fallback cards into the database, preserving data integrity and preventing review deck pollution.

Flashcard creation state SHALL be persistently reflected on the highlight card: upon card creation or initial page load where `HasFlashcard = true`, the action button transitions to a disabled badge displaying `<Check />` and localized text `notes.in_sm2` ("Đã Trong SM-2" / "In SM-2"), preserving state across browser refreshes. When card creation fails due to AI downtime or timeouts, the action button SHALL remain active in the unconverted state, allowing the user to retry when the service recovers.

#### Scenario: User creates SM-2 flashcard from highlight
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes`
- **THEN** client invokes `POST /api/v1/review/cards/from-highlight` with `highlightId` and current user `locale`
- **AND** backend creates or retrieves the `SpacedRepetitionCard` with `SourceType = CardSourceType.Highlight`, `SourceHighlightId = highlightId`, `FrontMarkdown = excerpt`, and `BackMarkdown = note / summary`
- **AND** frontend displays a success toast (`notes.toast_flashcard_success`) and immediately marks the card as created with the green `In SM-2` check badge.

#### Scenario: User attempts to create SM-2 flashcard when AI service fails or times out (Fail-Fast with No Database Writes)
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes` and the AI service times out, exceeds rate limits (429), or returns an error
- **THEN** backend aborts execution and immediately returns an error failure result (HTTP 400 Bad Request)
- **AND** does NOT insert, create, or persist any `SpacedRepetitionCard` records in the database, ensuring zero junk data
- **AND** frontend catches the API failure and presents a localized error toast (`notes.toast_flashcard_error`) to inform the user
- **AND** the highlight card action button remains in the active `⚡ Flashcard SM-2` state without transitioning to the disabled `In SM-2` state, enabling the user to retry later.
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

### Requirement: Technical Notes Board Layout Integration
The technical notes archive interface (`frontend/pages/notes.vue`) SHALL implement the `BoardLayout` archetype (`BoardLayout.vue`) in **flat open-canvas mode** (`:flat="true"`), rendering directly onto the obsidian background canvas (`bg-slate-50 dark:bg-canvas`) without an enclosing outer `.glass-card` container ("card tổng").

1. **Flat Open-Canvas Shell**:
   - The root layout SHALL NOT enclose the page sections within a parent `.glass-card` container or force an artificial overflow scroll box.
   - The page header, search and filter bars, notes content grid, and pagination controls SHALL flow naturally within `max-w-7xl mx-auto space-y-4`, preventing double-card nesting and eliminating artificial black margins.

2. **Header Slot (`#header`)**:
   - Houses the Notes Archive title, subtitle, and primary action triggers directly on the canvas without an enclosing card border.

3. **Filters Slot (`#filters`)**:
   - Houses the full-text search input with shortcut indicator and horizontal scrollable tag filter chips (#All, #Database, #Vue, #Kafka).

4. **Content Grid Slot (`#content`)**:
   - Renders saved technical highlights in a responsive auto-flowing grid: 1 column on mobile, 2 columns on tablets/small laptops (`md:grid-cols-2`), and 3 columns on standard desktop viewports (`xl:grid-cols-3 gap-4`).
   - Each individual note item SHALL render as its own self-contained `.glass-card`.

5. **Pagination Slot (`#pagination`)**:
   - Houses the paginated navigation controls (`BasePagination.vue`).

#### Scenario: Browsing Technical Highlights on Desktop
- **WHEN** an engineer views their saved highlights on a 1920x1080 display
- **THEN** highlights render as compact, structured cards distributed evenly across a 2-to-3 column grid spanning the available container width, with search and tag filters pinned at the top.

#### Scenario: Browsing Technical Highlights on Open Canvas
- **WHEN** an engineer views their saved highlights on `/notes`
- **THEN** the page renders directly on the obsidian canvas without an enclosing outer `.glass-card` shell
- **AND** the individual note cards sit as first-class elevation cards on the canvas
- **AND** zero double-card nesting occurs.

---
