# Spec Delta

## MODIFIED Requirements

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide responsive category filter toolbars adhering to strict single-row layout parity between the "My Library" (`mine`) and "Discover Catalog" (`discover`) tabs across all 7 DeepPace categories plus the "All" pill.
1. **Single-Row Parity**:
- Both tabs SHALL render the constrained search input (`sm:w-72 sm:flex-initial`) and quick category pills (`Tất Cả` / `All`, plus categories 0 through 3) alongside the Advanced Filter modal trigger button (`[ 🎛️ Bộ Lọc ]`).
- The My Library filter toolbar SHALL NOT render an inline, multi-word text reset button (`[ ⟲ Đặt lại bộ lọc ]`) on the toolbar row. Category resets SHALL be executed via the `Tất Cả` pill, query resets via the search clear button, and advanced filter resets within `LibraryFilterModal.vue` or the empty state action button.
- The right-hand category container SHALL prevent line-wrapping (`flex-nowrap` or horizontal scrolling) across standard 1080p desktop viewports with navigation sidebar open.
2. **Search Input Clear Action**:
- When the search query is non-empty, the clear button inside the search input SHALL render a clean close icon (`<X>`), matching the Discover tab, rather than embedding reset icons or localized text strings.

#### Scenario: User navigates to My Library with an active category filter
- **WHEN** an authenticated user visits `/library?category=2` on desktop (1440x900)
- **THEN** the search input, category pills, and `[ 🎛️ Bộ Lọc (1) ]` trigger button remain aligned on a single horizontal row without wrapping onto a second line below the search input.

#### Scenario: User clears search query in My Library
- **WHEN** a user enters text into the My Library search input
- **THEN** a compact `<X>` button appears inside the right side of the input, and clicking it immediately clears the search query and refreshes the book list.

#### Scenario: User filters library by Mental Models category
- **WHEN** user clicks the "Mô Hình Tư Duy" filter button on `/library`
- **THEN** the book catalog updates to display only books and articles categorized under `MentalModels (5)`
- **AND** the filter button transitions active styling with zero horizontal layout shift or width jumping.

#### Scenario: User filters library by Habits & Deep Work category
- **WHEN** user clicks the "Thói Quen & Tập Trung" filter button on `/library`
- **THEN** the book catalog updates to display only books and articles categorized under `HabitsProductivity (6)`.

#### Scenario: User switches category filter tab in library
- **WHEN** user clicks a category tab (e.g., "Tất Cả", "Frontend & Web", "Backend & Phân Tán", "Thiết Kế Mã", "Mô Hình Tư Duy")
- **THEN** the active tab updates its background, border, and text colors via `transition-colors`
- **AND** the button maintains its exact text width and baseline geometry without any border jitter, horizontal position jumping, or layout shifts across adjacent tabs.

#### Scenario: User clears or toggles category filter
- **WHEN** user clicks "Tất Cả" (All) after viewing a specific category
- **THEN** the category filter state is cleared
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.
