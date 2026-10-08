# Spec Delta

## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog and expose paginated/random browsing APIs alongside a dynamic metadata query endpoint `GET /api/v1/insights/meta`, adhering to the **DeepPace Studio** visual theme. Technical insights SHALL retain concrete code blocks (problematic vs idiomatic solution) with syntax highlighting, under-the-hood mechanics, and benchmark telemetry chips, while supporting technical craft, software design, mental models, and deep work habit domains.

#### Scenario: User requests next technical insight card
- **WHEN** user sends `GET /api/v1/insights/feed` with optional `category` or `tag` query parameters
- **THEN** the system returns a sequence of concise senior technical insight cards containing problem context, under-the-hood analysis, bad vs good code snippets, and benchmark performance stats.

#### Scenario: User navigates insights on frontend card reader
- **WHEN** user visits `/insights` and clicks "Next Insight ➔" or presses Space/ArrowRight
- **THEN** the card reader smoothly transitions to the next technical insight with syntax-highlighted code blocks and category badges
- **AND** the category filter row renders exclusively pure-text category chips (`[ Tất Cả Chủ Đề ]` and dynamic category names) while the dedicated View Mode Switcher independently displays the active view mode.

#### Scenario: Structured benchmark telemetry metric chips
- **WHEN** an insight card renders with benchmark telemetry statistics (`benchmarkStats` containing single or pipe-delimited multiple metrics)
- **THEN** the frontend parses and renders each metric as an individual, self-contained compact chip with a single Lucide `<Zap>` icon
- **AND** any leading raw emojis (`⚡`, `🔥`, `🚀`) in the data string are stripped to eliminate duplicate side-by-side icon rendering
- **AND** each metric chip renders with Dev-Learning Studio brand violet tokens (`bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300`).

#### Scenario: Responsive card header layout and telemetry placement
- **WHEN** user views an insight card on any screen width (mobile, tablet, or desktop)
- **THEN** the top header row cleanly separates topic taxonomy badges on the left from the bookmark interaction button on the right
- **AND** benchmark telemetry chips wrap gracefully without pushing the bookmark button out of view or compressing the card title.

#### Scenario: Client requests insights metadata and dynamic topic suggestions
- **WHEN** client sends `GET /api/v1/insights/meta`
- **THEN** system queries `TechInsights` to compile category metadata including category IDs, keys, localized English and Vietnamese labels, and published card counts
- **AND** queries the authenticated user's library `DocumentBooks` in PostgreSQL to extract suggested inspiration seeds from book categories and `DocumentChunk` chapter titles, grouped by category
- **AND** limits suggested topics to a maximum of 8 distinct items per category, filtering out raw chapter index prefixes (e.g. `CHƯƠNG X:`, `Tóm tắt chương`, `(Section Y)`)
- **AND** when fewer than 2 library topics remain for a category, supplements with domain-specific curated fallback topics
- **AND** returns HTTP 200 with `{ categories, suggestedTopics }`
- **AND** client dynamically populates filter chips and the AI generation modal suggestion pool from the user's ingested library documents without relying on hardcoded arrays or the removed `Topics` table.

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
- **THEN** page renders the tailored Saved empty state with the `BookmarkCheck` icon mounted visibly inside the centered icon wrapper
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

#### Scenario: Responsive AI generation modal height containment
- **WHEN** user clicks "+ Tạo Với AI" on `/insights` with any active category
- **THEN** the modal dialog container is constrained to a maximum height of `85dvh` with `overflow-y-auto`
- **AND** the suggested topic chips container maintains a maximum vertical scroll bounds (`max-h-36 overflow-y-auto`)
- **AND** the modal never expands beyond the top or bottom viewport edges regardless of how many topic chips are returned.

#### Scenario: Dynamic category-aware modal placeholders and guidance
- **WHEN** user opens the AI generation modal with `EngineeringCraft` (Mã Sạch & Thiết Kế) active
- **THEN** the placeholder displays software craft examples (e.g., `Refactoring God Class, Specification Pattern, TDD Invariants`)
- **WHEN** user opens the modal with `MentalModels` (Mô Hình Tư Duy) active
- **THEN** the placeholder displays cognitive models (e.g., `First Principles, Inversion (Pre-Mortem), Second-Order Thinking`)
- **WHEN** user opens the modal with `HabitsProductivity` (Thói Quen & Tập Trung Sâu) active
- **THEN** the placeholder displays habit systems (e.g., `Deep Work 90m Blocks, Ultradian Rhythms, Dopamine Reset`).

#### Scenario: Empty state visual integrity with contextual icons
- **WHEN** user views an empty category in Explore mode with zero matching insights
- **THEN** the centered icon container renders a visible Lucide `<Lightbulb>` component
- **AND** the container does not render an unstyled empty box.

#### Scenario: Localized category badge and sanitized tag display
- **WHEN** user views an active insight card in Vietnamese (`vi`)
- **THEN** the category pill badge renders the localized category label matching the active locale (e.g., `Hệ Thống Backend & Runtime`, `Mã Sạch & Thiết Kế`, `Mô Hình Tư Duy`) instead of hardcoded English
- **AND** all hashtag badges render with trimmed text without leading or internal spaces (e.g. `#.NET` rather than `# .NET`).

#### Scenario: Seed data availability across all practice domains
- **WHEN** the application starts up and runs database seeding via `TechInsightsSeeder`
- **THEN** initial high-quality seed insights are provisioned for all 7 categories (including `EngineeringCraft`, `MentalModels`, and `HabitsProductivity`)
- **AND** exploring any category immediately displays at least one ready insight card without requiring upfront on-demand AI synthesis.
