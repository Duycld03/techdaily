# Proposal

## Why

New accounts correctly start with an empty personal library, but learners must find and import a document before experiencing DeepPace. An administrator-curated discovery catalog lets them choose a prepared document and begin reading while preserving personal-library autonomy.

## What Changes

- Add an authenticated Discover tab alongside My Library, with paginated search, category, language and source-type filters, source attribution, table of contents and a bounded reading preview.
- Add an administrator workspace to upload PDFs or import URLs, edit catalog metadata, inspect processing results, explicitly prepare initial slices, publish and hide documents.
- Gate publication on successful extraction, initial-slice preparation, administrator review and recorded permission to distribute and process the content.
- Add a document to the learner's library as an independent prepared snapshot, without downloading or generating content again on adoption. Make adoption retry-safe and prevent duplicate active copies.
- Keep new personal libraries empty until learners explicitly add content; keep existing personal imports available.
- Add server-enforced catalog administration authorization and prevent catalog source content from leaking through personal-book APIs.
- Exclude community submissions, recommendation engines, external search, book purchasing, automatic content updates and bundled commercial books from this release.

## Capabilities

### New Capabilities

- `curated-catalog`: Administrator-owned catalog preparation, publication, discovery, previews and adoption into personal libraries.

### Modified Capabilities

- `library`: Add discovery navigation and owned snapshot adoption without weakening existing user-scoped catalog isolation or empty-library behavior.
- `auth`: Add server-verified administrator authorization for catalog management, with no public privilege-granting endpoint.

## Impact

- Domain/Application: catalog entities, publication rules, adoption transaction, dedicated DI handlers and DTOs; reuse ingestion service boundaries and existing document/chunk representations.
- Infrastructure: EF migrations, catalog persistence, explicit ingestion job destinations, preparation/retry handling and configured administrator authorization.
- API: new `/api/v1/catalog` and `/api/v1/admin/catalog` endpoints; existing personal-library contracts remain compatible.
- Frontend: `frontend/app/pages/library.vue`, a dedicated admin catalog page, catalog store/components and English/Vietnamese messages; existing layout archetypes and modal/select controls.
- Tests: authorization, content visibility, publication readiness, ingestion failure/retry, transactional adoption, ownership isolation and frontend state contracts; desktop/mobile visual verification in both locales during implementation.
- Operational dependency: an operator designates existing administrator accounts and administrators supply permitted source material. No third-party book is bundled by this change.
