# Tasks

## 1. Frontend Component Refactoring

- [x] 1.1 Implement `CatalogFilterModal.vue` with accessible backdrop, teleport, category radio pills, source type and language selectors, active count badge, and reset/apply events; verify props and event emissions in unit tests.
- [x] 1.2 Refactor Discover tab toolbar in `library.vue` to replace stacked `AppSelect` elements with category quick-filter pills and the `CatalogFilterModal` trigger; verify query state and filter application in page tests.
- [x] 1.3 Fix "Open Book" action in `CatalogCard.vue` and `CatalogPreviewModal.vue` to navigate directly to `/read/${document.existingBookId}`; verify router destination and fallback in unit tests.
- [x] 1.4 Adjust Table of Contents and preview container scroll constraints (`max-h-52`) and header padding in `CatalogPreviewModal.vue`; verify no outer modal scrollbar activates.
- [x] 1.5 Add localized strings in `en.json` and `vi.json` for catalog filter modal titles, options, and actions.

## 2. Verification and Quality Gates

- [x] 2.1 Run full frontend test suite (`npm test`) to ensure all data contracts, store mutations, and component assertions pass 100%.
- [x] 2.2 Drive headless Chromium via browser automation to capture Desktop (1440x900) and Mobile (390x844) screenshots in English and Vietnamese for the refactored Discover tab, the opened `CatalogFilterModal`, and the single-scroll `CatalogPreviewModal`.
- [x] 2.3 Run strict OpenSpec validation and verify behavioral requirements match the proposal and delta specs.
