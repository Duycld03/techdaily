# Proposal

## Why

When administrators create a web document draft in the Admin Catalog interface (`/admin/catalog`), submitting the form fails with HTTP `400 Bad Request` and validation error `"'Url' must not be empty."`. 

This occurs because `AdminCatalogDraftForm.vue` passes `{ sourceUrl: ... }` instead of `{ url: ... }` to `catalogStore.importUrl()`, omitting the required `url` property in the HTTP POST body sent to `/api/v1/admin/catalog/documents/import-url`. In addition, if an administrator inputs a URL in the primary URL input field but does not click the "Preview Extraction" button before clicking "Create Draft", the submission is blocked due to checking only the secondary metadata field `sourceUrl`.

Fixing this contract mismatch and URL fallback restores the web document ingestion workflow for administrators, ensuring web technical documentation can be seamlessly imported into the curated catalog.

## What Changes

- **Fix Web Import Payload Binding in `AdminCatalogDraftForm.vue`**:
  - Pass `url: targetUrl` instead of `sourceUrl: sourceUrl.value.trim()` when calling `catalogStore.importUrl()`, conforming to the `AdminImportCatalogUrlApiRequest` API contract.
  - Compute `targetUrl` as `previewUrlInput.value.trim() || sourceUrl.value.trim()`, allowing draft creation whether or not the administrator triggered preview extraction first.
  - Retain `authorOrSourceUrl: sourceUrl.value.trim() || undefined` in the payload for metadata and attribution.
- **Form Submission Validation**:
  - Validate that `targetUrl` is non-empty before initiating submission, providing localized user-facing feedback if neither URL field contains an address.
- **Unit & Integration Test Coverage**:
  - Add comprehensive Vitest unit tests in `AdminCatalogComponents.spec.ts` exercising `AdminCatalogDraftForm.vue` URL import submission, verifying exact payload serialization and store interaction.

## Capabilities

### Modified Capabilities
- `curated-catalog`: Update `Administrator draft ingestion` requirement scenarios to verify that web draft creation serializes and validates `url` conforming to the import contract with fallback to the primary URL input.

## Impact

- **Affected Components**:
  - Frontend: `frontend/app/components/catalog/admin/AdminCatalogDraftForm.vue`
  - Tests: `frontend/tests/components/AdminCatalogComponents.spec.ts`
- **API Contracts**: No changes to backend endpoints or DTO schemas; the frontend is brought into strict alignment with existing backend contracts in `CatalogEndpoints.cs`.
- **Breaking Changes**: None.
