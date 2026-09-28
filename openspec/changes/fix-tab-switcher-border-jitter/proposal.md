# Proposal

## Why

When switching category filter tabs on the Technical Library page (`/library`), the tab buttons exhibit horizontal layout jitter ("giật viền") and border flicker. This regression occurs because active buttons toggle between `font-bold` (weight 700) and `font-medium` (weight 500) under an unconstrained `transition-all` utility, causing dynamic glyph width changes and box-shadow/border animations that shift adjacent buttons horizontally. Similar unconstrained transitions and font-weight toggling remain in category chips and modal tab switchers across `/insights`, `/notes`, and `/review`.

## What Changes

- **Constant Font Weight & Geometry**:
  - In `library.vue` (Category Filters): Standardize base font weight to `font-semibold` across both active and inactive states. Active state applies brand text/background tokens without changing font weight or element width.
  - In `notes.vue` (Modal Tab Switcher): Standardize base font weight to `font-semibold` across Preview and Edit tabs, eliminating font-bold width expansion.
  - In `review.vue` (Modal Tab Switcher & Filter Pills): Standardize constant font weight across active/inactive states.
- **Scoped Color Transitions**:
  - Replace `transition-all` with `transition-colors duration-150` across category filter bars and sub-tabs in `frontend/pages/library.vue`, `frontend/pages/insights.vue`, `frontend/pages/notes.vue`, and `frontend/pages/review.vue`.
  - Maintain stable 1px border baseline (`border` with explicit color classes for active/inactive) to prevent 1px collapse or preflight border color flicker.
- **Systematic UI Standard Alignment**:
  - Extend the platform's `Universal Zero-Shift Segmented Controls & Tab Switchers` invariant to explicitly govern category filter chip bars and modal sub-tabs.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Refine requirement `Universal Zero-Shift Segmented Controls & Tab Switchers` to mandate constant font weight and `transition-colors` on category filter bars and modal sub-tabs across the platform.
- `library`: Refine requirement `Category Filter Bar & Catalog Navigation` to enforce constant width geometry, stable border baselines, and `transition-colors` when toggling category filters.

## Impact

- **Visual Quality**: Completely eliminates horizontal tab jitter, text width expansion, and border flashing during category switching on desktop and mobile.
- **Zero Breaking Changes**: No modifications to route queries, API contracts, store state, or database schemas.
- **Test Compatibility**: Preserves 100% pass rate on all existing Vitest suites and backend unit tests.
