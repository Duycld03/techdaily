# Tasks

## 1. Frontend Implementation

- [x] 1.1 Fix web document draft payload serialization in `frontend/app/components/catalog/admin/AdminCatalogDraftForm.vue` by passing `url: targetUrl` and `authorOrSourceUrl: sourceUrl.value.trim() || undefined` to `catalogStore.importUrl()`, verifying payload conforms to `AdminImportCatalogUrlApiRequest`.
- [x] 1.2 Update target URL resolution and validation in `AdminCatalogDraftForm.vue` to compute `const targetUrl = previewUrlInput.value.trim() || sourceUrl.value.trim()`, verifying that draft creation succeeds even if preview extraction is not triggered beforehand.

## 2. Frontend Testing & Verification

- [x] 2.1 Add unit tests for `AdminCatalogDraftForm.vue` in `frontend/tests/components/AdminCatalogComponents.spec.ts` covering web URL draft creation, verifying exact payload binding and store dispatch.
- [x] 2.2 Run full frontend test suite via `npm test` in `frontend/` to verify zero regression across existing components and stores.
- [x] 2.3 Perform headless Chromium visual and behavioral verification of the `/admin/catalog` draft form across desktop (1440px) and mobile (390px) viewports.
