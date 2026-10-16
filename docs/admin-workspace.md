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
- `ManageCategories`: Manage document taxonomy categories (create, update, delete, view).

Every account has exactly one persistent role enforced by foreign key (`Users.RoleId -> Roles.Id`). DeepPace seeds two system-protected roles:
1. `Learner` (`20000000-0000-4000-8000-000000000001`, key `learner`): Default role for all registered and OAuth-provisioned users, with zero administrative grants.
2. `Admin` (`20000000-0000-4000-8000-000000000002`, key `admin`): Protected administrator role with all seven administrative permissions (`ViewAdminDashboard`, `ManageCatalog`, `ManageAccounts`, `ReadAudit`, `ManageDocumentOperations`, `ManageRoles`, `ManageCategories`).
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

### 2. Readiness Gating & 401/403/503 Precedence
- **Required Protected-Grant Baseline**: Authorization readiness requires an exact-case match against the baseline set of all seven protected grants (`RoleDefaults.ProtectedAdminGrants`), not a coarse grant count. If any grant is missing or corrupted, if `Learner` holds any permissions, or if receipt lookup encounters a database error, required readiness fails.
- **Host Startup Failure (F03, F08, F09)**: When required readiness fails on startup, the application logs a critical error and halts with a non-zero exit code (`1`). It does not run HTTP listener or worker loops, preventing a "zombie" host where endpoints are reachable but ingestion consumers are dead.
- **Uninitialized Schema (Awaiting One-Time Initialization)**: When required schema and roles are intact but `AuthorizationInitializations` receipt is absent, the host starts normally:
  - **Real Learners**: Registration, sign-in, profile, and learning workflows operate without interruption.
  - **Administrative Endpoints (`/api/v1/admin/*`, `/api/v1/catalog/capabilities`)**: Evaluated with strict 401/403/503 precedence:
    1. **Anonymous / Stale Requests**: Fail authentication first, returning `401 Unauthorized` with `Cache-Control: no-store`.
    2. **Authenticated Learners (Uninitialized)**: Return `503 Service Unavailable` with `Cache-Control: no-store` and ProblemDetails (`Privileged authorization has not been initialized.`). This check runs between authentication and grant authorization, preventing grant policies from masking pending initialization with a misleading `403 Forbidden`.
    3. **Authenticated Learners (Initialized)**: Once initialized, grant policy evaluation runs; lacking administrative permissions yields `403 Forbidden` with `Cache-Control: no-store`.
- **Public Request & JWT Contracts**: Public request structures and JWT contracts remain unchanged; bearer tokens require valid user identity and matching `session_version`.
- **Safe Public Readiness Failures**: Administrative `503` responses and public `/health` failures expose only stable availability feedback and coarse readiness state. Provider/schema diagnostics remain in structured server logs, never response bodies.
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
  - `role`: authoritative `RoleDescriptorDto` (`id`, `key`, `nameEn`, `nameVi`) loaded directly from the database; never falls back to a synthetic or fabricated Learner role. Missing or unjoined roles return an availability error rather than masked defaults.
- `AdminAccountDetailDto`:
  - Same identity fields plus `role` (`RoleDescriptorDto`), `preferredLocale`, `adminControlVersion`, `hasPassword` (boolean), `hasGoogleAuth` (boolean), and latest suspension metadata (`suspendedByUserId`, `suspendedAt`, `suspensionReason`).
  - Explicitly excludes password hashes, OAuth subject tokens, and personal reading notes/quizzes.
- `AdminAccountActionRequest`:
  - `reason`: operator-entered reason (required, trimmed, 1-500 characters, no credential/token payloads).
  - `expectedVersion`: optimistic concurrency version (`AdminControlVersion`).

### Account Mutations
- `POST /api/v1/admin/users/{id}/suspend`: Transitions account to `Suspended`, revokes all active refresh tokens, advances `AuthenticationVersion` and `AdminControlVersion`, and commits an append-only `AdminAuditEvent`. Preserves authoritative target role descriptor in response.
- `POST /api/v1/admin/users/{id}/reactivate`: Transitions account to `Active`, advances `AdminControlVersion`, and commits an audit event. Does NOT revive previously revoked sessions; fresh sign-in is required. Preserves authoritative target role descriptor in response.
- `POST /api/v1/admin/users/{id}/revoke-sessions`: Revokes all active refresh tokens and advances `AuthenticationVersion` and `AdminControlVersion`. Deliberate self sign-out is permitted. Preserves authoritative target role descriptor in response.
- `PUT /api/v1/admin/users/{id}/role`: Assigns a database-backed role to an account (`AdminAssignRoleActionRequest`). Enforces the conjunctive `RoleAdmin` policy requiring BOTH `ManageAccounts` AND `ManageRoles` grants simultaneously. Advances `AdminControlVersion` and `AuthenticationVersion`, atomically revoking all active refresh token families for the target user across all devices to force immediate sign-out. Commits an atomic `User.AssignRole` audit record.
  - **Suspended Admin Role Changes**: The last-active administrator protection (`ACCOUNT_LAST_ADMIN_PROTECTED`) strictly applies only to the removal or demotion of an `Active` administrator. Reassigning the role of an already-`Suspended` administrator succeeds without violating minimum-active-admin constraints, preserving all suspension metadata (`suspendedAt`, `suspendedByUserId`, `suspensionReason`), advancing account/session versions, and revoking old refresh token families.
  - **Evaluation Precedence**: Unchanged role assignment (`ACCOUNT_ROLE_UNCHANGED`) is evaluated before evaluating minimum active administrator counts.

### Role Listing & Assignment Boundary
- `GET /api/v1/admin/roles`: Paginated directory of system roles and supported permissions for role assignment (`AdminGetRolesRequest` / `AdminRoleOptionDto`). Enforces `RoleAdmin` policy (requires both `ManageAccounts` and `ManageRoles`) and rate-limited to 120 reads/minute per user (`AdminRolesReadPolicy`). An administrator with `ManageRoles` alone cannot access `/admin/users` or retrieve role options.
- All-device session revocation: changing a user's role advances `AuthenticationVersion` and revokes all refresh token families. Any existing bearer token for that user fails validation against the fresh version check, requiring the user to sign in again to obtain credentials with the updated role.
- **Producer-Shaped Permission Explanations (F13, F14)**: `AdminRoleModal` consumes canonical `permissionKeys` from role option descriptors directly, displaying localized permission explanation badges (including Category permissions) and descriptions without consumer-side wire aliases or empty block collisions. Role discovery in `useAdminUsersStore` maintains dedicated loading and generation tracking independent of account directory pagination.
- **Retained Conflict Drafts & Matching Reload (F12, F14)**: When a 409 Concurrency Conflict occurs (`ACCOUNT_CONCURRENCY_CONFLICT`), the modal surfaces an explicit warning banner and a "Reload Account" (`admin.users.roleConflictReload`) button. Clicking reload reloads target user state while preserving draft justification reason and selected role inputs, clearing the conflict gate only upon matching non-null reload.
### Administrative Safeguards & Error Codes
- `ACCOUNT_SELF_SUSPENSION_BLOCKED` (409): An administrator cannot suspend their own account.
- `ACCOUNT_SELF_ROLE_CHANGE_BLOCKED` (409): An administrator cannot change their own role.
- `ACCOUNT_LAST_ADMIN_PROTECTED` (409): Demotion or suspension cannot leave zero active configured administrators. Does not block modifying accounts that are already suspended.
- `ACCOUNT_ROLE_UNCHANGED` (409): Target user already has the specified role.
- `ACCOUNT_CONCURRENCY_CONFLICT` (409): Account was modified by another operator; reload required.
- `Account.InvalidTransition` (409): Transition invalid (e.g. suspending an already suspended account).
- `Account.ReasonRequired` (400) / `Account.ReasonTooLong` (400) / `Account.InvalidReason` (400): Reason validation failure (blank, overlong >500 chars, or containing sensitive credential/token patterns).
- `Validation.ExpectedVersionInvalid` (400): Expected version must be a positive integer.
- `Role.InvalidId` (400): Role ID cannot be empty.
- `Account.NotFound` (404): Account does not exist or is soft-deleted.
- `Role.NotFound` (404): The specified role does not exist or is deleted.
- `Account.RoleUnavailable` (500/503): The target account's assigned role is unavailable or corrupted in the database.
- `AUTH_ACCOUNT_SUSPENDED` (403): Suspended account attempting sign-in.
- `AUTH_FORBIDDEN` (403): Unadmitted actor (inactive account, stale session token version, or missing required permission grants).
- `AUDIT_PERSISTENCE_UNAVAILABLE` (503): Administrative audit history could not be committed.
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
- `action`: typed action identifier (e.g. `User.Suspend`, `User.Reactivate`, `User.RevokeSessions`, `User.AssignRole`, `Catalog.UploadPdf`, `Catalog.ImportUrl`, `Catalog.UpdateMetadata`, `Catalog.Prepare`, `Catalog.Review`, `Catalog.Publish`, `Catalog.Hide`, `Catalog.DispatchFailed`).
- `targetType`: entity type (`User`, `CatalogDocument`, `Category`).
- `targetId`: entity identifier.
- `outcome`: `Succeeded`, `Accepted`, `Rejected`, `Failed`.
- `reason`: bounded operator reason (trimmed, max 500 chars). Omitted (null) on rejections involving sensitive or excessive inputs.
- `errorCode`: safe machine-readable error code on rejected/failed events.
- `correlationId`: HTTP trace or job correlation identifier.
- `changedFields`: list of modified property names.
- `statePayload`: approved state dictionary (e.g. `Status`, `AuthenticationVersion`, `AdminControlVersion`, `Visibility`, `ProcessingGeneration`).

### Outcome Semantics & Eligible Rejection Isolation
- `Succeeded`: Synchronous state transition completed and committed.
- `Accepted`: Asynchronous processing command (such as catalog extraction/preparation) was accepted and enqueued. This does NOT assert that extraction or AI processing has finished.
- `Rejected`: Authorized command was rejected by validation, protection, or concurrency rules. No entity mutation was committed.
  - **Actor Admission Barrier**: Rejection logging requires successful actor admission (active administrator, current presented session version, required permission grants). Unadmitted attempts and non-existent targets persist zero audit records.
  - **Isolated Clean Persistence**: Failed command transactions are completely rolled back and change tracking is discarded before eligible rejections are appended in an isolated, clean persistence context.
  - **Sanitized Metadata**: Invalid request bodies, overlong reasons, and sensitive inputs are never copied or truncated into history; the `reason` field is omitted on such rejections.
  - **Fail-Safe Persistence**: If rejection audit persistence fails, the operation safely returns `AUDIT_PERSISTENCE_UNAVAILABLE` (503) rather than failing silently or inventing unpersisted state.
- `Failed`: Infrastructure dispatch or execution failure (e.g., worker queue failure after acceptance commit).

## Capability Policies & Route Admission
- `GET /api/v1/admin/capabilities`: Authenticated discovery endpoint returning:
  `{ canManageCatalog, canManageAccounts, canReadAudit, canManageDocumentOperations, canViewAdminDashboard, canManageRoles, canManageCategories }`
  with `Cache-Control: no-store`.
- Policies:
  - `ViewAdminDashboard`: Requires `canViewAdminDashboard`.
  - `CatalogAdmin`: Requires `canManageCatalog`.
  - `AccountAdmin`: Requires `canManageAccounts`.
  - `AuditRead`: Requires `canReadAudit`.
  - `DocumentOperationsAdmin`: Requires `canManageDocumentOperations`.
  - `RoleAdmin`: Conjunction requiring BOTH `canManageAccounts` AND `canManageRoles`.
  - `CategoryAdmin`: Requires `canManageCategories`.
- **Contextual Returns (F15)**: Validated return URL pathnames preserve safe query parameters (e.g. `?search=architecture`) and hash fragments (`#category-systems`) upon successful administrator authentication or already-authenticated login entry. Unpermitted destinations fall back safely to the first accessible administrative route without loops or context loss.
- **Destination-Specific Recovery & Outlet Gating (F11)**: The committed admin outlet in `app.vue` is reactively gated by current-session destination permission (`adminAccess.canAccessDestination(route.path)`). When capabilities return transient 503, `AdminAuthorizationBoundary` displays a neutral recovery view with a localized "Try again" ("Thử lại") button. Permitted recovery restores access to the requested workspace without full-page reloads or loops. Unpermitted access denies cleanly.
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

The latest pre-push verification passed all 899 backend tests with no skips against an isolated, migrated PostgreSQL database, all 1008 frontend tests across 104 files, and the production Nuxt build. Executable-line coverage across modified non-generated backend production files was 82.75% (2874/3473 lines). The actual `Program` is exercised through `WebApplicationFactory<Program>` rather than a copied middleware pipeline, including initialization, current-session admission, required-grant startup refusal, and safe administrative/public-health failures.

Before running backend tests, set `TEST_POSTGRES_CONNECTION` to an isolated, migrated PostgreSQL database with `pgvector` installed before test connections open. The actual-host fixture also requires that test role to create/drop private databases and install the extension. Missing configuration fails clearly; connection strings are not included in fixture errors. Do not point this setting at production.
The backend CI service installs `vector` before launching the test runner, so a datasource cannot cache PostgreSQL type information before the extension exists. This preparation changes only the disposable CI database, not deployment schema or production authority.

Frontend lint currently passes with zero errors and 214 warnings. `npm run typecheck` still fails on existing application diagnostics present in `HEAD`; it is not a passing gate and is not replaced by the successful build. Existing transitive `System.Security.Cryptography.Pkcs` 10.0.0 advisories also remain outside this authorization change.

Commands from the repository root:

```sh
dotnet test backend/DeepPace.sln --configuration Release --no-restore --verbosity minimal --collect:"XPlat Code Coverage"
cd frontend
npm test
NUXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5001 API_INTERNAL_URL=http://127.0.0.1:5001 npm run build
```

`frontend/e2e/test-admin-workspace.mjs` uses real API login responses and refresh cookies, never fabricated browser identities. Run it against an isolated, migrated QA database containing active administrator and ordinary accounts plus a published catalog title named `Distributed Systems Field Notes`. Initialize the QA database authority using the one-time operator initialization command:
```sh
dotnet run --project backend/src/DeepPace.Api/DeepPace.Api.csproj -- authorization initialize --admin-user-id <admin_user_uuid>
```
Supply `ADMIN_QA_EMAIL`, `ADMIN_QA_PASSWORD`, `LEARNER_QA_EMAIL` and `LEARNER_QA_PASSWORD` through the environment; the administrator pair can also come from the gitignored `.env` E2E entries. An optional `ADMIN_QA_ACCOUNTS_FILE` is a private QA fixture file with `ordinaryEmail` and `ordinaryPassword` fields. Configuration-based allowlists (`CatalogAdministration__UserIds__*`) are completely obsolete and ignored. Never commit credentials or browser storage states.

```sh
TARGET_URL=http://127.0.0.1:3001 API_URL=http://127.0.0.1:5001 node e2e/test-admin-workspace.mjs
```

The browser suite passed 149 assertions and captured 39 screenshots. It covers real sign-in, regular-account denial, account replacement, remember-me cookies, SSR admission, refresh-only hydration, unknown administrator paths, direct API 401/403 rejection, catalog navigation, learning switch and shared logout. It simulates network/permission failures with Playwright response interception while retaining authentic sessions. The explicit health-probe button contacts the configured AI services once; loading the dashboard does not. A controlled browser clock verifies stale observations. Screenshots cover 1440×900 and 390×844 in English/Vietnamese and light/dark, including drawer keyboard focus, retries, denial and service states. Images and the assertion report are written to gitignored `frontend/e2e/screenshots/admin-workspace/`, inspected directly and presented with the implementation report.

### Automated Headless Chromium Role Authorization Verification
To verify the complete matrix of role authorization boundaries, contextual returns, transient recovery, and role modal conflict workflows:
This suite requires all four `ADMIN_QA_EMAIL`, `ADMIN_QA_PASSWORD`, `LEARNER_QA_EMAIL` and `LEARNER_QA_PASSWORD` environment variables. It has no embedded credentials or fallback identities and fails before authentication when any value is absent.
```sh
node frontend/e2e/verify-role-authorization-boundaries.mjs
```
The suite executes 31 assertions across 5 core journeys with real API tokens (`POST /api/v1/auth/login`) and captures 10 visual screenshots in `frontend/e2e/screenshots/role-authorization/`:
1. **Contextual Returns (F15)**: Authenticated administrator accessing `/admin/login?redirect=...` preserves query parameters (`?search=architecture`) and hash fragment (`#category-systems`) upon entry to `/admin/catalog`.
2. **Boundary Unavailable & Retry (F11, F12)**: Transient 503 capabilities render the neutral boundary with localized "Try again" ("Thử lại") button; clicking retry recovers into the authorized catalog shell.
3. **Producer-Shaped Permission Explanations (F13, F14)**: Role assignment modal displays localized permission tags (`ManageCategories`, etc.) from canonical `permissionKeys` on both Desktop (`1440x900`) and Mobile (`390x844`) in English and Vietnamese with verified body typography, zero clipping, and button collision safety.
4. **Role Conflict & Draft Preservation (F12, F14)**: 409 Conflict triggers warning banner with "Reload Account" ("Tải lại tài khoản") button; reload updates version while preserving draft justification text. A completion from a closed/reopened or replaced confirmation cannot change the current error, conflict gate or loading state. Only a matching accepted reload permits deliberate resubmission; actual close restores the initiating trigger without stealing focus on closed-dialog account selection.
5. **Destination Denial (F11)**: Unauthenticated or ordinary learner account is safely denied without privileged chrome exposure.

The pre-push browser check used authentic local QA sessions against the production frontend build at 1440×900 and 390×844 in English and Vietnamese. It exercised an intercepted conflict, a deferred superseded `503`, same-target close/reopen, retained drafts, a real matching account reload, and keyboard-close focus restoration. Controlled error interception sent no real role mutations. These checks supplement the component regressions; they do not claim a complete accessibility or production-deployment audit.

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

## Category Administration & Taxonomy Management

Authorized administrators with the `CategoryAdmin` policy manage the platform's official document taxonomy at `/admin/categories`.

### Permission Model
- **`ManageCategories`**: A dedicated permission key supported by the database authority. Exposed as `canManageCategories` in administrative capabilities (`GET /api/v1/admin/capabilities`).
- **`CategoryAdmin` Policy**: Enforces valid authentication, active account status, and possession of `ManageCategories`.
- Independent authority: Category administration does not grant catalog curation, account management, role assignment, or audit reading privileges.

### Endpoints & Rate Limits
| Method | Path | Authorization | Rate Limit | Description |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/categories` | Authenticated | Standard | List all non-deleted categories ordered by `SortOrder` then `Slug` |
| `GET` | `/api/v1/admin/categories` | `CategoryAdmin` | `CategoryAdminReads` (120/min) | Bounded search, optional `isActive` filter, pagination, usage statistics |
| `POST` | `/api/v1/admin/categories` | `CategoryAdmin` | `CategoryAdminWrites` (30/min) | Creates category; returns `201 Created` with `Location` header and descriptor |
| `GET` | `/api/v1/admin/categories/{id}` | `CategoryAdmin` | `CategoryAdminReads` (120/min) | Detailed descriptor with aggregate usage breakdown and `canDelete` flag |
| `PATCH` | `/api/v1/admin/categories/{id}` | `CategoryAdmin` | `CategoryAdminWrites` (30/min) | Updates English/Vietnamese names, sort order, and active state under concurrency version |
| `DELETE` | `/api/v1/admin/categories/{id}?expectedVersion=N` | `CategoryAdmin` | `CategoryAdminWrites` (30/min) | Soft-deletes unreferenced category; returns `204 No Content` |

### Lifecycle & Invariants
1. **Immutability of Identity & Slugs**:
   - Category `Slug` is strictly immutable once created.
   - Slugs must be trimmed lowercase ASCII kebab-case (`^[a-z0-9]+(-[a-z0-9]+)*$`, 1–100 characters).
   - Soft-deleted slugs may be reused by a new UUID; inactive slugs remain permanently reserved.
2. **Optimistic Concurrency Control**:
   - `ConcurrencyVersion` is a positive integer initialized to 1.
   - `PATCH` and `DELETE` operations require `ExpectedVersion`. Mismatched versions reject with `409 Conflict`.
3. **Inactive Categories & Content Retention**:
   - Existing content (books, catalog titles, quiz questions, tech insights) referencing an inactive category continues to function normally for reading, review, and search.
   - Inactive categories are rejected when attempting new assignments or adoptions (`Category.Inactive`).
4. **Reference-Safe Soft Deletion**:
   - A category can only be deleted if its total usage count across `DocumentBooks`, `CatalogDocuments`, `QuizQuestions`, and `TechInsights` (including soft-deleted records) is zero.
   - Referenced categories return `409 Conflict` (`Category.InUse`) advising the administrator to deactivate instead.
5. **Atomic Audit Logging**:
   - Every committed category mutation emits an administrative audit event (`Category.Create`, `Category.UpdateMetadata`, `Category.Reorder`, `Category.Activate`, `Category.Deactivate`, `Category.Delete`).
   - Rejection events are recorded without committing mutations.

### Error Codes (RFC 7807 Problem Details)
| Error Code | Status Code | Reason |
| --- | --- | --- |
| `Category.ValidationFailed` | `400 Bad Request` | Invalid slug regex, empty names, or invalid bounds |
| `Category.NotFound` | `404 Not Found` | Specified category ID does not exist or has been deleted |
| `Category.SlugConflict` | `409 Conflict` | Slug is already in use by an active or inactive category |
| `Category.Inactive` | `409 Conflict` | Attempted new assignment to an inactive category |
| `Category.InUse` | `409 Conflict` | Deletion blocked because category is referenced by content |
| `Category.ConcurrencyConflict` | `409 Conflict` | Version mismatch during update or deletion |
| `Category.Unavailable` | `503 Service Unavailable` | Authority store or audit persistence is temporarily unavailable |


### Request & Response Examples

#### 1. List Public / Learner Categories
```http
GET /api/v1/categories HTTP/1.1
Host: localhost:5001
Authorization: Bearer <learner_token>
```

Response:
```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": "10000000-0000-4000-8000-000000000001",
    "slug": "frontend-web",
    "nameEn": "Frontend & Web",
    "nameVi": "Frontend & Web",
    "sortOrder": 10,
    "isActive": true,
    "version": 1
  },
  {
    "id": "10000000-0000-4000-8000-000000000002",
    "slug": "backend-runtime",
    "nameEn": "Backend & Runtime",
    "nameVi": "Backend & Runtime",
    "sortOrder": 20,
    "isActive": true,
    "version": 1
  }
]
```

#### 2. List Administrative Categories (Paginated & Filterable)
```http
GET /api/v1/admin/categories?search=distributed&isActive=true&page=1&pageSize=20 HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
```

Response:
```http
HTTP/1.1 200 OK
Cache-Control: no-store
Content-Type: application/json

{
  "items": [
    {
      "category": {
        "id": "10000000-0000-4000-8000-000000000008",
        "slug": "distributed-systems",
        "nameEn": "Distributed Systems",
        "nameVi": "Hệ thống phân tán",
        "sortOrder": 70,
        "isActive": true,
        "version": 1
      },
      "booksCount": 0,
      "catalogDocumentsCount": 0,
      "quizQuestionsCount": 0,
      "techInsightsCount": 0,
      "totalUsageCount": 0,
      "canDelete": true,
      "id": "10000000-0000-4000-8000-000000000008",
      "slug": "distributed-systems",
      "nameEn": "Distributed Systems",
      "nameVi": "Hệ thống phân tán",
      "sortOrder": 70,
      "isActive": true,
      "version": 1,
      "usageCount": 0
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

#### 3. Get Category Detail by ID
```http
GET /api/v1/admin/categories/10000000-0000-4000-8000-000000000008 HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
```

Response:
```http
HTTP/1.1 200 OK
Cache-Control: no-store
Content-Type: application/json

{
  "category": {
    "id": "10000000-0000-4000-8000-000000000008",
    "slug": "distributed-systems",
    "nameEn": "Distributed Systems",
    "nameVi": "Hệ thống phân tán",
    "sortOrder": 70,
    "isActive": true,
    "version": 1
  },
  "booksCount": 12,
  "catalogDocumentsCount": 3,
  "quizQuestionsCount": 45,
  "techInsightsCount": 8,
  "totalUsageCount": 68,
  "canDelete": false,
  "id": "10000000-0000-4000-8000-000000000008",
  "slug": "distributed-systems",
  "nameEn": "Distributed Systems",
  "nameVi": "Hệ thống phân tán",
  "sortOrder": 70,
  "isActive": true,
  "version": 1,
  "usageCount": 68
}
```

#### 4. Create Category
```http
POST /api/v1/admin/categories HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "slug": "distributed-systems",
  "nameEn": "Distributed Systems",
  "nameVi": "Hệ thống phân tán",
  "sortOrder": 70,
  "isActive": true
}
```

Response:
```http
HTTP/1.1 201 Created
Location: /api/v1/admin/categories/10000000-0000-4000-8000-000000000008
Cache-Control: no-store
Content-Type: application/json

{
  "id": "10000000-0000-4000-8000-000000000008",
  "slug": "distributed-systems",
  "nameEn": "Distributed Systems",
  "nameVi": "Hệ thống phân tán",
  "sortOrder": 70,
  "isActive": true,
  "version": 1
}
```

#### 5. Edit Category Metadata
```http
PATCH /api/v1/admin/categories/10000000-0000-4000-8000-000000000008 HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "nameEn": "Distributed Systems & Cloud",
  "nameVi": "Hệ thống phân tán & Đám mây",
  "sortOrder": 75,
  "isActive": true,
  "expectedVersion": 1
}
```

Response:
```http
HTTP/1.1 200 OK
Cache-Control: no-store
Content-Type: application/json

{
  "id": "10000000-0000-4000-8000-000000000008",
  "slug": "distributed-systems",
  "nameEn": "Distributed Systems & Cloud",
  "nameVi": "Hệ thống phân tán & Đám mây",
  "sortOrder": 75,
  "isActive": true,
  "version": 2
}
```

#### 6. Deactivate Category
```http
PATCH /api/v1/admin/categories/10000000-0000-4000-8000-000000000008 HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "nameEn": "Distributed Systems & Cloud",
  "nameVi": "Hệ thống phân tán & Đám mây",
  "sortOrder": 75,
  "isActive": false,
  "expectedVersion": 2
}
```

Response:
```http
HTTP/1.1 200 OK
Cache-Control: no-store
Content-Type: application/json

{
  "id": "10000000-0000-4000-8000-000000000008",
  "slug": "distributed-systems",
  "nameEn": "Distributed Systems & Cloud",
  "nameVi": "Hệ thống phân tán & Đám mây",
  "sortOrder": 75,
  "isActive": false,
  "version": 3
}
```

#### 7. Delete Unused Category
```http
DELETE /api/v1/admin/categories/10000000-0000-4000-8000-000000000008?expectedVersion=3 HTTP/1.1
Host: localhost:5001
Authorization: Bearer <admin_token>
```

Response:
```http
HTTP/1.1 204 No Content
Cache-Control: no-store
```

#### 8. RFC 7807 Error Code Responses

##### Validation Failure (400 Bad Request)
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "code": "Category.ValidationFailed",
  "detail": "Slug must contain only lowercase ASCII letters, numbers, and hyphens (1-100 characters)."
}
```

##### Category Not Found (404 Not Found)
```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "code": "Category.NotFound",
  "detail": "The specified category was not found."
}
```

##### Slug Conflict (409 Conflict)
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "code": "Category.SlugConflict",
  "detail": "A category with slug 'distributed-systems' already exists."
}
```

##### Deletion Blocked by References (409 Conflict)
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "code": "Category.InUse",
  "detail": "The category cannot be deleted because it is referenced by existing content. Consider deactivating it instead."
}
```

##### Inactive Assignment Rejected (409 Conflict)
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "code": "Category.Inactive",
  "detail": "The category is inactive and cannot be assigned to new content."
}
```

##### Optimistic Concurrency Conflict (409 Conflict)
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "code": "Category.ConcurrencyConflict",
  "detail": "Category was modified by another operator. Reload and try again."
}
```

##### Service Unavailable (503 Service Unavailable)
```http
HTTP/1.1 503 Service Unavailable
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Service Unavailable",
  "status": 503,
  "code": "Category.Unavailable",
  "detail": "Category storage or audit persistence is temporarily unavailable."
}
```
## Release Notes: Admin-Managed Categories & Taxonomy Cutover

### Summary
DeepPace cut over from a compile-time numeric category enum (0..6) to database-backed, bilingual administrative category entities identified by stable UUIDs with unique slugs.

### Key Changes
1. **Bilingual Admin-Managed Taxonomy**:
   - Categories have English and Vietnamese names, immutable ASCII lowercase slugs, sort order (`0..10000`), active status, and optimistic concurrency version.
   - Managed at `/admin/categories` under the `ManageCategories` permission.
2. **Explicit Assignment & Safe Inactivation**:
   - Content ingestion and catalog drafting require explicit selection from active categories; no implicit fallbacks or long-lived enum maps exist.
   - Deactivating a category prevents future assignments while keeping existing documents, reading progress, and flashcard review queues intact.
3. **Reference-Guarded Deletion**:
   - Categories referenced by any document, catalog title, quiz question, or insight cannot be deleted (`Category.InUse`, `409 Conflict`). Deactivation is the supported workflow for obsolete categories with historical usage.
4. **Intentional API & Filter Incompatibility**:
   - Numeric category parameters (`0..6`) are retired. All public, learner, and administrative endpoints exclusively accept UUID strings. Clients should clear stale numeric query parameters.
   - DeepPace provides taxonomy category management, not an arbitrary user tag-management feature.
