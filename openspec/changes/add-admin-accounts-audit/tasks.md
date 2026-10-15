# Tasks

## 1. Domain

- [x] 1.1 Add Active/Suspended account state, suspension metadata, AuthenticationVersion and AdminControlVersion with domain methods; verify unit tests cover valid transitions, preserved learning data, monotonic versions and invalid transitions.
- [x] 1.2 Add the issued authentication version to refresh-token records without changing family/persistence semantics; verify domain tests and documented field invariants distinguish account-wide revocation from normal device logout.
- [x] 1.3 Define immutable AdminAuditEvent data and typed actions/outcomes with bounded reason and state payloads; verify construction tests reject invalid attribution/reasons and expose no credential or content fields.

## 2. Application

- [x] 2.1 Add permission, account/session authority, session-issuance and administrative unit-of-work interfaces with direct DI contracts; verify dependency checks keep Domain free of EF/I/O and handler tests can exercise the contracts without reflection dispatch.
- [x] 2.2 Implement paginated account list/detail and audit list/detail handlers with minimal DTO projections and validated filters; verify tests cover stable ordering, UTC range boundaries, soft-delete/pending-registration exclusion, empty results and private-field exclusion.
- [x] 2.3 Implement suspension, reactivation and all-device sign-out handlers with reason/expectedVersion validation and safe result codes; verify tests cover blank/overlong reasons, invalid transitions, stale retries, self/last-admin rejection and no mutation on rejection.
- [x] 2.4 Add the typed audit writer/coordinator to account actions and each existing catalog mutation, supplying a verified actor for preparation; verify handler tests cover upload/import/edit/prepare/review/publish/hide, recoverable rejections and no mutation events for reads/previews.
- [x] 2.5 Document account/audit DTOs, approved audit fields, outcome semantics and error codes in `docs/admin-workspace.md`; verify the contract notes match the implemented handlers and explain Accepted separately from processing completion.

## 3. Infrastructure

- [x] 3.1 Map account/control versions, refresh-token versions and append-only audit storage with indexes and additive migrations; verify an isolated PostgreSQL migration backfills version 1/Active correctly while preserving credentials, ownership, soft-delete filtering and partial email uniqueness.
- [x] 3.2 Implement shared operator-cohort capability resolution, including Active/non-deleted checks and catalog compatibility; verify service tests cover empty/malformed/duplicate configuration, learner denial, configuration removal and suspended administrators.
- [x] 3.3 Implement coordinated session issuance, version-aware refresh rotation/grace handling and caller-owned all-family revocation; verify service tests preserve cookie persistence, absolute expiry, reuse detection and per-family logout while rejecting stale account/session state.
- [x] 3.4 Implement account-control advisory/row locking and mutation/audit transactions with actor rechecks; verify PostgreSQL tests cover concurrent administrator suspensions, stale actor sessions, competing account actions and rollback of status/version/token changes when audit insertion fails.
- [x] 3.5 Add isolated PostgreSQL concurrency tests for login/refresh versus suspension or forced sign-out, including grace-window successors and fresh login after commit; verify no pre-revocation credentials remain usable, obtain actual passing runs, and report unavailable fixtures instead of counting silent skips as coverage.
- [x] 3.6 Implement audit persistence guards, bounded indexed readers and safe rejected/failed-event recording; verify relational tests prevent tracked event update/delete, retain historical IDs after identity deletion/rename, and exclude bodies, tokens, hashes, document text and raw exceptions from stored/query output.
- [x] 3.7 Integrate catalog mutation/audit commits with dispatch after acceptance commit and correlated dispatch-failure recording; verify rollback and queue-failure tests preserve truthful Accepted/Failed history, then update `docs/operations/curated-catalog.md` with recovery behavior and migration/rollback limits.

## 4. Api

- [x] 4.1 Register new DI services and AccountAdmin/AuditRead policies, add authenticated `/api/v1/admin/capabilities`, and preserve the legacy catalog capability response; verify API tests cover explicit flags, 401/403, spoofed client roles, configuration removal and no-store headers.
- [x] 4.2 Add account directory/detail and suspend/reactivate/revoke-sessions endpoints with reason/expectedVersion DTOs and strict unsupported-field rejection; verify endpoint tests cover safe projections, email/password/linkage-edit rejection, status actions, 400/404/409 errors and server-derived audit attribution.
- [x] 4.3 Add read-only audit list/detail endpoints with bounded query validation and UTC filters; verify endpoint tests cover actor/action/target/outcome filtering, deterministic paging, invalid ranges, denied reads and absence of supported edit/delete operations.
- [x] 4.4 Integrate current-account/version JWT admission and the coordinated issuer into password, verified registration, Google and refresh flows; verify integration tests using real JWT bearer middleware cover unexpired-token revocation, suspended/deleted users, legacy-token renewal, terminal 401, unavailable 503 and recovery without reactivation.
- [x] 4.5 Publish Scalar/OpenAPI contracts and update `docs/admin-workspace.md` with capability policies, session migration, history cutover and operator recovery; verify the schema documents actual responses/error codes and contains no credentials or invented historical events.

## 5. Frontend

UI tasks in this group remain unchecked until both task 5.7's full behavioral suite and task 5.8's inspected/presented browser screenshots pass, even if their code has been written.

- [x] 5.1 Read the canonical Vue, Vue Best Practices, Nuxt, Pinia, VueUse, Vitest, Vue Testing Best Practices and Antfu Design skills through explicit tool calls before editing frontend files; verify the selected patterns use the existing Nuxt app architecture, TypeScript setup components and project controls.
- [x] 5.2 Extend verified admin capabilities, SSR/client destination admission and shell navigation for Accounts/Administration log; verify route/store tests cover guest/learner deep links, legacy-token refresh during neutral SSR hydration, unavailable checks, stale capabilities, safe redirects and account replacement, then verify rendered navigation under task 5.8.
- [x] 5.3 Add typed account/audit API stores with paging, filters, selected IDs, command state and auth-generation cleanup; verify behavioral tests assert exact query/body contracts, single-flight submission, ambiguous-result reload, stale-result discard and terminal versus transient failure handling.
- [x] 5.4 Build `/admin/users` with MasterDetailLayout, bounded identity details and accessible reason/confirmation forms for suspension/reactivation/all-device sign-out; verify component tests cover validation, cancel/no-request, conflict/input preservation, successful refresh and deliberate self sign-out, then verify desktop/mobile rendering under task 5.8.
- [x] 5.5 Build `/admin/audit` with BoardLayout, filter/reset/pagination controls and read-only event details; verify component tests cover UTC query boundaries, loading/empty/error states, filter preservation and distinct outcomes, then verify desktop/mobile rendering under task 5.8.
- [x] 5.6 Add complete EN/VI navigation, action, reason, outcome and error translations and update the workspace usage guide; verify both locale key sets resolve every new message and document that email/credentials and private content are outside account administration.
- [x] 5.7 Run the complete `npm test` suite from `frontend` after adding behavioral coverage; verify all suites pass with no CSS/Tailwind-class assertions used as layout evidence, and record the actual result.
- [x] 5.8 Drive authenticated headless Chromium for accounts list/detail/confirmations and audit list/filters/details at 1440x900 and 390x844 in EN/VI and light/dark; verify long text, empty/error states, keyboard/mobile focus, confirmation cancellation and responsive typography through direct screenshot inspection, present desktop/mobile images to the user, and only then complete UI tasks.

## 6. Integration verification

- [x] 6.1 Run `dotnet test backend/tests/DeepPace.Tests/DeepPace.Tests.csproj` with the required isolated PostgreSQL fixtures and `npm run build` from `frontend`; verify both succeed and catalog lifecycle, learner ownership, refresh persistence and existing admin functionality retain their contracts.
- [x] 6.2 Exercise authentic multi-device suspension/reactivation/forced-sign-out plus account/audit history inspection through the running app; verify real JWT/refresh rejection, fresh-login recovery, self/last-admin guards, admin/learner switching, logout, SSR admission and late-response cleanup, and record observed results without exposing credentials.
- [x] 6.3 Run `openspec validate add-admin-accounts-audit --strict` and reconcile every delta scenario with implementation/test/browser evidence; verify all required tasks and both UI gates are complete before reporting implementation completion.
