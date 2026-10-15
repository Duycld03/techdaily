# Tasks

## 1. Domain & Application Layers

- [x] 1.1 Remove compliance fields (`PermissionBasis`, `PermissionEvidence`, `ConfirmedDistributionRights`, `ConfirmedAiProcessingRights`) and simplify `CheckPublicationEligibility` in `backend/src/DeepPace.Domain/Entities/CatalogDocument.cs`.
- [x] 1.2 Remove compliance validation rules and payload bindings in `AdminUploadCatalogPdfHandler.cs`, `AdminImportCatalogUrlHandler.cs`, and `AdminUpdateCatalogMetadataHandler.cs`.
- [x] 1.3 Update DTO records and mappings in `CatalogDtos.cs` and `CatalogDtoMappingExtensions.cs`.

## 2. Infrastructure & Database Layer

- [x] 2.1 Update `EntityConfigurations.cs` to remove property mappings for deleted compliance fields on `CatalogDocument`.
- [x] 2.2 Generate and apply EF Core migration `RemoveCatalogPermissionComplianceFields` to drop the 4 compliance columns from PostgreSQL.

## 3. Api Layer & Endpoints

- [x] 3.1 Update `CatalogEndpoints.cs` request records and route mappings to remove compliance fields.

## 4. Frontend Admin Catalog & Form Simplification

- [x] 4.1 Update `AdminCatalogDraftForm.vue` to expose all 7 official DeepPace categories and remove compliance/permission inputs and checkboxes.
- [x] 4.2 Streamline `AdminCatalogDocumentDetail.vue` and `AdminCatalogSlicesReview.vue` by removing the manual review confirmation button and enabling direct publishing.
- [x] 4.3 Support expanded navigation rail width (`md:w-84 lg:w-96`) in `MasterDetailLayout.vue` and `AdminCatalogNavRail.vue` to eliminate filter select text clipping, and remove duplicate "● Sẵn sàng" status text from catalog cards.
- [x] 4.4 Conditionally hide the "+ Tạo Bản Nháp Mới" button in `AdminCatalogHeader.vue` when the draft form is actively open.

## 5. Frontend Admin Shell, Header & Dashboard

- [x] 5.1 Update `useAdminAccessStore.ts` to preserve `isAuthorized = true` during background capability verification on client-side route transitions, eliminating the SPA flash reload.
- [x] 5.2 Polish `AdminHeader.vue` by removing `:touch-targets="true"` from `LocaleSelector` and centering the `ThemeToggle` button icon.
- [x] 5.3 Remove the redundant "Tiếp tục học tập" card from `AdminServicesPanel.vue` in the admin dashboard.

## 6. Frontend User Library

- [x] 6.1 Remove duplicate `● Sẵn sàng` / `● Ready to read` text from book cards above the progress bar in `frontend/app/pages/library.vue`.

## 7. Testing & Verification

- [x] 7.1 Update and execute backend test suites (`dotnet test backend/DeepPace.sln`) covering catalog entities, handlers, and endpoints.
- [x] 7.2 Update and execute frontend test suites (`npm test`) covering admin navigation and catalog state.
- [x] 7.3 Perform automated browser visual verification with headless Chromium on Desktop (1440x900) and Mobile (390x844) viewports.
