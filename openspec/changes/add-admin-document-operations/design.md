# Design

## Context

See `proposal.md` for motivation and the seven delta specs for behavioral contracts. This design depends on the account authority, capability endpoint, audit writer and administrative transaction boundary planned in `add-admin-accounts-audit`; that change currently has no completed implementation tasks.

Observed integration points:

- `UploadPdfHandler` and `ImportRemotePdfHandler` stream PDFs with an 80KB buffer and a 350MB limit. The source path exists only in an in-memory `PdfIngestJob`; `PdfIngestionWorker` deletes the file in `finally` even after failure. Production Compose currently has no persistent backend source volume.
- `PdfIngestionQueue` is an unbounded in-memory channel. Startup recovery marks Processing documents failed without a durable claim that distinguishes another live worker from an interrupted one.
- `ImportDocumentHandler` processes accepted web/Markdown text synchronously and persists the book only after slicing/enrichment. `AdminImportCatalogUrlHandler` also has synchronous work before persistence; its detected-PDF branch uses a separate unbounded download path that must be replaced with the existing bounded, SSRF-validated downloader.
- Personal ingestion currently appends chunks. Catalog ingestion has generation/lease checks and replaces slices, while `AdminPrepareCatalogHandler` accepts a temporary filesystem path. These paths cannot be reused unchanged for controlled recovery.
- Existing personal imports can become readable despite optional enrichment warnings. Catalog publication additionally requires successful initial formatting. Operations must preserve this distinction rather than redefine Ready.
- The older `PDF File Ingestion` spec describes eager Tier 2 curation, conflicting with the later `Lean 3-Slice Initial Ingestion` requirement and current worker. The library delta reconciles that wording with the established lean policy while adding durable admission. Its two-second response budget starts after validated source receipt; it cannot cover an arbitrary remote download.
- Main admin-workspace requirements now include SPA/header scenarios absent from the pending accounts delta. This change composes both sets and preserves Accounts/Audit navigation; apply must recheck the final prerequisite contracts before editing shared consumers.

## Goals / Non-Goals

**Goals:**

- Give every accepted source-processing command a recoverable identity and a truthful persisted lifecycle.
- Keep source admission, quota reservations, concurrency, completion and audit boundaries explicit without external I/O inside database transactions.
- Prevent a retry or replacement from overwriting learned content, stale generations or published catalog content.

**Non-Goals:**

- Build a general scheduler, introduce a message broker, or offer automatic/bulk failure retry and cancellation controls.
- Turn attempt history into private-content support access or new AI/audio regeneration tooling.
- Promise cryptographic protection from a host/database operator or fabricate historical attempts for legacy data.

## Decisions

### 1. Compose the existing admin foundation

Extend `/api/v1/admin/capabilities` with `canManageDocumentOperations`, resolved through the prerequisite's active-account/operator designation service. Use a `DocumentOperationsAdmin` policy for inventory/history/retry and preserve the catalog capability endpoint. A catalog retry additionally checks catalog-management permission; catalog source replacement uses the catalog policy. Owner replacement always checks the authenticated personal owner, even for administrators.

Reuse versioned JWT admission and the administrative coordinator. Recheck actor account/session authority and required capability inside administrative command transactions. Preserve no-store responses, shared login, SSR refresh behavior and generation-based privileged request cleanup. A new independent role or auth store would duplicate the established authority.

The prerequisite's post-commit queue signaling becomes a wake-up hint once this change supplies durable dispatch. A failed channel notification leaves accepted work Queued and discoverable; it must not falsely record failed processing or require a duplicate retry. Existing catalog audit acceptance is extended rather than recorded twice.

### 2. Persist attempts, source artifacts and control versions

Introduce rich domain lifecycle types and Application contracts, with EF, filesystem and scheduling implementations in Infrastructure:

| Record | Principal fields and responsibility |
| --- | --- |
| `IngestionAttempt` | Target kind/ID, predecessor ID, origin, trusted initiator ID, request ID/fingerprint, source ID, generation, stage/state, recorded progress, queued/started/ended UTC times, claim identity/expiry and safe error code |
| `IngestionSource` | Opaque storage identity, source kind, UTF-8/PDF byte length and integrity hash, quota account, declared language, creation/first-failure/expiry times, availability and deletion state; no browser-visible path |
| Storage reservation/accounting | Global and per-account charged/reserved bytes, upload reservation identity, expiry and cleanup state |
| Target additions | Positive `IngestionControlVersion`, current attempt ID/generation and persisted personal content language when no slices yet exist |

Use explicit nullable personal/catalog foreign keys with an exclusive-target constraint, or an equivalent validated typed target representation. Keep attempt/source metadata when a target is soft-deleted; prevent cascade deletion of audit/history. The target's current generation and a partial unique active-attempt index enforce one Queued/Running attempt. Index history by target/time/ID and worker discovery by state/claim expiry.

Backfill control versions to 1. Personal content language is derived only from a consistent surviving slice language; otherwise it is unknown until the owner explicitly selects it during replacement. Never substitute the interface locale as known source language. A source's bytes stay immutable; an explicit owner language correction changes the next attempt's declared language and control version.

Control versions advance on acceptance, source replacement, source-relevant metadata changes, deletion and catalog lifecycle changes affecting eligibility. Progress heartbeats do not invalidate every open form. Keep existing catalog `ConcurrencyVersion` and `ProcessingGeneration` synchronized with this boundary rather than introducing competing writable authorities.

### 3. Use a private persistent source store with bounded accounting

Add a dedicated backend volume, separate from public assets, database data and OS temporary storage. Use server-generated names within its configured root, restricted filesystem permissions and an Application storage abstraction. Validate root accessibility at startup/admission; do not fall back silently to `/tmp`. Multiple worker instances must share the source store they claim from; a local volume is supported only for workers using that same host/volume.

Proposed operator defaults, not additional user decisions:

| Setting | Default and constraint |
| --- | --- |
| `Ingestion__SourceStoragePath` | Explicit private persistent root; required for ingestion admission |
| `Ingestion__GlobalSourceQuotaBytes` | 5 GiB; positive and finite |
| `Ingestion__PerAccountSourceQuotaBytes` | 1 GiB; positive, finite and no larger than global quota |
| `Ingestion__FailedSourceRetentionDays` | 7; configurable from 1 through 7, never above 7 |
| Claim/cleanup timing | Ten-minute renewable claim, renewal before expiry; minute-based cleanup/reconciliation and deadline-triggered source expiry |

Reserve capacity before writing and account for all staging, active and failed source bytes. Known lengths reserve their validated bounded length; unknown remote lengths reserve the existing maximum PDF allowance and release unused bytes after capture. Markdown uses UTF-8 byte length. Use transactionally locked global/account counters, not a process-local dictionary. Retry reuses its existing source charge; explicit replacement needs capacity while both old and new sources exist. Lowering limits rejects new admissions until usage fits and does not evict active inputs.

For catalog sources, charge the trusted submitting administrator's account; personal inputs are charged to their owner. Store that quota account independently of immutable document ownership. Use one consistent lock order for user authority, global quota, account quota, target, attempt and source records. Standalone file-deletion accounting does not take a target lock in reverse order.

Each admitted source gets a hard expiry at creation plus the configured retention window, which is also no later than seven days after its first recorded failure. This conservative bound covers unobserved crashes and never promises a fresh seven-day window on retry. Record first failure for diagnosis but do not extend expiry. A newly and explicitly supplied replacement is a new source admission; command replay is not.

At success or expiry, atomically mark input unavailable/deletion-pending, revoke further claims/access, and delete promptly outside the database transaction. Cap claim renewal at source expiry and cancel processing when it expires. Retain charged bytes until physical deletion is confirmed. Failed deletion remains observable as safe cleanup state and is retried on the deadline/reconciliation path; startup excludes expired files before work discovery. Exclude retained inputs from long-lived backups so backups cannot defeat retention. Document cleanup outages honestly rather than presenting deletion-pending as deleted.

### 4. Make the queued attempt the durable dispatch obligation

Persist the Queued attempt and target state in the same acceptance transaction, including an Accepted audit event for admin commands. Workers discover these rows periodically and may also wake on existing channel signals. Claim through a conditional, short transaction using PostgreSQL row locking/skip-locked discovery. No claim or network work runs inside admission transactions.

Local PDFs are validated and staged before acceptance. For synchronous Markdown imports, reserve/capture accepted text, persist a Running inline claim and target, then execute the existing slicer/enrichment path outside the transaction; success still returns 201. For confirmed remote imports, perform request/security validation before admission, then persist the target and an inline fetch claim before external fetching. A failed fetch records Missing input and safe failure; it is never automatically repeated. After validated PDF capture, transition the same attempt to Queued for extraction.

Accepted requests canceled after commit retain their persisted identity. Problem details for post-admission failures include safe `documentId`, `scope` and `attemptId` extension fields. Do not invent successful acceptance when persistence is unavailable. Staging interrupted before admission is cleaned without creating an import history event.

Reuse shared extraction, paragraph-aware slicing, source-language and initial formatting services through direct DI. Keep personal synchronous import responses and PDF 202 envelopes compatible with additive fields. Catalog Markdown draft creation is not added; a retained Markdown snapshot for catalog recovery represents a previously accepted web article.

### 5. Fence workers and commit safe output once

Queued claims become Running with a unique claim token and current generation. Every progress, failure and completion write checks that token, generation, current target and unexpired claim. Replace fire-and-forget progress writes with awaited, cancellable writes. Renew claims with bounded calls; startup/periodic recovery changes only expired Running claims to Interrupted. Resuming delivery of an existing Queued attempt is delivery recovery, not automatic retry of a failure.

Prepare output outside the completion transaction, then lock/recheck target and attempt before committing output, document state and terminal history. New imports stage slices until safe completion. For failed legacy partial imports, reject unsafe learning-referenced output; replace unreferenced partial output only in an atomic transaction after confirming no learner references. Preserve old output until replacement commits and retain unique active slice-order indexes.

The no-learning-activity check needs a shared document mutation boundary, not a single preflight query. Coordinate completion with writes capable of creating references: highlights/notes, `CurateSliceHandler` questions, daily drill/review-card creation, quiz/progress records, pacer creation/switching, and chunk audio/cache references. Existing handlers creating these records must take the same document guard and recheck active ingestion before mutation. Conflicting work is rejected or commits first and makes recovery ineligible; it cannot pass a stale safety check. Read-only owner access keeps its existing authorization contract.

Catalog completion additionally checks visibility and existing publication/concurrency rules. Existing personal snapshots are never updated. Initial catalog formatting failure can remain Failed while source slices are readable; operations explains that state and directs catalog users to the existing explicit preparation action. No new generic AI, embedding or audio retry dispatcher is introduced.

### 6. Make retry commands replayable and audit their outcomes

An admin retry accepts `{ attemptId, expectedVersion, requestId, reason }`. Reasons are trimmed, required, limited to 500 characters and validated using the prerequisite's safe-reason rules. Compare the latest eligible failure, current control version, source expiry/integrity, ownership, readability, learner activity and catalog visibility under the same mutation boundary.

Persist a unique command identity scoped to actor, target and request ID with a fingerprint of canonical command data. An identical replay, after current authorization succeeds, returns the original attempt identity/status; changed data using the same ID returns 409. This also covers the original request after its output becomes Ready. New request IDs cannot revive a completed or active import. Proposed rate limit is five new retry commands per actor per five minutes; idempotent reads/replays must not create additional external work.

Record `DocumentIngestionRetryRequested` Accepted with the target, attempt ID, correlation and safe reason in the acceptance transaction. Terminal completion writes a linked `DocumentIngestionRetryCompleted` Succeeded/Failed event with the attempt transition; use a unique event identity per attempt/completion action. Cancellation caused by deleted target or expiry has a Failed completion with a safe code. Preserve historical initiator identity even if that administrator later loses permission.

Authorized identifiable validation/concurrency rejections use the prerequisite's clean rejection transaction. Policy-denied requests and ordinary reads do not become mutation events. Audit/storage outage returns failure without claiming an event. Owner resupply appears in ingestion history as owner-origin work, not as an invented administrator action. Catalog resupply uses the same typed administrative acceptance/completion contract.

### 7. Offer explicit source replacement through validated inputs

Personal replacement is an owner action labeled Provide source and retry. It preserves the failed book ID and current metadata and accepts the original source type only. Use current control version and request ID; reject actor/owner/role/storage fields. If the legacy content language is unknown, require an explicit owner language selection before admission. A successful command admits one recovery attempt immediately; it does not require a later administrator impersonation step.

Reuse PDF magic/extension, 350MB/10,000-page safety, streaming, cancellation, pinned DNS/redirect SSRF validation and web sanitization. Preview/fetch alone does not queue replacement. Remote replacement is a deliberate new fetch, whereas an admin retry with retained input never fetches again. Keep old usable input until a validated replacement commits; remove rejected staging and obsolete source artifacts afterward with correct accounting.

Catalog source replacement is restricted to eligible failed/interrupted nonpublished source ingestion and preserves attribution. Reject the legacy public `ReplacementTempFilePath` field with a safe validation/migration code. Existing preparation calls for readable catalog slices keep their independent formatting behavior. A source-ingestion failure is routed through the controlled retry or replacement command, so the old preparation path cannot bypass reason, version or duplicate protection.

### 8. Define bounded API and UI contracts

| Endpoint | Authority and behavior |
| --- | --- |
| `GET /api/v1/admin/documents` | DocumentOperationsAdmin; paged scope/owner/source/category/language/status/search filters |
| `GET /api/v1/admin/documents/{scope}/{id}` | DocumentOperationsAdmin; allowlisted metadata, current status/source and eligibility/control version |
| `GET /api/v1/admin/documents/{scope}/{id}/attempts` | DocumentOperationsAdmin; paged attempt state and UTC range filters |
| `POST /api/v1/admin/documents/{scope}/{id}/retry` | DocumentOperationsAdmin, additionally catalog permission for catalog targets; replayable reasoned command |
| `POST /api/v1/library/books/{id}/source/pdf` | Authenticated owner; streamed replacement plus version/request ID |
| `POST /api/v1/library/books/{id}/source/remote-pdf` | Authenticated owner; explicit validated URL replacement |
| `POST /api/v1/library/books/{id}/source/markdown` | Authenticated owner; accepted web/Markdown snapshot for the matching original source type |
| `POST /api/v1/admin/catalog/documents/{id}/source/pdf` | CatalogAdmin; validated replacement plus version/request ID/reason |
| `POST /api/v1/admin/catalog/documents/{id}/source/url` | CatalogAdmin; confirmed validated web/detected-PDF replacement plus version/request ID/reason |

New queued recovery responses use 202 with target and attempt IDs, state, control version and status location. Personal status fields are additive to the current lightweight response. Use no-tracking, database-side projections; join only bounded metadata and consistently exclude deleted targets. Inventory ordering is CreatedAt descending, ID descending, scope as final tie-breaker; history is QueuedAt descending then ID descending. UTC filters use inclusive from/exclusive to.

Use 400 for malformed commands/input, 401/403 for authentication/permission, 404 for absent/deleted targets or non-owner source replacement, 409 for stale/ineligible state, 429 for rate limits and 503 for unavailable admission/storage/authority. Source capacity uses a distinct 409 quota code, not a fabricated disk measurement. Map known codes for source missing/expired/integrity, learning-state protection, readability, publication, busy, stale version and quota. Raw legacy `ErrorMessage` and dynamic extraction-topic messages are never copied into admin DTOs.

Build `/admin/documents` with MasterDetailLayout: paginated filter/list rail and a metadata/processing detail panel with attempt-history pagination and an explicit retry dialog. Use validated route state for scope, status and selection, resetting page on filter changes. Label `/admin/catalog` Catalog and this workspace Documents; preserve all prerequisite destinations and smooth shell transitions. Dashboard processing metrics link to scope/status filters and remain persisted document counts, not attempt totals or live queue depth.

Reuse AppSelect, teleported modals, useToast, typography/tokens and existing EN/VI error resolvers. The personal Library failed-card action reuses the source-specific import experience, including language selection when needed. Preserve input on errors, show source expiry and blocked reasons, and reload eligibility after conflicts. Suppress duplicate submissions and discard old-session results through the shared privileged request generations. There is no source preview/download affordance in operations and no automatic user notification/contact workflow.

### 9. Verify persistence, authorization and rendered behavior

Use unit tests for lifecycle, validation, safe projection and state transitions. Use isolated PostgreSQL tests for claims, partial unique indexes, quota reservations, replay races, completion/learning-write locking, transaction rollback and restart discovery. Fault-inject storage deletion/write failure and process termination around acceptance/completion. Existing catalog ownership/adoption, worker and original-text tests remain regressions. Exercise actual JWT/current-capability enforcement rather than relying solely on fake authentication handlers.

Frontend Vitest tests cover exact query/command/source payloads, validation, RFC 7807 mapping, stale forms, loading/single-flight behavior, history paging and generation discard. Do not assert CSS classes for layout. Before checking any UI task complete, pass the full `npm test` suite and drive authenticated headless Chromium with real test sessions at 1440x900 and 390x844. Inspect and present EN/VI light/dark screenshots covering long metadata, empty/error states, history, retry confirmation, owner replacement, catalog recovery and shell navigation. Run the backend suite and production frontend build before implementation sign-off.

## Risks / Trade-offs

- [Retained private sources increase disk/privacy exposure] → Bound global/account usage, restrict storage, enforce fixed expiry and immediate success cleanup, exclude retained inputs from backups, and expose no source retrieval API.
- [Filesystem and database cannot share one transaction] → Use charged staging reservations, atomic source naming, persisted unavailable/deletion-pending states and restart reconciliation; release capacity only after confirmed deletion.
- [Cleanup downtime delays physical erasure] → Expired input is unavailable at its deadline, worker claims cannot outlive it, and deadline/startup cleanup retries promptly. Document actual deletion failures and do not claim deletion based only on database state.
- [A preflight learner-reference check races with new activity] → Share a document mutation guard across source completion and reference-creating handlers, then verify both commit orderings in PostgreSQL tests.
- [Source staging reserves more capacity than ultimately needed] → Release unused reservations after validated capture; keep the conservative reservation for unknown-length remote streams and return explicit quota guidance.
- [Multiple instances cannot read a host-local source] → Require a common persistent store for all claimants and deploy one worker/storage host until equivalent shared storage is configured.
- [New durability accidentally adds AI work] → Keep existing readiness/initial-formatting boundaries and block standalone regeneration; read requests and channel redelivery do not create new attempts.
- [Legacy or mixed workers overwrite new generations] → Drain and replace all old ingestion workers together before enabling durable admission. Legacy missing inputs stay explicitly unavailable.

## Migration Plan

1. Verify the implemented `add-admin-accounts-audit` authority/audit contracts before starting this change. Reconcile this change's own shared-capability/workspace deltas against the final prerequisite specs, preserving current SPA/mobile scenarios; do not silently implement or rewrite unrelated pending changes.
2. Add attempt/source/reservation schema, control versions and indexes; backfill only facts available from current records. Do not invent attempts or infer private source ownership from arbitrary files in `/tmp`.
3. Configure the private persistent volume, positive quota settings and cleanup. Validate permissions, restart persistence and source expiry locally before accepting new inputs. Use `.env` for environment settings and empty/non-secret committed defaults as appropriate.
4. Drain existing ingestion and replace backend workers together. Legacy Processing records whose work cannot be recovered become an explicit safe failed/interrupted document state with history unavailable; do not fabricate an attempt or overwrite a live old worker.
5. Enable durable admission, source-specific replacement and protected operations APIs; deploy the permission-aware UI. Existing successful documents, learner ownership, catalog publication and personal snapshots remain unchanged.
6. Update the operational runbook with quota/retention settings, restart/expiry/deletion failure recovery, privacy boundaries, replacement-path migration and verification evidence. Restart nginx after a container rebuild as required by project operations.
7. Roll back the presentation independently. Backend rollback must preserve generation fencing, expiry/cleanup and persisted accepted work; stop new admission before a coordinated worker transition. Prefer forward repair to an old binary that deletes failed sources or ignores durable attempts, and retain audit/history rather than running a destructive down migration.
