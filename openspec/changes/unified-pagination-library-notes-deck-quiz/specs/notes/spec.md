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

The `/notes` interface SHALL support flexible presentation modes:
1. **Numbered Pagination**: Standard pagination controls (`< 1 2 3 ... 8 >`) enabling direct jumping to any page in the user's reading history.
2. **Streamed "Load More"**: An optional append action ("Load More Notes" / "Tải thêm ghi chú") that retrieves subsequent pages and appends them to the current list without resetting the scroll position.
3. **Dynamic Tag Chip Bar**: A horizontal scrollable bar rendered above the list with a default "Tất cả (Total)" chip and dynamic `#tag (count)` chips populated from `tagCounts`.
4. **Conjunctive Filtering & Page Reset**: Selecting a tag chip or updating the search query SHALL conjunctively filter highlights and automatically reset the active page to 1.
5. **Persistent Badge & Inline Edit Preservation**: Highlights converted into flashcards (`HasFlashcard = true`) SHALL retain their disabled green `<Check />` "In SM-2" badge across page turns, stream appends, and inline reflection updates.
6. **Two-Way URL Query Synchronization**: The active `page`, `tag`, and `search` query SHALL synchronize bidirectionally with URL query parameters (`?page=N&tag=T&search=S`).

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
