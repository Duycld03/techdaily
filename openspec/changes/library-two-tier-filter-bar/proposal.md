# Proposal

## Why

On the `/library` page ("My Library" tab), the search input and all 8 category filter buttons are crammed into a single row using `flex-wrap` next to a constrained search input (`sm:w-72`). Because DeepPace encompasses 7 senior engineering disciplines (Frontend, Backend, Database, System Design, Engineering Craft, Mental Models, Habits & Productivity) plus "All", several possessing lengthy localized labels in Vietnamese and English, the pills wrap onto 2–3 lines. This causes vertical misalignment with the search box, layout bloat, and broken toolbar geometry.

Instead of hiding non-coding pillars (such as Engineering Craft, Mental Models, and Habits) behind a filter modal, a 2-Tier Header architecture preserves 100% of the platform's core senior developer domains while guaranteeing zero line-wrapping and layout stability across all viewports.

## What Changes

- **Restructure Library Filters into 2 Tiers**:
  - **Tier 1 (Search & Quick Controls)**: Dedicated row for the document search input (`max-w-md` on desktop, full-width on mobile) alongside search clear and quick filter reset controls.
  - **Tier 2 (Comprehensive Category Bar)**: Full-width row dedicated to category navigation chips ("All" plus all 7 DeepPace categories) styled with `whitespace-nowrap flex-nowrap overflow-x-auto no-scrollbar`.
- **Responsive Layout Invariance**:
  - Desktop viewports (1080p, 1440p): All 8 category pills render comfortably in a single uninterrupted row across the full container width without crowding the search bar.
  - Mobile & Tablet viewports (390px, 768px): The category bar functions as a smooth horizontal chip carousel, preventing multi-row layout wrapping and preserving consistent toolbar height.
- **Full Category Parity**:
  - Keep all 7 DeepPace categories visible and directly selectable with 1 click:
    1. `All`
    2. `FrontendWeb (0)`
    3. `BackendRuntime (1)`
    4. `DatabaseStorage (2)`
    5. `SystemDesign (3)`
    6. `EngineeringCraft (4)`
    7. `MentalModels (5)`
    8. `HabitsProductivity (6)`

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `library`: Updates `Category Filter Bar Layout Stability` to specify the 2-tier layout architecture for `/library` filter controls, separating search inputs and category pills into dedicated tiers while maintaining zero layout shift and non-wrapping horizontal scrolling.

## Impact

- **Affected Files**:
  - `frontend/app/pages/library.vue`: The `#filters` slot layout for `currentTab === 'mine'`.
  - `openspec/specs/library/spec.md`: Behavioral requirements for category filter stability and layout structure.
- **Backend & API Impact**: None. Existing endpoints (`GET /api/v1/library/books`) already support category and search parameters.
- **Zero Breaking Changes**: Preserves URL query synchronization (`category`, `search`, `page`) and state management in `useLibraryStore`.
