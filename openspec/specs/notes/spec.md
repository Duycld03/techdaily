# notes Specification

## Purpose
Provides a dedicated reading notes and highlights management hub (`/notes`), enabling software engineers to curate chapter highlights, edit personal technical reflections and tags inline, filter by tags and search keywords, and deliberately generate SuperMemo SM-2 flashcards without interface clutter from saved insights.

## Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Dev-Learning Studio** visual theme:

1. **Obsidian Studio Canvas & Glass Controls:**
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate backgrounds.
   - The search input SHALL render with subtle dark glass backgrounds (`bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08]`) and hairline borders.
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents (`bg-brand-600 text-white border-transparent shadow-sm`) and inactive chips with dark glass styling (`bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200 dark:border-white/[0.08] hover:border-white/[0.16]`).

2. **Glass Card Highlight Items:**
   - Highlight cards SHALL render as `.glass-card` containers with translucent hairline borders (`border-white/[0.08]`), elevating slightly on hover.
   - Excerpt quotes SHALL display subtle left accent borders in Iris Violet (`border-l-2 border-brand-500/60 bg-white/[0.02]`) with high-contrast text.
   - Flashcard action buttons SHALL use Iris Violet brand styling (`bg-brand-600/10 text-brand-400 border-brand-500/20 hover:bg-brand-600 hover:text-white`), transitioning to a clean green badge (`bg-emerald-500/10 text-emerald-400 border-emerald-500/20`) when converted to SM-2.
   - Tag badges within cards SHALL render with neutral dark glass styling (`bg-white/[0.04] text-slate-400 border-white/[0.06]`).

3. **Inline Note & Tag Editor:**
   - The inline editor textarea and tag inputs SHALL render with `.glass-panel` styling over `dark:bg-canvas-elevated`, with Iris Violet save button accents (`bg-brand-600 hover:bg-brand-500 text-white`).

4. **Data Model & Scoped Behavior:**
   - The page SHALL NOT include a "Saved Insights" tab, delegating all insight bookmarking exclusively to `/insights`.
   - The backend endpoint `GET /api/v1/notes/highlights` SHALL support offset pagination and search filtering.

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
The technical notes archive interface (`frontend/pages/notes.vue`) SHALL implement the `BoardLayout` archetype (`BoardLayout.vue`), replacing the narrow single-column list with an expansive, auto-flowing knowledge card board.
1. **Header Slot (`#header`)**:
   - Houses the Notes Archive title, subtitle, and primary action triggers.
2. **Filters Slot (`#filters`)**:
   - Houses the full-text search input with ⌘K shortcut badge and horizontal scrollable tag filter chips (#All, #Database, #Vue, #Kafka).
3. **Content Grid Slot (`#content`)**:
   - Renders saved technical highlights in a responsive auto-flowing grid: 1 column on mobile, 2 columns on tablets/small laptops (`md:grid-cols-2`), and 3 columns on standard desktop viewports (`xl:grid-cols-3 gap-4`).
   - Eliminates $> 400\text{px}$ dead black margins on both sides of the screen.
4. **Pagination Slot (`#pagination`)**:
   - Houses the paginated navigation controls (`BasePagination.vue`).

#### Scenario: Browsing Technical Highlights on Desktop
- **WHEN** an engineer views their saved highlights on a 1920x1080 display
- **THEN** highlights render as compact, structured cards distributed evenly across a 2-to-3 column grid spanning the available container width, with search and tag filters pinned at the top.
