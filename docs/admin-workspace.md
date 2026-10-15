# Administrator workspace

The administrator workspace shares DeepPace accounts, authentication and the browser refresh-token session with learning. Administrative authority is strictly database-backed via explicit user-role membership (`Users.RoleId` referencing `Roles.Id`) and protected permission grants (`RolePermissions`).

Deployment user allowlists (such as legacy `CatalogAdministration__UserIds__*`) are completely obsolete and ignored; configuration never participates in runtime authorization.

## Roles & Permissions Model

DeepPace defines supported permission keys in code, not user-defined permissions:
- `ViewAdminDashboard`: Access to administrative overview, service status metrics, and AI health probes.
- `ManageCatalog`: Curate, import, prepare, review, publish, and manage catalog documents.
- `ManageAccounts`: Inspect accounts, suspend, reactivate, and revoke user sessions.
- `ReadAudit`: Read immutable administrative audit event history.
- `ManageDocumentOperations`: Inspect ingestion queues, retry processing, and replace document sources.
- `ManageRoles`: Assign roles to user accounts (enforced in conjunction with `ManageAccounts`).

Every account has exactly one persistent role enforced by foreign key (`Users.RoleId -> Roles.Id`). DeepPace seeds two system-protected roles:
1. `Learner` (`20000000-0000-4000-8000-000000000001`, key `learner`): Default role for all registered and OAuth-provisioned users, with zero administrative grants.
2. `Admin` (`20000000-0000-4000-8000-000000000002`, key `admin`): Protected administrator role with all six administrative permissions.

### Absence of Role / Grant Editor
DeepPace intentionally does NOT provide a dynamic role-creation or arbitrary permission-grant editor. System roles and their supported permissions are immutable database-backed definitions defined by the schema and migrations. Administrators cannot invent new permissions, modify existing role grant sets, or create bespoke custom roles; account administration only permits assigning accounts to bounded, existing database roles.

The user profile field `TargetRole` is purely a career/learning goal (e.g. "Senior System Architect") and has zero authorization effect.
## Operator Initialization & Cutover Guidance

### 1. One-Time Operator Initialization
Before starting web/worker workloads on a fresh deployment or after migration, an operator must explicitly initialize database authority from validated existing account IDs:
```bash
dotnet run --project backend/src/DeepPace.Api/DeepPace.Api.csproj -- authorization initialize --admin-user-id <uuid> [--admin-user-id <uuid2>...]
```

Initialization behavior:
- Enforces PostgreSQL transactional advisory locking (`pg_advisory_xact_lock(4354, 524)`).
- Verifies that all provided UUIDs exist in `Users` and are not soft-deleted.
- Verifies that at least one administrator is `Active` (rejects all-suspended sets).
- Assigns `RoleDefaults.AdminRoleId`, advances `AuthenticationVersion` and `AdminControlVersion`, and revokes all existing refresh tokens for promoted accounts.
- Persists an immutable receipt in `AuthorizationInitializations` (`Scope = "database-roles-v1"`).
- Generates zero user-attributed audit entries (initialization is operational evidence, not user action).
- Refuses subsequent or repeated initialization runs (`AUTHORIZATION_ALREADY_INITIALIZED`).

### 2. Readiness Gating
- If role schema or protected seed roles/permissions are missing, `/health` and API readiness return `503 Service Unavailable` with Problem Details, preventing work admission.
- If schema is ready but operator initialization has not yet occurred, real Learner registration and sign-in remain fully available, while capability discovery (`/api/v1/catalog/capabilities`) and privileged administration APIs (`/api/v1/admin/*`) return `503 Service Unavailable`.

### 3. Startup Identity Hardening
- Production startup no longer creates default accounts or fallbacks (e.g. legacy `e2e_prod_agent@deeppace.local`).
- Any needed QA identity provisioning is confined to explicit isolated test fixtures.
- Normal application startup/restart never creates accounts, resets credentials, or alters memberships.

### 4. Backup & Rollback Boundaries
- Operators must take a full PostgreSQL database backup before applying migrations or running initialization.
- If rollback is necessary, operators must perform a full pre-cutover database restore accompanied by previous matched application binaries. Do not drop roles or attempt partial data patching.
## Inventory metrics

`GET /api/v1/admin/dashboard/overview` returns only aggregates from a PostgreSQL repeatable-read transaction:

- `accountCount`: non-deleted users.
- `personalDocuments`: non-deleted owned `DocumentBook` rows, including independent adopted copies. Legacy unowned books are excluded. Counts reflect book state, without fetching titles, source text or private learning content.
- `catalogDocuments`: non-deleted catalog titles, publication states and processing states. Adopting a title does not increase this catalog total.
- `observedAt`: UTC observation time, displayed in the selected interface locale.

Pending/Processing/Ready/Failed counts describe persisted document states. They are not worker queue depth. Unavailable reads return HTTP 503 rather than invented zero counts; the UI keeps the last successful observation visibly stale where available.

## Service observations

`GET /api/v1/admin/dashboard/status` performs a database connectivity check bounded to five seconds. A successful HTTP response measures API responsiveness for that request only; it does not assert universal system health. The initial dashboard load and manual refresh make no external AI request.

`POST /api/v1/admin/dashboard/ai-probe` explicitly checks text generation and embeddings. The shared application probe has a 15-second overall deadline and propagates request cancellation. Requests are limited to one per administrator per minute. Provider failures return HTTP 503 with sanitized per-service observations; duplicate/rate-limited requests return HTTP 429. Missing `Gemini__Model` or `Gemini__ApiKey` fails immediately without contacting providers. A measured result older than five minutes is stale. Probe results are account-scoped, remain in memory and reset on reload.

All dashboard routes independently require administrator authorization and send `Cache-Control: no-store`. Anonymous callers receive 401; authenticated non-administrators receive 403 before inventory/probe services run. The legacy `/api/v1/system/ai-health` response shape is preserved and uses the same probe implementation.

## Navigation and session lifetime

Dashboard uses `BentoDashboardLayout`, login `StudioLayout`, and catalog `MasterDetailLayout`, with shared DeepPace styling and English/Vietnamese, dark/light parity. Switch to learning opens `/today` without replacing the account or issuing tokens. Logout ends the shared browser refresh-token family and returns to `/admin/login`; other devices retain their independent sessions. Permission denial clears administrative state while preserving a valid learner session. Account replacement, logout and revoked permission abort privileged requests and discard late results.

The workspace includes inventory overview, catalog management, account access control (`/admin/users`), and an immutable administration audit log (`/admin/audit`). Account credentials (passwords, OAuth subjects) and private personal learning records remain strictly outside administrative exposure.

## Account Administration & Access Control

Authorized administrators with `AccountAdmin` permission manage user identity access states at `/admin/users`:

### Account DTOs
- `AdminAccountSummaryDto`:
  - `id`: unique user identifier (`Guid`).
  - `email`: user email address.
  - `name`: user display name.
  - `avatarUrl`: optional avatar URL.
  - `status`: `Active` or `Suspended`.
  - `createdAt`: account creation timestamp (ISO UTC).
- `AdminAccountDetailDto`:
  - Same identity fields plus `preferredLocale`, `adminControlVersion`, `hasPassword` (boolean), `hasGoogleAuth` (boolean), and latest suspension metadata (`suspendedByUserId`, `suspendedAt`, `suspensionReason`).
  - Explicitly excludes password hashes, OAuth subject tokens, and personal reading notes/quizzes.
- `AdminAccountActionRequest`:
  - `reason`: operator-entered reason (required, trimmed, 1-500 characters, no credential/token payloads).
  - `expectedVersion`: optimistic concurrency version (`AdminControlVersion`).

### Account Mutations
- `POST /api/v1/admin/users/{id}/suspend`: Transitions account to `Suspended`, revokes all active refresh tokens, advances `AuthenticationVersion` and `AdminControlVersion`, and commits an append-only `AdminAuditEvent`.
- `POST /api/v1/admin/users/{id}/reactivate`: Transitions account to `Active`, advances `AdminControlVersion`, and commits an audit event. Does NOT revive previously revoked sessions; fresh sign-in is required.
- `POST /api/v1/admin/users/{id}/revoke-sessions`: Revokes all active refresh tokens and advances `AuthenticationVersion` and `AdminControlVersion`. Deliberate self sign-out is permitted.
- `PUT /api/v1/admin/users/{id}/role`: Assigns a database-backed role to an account (`AdminAssignRoleActionRequest`). Enforces the conjunctive `RoleAdmin` policy requiring BOTH `ManageAccounts` AND `ManageRoles` grants simultaneously. Advances `AdminControlVersion` and `AuthenticationVersion`, atomically revoking all active refresh token families for the target user across all devices to force immediate sign-out. Commits an atomic `User.AssignRole` audit record.

### Role Listing & Assignment Boundary
- `GET /api/v1/admin/roles`: Paginated directory of system roles and supported permissions for role assignment (`AdminGetRolesRequest` / `AdminRoleOptionDto`). Enforces `RoleAdmin` policy (requires both `ManageAccounts` and `ManageRoles`) and rate-limited to 120 reads/minute per user (`AdminRolesReadPolicy`). An administrator with `ManageRoles` alone cannot access `/admin/users` or retrieve role options.
- All-device session revocation: changing a user's role advances `AuthenticationVersion` and revokes all refresh token families. Any existing bearer token for that user fails validation against the fresh version check, requiring the user to sign in again to obtain credentials with the updated role.
### Administrative Safeguards & Error Codes
- `ACCOUNT_SELF_SUSPENSION_BLOCKED` (409): An administrator cannot suspend their own account.
- `ACCOUNT_SELF_ROLE_CHANGE_BLOCKED` (409): An administrator cannot change their own role.
- `ACCOUNT_LAST_ADMIN_PROTECTED` (409): Demotion or suspension cannot leave zero active configured administrators.
- `ACCOUNT_ROLE_UNCHANGED` (409): Target user already has the specified role.
- `ACCOUNT_CONCURRENCY_CONFLICT` (409): Account was modified by another operator; reload required.
- `Account.InvalidTransition` (409): Transition invalid (e.g. suspending an already suspended account).
- `Account.ReasonRequired` (400) / `Account.ReasonTooLong` (400): Reason validation failure.
- `Role.InvalidId` (400): Role ID cannot be empty.
- `Account.NotFound` (404): Account does not exist or is soft-deleted.
- `Role.NotFound` (404): The specified role does not exist or is deleted.
- `AUTH_ACCOUNT_SUSPENDED` (403): Suspended account attempting sign-in.
- `AUTHORIZATION_UNAVAILABLE` (503): Privileged authority or database is temporarily unavailable.
- `Too Many Requests` (429): Exceeded named per-user limit with `Retry-After: 60`.
## Administration Audit Log

Authorized administrators with `AuditRead` permission inspect administrative action history at `/admin/audit`:

### Approved Audit Fields
Events contain:
- `id`: immutable event identifier (`Guid`).
- `occurredAt`: server UTC timestamp.
- `actorUserId`: authenticated administrator identifier.
- `actorEmail`: looked up dynamically from current identity; remains inspectable by historical ID if user is later renamed or soft-deleted.
- `action`: typed action identifier (e.g. `User.Suspend`, `User.Reactivate`, `User.RevokeSessions`, `Catalog.UploadPdf`, `Catalog.ImportUrl`, `Catalog.UpdateMetadata`, `Catalog.Prepare`, `Catalog.Review`, `Catalog.Publish`, `Catalog.Hide`, `Catalog.DispatchFailed`).
- `targetType`: entity type (`User`, `CatalogDocument`).
- `targetId`: entity identifier.
- `outcome`: `Succeeded`, `Accepted`, `Rejected`, `Failed`.
- `reason`: bounded operator reason (trimmed, max 500 chars).
- `errorCode`: safe machine-readable error code on rejected/failed events.
- `correlationId`: HTTP trace or job correlation identifier.
- `changedFields`: list of modified property names.
- `statePayload`: approved state dictionary (e.g. `Status`, `AuthenticationVersion`, `AdminControlVersion`, `Visibility`, `ProcessingGeneration`).

### Outcome Semantics
- `Succeeded`: Synchronous state transition completed and committed.
- `Accepted`: Asynchronous processing command (such as catalog extraction/preparation) was accepted and enqueued. This does NOT assert that extraction or AI processing has finished.
- `Rejected`: Authorized command was rejected by validation, protection, or concurrency rules. No entity mutation was committed.
- `Failed`: Infrastructure dispatch or execution failure (e.g., worker queue failure after acceptance commit).

## Capability Policies & Route Admission
- `GET /api/v1/admin/capabilities`: Authenticated discovery endpoint returning:
  `{ canManageCatalog, canManageAccounts, canReadAudit, canManageDocumentOperations, canViewAdminDashboard, canManageRoles }`
  with `Cache-Control: no-store`.
- Policies:
  - `ViewAdminDashboard`: Requires `canViewAdminDashboard`.
  - `CatalogAdmin`: Requires `canManageCatalog`.
  - `AccountAdmin`: Requires `canManageAccounts`.
  - `AuditRead`: Requires `canReadAudit`.
  - `DocumentOperationsAdmin`: Requires `canManageDocumentOperations`.
  - `RoleAdmin`: Conjunction requiring BOTH `canManageAccounts` AND `canManageRoles`.
- Rate Limiting:
  - `AdminRolesReadPolicy`: 120 requests/minute per administrator.
  - `AdminRolesWritePolicy`: 30 requests/minute per administrator.
  - `AiEndpointsPolicy`: 10 requests/minute per administrator.
- JWT Admission:
  - Tokens must contain a positive `session_version` claim.
  - Server verifies that `session_version == user.AuthenticationVersion` and `user.Status == Active` against the database on each authenticated request.
  - Stale tokens or tokens for suspended/deleted users return 401.
  - A database outage or missing receipt returns 503 with problem details, preserving client credentials for retry.
## Verification

The implementation was checked with the full backend suite (493 passing tests), the full frontend suite (774 passing tests across 88 files), and a production Nuxt build. Regression coverage includes independent aggregate scopes, authorization, bounded and sanitized probes, area-aware session expiry, account replacement, permission revocation, single-flight refresh, hydration boundaries and shell routing.

Commands from the repository root:

```sh
dotnet test backend/tests/DeepPace.Tests/DeepPace.Tests.csproj --no-restore --verbosity minimal
cd frontend
npm test
NUXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5001 API_INTERNAL_URL=http://127.0.0.1:5001 npm run build
```

`frontend/e2e/test-admin-workspace.mjs` uses real API login responses and refresh cookies, never fabricated browser identities. Run it against an isolated, migrated QA database containing active administrator and ordinary accounts plus a published catalog title named `Distributed Systems Field Notes`. Configure `CatalogAdministration__UserIds__0` for that administrator. Supply `ADMIN_QA_EMAIL`, `ADMIN_QA_PASSWORD`, `LEARNER_QA_EMAIL` and `LEARNER_QA_PASSWORD` through the environment; the administrator pair can also come from the gitignored `.env` E2E entries. An optional `ADMIN_QA_ACCOUNTS_FILE` is a private QA fixture file with `ordinaryEmail` and `ordinaryPassword` fields. Never commit credentials or browser storage states.

```sh
TARGET_URL=http://127.0.0.1:3001 API_URL=http://127.0.0.1:5001 node e2e/test-admin-workspace.mjs
```

The browser suite passed 149 assertions and captured 39 screenshots. It covers real sign-in, regular-account denial, account replacement, remember-me cookies, SSR admission, refresh-only hydration, unknown administrator paths, direct API 401/403 rejection, catalog navigation, learning switch and shared logout. It simulates network/permission failures with Playwright response interception while retaining authentic sessions. The explicit health-probe button contacts the configured AI services once; loading the dashboard does not. A controlled browser clock verifies stale observations. Screenshots cover 1440×900 and 390×844 in English/Vietnamese and light/dark, including drawer keyboard focus, retries, denial and service states. Images and the assertion report are written to gitignored `frontend/e2e/screenshots/admin-workspace/`, inspected directly and presented with the implementation report.

Before archive, `openspec validate add-admin-workspace-dashboard --strict` passed. The four capability deltas were merged into the main specifications and all 27 main specs passed `openspec validate --specs`. The complete planning artifacts and 19 finished tasks are archived at `openspec/changes/archive/2026-10-15-add-admin-workspace-dashboard/`. The catalog-auth prerequisite is represented in the main specification, with its previous inline denial replaced by administrator-login redirection.

## Release Notes: Database-Backed Roles & Authorization Boundary

### Summary
DeepPace migrates from configuration-based administrator allowlists to a secure, database-backed role-based access control (RBAC) architecture.

### Key Architectural Changes
1. **One Role Per User**:
   - Database constraint: each user has exactly one assigned role stored in `Users.RoleId` referencing `Roles.Id`.
   - Default assignment: all self-registered and Google OAuth provisioned accounts receive the default system `Learner` role with zero administrative grants.
2. **Role Assignment Permission Requirements**:
   - Role assignment (`PUT /api/v1/admin/users/{userId}/role`) and role listing (`GET /api/v1/admin/roles`) require the conjunctive `RoleAdmin` policy (`ManageAccounts` AND `ManageRoles`).
   - `ManageRoles` alone does not grant access to account administration, role options, or any admin destination.
3. **Target All-Device Session Revocation**:
   - Changing a user's role automatically advances both `AdminControlVersion` and `AuthenticationVersion` while revoking all refresh tokens for that user across all devices.
   - Immediate session invalidation ensures users cannot act under obsolete permissions.
4. **Protected Roles & Invariants**:
   - Protected seed roles (`Learner` and `Admin`) cannot be deleted or renamed.
   - Safety invariant: demoting or suspending an administrator cannot leave zero active administrators (`ACCOUNT_LAST_ADMIN_PROTECTED`). Self-demotion is strictly blocked (`ACCOUNT_SELF_ROLE_CHANGE_BLOCKED`).
5. **No Role or Grant Editor**:
   - Supported permissions and role definitions are immutable code/database constants. No user-defined role creation or ad-hoc grant editor exists.
