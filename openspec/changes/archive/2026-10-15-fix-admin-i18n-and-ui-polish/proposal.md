# Proposal

## Why

Administrative workflows across `/admin/audit`, `/admin/documents`, and `/admin/catalog` have several UX and internationalization regressions:
1. `AdminCatalogDocumentDetail.vue` renders an unnecessary bullet dot inside the processing status badge, introducing visual noise.
2. `/admin/audit` uses a native HTML `<select><option>` control, violating DeepPace Pillar 2 (Prohibition of Native HTML Form Select & Browser Dialogs) and rendering OS-native menus that clash with dark theme styling.
3. Multiple admin interface labels, status badges, pagination controls, date placeholders, and error/ineligible messages remain hardcoded in English when viewing in Vietnamese (`vi`).

This change eliminates native form controls in favor of `AppSelect.vue`, cleans up status badge iconography, and completes 100% bilingual localization across all administrator surfaces.

## What Changes

- **Catalog Document Detail Polish (`AdminCatalogDocumentDetail.vue`)**:
  - Remove the bullet indicator dot (`w-2 h-2 rounded-full`) from the processing status badge, displaying a clean, uniform status text pill.
- **Audit Log UI Parity & Localization (`audit.vue`)**:
  - Replace raw HTML `<select>` with custom accessible `AppSelect.vue` for the outcome filter dropdown.
  - Localize date filter placeholders (`From (YYYY-MM-DD)` and `To (YYYY-MM-DD)`) via `$t('admin_audit.filter_from')` and `$t('admin_audit.filter_to')`.
  - Localize event outcome badges in the event list and detail modal (`Succeeded`, `Accepted`, `Rejected`, `Failed`) using `admin_audit.outcome_*` keys.
- **Document Operations Localization (`documents.vue`)**:
  - Localize document processing status badges (`Ready`, `Failed`, `Processing`, `Pending`) in both the master list cards and the detail header.
  - Localize document scope badges (`Personal` $\rightarrow$ "Cá nhân", `Catalog` $\rightarrow$ "Catalog").
  - Localize pagination buttons (`Prev`/`Next` $\rightarrow$ "Trước"/"Sau").
  - Localize retry ineligible warning banner by mapping `ineligibleReasonCode` to localized messages.
  - Localize source storage availability (`Missing` $\rightarrow$ "Thiếu nguồn", `Available` $\rightarrow$ "Khả dụng", etc.).
  - Localize the Timestamps card heading and labels (`Created:`, `Observed:`).
  - Localize ingestion attempt execution states and timestamp labels (`Queued:`, `Started:`, `Ended:`).
- **Locale Dictionaries (`en.json`, `vi.json`)**:
  - Add missing translation keys under `admin_audit` and `admin_documents`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `curated-catalog`: Update `AdminCatalogDocumentDetail` requirements to specify clean pill status presentation without bullet dot markers.
- `admin-workspace`: Require 100% complete bilingual localization and accessible `AppSelect` controls across `/admin/audit` and `/admin/documents`.

## Impact

- Frontend: `AdminCatalogDocumentDetail.vue`, `pages/admin/audit.vue`, `pages/admin/documents.vue`, `en.json`, `vi.json`.
- Zero backend API or database schema modifications.
- Zero breaking changes.
