# Spec Delta

## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View

The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Showcase Design System** note card archetype with uniform row geometry:

1. **Obsidian Studio Canvas & Glass Controls**:
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base).
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents and inactive chips with dark glass styling.
   - The "Tất cả" (All) filter selector SHALL display the user's total unfiltered highlights count (`TotalAllCount`) invariant across active tag or search queries, guaranteeing that selecting a tag does not reduce or mutate the total count shown on the "Tất cả" button.
   - Tag filter chips and the "Tất cả" button SHALL enforce a constant font weight (`font-semibold`) and scoped transitions (`transition-colors duration-150`), eliminating text width expansion and horizontal layout jitter.

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

#### Scenario: Tag filtering preserves invariant total count on All button
- **GIVEN** a user has 8 total highlights across tags `#1 (2)` and `#abc (1)`
- **WHEN** the user selects tag `#1`
- **THEN** the `#1` tag button displays active styling
- **AND** the "Tất cả" button continues to display `Tất cả (8)` with invariant button width, rather than mutating to `Tất cả (2)`.
