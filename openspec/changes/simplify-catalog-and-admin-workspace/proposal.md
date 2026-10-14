# Proposal

## Why

The current admin curated catalog ingestion and review workflow imposes bureaucratic copyright compliance fields (`PermissionBasis`, `PermissionEvidence`, mandatory checkboxes) and manual two-step review friction that clutter the database and frontend form. Simultaneously, the admin workspace suffers from artificial SPA full-screen flash/reloads caused by dropping `isAuthorized` during background re-verification, severe layout width imbalances with truncated filter text, duplicate action buttons, mismatched header controls, redundant dashboard cards, and duplicate "Ready" status indicators across admin and library cards.

## What Changes

- **Backend & Database Simplification**:
  - Remove bureaucratic compliance fields (`PermissionBasis`, `PermissionEvidence`, `ConfirmedDistributionRights`, `ConfirmedAiProcessingRights`) from `CatalogDocument` domain entity, configurations, and DTOs.
  - Create EF Core migration dropping these 4 columns from the `CatalogDocuments` table in PostgreSQL.
  - Simplify publication eligibility in `CatalogDocument.CheckPublicationEligibility()` by removing the manual review gating (`ReviewedByUserId`), enabling immediate publication once slices are extracted and ready.
  - Remove validation rules for permission compliance fields in `AdminUploadCatalogPdfHandler`, `AdminImportCatalogUrlHandler`, and `AdminUpdateCatalogMetadataHandler`.
- **Admin Catalog UI & Form Simplification**:
  - Expand `AdminCatalogDraftForm.vue` category options to all 7 official DeepPace categories (including `MentalModels` and `HabitsProductivity` for books like *Atomic Habits*).
  - Strip all legal/copyright inputs and checkboxes from `AdminCatalogDraftForm.vue` and `AdminCatalogDocumentDetail.vue`, retaining only clean metadata (Source File/URL, Title, Category, Language, Author/Attribution).
  - Remove the manual "Confirm Administrator Review" ("Xác Nhận Xem Xét") button from `AdminCatalogSlicesReview.vue` and `AdminCatalogDocumentDetail.vue`.
  - Widen `AdminCatalogNavRail.vue` container in `MasterDetailLayout.vue` from `md:w-72` to `md:w-84 lg:w-96` so filter dropdowns ("Tất cả hiển thị", "Tất cả trạng thái") fit without text truncation.
  - Remove duplicate "● Sẵn sàng" status text from `AdminCatalogNavRail.vue` item cards.
  - Conditionally hide the top header "+ Tạo Bản Nháp Mới" button in `AdminCatalogHeader.vue` when the draft form is already open.
- **Admin Workspace SPA Navigation Fix**:
  - In `useAdminAccessStore.ts`, preserve `state.value = 'authorized'` and `isAuthorized = true` during background re-verification on route changes, preventing `app.vue` from unmounting `AdminShell` and flashing the full-screen authorization boundary.
- **Admin Header & Dashboard Polish**:
  - Remove `:touch-targets="true"` from `LocaleSelector` in `AdminHeader.vue` to make the `EN | VI` toggle compact matching the user header.
  - Fix `ThemeToggle` alignment in `AdminHeader.vue` by centering the icon and normalizing size.
  - Remove the redundant "Tiếp tục học tập" service card from `AdminServicesPanel.vue` on the admin dashboard.
- **User Library Card Cleanup**:
  - Remove duplicate `● Sẵn sàng` / `● Ready to read` text above the progress bar in `frontend/app/pages/library.vue`.

## Capabilities

### Modified Capabilities
- `curated-catalog`: Streamline catalog document ingestion, publication eligibility, and metadata schemas by removing permission compliance fields and manual review prerequisites.
- `admin-workspace`: Smooth SPA navigation transitions without shell unmounting, polished header sizing/alignment, balanced navigation rail widths, and decluttered dashboard cards.
- `library`: Remove redundant status indicators from book cards to maintain clean, minimal card hierarchy.

## Impact

- **Backend**: Drops 4 database columns via migration; updates `CatalogDocument`, handlers, and endpoint contracts.
- **Frontend**: Streamlines catalog forms, refines admin sidebar/header layout, eliminates SPA navigation flash, and removes duplicate status labels.
- **Dependencies**: No external library additions.
