# Spec Delta

## MODIFIED Requirements

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide a unified, responsive search and filter toolbar layout synchronized across both the "My Library" and "Discover" tabs.

On both "My Library" and "Discover" tabs, the primary toolbar SHALL present:
1. A search input (`sm:w-72 sm:flex-initial`) with dedicated clear trigger (`X`) and search execution on Enter or clear.
2. Quick category pills for top technical domains:
   - `All`: "Tất Cả" / "All"
   - `FrontendWeb (0)`: "Frontend & Web"
   - `BackendRuntime (1)`: "Backend & Runtime"
   - `DatabaseStorage (2)`: "Cơ Sở Dữ Liệu" / "Database & Storage"
   - `SystemDesign (3)`: "Hệ Thống Phân Tán" / "Distributed Systems"
3. An Advanced Filter trigger button (`[ 🎛️ Bộ Lọc ]` / "Filter") displaying an active filter badge counter when any non-default filter is applied.

Clicking the Advanced Filter trigger button SHALL open an accessible filter dialog modal permitting users to filter documents by all 7 DeepPace categories (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`, `MentalModels`, `HabitsProductivity`), content language, and document format, with dedicated reset and apply actions.

Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift when switching categories.

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

#### Scenario: User filters My Library with quick category chips
- **WHEN** user clicks "Backend & Runtime" in My Library
- **THEN** the book list updates to show only personal books under `BackendRuntime (1)`
- **AND** the button styling transitions with zero layout shift or width jumping.

#### Scenario: User opens advanced filter modal in My Library
- **WHEN** user clicks the "Bộ Lọc" button in My Library
- **THEN** the advanced filter modal opens, displaying all 7 domain categories, language options, and source formats
- **AND** the trigger button displays the active filter count badge when filters are applied.

#### Scenario: User applies deep category filter from modal in My Library
- **WHEN** user selects `EngineeringCraft (4)` or `MentalModels (5)` in the advanced filter modal and clicks Apply
- **THEN** the modal closes, the catalog updates to display the matching personal books, and the filter counter badge increments.

#### Scenario: User clears or resets filters in My Library
- **WHEN** user clicks Reset in the filter modal or clicks the "Tất Cả" quick pill
- **THEN** all category and search filters are cleared, and the book list reloads all documents.
