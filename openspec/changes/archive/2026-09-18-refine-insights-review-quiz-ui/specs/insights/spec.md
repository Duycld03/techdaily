## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog decoupled from library documents and expose paginated/random browsing APIs alongside a dynamic metadata query endpoint `GET /api/v1/insights/meta`.

The frontend `/insights` page SHALL provide a prominent, dedicated View Mode Switcher positioned directly below the Header Banner, separating feed viewing modes from category taxonomy:
1. `[ 🌐 Khám Phá ]` (Explore mode, browsing the general or category-filtered insights feed)
2. `[ 🔖 Đã Lưu (N) ]` (Saved bookmarks mode, displaying the total count $N$ of bookmarked insights)

The horizontal category filter bar SHALL remain active and independent for BOTH modes:
- In Explore mode, selecting a category filters the general feed by that category.
- In Saved mode, selecting a category filters the user's bookmarked insights by that category.
- Selecting "All Topics" in Saved mode displays all of the user's bookmarked insights across all categories.
- The category chip row SHALL consist exclusively of topic filter chips (`[ Tất Cả Chủ Đề ]` and dynamic category names) without embedding saved bookmark toggle buttons.

When in Saved mode and no bookmarked insights match the selected filter (or the user has zero saved bookmarks), the `/insights` page SHALL display a tailored empty state comprising:
- Icon: `BookmarkCheck` in an indigo badge container
- Title: "Bạn chưa lưu mẫu kiến thức nào" (localized via `insights.saved_empty_title`)
- Description: "Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau." (localized via `insights.saved_empty_desc`)
- Action CTA: `[ 🌐 Khám Phá Kiến Thức Ngay ]` (localized via `insights.saved_empty_cta`), transitioning the view mode back to Explore mode.

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
