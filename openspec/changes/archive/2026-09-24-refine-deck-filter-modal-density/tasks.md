# Tasks: Refine Deck Filter Modal Layout & Sort Options Density

## 1. Frontend Component Refactoring

- [x] 1.1 Refactor Section 4 ("Sort Options" / "Sắp xếp theo") in `frontend/components/review/AdvancedFilterModal.vue` from `grid grid-cols-1 sm:grid-cols-2 gap-2` to `flex flex-wrap items-center gap-2`, standardizing sort option buttons into content-sized pill chips (`px-3.5 py-2 rounded-xl text-xs font-semibold whitespace-nowrap text-center cursor-pointer`) matching Sections 1–3.
- [x] 1.2 Align active and inactive chip tokens for all 4 sort buttons (`localSortBy === null`, `nextReviewDate_desc`, `difficulty`, `recent`), ensuring zero horizontal stretching across desktop and mobile viewports.

## 2. Automated Testing & Verification

- [x] 2.1 Run Vitest suite for review components (`tests/components/reviewBentoComponents.spec.ts` and `tests/pages/review.spec.ts`) to ensure 100% pass rate.
- [x] 2.2 Verify visual rendering in headless browser across Light Mode and Dark Mode, confirming balanced density and natural pill chip dimensions without horizontal over-scaling.
