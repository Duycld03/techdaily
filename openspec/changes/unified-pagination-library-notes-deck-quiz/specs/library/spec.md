## ADDED Requirements

### Requirement: Paginated Book Catalog Browsing and State Synchronization
The library system SHALL expose an endpoint `GET /api/v1/library/books` accepting optional query parameters: `category` (enum/integer), `search` (string), `page` (integer, default 1, minimum 1), and `pageSize` (integer, default 12, minimum 1, maximum 100). The endpoint SHALL return a structured envelope containing:
1. `books`: A list of `BookDto` items matching filter criteria, ordered by `CreatedAt` descending.
2. `totalCount`: Total number of books matching the query across the entire catalog.
3. `page`: The current active page number (1-based).
4. `pageSize`: The page size applied to the query.
5. `totalPages`: The total number of pages calculated as $\lceil \text{totalCount} / \text{pageSize} \rceil$, or 0 if `totalCount` is 0.

The `/library` page interface SHALL render book cards in a responsive grid aligned to multiples of 12 (1 column on mobile, 2 columns on tablet, 3 columns on desktop, 4 columns on large monitors), preventing ragged trailing rows.

The `/library` page SHALL render numbered pagination controls (`< 1 2 3 ... 8 >`) when `totalPages > 1`:
1. **Numbered Page Buttons**: Direct access buttons for available pages with an active highlight indicator on the currently viewed page.
2. **Ellipsis Compaction**: Pages beyond the visible window SHALL be truncated with non-clickable ellipsis (`...`) indicators.
3. **Previous / Next Controls**: Navigational buttons to decrement or increment the active page, automatically disabled on boundary pages (`page === 1` and `page === totalPages`).
4. **Filter Reset**: Applying a new category filter or entering a search query SHALL automatically reset the active page to 1.
5. **Two-Way URL Query Synchronization**: The active `page`, `category`, and `search` query SHALL synchronize bidirectionally with browser URL query parameters (`?page=N&category=C&search=S`). Reloading the page or sharing the URL SHALL restore the exact catalog page and filter state.

#### Scenario: User browses the first page of the technical library
- **WHEN** an authenticated or anonymous user navigates to `/library`
- **THEN** client calls `GET /api/v1/library/books?page=1&pageSize=12`
- **AND** backend returns up to 12 book cards along with `totalCount`, `page = 1`, `pageSize = 12`, and `totalPages`
- **AND** client renders the books in a balanced responsive grid with page 1 highlighted in the pagination controls.

#### Scenario: User navigates to a subsequent catalog page
- **GIVEN** the library catalog has 30 books (`totalPages = 3`)
- **WHEN** the user clicks page number "2" in the pagination bar
- **THEN** client updates the URL query string to `?page=2`
- **AND** fetches books via `GET /api/v1/library/books?page=2&pageSize=12`
- **AND** smoothly replaces the grid items with books 13 through 24 and highlights button "2".

#### Scenario: User filters catalog by category or search term
- **GIVEN** the user is currently viewing page 3 of the catalog (`?page=3`)
- **WHEN** the user selects category "Backend .NET" or enters search term "architecture"
- **THEN** client resets the active page to 1
- **AND** updates URL query string to `?category=1&search=architecture&page=1`
- **AND** requests page 1 of the filtered result set from the backend.

#### Scenario: User enters direct URL with pagination and filter parameters
- **WHEN** a user navigates directly to `/library?category=2&search=postgres&page=2` via bookmark or shared link
- **THEN** client parses `category = 2`, `search = 'postgres'`, and `page = 2` from the route query
- **AND** dispatches `GET /api/v1/library/books?category=2&search=postgres&page=2&pageSize=12`
- **AND** renders the second page of matching database books with active filter states displayed.

#### Scenario: User navigates using browser back and forward history
- **GIVEN** the user navigated from page 1 to page 2 and then page 3 in the library
- **WHEN** the user clicks the browser "Back" button
- **THEN** client responds to the URL query change to `?page=2`
- **AND** fetches and displays page 2 items without a full page reload or layout flickering.

#### Scenario: User requests an out-of-bounds page number
- **WHEN** a user manually enters a URL with `?page=999` exceeding `totalPages`
- **THEN** backend returns an empty `books` list with the accurate `totalCount` and `totalPages`
- **AND** client displays a friendly empty state prompting the user to return to page 1.

#### Scenario: Mobile viewport renders compact responsive pagination controls
- **GIVEN** the user accesses `/library` on a narrow mobile viewport ($< 640\text{px}$)
- **WHEN** the catalog contains multiple pages
- **THEN** pagination controls condense the numbered buttons to the current page and immediate neighbors while maintaining minimum touch target dimensions ($\ge 40\text{px} \times 40\text{px}$) and touch accessibility.
