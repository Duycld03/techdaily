## 1. Component Template & Layout Updates

- [x] 1.1 In `frontend/components/graph/GraphControlBar.vue`, replace `overflow-x-auto no-scrollbar` with `flex-wrap` on Category Pillars, Node Types, and Mastery filter containers to prevent text clipping and truncation.
- [x] 1.2 In `frontend/components/graph/GraphControlBar.vue`, populate missing i18n keys in `categoryPills` for `DatabaseStorage`, `SystemDesign`, `FrontendWeb`, and `EngineeringCraft`.

## 2. Localization & Copy Refinement

- [x] 2.1 In `frontend/i18n/locales/en.json`, add translation keys for `databaseStorage`, `systemDesign`, `frontendWeb`, and `engineeringCraft` under `graph.filters`.
- [x] 2.2 In `frontend/i18n/locales/vi.json`, add translation keys for the 5 engineering pillars and streamline `"allPillars"` and `"backendRuntime"` for a concise horizontal footprint.

## 3. Automated Tests & Quality Assurance

- [x] 3.1 Update unit tests in `frontend/tests/components/graph/GraphControlBar.spec.ts` to verify that all 6 category pills render without clipping inside a `flex-wrap` container and translate correctly.
- [x] 3.2 Run `npm --prefix frontend test` and verify all tests pass with zero regressions.
- [x] 3.3 Validate OpenSpec change integrity by running `openspec validate graph-control-bar-category-pills-overflow-fix --type change` and verifying 0 errors.
