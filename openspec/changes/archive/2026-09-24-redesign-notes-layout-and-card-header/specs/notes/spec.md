# Spec Delta: notes

## MODIFIED Requirements

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

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Dev-Learning Studio** visual theme:

1. **Obsidian Studio Canvas & Glass Controls**:
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate backgrounds.
   - The search input SHALL render with subtle dark glass backgrounds (`bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08]`) and hairline borders.
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents (`bg-brand-600 text-white border-transparent shadow-sm`) and inactive chips with dark glass styling (`bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200 dark:border-white/[0.08] hover:border-white/[0.16]`).

2. **Glass Card Highlight Items**:
   - Highlight cards SHALL render as `.glass-card` containers with translucent hairline borders (`border-white/[0.08]`), elevating slightly on hover.
   - Excerpt quotes SHALL display subtle left accent borders in Iris Violet (`border-l-2 border-brand-500/60 bg-white/[0.02]`) with high-contrast text.
   - Flashcard action buttons SHALL use Iris Violet brand styling (`bg-brand-600/10 text-brand-400 border-brand-500/20 hover:bg-brand-600 hover:text-white`), transitioning to a clean green badge (`bg-emerald-500/10 text-emerald-400 border-emerald-500/20`) when converted to SM-2.
   - Tag badges within cards SHALL render with neutral dark glass styling (`bg-white/[0.04] text-slate-400 border-white/[0.06]`).

3. **Card Header (Source Document Reference)**:
   - The top row of each highlight card SHALL be dedicated exclusively to document source metadata:
     - `BookOpen` icon, Book Title (`font-bold truncate text-slate-800 dark:text-slate-200`), and Chapter Title (`truncate text-slate-500 dark:text-slate-400`).
     - A compact delete icon button (`Trash2`, `p-1.5 rounded-lg text-slate-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/30`) on the far right.
   - Action buttons with full text labels SHALL NOT be placed in the card header, preventing horizontal layout compression and eliminating text truncation into clipped characters (`📖 : • \`).

4. **Card Body (Excerpt & Reflection)**:
   - Excerpt quotes SHALL display subtle left accent borders in Iris Violet (`border-l-2 border-brand-500/60 bg-white/[0.02] pl-3 py-1`) with high-contrast text.
   - Reflection notes and inline editing forms SHALL render beneath the excerpt quote.

5. **Card Footer (Actions & Tags)**:
   - Each highlight card SHALL feature a dedicated footer section separated by a top border divider (`pt-3 border-t border-slate-100 dark:border-white/[0.04]`).
   - The footer SHALL organize metadata and primary actions:
     - Left: Technical tag badges with tag selection triggers (`#tag`).
     - Right: Action buttons:
       - **Edit Note**: Button with `Pencil` icon and localized text ("Edit Note" / "Chỉnh sửa ghi chú").
       - **Flashcard SM-2**: Button with `Zap` or `Check` icon and localized state text ("Flashcard SM-2" or "In SM-2" / "Đã Trong SM-2").

6. **Inline Note & Tag Editor**:
   - The inline editor textarea and tag inputs SHALL render with `.glass-panel` styling over `dark:bg-canvas-elevated`, with Iris Violet save button accents (`bg-brand-600 hover:bg-brand-500 text-white`).

7. **Data Model & Scoped Behavior**:
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

#### Scenario: Highlight card header displays legible book and chapter titles
- **WHEN** an authenticated user views a note card on `/notes`
- **THEN** the card header displays the book title and chapter title with full horizontal space
- **AND** text is not squished or truncated into unreadable dots (`: • \`) by adjacent multi-button clusters
- **AND** hovering or inspecting displays the full document titles.

#### Scenario: Interacting with highlight actions in the card footer
- **WHEN** user views the bottom of a note card on `/notes`
- **THEN** the tag badges appear on the left side of the footer divider
- **AND** the "Edit Note" and "Flashcard SM-2" action buttons appear on the right side of the footer divider with comfortable click targets.
