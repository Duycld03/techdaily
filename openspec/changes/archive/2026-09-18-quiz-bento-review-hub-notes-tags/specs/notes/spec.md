## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`). The page SHALL NOT include a "Saved Insights" tab, delegating all insight bookmarking exclusively to `/insights` (`[Đã Lưu]`).

The backend endpoint `GET /api/v1/notes/highlights` SHALL support offset pagination and search filtering with query parameters: `page` (integer, default 1, minimum 1), `pageSize` (integer, default 15, minimum 1, maximum 100), optional `tag` (string), and optional `search` (string). The response SHALL return a structured envelope containing:
1. `highlights`: A list of `HighlightDto` items for the requested page slice, ordered by `CreatedAt` descending.
2. `totalCount`: The total number of highlights matching the filter criteria.
3. `page`: The current active page number (1-based).
4. `pageSize`: The page size applied to the query.
5. `totalPages`: The total number of pages calculated as $\lceil \text{totalCount} / \text{pageSize} \rceil$, or 0 if `totalCount` is 0.
6. `tagCounts`: A list of `TagCountDto` objects (`{ tag: string, count: number }`) computed across the authenticated user's entire highlight corpus, ensuring tag frequencies in the UI remain complete and accurate regardless of pagination.

The `/notes` interface SHALL feature a horizontal scrollable dynamic tag chip bar located above the highlights list:
1. **Default Chip**: `Tất cả (N)` displaying the total count of saved reading highlights, active by default (`selectedTag === null`).
2. **Dynamic Tag Chips**: Automatically extracted from all non-empty tags across the user's saved highlights (e.g. `#dotnet (4)`, `#architecture (3)`, `#performance (2)`), deduplicated, and sorted in descending order of frequency, then alphabetically.
3. **Interactive Tag Selection**: Clicking any tag chip filters the highlights list to show only highlights containing that specific tag, highlighting the active chip with distinct brand accent styling; clicking an already selected chip or the "Tất cả" chip resets the filter to show all highlights.
4. **Seamless Conjunctive Filtering**: The active tag filter composes conjunctively (`AND`) with the text search query in `highlightSearchQuery`, filtering highlights to those that match both the selected tag and the search keyword across excerpt text, personal reflection note, book title, or chapter title.
5. **Horizontal Scrollable Container**: The tag bar supports smooth horizontal touch/wheel scrolling on mobile and desktop viewports (`overflow-x-auto no-scrollbar`), maintaining touch-friendly button sizing ($\ge 36\text{px}$ height) and preventing multi-row wrap clutter.

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
