## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog decoupled from library documents and expose paginated/random browsing APIs alongside a dynamic metadata query endpoint `GET /api/v1/insights/meta`.

The frontend `/insights` category filter bar SHALL present a uniform, 100% pure-text chip row without icon components or emoji prefixes, rendering localized labels for "All Topics", each active category, and the "Saved" (`Đã Lưu` / `Saved`) bookmark filter chip.

#### Scenario: User requests next technical insight card
- **WHEN** user sends `GET /api/v1/insights/feed` with optional `category` or `tag` query parameters
- **THEN** the system returns a sequence of concise senior technical insight cards containing problem context, under-the-hood analysis, bad vs good code snippets, and benchmark performance stats.

#### Scenario: User navigates insights on frontend card reader
- **WHEN** user visits `/insights` and clicks "Next Insight ➔" or presses Space/ArrowRight
- **THEN** the card reader smoothly transitions to the next technical insight with syntax-highlighted code blocks and category badges
- **AND** the category filter row renders exclusively pure-text chips (`[ Tất Cả Chủ Đề ]`, dynamic category names, and `[ Đã Lưu ]`) without icon components or emoji symbols.

#### Scenario: Client requests insights metadata and dynamic topic suggestions
- **WHEN** client sends `GET /api/v1/insights/meta`
- **THEN** system queries `TechInsights` to compile category metadata including category IDs, keys, localized English and Vietnamese labels, and published card counts
- **AND** queries active `Topics` (from the 30-Day Curriculum) in PostgreSQL to extract curated topic titles grouped by category
- **AND** returns HTTP 200 with `{ categories, suggestedTopics }`
- **AND** client dynamically populates filter chips and the AI generation modal suggestion pool without relying on hardcoded arrays.
