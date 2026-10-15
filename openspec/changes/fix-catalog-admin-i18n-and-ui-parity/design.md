# Design

## Context

See `proposal.md` for user-reported defects and motivations.
Currently, Nuxt 4 frontend uses `@nuxtjs/i18n` with English and Vietnamese locale dictionaries. Backend ASP.NET Core Minimal APIs return RFC 7807 problem details with error codes under `extensions["code"]`.
In `frontend/app/pages/library.vue`, the filter toolbars for My Library (`currentTab === 'mine'`) and Discover Catalog (`currentTab === 'discover'`) diverged when an inline text button `[ ⟲ Đặt lại bộ lọc ]` was placed on My Library's right container, causing line overflow on standard desktop viewports.
In Catalog Admin (`frontend/app/components/catalog/admin/`), backend serialization of enums (`Category`, `SourceType`, `ProcessingStatus`, `CatalogVisibility`) produces string names rather than numbers, causing type mismatches with component option lists.

## Goals / Non-Goals

**Goals:**
- Eliminate toolbar overflow wrapping on `/library` so that both My Library and Discover tabs share a strictly single-row layout across desktop viewports (1440x900).
- Localize all `Catalog.*` and `Crawler.*` API error codes so that administrators receive user-friendly Vietnamese and English toast messages instead of raw English HTTP detail phrases.
- Normalize backend string enum values (`doc.category`) in Catalog Admin to numeric IDs so `AppSelect` displays the localized category label.
- Enforce lifecycle immutability in the admin UI: disable metadata editing and hide the duplicate Publish button when a document is in `Published` status.
- Conditionally omit the "Prepare Initial Slices" action on already prepared or published catalog entries.

**Non-Goals:**
- Backend API schema changes or database migrations (backend contracts remain 100% stable).
- Modifying learner document reading or adoption logic.
- Adding new administrative endpoints.

## Decisions

### 1. Parity Alignment of My Library Toolbar
- **Decision**: Remove the inline `[ ⟲ Đặt lại bộ lọc ]` button from `library.vue` `#filters` container for `currentTab === 'mine'`.
- **Rationale**: Discover Catalog does not have this inline button and fits comfortably on a single row with 6 items (`All` + 4 categories + Advanced Filter button). In My Library, category clearing is already intuitively handled by clicking `Tất Cả` (All), search clearing is handled by the search input clear icon, and advanced filter resets occur in `LibraryFilterModal.vue`. Furthermore, when a query produces zero results, the centered empty state already features a prominent `[ ⟲ Đặt lại bộ lọc ]` button.
- **Search Clear Action Fix**: Restore `<X class="w-3.5 h-3.5" />` inside the search input instead of `<RotateCcw><span>...</span>`.

### 2. Dotted Error Code Resolution in `useApiError.ts`
- **Decision**: Update `useApiError.ts` to support both nested object resolution (`api_errors.Catalog.MissingPdfFile`) and normalized snake/flat keys (`api_errors.CATALOG_MISSING_PDF_FILE`).
- **Rationale**: Vue-i18n interprets dots as nested object paths. Backend returns domain error codes formatted as `Catalog.<ErrorName>` (e.g. `Catalog.MissingPdfFile`). By supporting both, we avoid falling through to raw English `title: detail` strings.
- **Dictionary Additions**:
  ```json
  "api_errors": {
    "Catalog": {
      "MissingPdfFile": "Tệp tải lên tạm thời không còn khả dụng. Vui lòng tải lại tệp PDF để chuẩn bị.",
      "PublishedImmutable": "Tài liệu đã xuất bản ở chế độ chỉ đọc. Vui lòng ẩn tài liệu để chỉnh sửa metadata.",
      "NotFound": "Không tìm thấy tài liệu trong danh mục tuyển chọn.",
      "Busy": "Tài liệu đang được xử lý trong tiến trình nền. Vui lòng đợi trong giây lát.",
      "ConcurrencyConflict": "Dữ liệu tài liệu vừa được quản trị viên khác cập nhật. Vui lòng tải lại trang.",
      "PublicationIneligible": "Tài liệu chưa đủ điều kiện xuất bản.",
      "PublicationFailed": "Xuất bản tài liệu thất bại.",
      "InvalidOperation": "Thao tác không hợp lệ trên trạng thái tài liệu hiện tại."
    },
    "Crawler": {
      "Failed": "Không thể trích xuất nội dung từ đường dẫn web này.",
      "NoContent": "Không tìm thấy nội dung bài viết kỹ thuật có thể trích xuất."
    }
  }
  ```

### 3. Enum String Normalization in `AdminCatalogDocumentDetail.vue`
- **Decision**: Add a `parseCategory` helper in `AdminCatalogDocumentDetail.vue` that normalizes string enum values (`"SystemDesign"`, `"DatabaseStorage"`, etc.) into their corresponding numeric index `0..6`.
- **Rationale**: System.Text.Json serializes C# enums as string names by default. `AppSelect` matches options by `String(opt.value) === String(modelValue)`. Normalizing ensures `editCategory` holds `3` when `doc.category` is `"SystemDesign"`, displaying "Hệ Thống Phân Tán" instead of falling back to `"Select option..."`.
- **AppSelect Placeholder**: In `AppSelect.vue`, update default placeholder to `props.placeholder || (i18n ? t('common.select_option') : 'Select option...')` with `"select_option": "Chọn một mục..."`.

### 4. Admin Publication Lifecycle Gating & Read-Only Published Form
- **Decision**:
  - When `document.visibility === 'Published'`:
    - Disable metadata inputs (`editTitle`, `editCategory`, `editLanguage`, `editSourceUrl`, `editAttribution`) and the `✓ Lưu Thay Đổi` button.
    - Render an informative notice banner: *"Tài liệu đang xuất bản ở chế độ chỉ đọc. Vui lòng ẩn tài liệu khỏi Khám Phá nếu cần điều chỉnh metadata."*
    - Hide the "Publish to Discover" button, rendering only `[ ✕ Ẩn Khỏi Khám Phá ]`.
  - When `document.slices.length > 0` or `document.processingStatus === 'Ready'`:
    - Hide the `[ ⊙ Chuẩn Bị 3 Lát Cắt Đầu ]` action in `AdminCatalogSlicesReview.vue`.
    - Display summary text: *"Tổng cộng {total} lát cắt đã được chuẩn bị và sẵn sàng."*

### 5. SourceType Normalization in Catalog Cards
- **Decision**: In `CatalogCard.vue` and `CatalogPreviewModal.vue`, normalize `sourceType` comparisons (`doc.sourceType === 'PdfBook' || doc.sourceType === 2`) and update category mappings to the canonical 7 DeepPace pillars (removing legacy `cloud` reference).

## Risks / Trade-offs

- **Risk**: Administrators wanting to quick-edit title on a published document must hide it first.
  - **Mitigation**: This directly aligns with backend domain invariants (`Catalog.PublishedImmutable`). Editing metadata on a live published document invalidates cache and learner adoption snapshots; requiring unpublishing ensures intentional updates and re-review.
- **Risk**: Removal of inline reset button requires one extra click (clicking `Tất Cả` or clearing input).
  - **Mitigation**: This matches Discover exactly, eliminates visual collision on all 1080p and laptop screens, and is intuitive since clicking `Tất Cả` resets category filters.
