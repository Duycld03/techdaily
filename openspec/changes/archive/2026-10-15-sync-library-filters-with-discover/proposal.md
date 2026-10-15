# Proposal

## Why

On `/library`, the "My Library" tab (`currentTab === 'mine'`) and the "Discover" tab (`currentTab === 'discover'`) have diverged in their search and filtering toolbar architecture. The Discover tab presents a clean, compact toolbar consisting of a search input (`sm:w-72`), quick category chips (`All`, `FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`), and an Advanced Filter button (`[ 🎛️ Bộ Lọc ]`) that displays an active filter badge count and opens an advanced filter modal with all 7 categories, source formats, and languages.

In contrast, My Library uses a divergent filter layout that either wraps category chips or splits controls into arbitrary tiers without an advanced filter button. This creates interface fragmentation and inconsistent mental models for learners toggling between their personal documents and the curated catalog. Synchronizing the My Library filter bar with the Discover pattern restores design harmony, guarantees identical interaction ergonomics, and provides a clean modal for deep multi-criteria filtering.

## What Changes

- **Synchronize My Library Toolbar Layout with Discover**:
  - **Search Input**: Match the Discover layout with a responsive, constrained search box (`sm:w-72 sm:flex-initial`) with instant clear (`X`) and keyboard trigger (`Enter`).
  - **Quick Category Chips**: Align My Library with the same top 4 domain pillars as Discover:
    1. `All` (`library.categories.all`)
    2. `FrontendWeb (0)` (`library.categories.frontend`)
    3. `BackendRuntime (1)` (`library.categories.backend`)
    4. `DatabaseStorage (2)` (`library.categories.database`)
    5. `SystemDesign (3)` (`library.categories.system_design`)
  - **Advanced Filter Trigger (`[ 🎛️ Bộ Lọc ]`)**: Add the same filter trigger button with `SlidersHorizontal` icon, localized label (`library.discover.advanced_filter_btn`), and dynamic active filter counter badge.
- **Advanced Filter Modal for My Library**:
  - Provide an accessible filter dialog (`LibraryFilterModal.vue` or unified modal) accessible via the trigger button.
  - Allow learners to filter by all 7 DeepPace domain categories (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`, `MentalModels`, `HabitsProductivity`), language (`en`, `vi`), and source format where supported.
  - Include quick reset (`RotateCcw`) and apply actions with sticky modal footer buttons.
- **Query & State Synchronization**:
  - Synchronize active filter counts and query parameters (`category`, `search`, `page`) seamlessly through `useLibraryStore` and Vue router query state.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `library`: Updates `Category Filter Bar Layout Stability` and library browsing requirements to define the synchronized toolbar architecture between personal library and catalog discovery, establishing shared quick category chips, responsive search geometry, and advanced filter modal invocation.

## Impact

- **Affected Files**:
  - `frontend/app/pages/library.vue`: The `#filters` slot layout for `currentTab === 'mine'`.
  - `frontend/app/components/library/LibraryFilterModal.vue` (or `CatalogFilterModal.vue` generalization): Modal interface for multi-criteria personal document filtering.
  - `openspec/specs/library/spec.md`: Delta specifications for synchronized toolbar ergonomics.
- **Backend & API Impact**: None. Existing endpoints (`GET /api/v1/library/books`) already accept `category`, `search`, `page`, and `pageSize`.
- **Zero Breaking Changes**: Fully backwards-compatible with existing book queries, bookmarks, and pagination.
