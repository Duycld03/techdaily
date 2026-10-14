# Proposal

## Why

The catalog administration surface at `/admin/catalog` currently exists as a single monolithic 1,296-line file (`frontend/app/pages/admin/catalog.vue`), mixing document ingestion, URL scraping preview, metadata editing, slice preparation review, modal inspection, and publication controls. Additionally, approximately 75% of user-facing labels, input placeholders, filter dropdown options, and toast messages are hardcoded in English, violating DeepPace bilingual invariants and mobile typography standards (`text-xs` overused for body and inputs). 

Refactoring this surface into cohesive, single-responsibility components and completing full bilingual localization now ensures high maintainability and provides a clean, well-tested building block for the pending `add-admin-workspace-dashboard` change to mount into the isolated administrator shell.

## What Changes

- **Component Decomposition:** Deconstruct `frontend/app/pages/admin/catalog.vue` into focused components under `frontend/app/components/catalog/admin/`:
  - `AdminCatalogHeader.vue`: Page title, subtitle, refresh trigger, and new draft initiation.
  - `AdminCatalogNavRail.vue`: Search filter, visibility/status dropdowns, document cards with status pills, and empty states.
  - `AdminCatalogDraftForm.vue`: Ingestion source tabs (PDF drag-and-drop upload vs. URL import and preview), metadata fields, distribution rights checkboxes, and submission buttons.
  - `AdminCatalogDocumentDetail.vue`: Header overview banner, revision badges, processing failure/retry controls, metadata editor, and publication eligibility controls.
  - `AdminCatalogSlicesReview.vue`: Slice counters, preparation trigger, administrator review confirmation, and slice list with status indicators.
  - `AdminCatalogSliceModal.vue`: Accessible modal dialog inspecting chapter title, estimated read time, executive summary, and raw markdown.
- **Complete Bilingual i18n Localization:**
  - Add missing translation keys to both `frontend/app/i18n/locales/en.json` and `frontend/app/i18n/locales/vi.json` covering:
    - Ingestion tabs, file upload instructions, URL preview indicators, and source constraints.
    - All form field labels, mandatory asterisks, and context-specific placeholders.
    - Filter options (`All Visibility`, `Draft`, `Published`, `Hidden`, `All Processing`, `Pending`, `Processing`, `Ready`, `Failed`).
    - Status badges, processing progress texts, failure diagnostics, and slice preparation summaries.
    - All 14 toast notifications (validation warnings, upload errors, async processing initiations, publication confirmations).
  - Replace all hardcoded English strings in template markup and script logic with `$t(...)` and `t(...)`.
- **Design System & Responsive Typography Alignment:**
  - Upgrade form labels, input values, and descriptive text from `text-xs` (12px) to minimum `text-sm` (14px) on mobile and `text-base` (16px) on desktop/tablet, strictly restricting `text-xs` to compact pills, badges, and timestamps.
  - Apply `whitespace-nowrap shrink-0` and responsive gap sizing across all buttons, filter controls, and badges to prevent text collisions between English and Vietnamese layouts.
- **Preserve Business Logic & API Contracts:**
  - Preserve all existing `useCatalogStore` integrations, polling mechanisms, concurrency tokens (`concurrencyVersion`, `revision`), and RFC 7807 problem details handling without breaking existing unit or integration tests.

## Capabilities

### Modified Capabilities

- `curated-catalog`: Update administrator catalog interface requirements to mandate modular component decomposition, 100% bilingual localization coverage, and strict compliance with DeepPace responsive typography invariants.
