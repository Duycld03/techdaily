# Design

## Context

See `proposal.md - Why` for motivation.

In `frontend/app/pages/library.vue`, the toolbar for the Discover tab (`currentTab === 'discover'`) uses a unified, single-row layout containing:
1. A constrained, responsive search box (`sm:w-72 sm:flex-initial`).
2. Quick category pills for the top technical domains (`All`, `FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`).
3. An Advanced Filter trigger button (`[ 🎛️ Bộ Lọc ]`) with an active filter badge counter that opens `CatalogFilterModal.vue`.

In contrast, the My Library tab (`currentTab === 'mine'`) currently uses a divergent 2-tier or wrapped filter bar, creating visual inconsistency and lacking an advanced filter modal.

## Goals / Non-Goals

**Goals:**
- Synchronize My Library toolbar markup with Discover:
  - Match search input geometry, clear button, and placeholder.
  - Render identical quick category chips (`quickCategories` matching `quickDiscoverCategories`).
  - Introduce the Advanced Filter button (`[ 🎛️ Bộ Lọc ]`) with active filter badge counter.
- Create an accessible `LibraryFilterModal.vue` for My Library:
  - Support filtering across all 7 DeepPace domain categories (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`, `MentalModels`, `HabitsProductivity`).
  - Support content language filtering (`en`, `vi`).
  - Include quick reset (`RotateCcw`) and apply handlers with sticky modal actions.
- Synchronize active filter counter and ensure full URL query synchronization (`category`, `search`, `language`, `page`).
- Maintain 100% Vitest test pass rate and visual layout stability across desktop and mobile viewports.

**Non-Goals:**
- Backend endpoint or database modifications (existing `GET /api/v1/library/books` already supports `category`, `search`, `page`, and `pageSize`).
- Modifying document cards, book readers, or the import modal.

## Decisions

### 1. Dedicated `LibraryFilterModal.vue` vs Shared Component

**Decision:** Create a dedicated `LibraryFilterModal.vue` in `frontend/app/components/library/` following the established `AppModal.vue` and `CatalogFilterModal.vue` conventions.
**Rationale:** `CatalogFilterModal.vue` contains catalog-specific ingestion source types (Web article, PDF, Markdown) that map to curated documents. A dedicated `LibraryFilterModal.vue` cleanly bounds personal library filters (Category 0..6 and Language) without cluttering props or adding unused catalog fields.

### 2. Quick Domain Chips Definition

**Decision:** Standardize `quickCategories` for My Library to match Discover:
- `All`: `undefined`
- `FrontendWeb`: `0`
- `BackendRuntime`: `1`
- `DatabaseStorage`: `2`
- `SystemDesign`: `3`

Categories 4 (`EngineeringCraft`), 5 (`MentalModels`), and 6 (`HabitsProductivity`) are fully accessible via the Advanced Filter modal ("Bộ Lọc").
**Rationale:** Guarantees zero line-wrapping and preserves identical visual rhythm between both library tabs on desktop and mobile.

### 3. Active Filter Badge & Reset Logic

**Decision:** Compute `activeLibraryFilterCount` as:
```typescript
const activeLibraryFilterCount = computed(() => {
  let count = 0
  if (selectedCategory.value !== undefined) count++
  if (selectedLanguage.value !== undefined && selectedLanguage.value !== '') count++
  return count
})
```
When `activeLibraryFilterCount > 0`, the trigger button renders a badge with the count (e.g., `1`, `2`). Clicking reset in the modal or selecting "All" clears all filters and updates the URL.

## Risks / Trade-offs

- **Risk:** Deep categories (e.g. Mental Models, Habits & Focus) require opening the modal when not in the top 4 quick pills.
  - **Mitigation:** The "Bộ Lọc" button is immediately adjacent to the chips, features an active filter count badge, and opens a lightweight, instantly responding modal.
- **Risk:** Existing unit tests asserting the old 2-tier DOM structure (`library-reset-filters-btn` or 8 category chips in My Library) will fail.
  - **Mitigation:** Update Vitest component and page tests in `frontend/tests/pages/library.spec.ts` to assert the new synchronized toolbar structure, quick chips, and filter modal interactions.
