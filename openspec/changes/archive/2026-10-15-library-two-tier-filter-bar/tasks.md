# Tasks

## 1. Frontend Implementation

- [x] 1.1 Restructure `#filters` slot in `frontend/app/pages/library.vue` for `currentTab === 'mine'` into Tier 1 (Search row) and Tier 2 (Category chips row), verifying search input and clear button functionality.
- [x] 1.2 Implement Tier 2 category chip carousel with `whitespace-nowrap flex-nowrap overflow-x-auto no-scrollbar` containing "All" and all 7 DeepPace categories, verifying zero line-wrapping on desktop and smooth horizontal scrolling on mobile.
- [x] 1.3 Implement Tier 1 active filter reset action (`resetFilters`) that clears both active category and search term, verifying that `selectedCategory` and `searchQuery` reset cleanly and trigger book reload.

## 2. Frontend Testing & Verification

- [x] 2.1 Update and run frontend test suites (`npm test`) covering library state transitions, category switching, and URL query synchronization, verifying all tests pass.
- [x] 2.2 Perform visual inspection on desktop (1440x900) and mobile (390x844) viewports, verifying zero multi-row wrapping, consistent toolbar height, and responsive typography standards.
