# Spec Delta

## MODIFIED Requirements

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide a 2-Tier Header filter architecture that partitions document search controls and category navigation chips into two dedicated rows:
1. **Tier 1 (Search & Global Filter Actions)**: The document search input SHALL reside on its own top row with responsive width (`max-w-md` on desktop viewports, full width on mobile), clear button (`X`), and an active filter reset action.
2. **Tier 2 (Full-Width Category Navigation Chips)**: The category filter chips SHALL span the full width of the container on a dedicated second row, rendering "All" plus all 7 DeepPace categories:
   - `All`: "Tất Cả" / "All"
   - `FrontendWeb (0)`: "Frontend & Web"
   - `BackendRuntime (1)`: "Backend & Runtime"
   - `DatabaseStorage (2)`: "Cơ Sở Dữ Liệu" / "Database & Storage"
   - `SystemDesign (3)`: "Hệ Thống Phân Tán" / "System Design"
   - `EngineeringCraft (4)`: "Nghệ Thuật Lập Trình" / "Engineering Craft"
   - `MentalModels (5)`: "Mô Hình Tư Duy" / "Mental Models"
   - `HabitsProductivity (6)`: "Thói Quen & Hiệu Suất" / "Habits & Productivity"
3. **Non-Wrapping Responsive Behavior**: The Tier 2 category chip container SHALL enforce `whitespace-nowrap flex-nowrap overflow-x-auto no-scrollbar` across all viewports. Chips SHALL NEVER wrap into multiple vertical lines. On narrow viewports (mobile and tablet), the container SHALL scroll horizontally as a touch-friendly chip carousel.
4. **Transition and Geometry Invariance**: Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift or width jitter when switching categories.

#### Scenario: User views personal library on desktop viewport
- **WHEN** user views `/library` on a 1440px or 1080p desktop viewport
- **THEN** Tier 1 displays the search bar anchored at the top
- **AND** Tier 2 renders all 8 category chips in a single horizontal row spanning the full width without line-wrapping.

#### Scenario: User views personal library on mobile viewport
- **WHEN** user views `/library` on a 390px mobile viewport
- **THEN** Tier 1 displays the search input spanning full width
- **AND** Tier 2 renders the category chips in a single horizontally scrollable row
- **AND** the category chips do not wrap onto multiple lines or expand the vertical height of the toolbar.

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
- **WHEN** user clicks "Tất Cả" (All) or the reset filter action after viewing a specific category
- **THEN** the category filter state is cleared
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.
