# Tasks

## 1. Frontend Component & Layout Implementation

- [x] 1.1 Create `frontend/app/components/library/LibraryFilterModal.vue` adhering to `AppModal.vue` standards, supporting all 7 DeepPace categories (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`, `MentalModels`, `HabitsProductivity`), content languages (`en`, `vi`), with reset (`handleReset`) and apply (`handleApply`) actions.
- [x] 1.2 Refactor the `#filters` slot in `frontend/app/pages/library.vue` for `currentTab === 'mine'` to mirror Discover's single-row toolbar layout: constrained search box (`sm:w-72 sm:flex-initial`) with clear button, `quickCategories` matching Discover (`All`, `0`, `1`, `2`, `3`), and the Advanced Filter trigger button (`[ 🎛️ Bộ Lọc ]`) with active badge counter.
- [x] 1.3 Connect `LibraryFilterModal.vue` state (`isLibraryFilterModalOpen`, `selectedCategory`, `selectedLanguage`, `activeLibraryFilterCount`) with `useLibraryStore`, ensuring reactive query synchronization (`category`, `search`, `language`, `page`).
- [x] 1.4 Verify and update any needed localization entries in `frontend/app/i18n/locales/en.json` and `frontend/app/i18n/locales/vi.json` for filter modal titles, options, and actions.

## 2. Testing & Dual-Gate Visual Verification

- [x] 2.1 Update unit tests in `frontend/tests/pages/library.spec.ts` (and add component tests for `LibraryFilterModal.spec.ts`) to verify quick category selection, advanced filter modal events, active count badge display, and reset behavior.
- [x] 2.2 Run full frontend test suite (`npm test`) and ensure 100% test pass rate across all suites.
- [x] 2.3 Execute headless Chromium automated browser verification for both Desktop (1440x900) and Mobile (390x844) viewports across English and Vietnamese, inspecting toolbar layout parity between My Library and Discover tabs.
