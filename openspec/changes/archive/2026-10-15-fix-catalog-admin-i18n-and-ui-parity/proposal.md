# Proposal

## Why

Recent visual audits and user testing revealed UI fragmentation and runtime error leakage across the Personal Library (`/library`) and Catalog Administration (`/admin/catalog`) workspaces. In My Library, an extra "Reset Filters" action button causes the category toolbar to overflow and awkwardly wrap to a second row under the search box (breaking visual parity with the single-row Discover tab), while an improper clear icon is embedded inside the search input. In Catalog Administration, RFC 7807 problem details leak raw English errors (such as missing temporary PDF files) because catalog error codes lack internationalization mappings, the category dropdown displays a broken English placeholder `"Select option..."` due to enum-string vs numeric ID mismatches, the "Prepare Initial Slices" action is improperly enabled on already published documents, and metadata editing is improperly allowed on immutable published documents.

Resolving these issues establishes seamless, 100% visual parity between My Library and Discover, enforces correct administrative lifecycle gating, and guarantees fully localized, user-friendly feedback across both English and Vietnamese locales.

## What Changes

- **Personal Library Filter Toolbar Parity**:
  - Remove the inline `[ ⟲ Đặt lại bộ lọc ]` button from the My Library toolbar to prevent overflow wrapping, matching Discover's single-row layout where category resetting is handled by clicking `All` (`Tất Cả`) and advanced resets occur within `LibraryFilterModal.vue`.
  - Fix the search input clear action to display `<X>` instead of `<RotateCcw><span>...</span>`.
  - Ensure horizontal container prevents line-breaks on compact viewports with smooth horizontal scrolling where needed.
- **Catalog Error Code Internationalization (`useApiError` & Locales)**:
  - Add comprehensive translations for all `Catalog.*` and `Crawler.*` error codes (`Catalog.MissingPdfFile`, `Catalog.PublishedImmutable`, `Catalog.NotFound`, `Catalog.Busy`, `Catalog.ConcurrencyConflict`, `Catalog.PublicationIneligible`, `Catalog.PublicationFailed`, `Catalog.InvalidOperation`, `CatalogSlice.NotFound`, `Crawler.Failed`, `Crawler.NoContent`) to `frontend/app/i18n/locales/vi.json` and `frontend/app/i18n/locales/en.json`.
  - Enhance `useApiError.ts` to properly resolve dotted/nested error codes (`Catalog.MissingPdfFile` -> `api_errors.Catalog.MissingPdfFile` or `api_errors.CATALOG_MISSING_PDF_FILE`).
- **Catalog Administration Lifecycle & Form Hardening**:
  - Normalize `doc.category` from backend enum strings (`"SystemDesign"`, `"DatabaseStorage"`, etc.) to numeric IDs (`0..6`) in `AdminCatalogDocumentDetail.vue` so `AppSelect` displays the localized category label instead of falling back to `"Select option..."`.
  - Localize the default fallback placeholder in `AppSelect.vue` to respect current locale (`"Chọn một mục..."` / `"Select an option..."`).
  - Update `admin_catalog.metadata` i18n title from obsolete *"Metadata & Quyền Phân Phối"* to *"Thông Tin & Metadata Tài Liệu"* / *"Document Metadata"*.
  - Conditionally render the "Prepare Initial Slices" button in `AdminCatalogSlicesReview.vue` only when slices have not yet been prepared (`slices.length === 0` and document is not ready/published), preventing erroneous 400 Bad Request calls on published documents.
  - Make metadata form fields read-only and disable `Lưu Thay Đổi` when `document.visibility === 'Published'`, showing a helpful banner explaining that published documents are immutable and must be hidden to modify.
  - Conditionally render the Publish button only when `document.visibility !== 'Published'`.
  - Localize unmet publication requirements listed in the gating banner.
  - Fix `sourceType` string comparisons in `CatalogCard.vue`, `CatalogPreviewModal.vue`, and failure retry blocks to support both string enum names (`"PdfBook"`, `"WebDocUrl"`) and numeric values, removing outdated category mappings.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `library`: Synchronize My Library filter toolbar layout and controls to match Discover, ensuring strict single-row layout and correct search input clear button.
- `curated-catalog`: Harden Catalog Admin lifecycle gating, parse category enum strings, prevent illegal preparation/metadata mutations on published documents, and localize all catalog error messages and unmet requirements.

## Impact

- **Frontend**: `frontend/app/pages/library.vue`, `frontend/app/components/catalog/admin/AdminCatalogDocumentDetail.vue`, `frontend/app/components/catalog/admin/AdminCatalogSlicesReview.vue`, `frontend/app/components/catalog/CatalogCard.vue`, `frontend/app/components/catalog/CatalogPreviewModal.vue`, `frontend/app/components/common/AppSelect.vue`, `frontend/app/composables/useApiError.ts`, and locale dictionaries (`en.json`, `vi.json`).
- **Backend / Database**: Zero breaking changes; preserves all existing endpoints and RFC 7807 problem details contracts while expanding frontend localization.
- **Tests**: Unit tests in `frontend/tests/pages/library.spec.ts`, `frontend/tests/components/AdminCatalogComponents.spec.ts`, and Chromium headless visual verification.
