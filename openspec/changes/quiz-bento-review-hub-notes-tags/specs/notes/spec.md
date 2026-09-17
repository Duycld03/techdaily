## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying all user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`). The page SHALL NOT include a "Saved Insights" tab, delegating all insight bookmarking exclusively to `/insights` (`[Đã Lưu]`).

The `/notes` page SHALL feature a horizontal scrollable dynamic tag chip bar located above the highlights list:
1. **Default Chip**: `Tất cả (N)` displaying the total count of saved reading highlights, active by default (`selectedTag === null`).
2. **Dynamic Tag Chips**: Automatically extracted from all non-empty tags across the user's saved highlights (e.g. `#dotnet (4)`, `#architecture (3)`, `#performance (2)`), deduplicated, and sorted in descending order of frequency, then alphabetically.
3. **Interactive Tag Selection**: Clicking any tag chip filters the highlights list to show only highlights containing that specific tag, highlighting the active chip with distinct brand accent styling; clicking an already selected chip or the "Tất cả" chip resets the filter to show all highlights.
4. **Seamless Conjunctive Filtering**: The active tag filter composes conjunctively (`AND`) with the text search query in `highlightSearchQuery`, filtering highlights to those that match both the selected tag and the search keyword across excerpt text, personal reflection note, book title, or chapter title.
5. **Horizontal Scrollable Container**: The tag bar supports smooth horizontal touch/wheel scrolling on mobile and desktop viewports (`overflow-x-auto no-scrollbar`), maintaining touch-friendly button sizing ($\ge 36\text{px}$ height) and preventing multi-row wrap clutter.

#### Scenario: User navigates to reading notes hub
- **GIVEN** an authenticated user who opens `/notes`
- **WHEN** the page loads reading highlights
- **THEN** system loads all highlights via `GET /api/v1/notes/highlights`
- **AND** returns highlight DTOs containing `HasFlashcard = true` for highlights that have already been converted into an SM-2 spaced repetition card
- **AND** renders a single unified list of reading highlights where highlights with `hasFlashcard: true` automatically display the disabled "In SM-2" check badge across initial page loads and refreshes without "Saved Insights" tabs or bookmark unpinning modals.

#### Scenario: User filters notes by search query or tag
- **GIVEN** an authenticated user viewing their reading highlights on `/notes`
- **WHEN** user types a search string or clicks a tag pill in `/notes`
- **THEN** client instantly filters highlights matching selected text, reflection note, book title, chapter title, or tag names without making additional server round-trips.

#### Scenario: User deletes a reading highlight
- **GIVEN** an authenticated user viewing a highlight card on `/notes`
- **WHEN** user clicks `Delete` on a highlight card and confirms in the deletion dialog
- **THEN** client invokes `DELETE /api/v1/notes/highlights/{id}`
- **AND** removes the highlight card from the local view and displays a confirmation toast (`notes.toast_delete_success`).

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
