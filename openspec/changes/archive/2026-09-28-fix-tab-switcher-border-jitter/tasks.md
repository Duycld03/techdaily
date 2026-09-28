# Tasks

## 1. Frontend - Technical Library Category Filter Jitter Elimination

- [x] 1.1 In `frontend/pages/library.vue`, update the category filter buttons to enforce constant `font-semibold` in the base class, eliminating conditional `font-bold` and `font-medium` toggling.
- [x] 1.2 In `frontend/pages/library.vue`, replace `transition-all` with `transition-colors duration-150` on the category filter buttons to prevent width, box-shadow, and font metric animation.
- [x] 1.3 In `frontend/pages/library.vue`, ensure constant 1px border geometry across both active (`border-slate-300 dark:border-white/[0.12]`) and inactive (`border-slate-200/80 dark:border-white/[0.08]`) states.

## 2. Frontend - System-Wide Tab Switcher & Filter Pill Alignment

- [x] 2.1 In `frontend/pages/insights.vue`, replace `transition-all` with `transition-colors duration-150` on category filter chips (lines 286-291) and code solution/problem sub-tabs (lines 418, 431).
- [x] 2.2 In `frontend/pages/notes.vue`, update modal preview/edit tab buttons (lines 602-618) to standardize on constant `font-semibold` and `transition-colors duration-150`.
- [x] 2.3 In `frontend/pages/review.vue`, replace `transition-all` with `transition-colors duration-150` on card filter chips (lines 668-700) and modal tabs (lines 806-821).

## 3. Verification & Visual Gate Protocol

- [x] 3.1 Execute Gate 1 test suite (`npm test`) to verify all existing component and page unit tests pass with zero failures.
- [x] 3.2 Execute Gate 2 headless browser visual verification on `/library` across Desktop (1440x900) and Mobile (390x844) in both English and Vietnamese, confirming zero layout shift, text width jump, or border flicker when toggling category filter pills.
