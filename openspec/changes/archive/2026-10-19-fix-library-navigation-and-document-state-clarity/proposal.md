# Proposal

## Why

Learner navigation still exposes catalog administration, while crowded library toolbars and inconsistent URL/store state make search, clearing, filtering, and pagination unreliable. Document operations also omits catalog publication visibility and mislabels technical readiness as learner availability, compounded by untranslated scope labels and raw category metadata rendering.

## What Changes

- Remove administrative destinations from the learner desktop sidebar and mobile navigation, regardless of the account's administrative permissions; retain the dedicated administrator shell and existing authorization.
- Make My Library and Discover toolbars usable with bilingual category names: protect the search field and filter action, show a bounded metadata-ordered quick-category subset, and retain every category in advanced filters.
- Align the active tab's committed search, supported filters, and page across URL, controls, store, and requests, including Enter, clear, reset, reload, browser history, and query-driven page remounts. Retain compatible URL criteria when switching tabs, reset pagination, and remove Discover-only source type when opening My Library.
- Complete the existing My Library language control through the authenticated books query and server-side filtering, including correct filtered counts and paging.
- Project each catalog document's authoritative publication visibility into operations inventory and detail, independently of processing status; explain completed processing without claiming that the document is published or accessible for new adoption.
- Use Vietnamese curated-document terminology in operations, localize system-owned/source-type metadata, and consume the real category descriptor contract instead of displaying its object as JSON.
- Provide implementation tasks with explicit prerequisite skill reads and behavioral plus bilingual desktop/mobile verification gates.

## Capabilities

### New Capabilities

None. This change extends existing capability boundaries.

### Modified Capabilities

- `library`: Define consistent active-tab URL/search/filter/page transitions, stable search toolbar behavior, and working server-side language filtering for personal books.
- `curated-catalog`: Make Discover clear/reset and quick-filter behavior explicit without changing publication, preview, adoption, or personal-snapshot independence.
- `admin-workspace`: Require learner navigation isolation in both directions and correct the existing Vietnamese `Catalog` wording in the localization contract.
- `admin-document-operations`: Expose publication visibility alongside processing status for catalog records and require truthful readiness explanations and localized descriptor-based metadata.

## Impact

- Frontend: `useNavigationMenu`, its desktop/mobile consumers, `pages/library.vue`, the existing library/catalog filter components, `useCatalogStore`, `useLibraryStore`, `pages/admin/documents.vue`, `useAdminDocumentsStore`, shared category formatting, locale dictionaries, and generated OpenAPI types.
- Backend: the books list endpoint/request/handler and the operations inventory/detail DTOs and projections. API additions are an optional personal-library `language` query and nullable operations `visibility`; existing endpoint paths, response envelopes, and permission checks remain.
- Tests and documentation: focused library/navigation/operations behavior coverage, real category-shaped fixtures, existing backend handler/API tests, and the library/admin workflow sections of `README.md` and `docs/admin-workspace.md` during implementation.
- No database migration, new dependency, synchronization job, authentication change, learner-content access, processing retry-policy change, or automatic generation. Catalog hiding continues to block discovery and new adoption without changing already adopted personal copies.
- This proposal creates planning artifacts only; implementation requires a separate apply request.
