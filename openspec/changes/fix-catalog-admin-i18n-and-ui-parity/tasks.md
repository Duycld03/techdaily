# Tasks

## 1. Frontend Personal Library Toolbar Parity

- [x] 1.1 Remove inline `[ ⟲ Đặt lại bộ lọc ]` button from `#filters` slot in `frontend/app/pages/library.vue` to eliminate overflow wrapping, and fix search input clear icon to `<X class="w-3.5 h-3.5" />`.
- [x] 1.2 Enforce single-row layout parity with Discover on `/library`, preventing accidental flex line-breaks across standard 1080p desktop viewports with navigation sidebar open.

## 2. Frontend Error Handling & Localization

- [x] 2.1 Enhance `frontend/app/composables/useApiError.ts` to support dotted/nested domain error codes (`Catalog.<Code>`) and flat aliases, preventing raw HTTP problem details phrases from leaking into user toasts.
- [x] 2.2 Add comprehensive translations for all `Catalog.*` and `Crawler.*` error codes to `frontend/app/i18n/locales/vi.json` and `frontend/app/i18n/locales/en.json`.
- [x] 2.3 Update `admin_catalog.metadata` i18n title in `en.json` and `vi.json` to "Document Metadata" / "Thông Tin & Metadata Tài Liệu", add unmet requirement translation keys, and add `common.select_option` ("Select an option..." / "Chọn một mục...").
- [x] 2.4 Update `frontend/app/components/common/AppSelect.vue` to localize the default fallback placeholder using `t('common.select_option')`.

## 3. Frontend Catalog Admin Form & Lifecycle Hardening

- [x] 3.1 Implement category enum string normalization (`parseCategory`) in `frontend/app/components/catalog/admin/AdminCatalogDocumentDetail.vue` to map backend string enum names (`"SystemDesign"`, `"DatabaseStorage"`, etc.) to numeric IDs `0..6`, ensuring `AppSelect` displays the localized category label.
- [x] 3.2 Make metadata input fields read-only and disable `Lưu Thay Đổi` button in `AdminCatalogDocumentDetail.vue` when `document.visibility === 'Published'`, adding an informative notice banner that published documents must be hidden to modify.
- [x] 3.3 Conditionally render the "Publish to Discover" button in `AdminCatalogDocumentDetail.vue` only when `document.visibility !== 'Published'`, presenting only the "Hide from Discover" action on published documents.
- [x] 3.4 Conditionally render the "Prepare Initial Slices" button in `frontend/app/components/catalog/admin/AdminCatalogSlicesReview.vue` only when slices are unextracted (`document.slices.length === 0 && document.processingStatus !== 'Ready'`), and show a clean slice-ready summary when slices exist.
- [x] 3.5 Fix `sourceType` string/number normalization and update category mappings to the 7 canonical DeepPace pillars in `frontend/app/components/catalog/CatalogCard.vue` and `CatalogPreviewModal.vue`.

## 4. Testing & Dual-Gate Verification

- [x] 4.1 Update frontend Vitest test suites (`frontend/tests/pages/library.spec.ts` and `frontend/tests/components/AdminCatalogComponents.spec.ts`) to verify single-row parity, category enum normalization, and published document read-only state.
- [x] 4.2 Execute full backend and frontend test suites (`dotnet test backend/DeepPace.sln` and `npm --prefix frontend test`), ensuring 100% test pass rate.
- [x] 4.3 Execute automated headless Chromium visual verification on Desktop (1440x900) and Mobile (390x844) viewports across `/library` and `/admin/catalog`, presenting screenshot proof for both English and Vietnamese viewports.
