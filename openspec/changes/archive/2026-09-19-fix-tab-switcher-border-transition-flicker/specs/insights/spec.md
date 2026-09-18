# Spec Delta

## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog decoupled from library documents and expose paginated/random browsing APIs alongside a dynamic metadata query endpoint `GET /api/v1/insights/meta`, adhering to the **Dev-Learning Studio** visual theme:

1. **Obsidian Studio Canvas & Header Elevation:**
   - The `/insights` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate backgrounds.
   - The Header Banner SHALL render using `.glass-panel` elevation with translucent hairline borders (`border-white/[0.08]`), removing legacy indigo-to-slate gradient overlays.
   - The Shuffle button and "Generate with AI" button SHALL use Studio action styling with Iris Violet accents (`bg-brand-600 hover:bg-brand-500 text-white`).

2. **Dedicated View Mode Switcher & Filter Chips (Zero-Width-Shift Transitions):**
   - The View Mode Switcher (`[ 🌐 Khám Phá ]` / `[ 🔖 Đã Lưu (N) ]`) SHALL render with glass pill styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`).
   - Switcher buttons SHALL maintain a constant 1px border geometry across both active and inactive states (`border border-transparent` base/inactive; `border-slate-200/80 dark:border-white/[0.12]` active).
   - Switcher button animations SHALL be constrained to `transition-colors`, preventing `border-width` collapse, default preflight gray color flashing, or subpixel jitter when switching view modes.
   - Category filter chips SHALL render with subtle dark glass styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`), highlighting the active topic chip cleanly.

3. **Glass Insight Card Reader:**
   - Technical insight cards SHALL render as `.glass-card` containers with hairline borders (`border-white/[0.08]`).
   - Code blocks (problematic vs idiomatic solution) SHALL render in high-contrast obsidian panels (`bg-black/40 border-white/[0.06]`) with syntax highlighting.
   - Tailored Saved empty state SHALL render in a `.glass-card` container with Iris Violet action CTA.

4. **On-Demand AI Synthesis Modal:**
   - The AI generation modal SHALL render with `.glass-panel` elevation, dark glass input fields, and Iris Violet generate CTA.

#### Scenario: User requests next technical insight card
- **WHEN** user sends `GET /api/v1/insights/feed` with optional `category` or `tag` query parameters
- **THEN** the system returns a sequence of concise senior technical insight cards containing problem context, under-the-hood analysis, bad vs good code snippets, and benchmark performance stats.

#### Scenario: User navigates insights on frontend card reader
- **WHEN** user visits `/insights` and clicks "Next Insight ➔" or presses Space/ArrowRight
- **THEN** the card reader smoothly transitions to the next technical insight with syntax-highlighted code blocks and category badges
- **AND** the category filter row renders exclusively pure-text category chips (`[ Tất Cả Chủ Đề ]` and dynamic category names) while the dedicated View Mode Switcher independently displays the active view mode.

#### Scenario: Client requests insights metadata and dynamic topic suggestions
- **WHEN** client sends `GET /api/v1/insights/meta`
- **THEN** system queries `TechInsights` to compile category metadata including category IDs, keys, localized English and Vietnamese labels, and published card counts
- **AND** queries active `Topics` (from the 30-Day Curriculum) in PostgreSQL to extract curated topic titles grouped by category
- **AND** returns HTTP 200 with `{ categories, suggestedTopics }`
- **AND** client dynamically populates filter chips and the AI generation modal suggestion pool without relying on hardcoded arrays.

#### Scenario: User toggles between Explore and Saved view modes
- **GIVEN** an authenticated user on `/insights` with 5 bookmarked insights
- **WHEN** user clicks `[ 🔖 Đã Lưu (5) ]` on the View Mode Switcher
- **THEN** client sets `viewMode = 'saved'`, sets `onlyBookmarked = true`, and fetches the user's bookmarked insights
- **AND** the View Mode Switcher visually highlights the Saved tab with active styling
- **WHEN** user clicks `[ 🌐 Khám Phá ]`
- **THEN** client sets `viewMode = 'explore'`, sets `onlyBookmarked = false`, and restores the general insights feed.

#### Scenario: User filters saved bookmarks by category
- **GIVEN** user is in Saved mode (`viewMode = 'saved'`) with bookmarked insights spanning multiple categories
- **WHEN** user clicks the `.NET & C#` category chip in the category filter bar
- **THEN** client sends `GET /api/v1/insights/feed?category=1&onlyBookmarked=true&page=1&pageSize=50`
- **AND** the card reader displays only bookmarked insights belonging to `.NET & C#`
- **AND** the category chip retains active selection styling.

#### Scenario: User views tailored empty state in Saved mode with zero bookmarks
- **GIVEN** an authenticated user who has not saved any insights (`bookmarkedInsights.length === 0`)
- **WHEN** user switches to `[ 🔖 Đã Lưu (0) ]` mode
- **THEN** page renders the tailored Saved empty state with the `BookmarkCheck` icon
- **AND** displays title "Bạn chưa lưu mẫu kiến thức nào"
- **AND** displays description "Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau."
- **AND** displays the CTA button `[ 🌐 Khám Phá Kiến Thức Ngay ]`.

#### Scenario: User clicks CTA in Saved empty state to return to Explore mode
- **GIVEN** user is viewing the tailored Saved empty state on `/insights`
- **WHEN** user clicks `[ 🌐 Khám Phá Kiến Thức Ngay ]`
- **THEN** client transitions `viewMode` back to `'explore'`
- **AND** loads the general insights feed for the active category.

#### Scenario: User bookmarks an insight card and views updated bookmark count in switcher
- **GIVEN** user is browsing in Explore mode and the View Mode Switcher displays `[ 🔖 Đã Lưu (3) ]`
- **WHEN** user clicks the bookmark button on an unbookmarked insight card
- **THEN** client calls `POST /api/v1/insights/{id}/bookmark`
- **AND** card bookmark status toggles to active
- **AND** the View Mode Switcher counter reactively increments to `[ 🔖 Đã Lưu (4) ]`.
