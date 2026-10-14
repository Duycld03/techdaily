# Design

## Context

See proposal.md for motivation. `DocumentBook` carries nullable `CreatedByUserId`; `GetBooksHandler` filters by the caller when supplied. `GetBookByIdRequest` currently carries only BookId and its handler loads by identifier. `DocumentChunk` owns text, formatting state, language and embeddings; learning entities reference its identifier. `User` has no administrator permission field. PDF ingestion uses temporary files and an in-memory channel; `PdfIngestionWorker` writes directly to personal book/chunk tables and prepares initial slices. Existing tests include `LibraryEndpointsTests`, `GetBookByIdAndSliceHandlerTests`, `GetBooksHandlerTests`, and frontend library page/store tests.

The library spec contains older eager-formatting language alongside the later Lean 3-Slice Initial Ingestion requirement. This change explicitly follows the latter: readable source for all slices, preparation of only the first three, on-demand formatting thereafter. It does not undertake a general cleanup of historical specs. The configured context also lists older AI models; new processing must follow AGENTS.md's current fail-loud model configuration rather than copying a model fallback.

## Goals / Non-Goals

**Goals:** Establish a catalog boundary that cannot leak drafts into personal learning queries; reuse extraction/formatting logic; support safe adoption without new network or AI dependencies in the adoption transaction.

**Non-Goals:** A general RBAC management product, shared mutable reading state, automatic synchronization of adopted copies, community submissions, full-text external search, exhaustive pre-generation of every challenge, or a new AI provider.

## Decisions

### 1. Separate catalog storage, independent personal snapshots

Introduce `CatalogDocument` and `CatalogSlice`, rather than overloading `DocumentBook.IsPublished` to mean globally visible. CatalogDocument holds title, description, category, language, source type, source URL/author, attribution text, permission basis/evidence, distribution and AI-processing confirmations, visibility, processing state, content revision, concurrency version, creator/reviewer/publisher identifiers and publication time. Private permission evidence is excluded from learner DTOs. CatalogSlice holds ordered source and formatted content, summaries, takeaways, reading duration and optional valid embeddings, with the same content semantics as personal chunks. Apply soft-delete filters and active slice-order uniqueness.

Add nullable `SourceCatalogDocumentId` and `SourceCatalogRevision` to DocumentBook; copy attribution into a personal provenance field so it survives catalog edits. Add a partial unique index on `(CreatedByUserId, SourceCatalogDocumentId)` where neither source nor owner is null and IsDeleted is false. New copies receive unique slugs and new book/chunk IDs. Existing books retain null provenance. Do not copy personal artifacts or cached audio objects. Prepared content means source text and initial formatting; daily challenges remain governed by existing explicit learning actions.

Alternative: membership rows referencing shared DocumentBook content save storage, but require changes throughout editing, deletion, chunk curation, graph, audio and ownership logic. Independent copies are simpler for this release and preserve existing reader contracts.

### 2. Explicit preparation before publication

Reuse `IPdfExtractor`, crawler, SSRF validation, slicing and AI formatting through dedicated application service boundaries. Refactor worker persistence into an explicit destination adapter for personal versus catalog content; never temporarily persist catalog drafts as public or unowned DocumentBooks. Existing personal ingestion retains its behavior. Admin PDF upload and confirmed URL import explicitly authorize preparation; browsing and metadata edits do not.

All extracted slices must exist and contain readable text; first min(3,N) must successfully complete formatting. Extraction readiness and publication eligibility are distinct. AI failure leaves an inspectable draft with a structured error. Use the configured low-latency model, CancellationToken propagation and existing input limits; no silent fallback generation.

Persist a per-entry processing generation/lease in the database. Only one active run can write; worker writes verify generation so a superseded run cannot overwrite a retry. A retry atomically replaces that attempt's extracted slice set or resumes formatting after completed extraction, retaining unique slice order. Mark stale interrupted work recoverable on restart. Temporary PDFs remain temporary; if extraction cannot resume because its file has gone, ask the admin to upload again. URL retries revalidate redirects and addresses. This bounds the first release without adding a durable file archive.

Publication requires explicit admin review of the current revision plus attribution and permission fields. Published entries are immutable: hide before editing, increment revision and clear review on changes; reprepare when source content/language changes. Store actor/time for publish and hide. Hide is a discovery operation, not a recall of existing copies.

### 3. Atomic adoption with database concurrency control

In one PostgreSQL transaction, lock the catalog document row, check Published state and preparation readiness, check for an existing active owned copy, and clone the current revision's content. Hide/edit/publish operations take the same row lock. The unique active-copy index is the final duplicate guard; on conflict, roll back and return the winning existing copy. Copy slices in bounded batches inside the transaction, without external requests. A lost response is safe to retry. Cancellation or failure rolls back all inserts. A committed adoption before hiding remains valid; hiding first rejects a new adoption. A deleted personal copy can be replaced while the entry is published, without reviving old learning records.

Adoption does not create a pacer, mark a slice read, or switch the active book. The response includes BookId and AlreadyAdded. Catalog listing/detail join active owned copies to expose ExistingBookId. An already-owned copy remains available through My Library even if the source is hidden.

### 4. Administrator policy without public role administration

Use a server configuration array `CatalogAdministration:UserIds`, supplied through .env-compatible keys such as `CatalogAdministration__UserIds__0`. Default is empty. A dedicated authorization policy checks the authenticated subject against configured IDs and verifies the user is active. Permission changes take effect when configuration is loaded/reloaded; restart is acceptable for environment changes. Do not embed the allowlist in frontend output or depend on stale role claims. Expose only `{ canManageCatalog }` from authenticated `GET /api/v1/catalog/capabilities`.

Alternative: persistent roles with invitation and role management add unrelated administrative scope. The operator-controlled allowlist bootstraps existing real accounts without hardcoded IDs, default passwords or bypass authentication.

### 5. API contracts and access boundaries

Use direct DI handlers and RFC 7807 errors. Proposed routes:

| Route | Contract |
|---|---|
| GET `/api/v1/catalog/documents` | Authenticated; search, category, language, sourceType, page=1, pageSize=12 (max 100); metadata, ExistingBookId, counts; published only |
| GET `/api/v1/catalog/documents/{id}` | Authenticated; metadata, attribution, TOC, first-slice preview, ExistingBookId; no arbitrary learner slice route |
| POST `/api/v1/catalog/documents/{id}/adopt` | Authenticated; no owner ID in payload; 201 for new copy or 200 for existing copy; 404 if unavailable |
| GET `/api/v1/catalog/capabilities` | Authenticated; boolean management capability |
| GET `/api/v1/admin/catalog/documents` | Administrator; paginated drafts/published/hidden with processing state |
| POST `/api/v1/admin/catalog/documents/upload-pdf` | Administrator; multipart metadata/file; 202 with draft ID |
| POST `/api/v1/admin/catalog/preview-url` | Administrator; validated extraction preview, no automatic publication |
| POST `/api/v1/admin/catalog/documents/import-url` | Administrator; confirmed URL plus metadata; 202; validate source again |
| GET `/api/v1/admin/catalog/documents/{id}` and `/slices/{order}` | Administrator; review content and processing details |
| PUT `/api/v1/admin/catalog/documents/{id}` | Administrator; metadata and expected version; reject published edits |
| POST `/api/v1/admin/catalog/documents/{id}/prepare` | Administrator; explicit prepare/retry, optional replacement PDF; 202 or 409 if busy |
| POST `/api/v1/admin/catalog/documents/{id}/publish` or `/hide` | Administrator; expected version and explicit review confirmation on publish |

Return 400 for invalid input, 409 for stale versions, busy work or unmet publication readiness, 401/403 for authentication/administration failures. Discovery ordering is PublishedAt descending then ID, search matches title/description, filters combine conjunctively. Preview renders through the existing sanitized Markdown component; external source links use target blank and noopener noreferrer.

Do not widen `GET /library/books`. Propagate caller identity to personal book/slice/status/export/curation and learning operations wherever it is missing, with owner-only lookup returning 404 for other owners. Catalog entities are never accepted as personal books. Test access via quiz grounding, pacer switching, graph and audio as well as reader endpoints. This is necessary to preserve adopted-copy isolation, not permission to perform unrelated auth refactoring.

### 6. UI integration and verification

Use library query `tab=mine|discover`, default mine. Keep each tab's results and query state separate in a catalog store; reset pagination on filter changes and discard stale responses. Use existing BoardLayout for the catalog/list and MasterDetailLayout for `/admin/catalog`. Reuse AppSelect, AppModal, toasts, pagination and source-language controls. Add discovery links to the empty states of library/today/roadmap. Admin navigation depends on fresh capability lookup; backend enforcement is authoritative.

The admin form separates metadata, source upload/import, preparation status and review/publication controls. Learner adoption presents loading, retry and Open Book states without ingestion jargon. Loading or errors preserve entered metadata. Both locales use the responsive typography and no-wrap action rules.

Behavioral tests cover payloads, authorization, async state, filters and errors, not CSS classes. Verify PostgreSQL transaction/partial-index behavior against PostgreSQL, since existing SQLite tests cannot prove production concurrency semantics. Frontend implementation requires all npm test suites and direct automated Chromium screenshot inspection at 1440x900 and 390x844 in both languages using authentic accounts, including administrator and non-admin states. Present screenshots before marking UI tasks complete.

## Risks / Trade-offs

- Storage grows with adoptions -> reuse prepared text/embeddings but accept per-user copies; measure transaction duration and storage on a large fixture before release.
- Existing owner-unaware readers -> explicitly thread user IDs through affected handlers and test cross-user identifiers across learning integrations.
- Process restarts lose in-memory queue work -> persist attempt state, surface recoverable interruption and require re-upload if a temporary source is gone.
- Metadata permission confirmation is not license verification -> administrators must provide suitable material; the change ships no third-party book corpus.
- A hidden source remains in existing copies -> make this behavior explicit to administrators; global content recall is outside this release.

## Migration Plan

1. Add catalog tables, nullable personal provenance and filtered indexes; do not backfill or expose existing personal/unowned books.
2. Deploy API/worker authorization and catalog paths, preserving personal ingestion contracts; configure existing real admin user IDs in the environment.
3. Deploy discovery/admin UI. With an empty catalog, retain personal import guidance and no fabricated entries.
4. Administrator imports a permitted fixture, reviews preparation and explicitly publishes; verify adoption and isolation with a second real account.
5. Roll back UI/API to the previous deployment if needed while retaining additive tables and owned snapshots. Remove schema only after backup and checking provenance references; do not delete adopted personal content to roll back discovery.
