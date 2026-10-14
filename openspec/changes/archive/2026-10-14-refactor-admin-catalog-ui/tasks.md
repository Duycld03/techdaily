# Tasks

## 1. Localization Foundation

- [x] 1.1 Add all missing English catalog administration keys in `frontend/app/i18n/locales/en.json`, including filter options, form labels, placeholders, status badges, slice descriptions, modal copy, and 14 toast notifications.
- [x] 1.2 Add natural, accurate Vietnamese translations matching the new `en.json` keys in `frontend/app/i18n/locales/vi.json`, ensuring complete key parity across both locales and verifying JSON syntax.

## 2. Component Decomposition

Before creating components, read the mandatory skills (`Vue`, `Nuxt`, `Pinia`, `Vitest`, `Antfu Design`) and inspect DeepPace tokens, shared accessible components (`AppSelect`, `AppModal`), and layout archetypes.

- [x] 2.1 Create `frontend/app/components/catalog/admin/AdminCatalogHeader.vue` containing the header action bar, refresh trigger, and New Draft button with responsive typography, `$t` translations, and `whitespace-nowrap shrink-0` actions.
- [x] 2.2 Create `frontend/app/components/catalog/admin/AdminCatalogNavRail.vue` containing search input, visibility and status filter dropdowns (`AppSelect`), document cards, status indicators, and empty/loading states with complete `$t` localization.
- [x] 2.3 Create `frontend/app/components/catalog/admin/AdminCatalogDraftForm.vue` containing source mode tabs (PDF drag-and-drop upload vs. URL import and preview), metadata input fields, distribution rights checkboxes, and cancel/submit actions using `text-sm sm:text-base` typography.
- [x] 2.4 Create `frontend/app/components/catalog/admin/AdminCatalogSlicesReview.vue` containing slice extraction counts, slice preparation action, administrator review confirmation, and the slice list with Raw/Prepared badges and inspect triggers.
- [x] 2.5 Create `frontend/app/components/catalog/admin/AdminCatalogDocumentDetail.vue` containing the document overview banner, revision badge, processing failure/retry section, metadata editor form, publication eligibility notice, and publish/hide action buttons.
- [x] 2.6 Create `frontend/app/components/catalog/admin/AdminCatalogSliceModal.vue` wrapping `AppModal` with accessible slice details (chapter title, estimated read time, summary markdown, and source markdown).

## 3. Page Orchestration & Integration

- [x] 3.1 Refactor `frontend/app/pages/admin/catalog.vue` to compose the 6 extracted components, reducing file size from 1,296 lines to under 200 lines while preserving all `catalogStore` state bindings, selection tracking, and polling lifecycles.
- [x] 3.2 Replace all remaining hardcoded strings, placeholders, and inline toast text with `$t()` and `t()` calls, eliminating English hardcoding from the entire catalog administration surface.

## 4. Verification and Visual Inspection

- [x] 4.1 Run `npm test` across all frontend test suites to verify that `catalog.spec.ts` and related tests pass with zero regressions.
- [x] 4.2 Execute headless browser visual verification on Desktop (1440x900) and Mobile (390x844) viewports in both English and Vietnamese, inspecting typography sizing, no-wrap button stability, and layout responsiveness.
