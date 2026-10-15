# Design

## Context

See `proposal.md` for motivation and the four delta specs for behavioral contracts.

The current implementation provides useful boundaries to extend:

- `User` has identity and learning preferences, but no suspension state or authentication version. Its existing soft-delete filter and partial email uniqueness must continue to apply.
- `Program.cs` validates JWT signature, issuer, audience and lifetime. It does not query current account/session state. `AuthEndpoints.cs` issues tokens directly for password, registration, Google and refresh flows.
- `RefreshTokenService` stores hashed, family-based tokens and exposes `RevokeAllForUserAsync`; that method cannot invalidate unexpired JWTs. Its existing rotation and grace-window paths need transaction-aware account checks when integrated with revocation.
- `CatalogAdminService` resolves `CatalogAdministration:UserIds` and verifies an existing non-deleted user. Dashboard and catalog reuse its policy. `/api/v1/catalog/capabilities` currently returns `canManageCatalog`.
- Catalog mutations use directly injected handlers and persist through `IDeepPaceDbContext`. Publication and hiding retain latest actor fields; upload/preparation also cross an in-memory queue boundary. There is no append-only audit store or shared administrative transaction coordinator.
- The admin shell verifies one capability through `useAdminAccessStore`. Auth generations already abort or discard privileged responses after logout, account replacement and permission denial. New stores should participate in those boundaries.

## Goals / Non-Goals

**Goals:**

- Define a single persisted account/session authority shared by JWT admission, sign-in, refresh and administrator actions.
- Make administrative state changes and their audit events atomic without introducing reflection-based dispatch or outward layer dependencies.
- Bound query work and returned data, preserve catalog concurrency rules, and reuse the existing admin admission and UI primitives.

**Non-Goals:**

- This is not a general event-sourcing system, a distributed session cache or a durable ingestion-job redesign.
- Application append-only history is not a cryptographic guarantee against a database operator. Retention/export and historical backfill are separate work.
- A successful revocation blocks subsequent authentication checks; it does not retroactively cancel every HTTP request already admitted before the commit. Account mutations additionally recheck the actor inside their transaction.

## Decisions

### 1. Reuse operator designation with explicit capabilities

Use the existing configured administrator cohort as the initial authority for `canManageCatalog`, `canManageAccounts` and `canReadAudit`. Add an Application permission abstraction and Infrastructure resolver, retaining the current catalog service/policy compatibility. Resolve unique valid configured IDs against Active, non-deleted accounts; no hardcoded administrator and no client-selected role.

Expose `GET /api/v1/admin/capabilities` to authenticated accounts, returning explicit false flags for a learner. Preserve `/api/v1/catalog/capabilities` and its current `canManageCatalog` contract. Account endpoints use an `AccountAdmin` policy and audit endpoints an `AuditRead` policy. Dashboard/catalog keep their current policy meaning. Capability responses and all admin responses use `Cache-Control: no-store`, including rejected responses.

This keeps designation unchanged while making destination checks explicit. A database role editor would introduce permission-management scope; trusting a role claim or browser flag would allow stale access.

### 2. Separate account status, command concurrency and authentication version

Add domain-managed user fields:

| Field | Meaning |
| --- | --- |
| `AccountStatus` | Active or Suspended, separate from `IsDeleted` |
| `AuthenticationVersion` | Positive, monotonically increasing version invalidating all earlier sessions |
| `AdminControlVersion` | Optimistic concurrency version returned to account action forms |
| Suspension actor/time/reason | Bounded metadata for the latest suspension; full action history belongs to audit |

Domain methods apply suspension, reactivation and forced session revocation. Suspension and forced revocation increase `AuthenticationVersion`; every successful account command increases `AdminControlVersion`. Reactivation does not restore any earlier authentication version. Reasons are trimmed, required and limited to 500 characters. An inappropriate status transition returns a conflict.

Add the authentication version to refresh-token records and a `session_version` claim to newly issued JWTs. Existing rows start Active with both versions at 1; existing refresh records are backfilled to version 1. Soft-deleted rows remain excluded. AdminControlVersion is an EF concurrency token, and updates still use transactional locks where required.

Using soft deletion for suspension would disrupt ownership and restoration semantics. Expiry-only JWT checks or refresh-only revocation cannot satisfy immediate all-device sign-out.

### 3. Make session issuance and revocation share one transaction boundary

Create a directly injected account/session authority service. JWT bearer admission reads current non-deleted account status and authentication version after normal cryptographic validation. Require a matching positive claim; never substitute a version for missing claims. Start with a narrow database projection per authenticated request, without a positive-state cache that could delay revocation.

All access/refresh issuance paths use a coordinated session issuer that locks the user row, rechecks account state, and issues credentials for that persisted version. Refresh rotation locks/checks the current user and token before creating a successor, including the existing grace-window branch. Use conditional token rotation to preserve the current refresh-token concurrency contract, persistence mode, absolute expiry and reuse detection. Ordinary per-family logout does not increment the account-wide version.

Account suspension and forced sign-out lock the target, advance its version and revoke all unrevoked refresh families in the same transaction as the audit event. `RevokeAllForUserAsync` must support the caller's unit of work instead of committing a detached transaction. A rotation committed before revocation produces credentials that the later version change invalidates; a rotation after revocation cannot use the old family. Explicit fresh sign-in after a forced sign-out remains allowed for Active accounts.

Correct credentials for a Suspended account return 403 with `AUTH_ACCOUNT_SUSPENDED`; old access or refresh credentials return 401. State-store outages map to 503 through problem details, not to invalid credentials or successful admission. Registration, Google linkage and password recovery cannot reset account status. Existing password recovery behavior remains valid; this change does not add administrator credential editing.

An access-token denylist would require tracking individual JWTs and misses account-wide state. A short JWT lifetime still leaves an exposure window. The persisted version gives a single account-wide revocation point.

### 4. Serialize account control and recheck lockout safeguards

An Infrastructure transaction service implements the Application account-control contract. For each account command, acquire a PostgreSQL transaction-scoped advisory lock dedicated to account control, then lock participating user rows in identifier order. Re-read actor status, authentication version and configured permission, target state, expected AdminControlVersion, and the active configured administrator count before applying a mutation.

The coarse account-control lock serializes low-volume administrative commands across API instances and prevents two administrators from suspending each other into a lockout. Session issuance/rotation takes only the target user-row lock and never takes the advisory lock, avoiding a reversed lock order. Domain validation enforces transitions; server permission and last-admin checks use authoritative configuration and database state. Operator configuration changes remain deployment-controlled and are outside this transaction.

Reject self-suspension and any transition leaving no active configured administrator. Allow deliberate self all-device sign-out: commit it and its audit record, then have the browser clear its session and return to admin login. UI disabling is guidance; API guards remain authoritative.

All commands carry `expectedVersion`. A stale retry returns 409 instead of repeating revocation and potentially ending a newly issued session. After an ambiguous network result, the UI reloads current state rather than automatically resubmitting. A persistent idempotency-key subsystem is unnecessary for this first scope.

### 5. Persist a dedicated append-only audit entity

Create `AdminAuditEvent` as an immutable domain record with its own ID and UTC occurrence time, without the mutable/soft-delete lifecycle of `BaseEntity`. Store actor/target identifiers as historical values without cascading deletion. The Application audit writer accepts typed, allowlisted action data rather than arbitrary serialized objects.

Events contain action, target type/ID, outcome (`Succeeded`, `Accepted`, `Rejected`, `Failed`), bounded reason when applicable, safe error code, server correlation ID, changed-field names and a small approved state payload. Approved state values include account status/control versions and catalog visibility/revision/processing generation. Do not snapshot identity emails, password state values, permission evidence, document text, notes, answers, tokens, headers or provider exceptions. Display current actor identity through an optional bounded lookup, with stored IDs as the fallback.

Administrative reason text is intentional operator input. Provide concise operational-reason guidance, validate its length, and reject recognizable credential/token payloads; rejected input is not copied verbatim. The writer excludes private fields structurally and maps failures to known codes. Never pass full DTOs, requests, exceptions or entities to it.

The application exposes only reads and append operations. Persistence guards reject tracked update/delete operations on audit records. Add indexes for `(OccurredAt, Id)`, `(ActorUserId, OccurredAt, Id)` and `(TargetType, TargetId, OccurredAt, Id)`; add further composite indexes only if actual query plans justify them.

A latest-action column loses history. Generic HTTP-body logging would expose private data and cannot prove that an operation committed. Database triggers alone lack typed application outcomes and bounded reasons.

### 6. Couple account/catalog commits with audit records

Use an Application administrative mutation coordinator with an Infrastructure relational unit of work. Actor and correlation information come from verified server request context. Successful account changes, refresh revocations and audit insertion share one commit. Catalog draft creation, metadata editing, preparation request state, review, publication and hiding likewise stage their event before the corresponding commit. Keep catalog publication, review and concurrency checks intact.

Catalog upload/import/preparation events use `Accepted` for processing commands and `Succeeded` for completed synchronous state changes. Preserve current response and input contracts; do not require a new reason field for existing catalog actions. Add the verified actor to preparation requests that currently lack it. Existing actor fields continue to serve document detail views.

Do not enqueue work before the acceptance transaction commits. Dispatch existing queue work after commit; if dispatch fails, preserve the committed Accepted event and append a safe Failed dispatch event with the same correlation ID and target. Return a recoverable response referencing the created/accepted draft so retry uses its current state. This is explicit command history, not a promise of atomic durable delivery or worker-completion history.

Authorized, identifiable validation/protection/concurrency failures append a Rejected event through a clean unit of work after any mutation transaction is rolled back. Do not reuse tracked changes that might accidentally commit. Infrastructure failures use safe Failed events when persistence is available; a database outage cannot guarantee a durable failure event and returns failure without claiming one. Anonymous requests, policy-denied learner traffic, normal reads and previews do not create administrative mutation history.

Writing events after a successful state commit would allow unaudited changes. Treating acceptance as processing completion would mislead operators. A durable outbox/job console would be useful separately but expands the approved scope.

### 7. Use bounded API contracts and reusable admin pages

Proposed API surface:

| Endpoint | Policy and behavior |
| --- | --- |
| `GET /api/v1/admin/capabilities` | Authenticated; explicit destination flags |
| `GET /api/v1/admin/users` | AccountAdmin; search, status, page, pageSize |
| `GET /api/v1/admin/users/{id}` | AccountAdmin; bounded identity/access-state detail |
| `POST /api/v1/admin/users/{id}/suspend` | AccountAdmin; reason and expectedVersion |
| `POST /api/v1/admin/users/{id}/reactivate` | AccountAdmin; reason and expectedVersion |
| `POST /api/v1/admin/users/{id}/revoke-sessions` | AccountAdmin; reason and expectedVersion |
| `GET /api/v1/admin/audit` | AuditRead; actor/action/target/outcome/from/to filters and paging |
| `GET /api/v1/admin/audit/{id}` | AuditRead; approved event detail only |

Default page size is 20, maximum 100, with validated positive page numbers and search limits. Account ordering is CreatedAt descending then ID descending; audit ordering is OccurredAt descending then ID descending. Time filters use ISO-8601 UTC, inclusive `from` and exclusive `to`. Use no-tracking projections, parameterized filters and server pagination; do not load all identities or events to filter in the browser.

Use 400 for invalid input, 404 for missing/deleted targets, 409 for stale version, invalid transitions and lockout protection, and 503 for unavailable authority/persistence. Publish safe machine-readable error codes and localized messages in EN/VI. No actor, email, password, session token or administrator-assignment fields are accepted in account mutation DTOs. Reject unsupported mutation fields rather than silently accepting credential edits.

Extend the shared access store to hold a verified capability snapshot and check the destination's flag on SSR and client navigation. New stores use the existing privileged-request option and session/admin generations. Clear account selection, lists, audit filters/results and pending requests on logout/account replacement or definitive admin denial. A permission-only 403 preserves the learner identity; terminal authentication failure clears the shared session.

SSR does not rotate refresh cookies. If an older access token is rejected while a refresh cookie may renew it, render the existing neutral admission boundary and let client hydration attempt refresh before a terminal redirect. No protected page, account data or audit event is rendered during that renewal. A denied refresh still follows the existing area-aware session cleanup.

Use MasterDetailLayout for `/admin/users`: directory/filter rail and selected account detail, with an accessible mobile detail flow. Use BoardLayout for `/admin/audit`: filter/action region, paginated event list and a read-only detail panel/dialog. Reuse AppSelect, teleported dialogs, useToast, design tokens and icons. Every account confirmation names the target and consequence; self sign-out explicitly names the current-session effect. Full reasons/details remain readable on mobile. Keep selected IDs in validated route query state where useful; invalid selections offer reload or return to list.

An independent auth store, UI framework or raw select/dialog would duplicate existing boundaries and violate the project conventions.

### 8. Verify contracts and rendered behavior separately

Backend tests must use real JWT bearer validation for the new authentication boundary, not only the existing fake test authentication handlers. Cover Active/Suspended/deleted identities, every sign-in/refresh path, unexpired token revocation, legacy token renewal, ordinary logout isolation, configuration removal, self/last-admin protection, stale retries, transactional rollback and allowlisted audit output. Use isolated PostgreSQL fixtures for row/advisory-lock and concurrent issuance/revocation tests; configure credentials through environment settings and report skipped/unavailable checks honestly.

Frontend Vitest tests cover exact command/query payloads, reason validation, conflict/error states, single-flight submissions, capability admission and generation-based result discard. Do not assert Tailwind classes as evidence of layout.

Before any frontend UI task is checked complete, pass the entire `npm test` suite and drive authenticated headless Chromium. Inspect and present screenshots at 1440x900 and 390x844 in EN/VI and light/dark for account list/detail/confirmation and audit list/filter/detail states. Include long identifiers/reasons, empty/error states and keyboard/mobile navigation. Recheck existing admin login/dashboard/catalog and learner switching/logout where the shared shell or auth boundary changed. Run the backend suite and production frontend build before implementation sign-off.

## Risks / Trade-offs

- [Per-request state queries increase database dependence] → Use indexed narrow projections, measure authenticated request latency, and return 503 without credential destruction when authority is unavailable. Introduce caching only with equivalent revocation guarantees in a later design.
- [Concurrent issuance or stale grace-window results revive sessions] → Share user-row locking and stored authentication versions across every issuer/refresh path; test the commit ordering on PostgreSQL.
- [Shared account-control locking limits throughput] → Administrative commands are infrequent; keep transactions short and exclude external I/O. Queries and normal session refreshes do not take this global lock.
- [Free-text reasons contain inappropriate information] → Bound and validate intentional input, guide operators, reject recognizable credential payloads, and never auto-copy private content or arbitrary exception/request text.
- [Audit integration accidentally changes catalog behavior] → Preserve existing DTO/lifecycle/concurrency contracts and run catalog isolation and rollback regressions; keep asynchronous acceptance distinct from completion.
- [Queue acceptance survives a process failure without dispatch] → Keep acceptance history truthful and use existing catalog retry/recovery; durable job delivery is outside this change.
- [Old tokens or mixed backend versions bypass new controls] → Reject missing versions, permit eligible refresh migration, and replace backend instances together before exposing account controls.
- [History grows and current identities change] → Use bounded indexed queries and retain immutable IDs. Retention/export is deferred without adding deletion controls now.

## Migration Plan

1. Add relational migrations for account status/versions, refresh-token versions, immutable audit storage and indexes. Backfill existing accounts/refresh records to version 1 without touching credentials, ownership or soft deletion. Validate migrations against an isolated PostgreSQL database.
2. Integrate the session authority and administrative transactions, then deploy backend instances together. No instance that ignores suspension/version checks may continue serving authenticated traffic once account controls are enabled.
3. Retain operator configuration and the catalog capability endpoint; deploy the new capability consumer and pages. Eligible existing refresh families obtain versioned JWTs transparently; browsers without eligible refresh credentials sign in again. Preserve remember-me behavior and cookie/storage migration.
4. History begins with the new implementation; do not invent events by treating old latest-actor fields as complete history. Document this boundary, permission designation, new API contracts, error codes and recovery behavior.
5. Roll back presentation independently if needed. Once suspension/revocation has been used, any backend rollback must retain enforcement of persisted account/version state and preserve audit records; prefer a forward repair over restoring an older binary that ignores these controls. A destructive down migration is not the normal recovery path.
