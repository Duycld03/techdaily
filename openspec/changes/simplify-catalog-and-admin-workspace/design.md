# Design

## Context

See `proposal.md` for background and motivation.
The `CatalogDocument` entity currently contains 4 compliance columns (`PermissionBasis`, `PermissionEvidence`, `ConfirmedDistributionRights`, `ConfirmedAiProcessingRights`) and requires an explicit prior `ReviewedByUserId` before publication. The frontend admin catalog ingestion form exposes corresponding inputs that create substantial friction. Concurrently, `useAdminAccessStore` toggles `state.value = 'checking'` during every route transition's background capability check, causing `app.vue` to unmount `AdminShell` and mount `AdminAuthorizationBoundary` with a visible loading flash.

## Goals / Non-Goals

**Goals:**
- Cleanly drop obsolete compliance columns via EF Core migration in PostgreSQL.
- Streamline `CatalogDocument.CheckPublicationEligibility()` to require only readable, prepared slices and attribution.
- Eliminate admin SPA transition flashing by keeping `isAuthorized = true` during background re-verification.
- Fix UI layout balance in `MasterDetailLayout` and `AdminCatalogNavRail`, preventing text truncation of filter selects.
- Polish header toggle controls and remove redundant dashboard/library cards and status text.

**Non-Goals:**
- Altering the learner reading engine, SM-2 algorithms, or personal book storage.
- Modifying JWT authentication or role verification logic.

## Decisions

### Decision 1: Database & Domain Entity Schema Simplification
- **Action**: Remove `PermissionBasis`, `PermissionEvidence`, `ConfirmedDistributionRights`, and `ConfirmedAiProcessingRights` from `CatalogDocument`, `EntityConfigurations.cs`, and generate a dedicated EF Core migration `RemoveCatalogPermissionComplianceFields`.
- **Rationale**: The compliance proofs were bureaucratic overhead without operational utility. Dropping them cleans the relational model and eliminates boilerplate across handlers, DTOs, and API endpoints.

### Decision 2: Streamlined Publication Eligibility
- **Action**: In `CatalogDocument.CheckPublicationEligibility()`, remove the requirement for `ReviewedByUserId != null`. Ensure publication requires:
  1. `Slices.Count > 0` and all slices have readable source content.
  2. The initial `min(3, Slices.Count)` slices have `IsAiFormatted == true`.
- **Rationale**: The administrator directly invoking the `Publish` endpoint is inherently performing the editorial review and authorization. Retaining a separate "Confirm Review" button was redundant friction.

### Decision 3: Non-Flashing Admin SPA Route Guard
- **Action**: In `useAdminAccessStore.ts`, when `state.value === 'authorized'`, executing `verify()` in the background will NOT reassign `state.value = 'checking'`.
- **Rationale**: In `app.vue`:
  ```html
  <AdminShell v-if="adminAccess.isAuthorized"><NuxtPage ... /></AdminShell>
  <AdminAuthorizationBoundary v-else />
  ```
  If `state` drops to `'checking'`, `isAuthorized` becomes `false`, immediately unmounting `AdminShell` and creating a jarring full-page reload appearance. Keeping `isAuthorized` true during background revalidation preserves SPA smoothness. If the verification endpoint returns 401/403, `state` transitions directly to `'denied'`.

### Decision 4: Responsive Navigation Rail Geometry & Text Fitting
- **Action**: Allow `MasterDetailLayout` to support an expanded navigation rail width (`md:w-84 lg:w-96`, 336px–384px) for catalog administration.
- **Rationale**: In Vietnamese, "Tất cả hiển thị" and "Tất cả trạng thái" in a 2-column grid inside a 288px container (`md:w-72`) were severely truncated. An expanded width gives both select dropdowns adequate space.

### Decision 5: Header Alignment & Duplicate Status Cleanup
- **Action**:
  - Remove `:touch-targets="true"` from `LocaleSelector` in `AdminHeader.vue`.
  - Add flex centering to `ThemeToggle.vue` or remove `min-h-11 min-w-11` override in `AdminHeader.vue`.
  - Remove redundant `● Sẵn sàng` status badge in `AdminCatalogNavRail.vue` and `frontend/app/pages/library.vue`.
  - Remove redundant "Tiếp tục học tập" card from `AdminServicesPanel.vue`.

## Risks / Trade-offs

- **Risk**: Dropping database columns in existing environments.
  - **Mitigation**: Generated EF Core migration uses standard `migrationBuilder.DropColumn` calls. Since this is non-production / development data, dropping empty or dummy compliance text is completely safe.
- **Risk**: Existing unit tests failing due to missing fields.
  - **Mitigation**: Update all handler, domain, and API tests in `DeepPace.Tests` to remove assertions on the 4 dropped fields.
