# Design

## Context

See `proposal.md` for motivation and the six delta specs for observable behavior. This changes the existing .NET 10/Nuxt 4 application; it does not introduce an identity provider.

Observed integration points:

| Area | Evidence | Design consequence |
| --- | --- | --- |
| Role-free account | `Domain/Entities/User.cs` has `TargetRole`, `AuthenticationVersion` and `AdminControlVersion`, but no authorization role | Add a separate RoleId; preserve the learning goal and existing account versions |
| Three allowlist consumers | `AdminPermissionResolver`, `CatalogAdminService`, `AdministrativeUnitOfWork` each read CatalogAdministration user IDs | Replace every runtime path, not just capability discovery |
| Account commands | AdministrativeUnitOfWork uses the DPACCTRL PostgreSQL transaction advisory lock and ordered account row locks | Reuse this serialization for role assignment and status controls |
| Session authority | `AccountSessionAuthority` compares session_version on every bearer request; `SessionIssuer` and `RefreshTokenService` issue and rotate versioned sessions | Reuse the version boundary and make final issuance/refresh checks consistent with role commands |
| Rejection history | AdministrativeUnitOfWork starts its clean-event transaction on the same context after rollback and swallows rejection-write errors | Use an independent context after the failed transaction is disposed |
| Dashboard | AdminDashboardEndpoints currently requires CatalogAdmin; frontend fallback admission treats any capability as dashboard access | Introduce an explicit dashboard permission and capability-aware landing |
| Startup | Program catches migration failures and continues; it calls E2EAccountSeeder, which has committed credential defaults and can reset matching account passwords | Fail closed on required schema and remove startup identity provisioning from the production path before RBAC cutover |

The startup seeder is an observed source risk, not evidence that any particular deployment or account is compromised. Its removal from the production startup path is a narrowly required integration correction: a recurring password/reset identity writer must not become an alternative administrative bootstrap. Existing accounts are not deleted as part of this change.

## Goals / Non-Goals

**Goals:**
- One database authority model, with current permission and presented-session checks at privileged mutation admission.
- Small explicit schema and a reusable capability contract; no role-name authorization switches.
- Atomic role/account/session/history changes and a rehearsed initialization that cannot silently run twice.
- Restricted roles can enter only their permitted destinations without a dashboard/catalog umbrella grant.

**Non-Goals:**
- No role-definition editor, permission editor, user override grants, multi-role join table, inheritance or arbitrary policy-expression engine.
- No password algorithm, OAuth, cookie/storage, learner ownership, learning-goal or SM-2 redesign.
- No automatic emergency-admin recovery, startup QA accounts, background elevation or ongoing configuration fallback.
- No claim of runtime security verification from these planning files.

## Decisions

### 1. One required role with explicit grants

Add Domain Role and RolePermission representations and required `User.RoleId`. Keep the existing rich account transition methods; add a role transition that rejects unchanged assignment and advances AuthenticationVersion and AdminControlVersion once without changing status, suspension metadata, credentials or learning data. Role changes are not generic profile updates.

| Table | Shape and constraints |
| --- | --- |
| Roles | UUID Id; immutable lowercase ASCII Key (1–64); NameEn/NameVi (trimmed 1–100); IsProtected; existing timestamps and IsDeleted; partial unique nondeleted Key |
| RolePermissions | Composite primary key (RoleId, PermissionKey); restricted FK to Roles; bounded exact-case action key; no wildcard or implicit default grant |
| Users | Non-null RoleId, restricted FK and role/status index; server/database default is the fixed Learner identity |
| AuthorizationInitializations | Primary key Scope=`database-roles-v1`; CompletedAt UTC and sorted imported Admin user IDs as bounded operational metadata; immutable completion receipt, no invented ActorUserId |

Use fixed migration identities `20000000-0000-4000-8000-000000000001` for `learner` and `20000000-0000-4000-8000-000000000002` for protected `admin`. These identify two seed records; they are not a hardcoded list of all assignable roles. Role options query the database. Both initial roles are protected from application-driven definition changes; there are no definition mutation endpoints in this change. A controlled future migration may introduce another role and grants without altering Users.

Persist permission keys matching the existing AdminPermission names: ManageCatalog, ManageAccounts, ReadAudit, ManageDocumentOperations, plus ViewAdminDashboard and ManageRoles. Explicitly seed all six for protected Admin and none for Learner. Use ordinal case-sensitive recognized-key matching; unknown/case-variant/wildcard rows grant nothing and produce safe diagnostics. Do not use an enum's numeric storage or an `Admin => every future permission` shortcut. A later permission requires a new explicit grant migration.

Read role metadata as `{ id, key, nameEn, nameVi }`. Auth session and own-profile responses include only the caller's role descriptor, not the role directory or permission arrays. Existing targetRole remains the learning goal. Admin account summaries/details add the same descriptor. Role-option results add recognized permissionKeys for an authorized assignment explanation.

Alternative rejected: Users.Role enum or mutable role name. It couples each new role to code/schema branching. Multiple simultaneous memberships and per-user grants add precedence and revocation complexity not needed by this scope.

### 2. Current authority, effective capabilities and availability

Refactor IAdminPermissionResolver to return a typed Result/authority outcome so unavailable is distinct from denied. Query nondeleted Active users, valid nondeleted role and recognized grants with narrow no-tracking projections. Remove IConfiguration from resolver, CatalogAdminService and AdministrativeUnitOfWork authority decisions. Keep CatalogAdminService only for its existing catalog-facing contract if still used; delegate permission resolution rather than retaining a second authority implementation. Remove obsolete helpers/config examples after all callers migrate.

Use a request-scoped authority snapshot for a single request if useful, never a cross-request privilege cache. All capability fields are explicit, with false as the only missing-field default; remove the current implicit true default for document operations. Preserve the existing four fields and `/api/v1/catalog/capabilities` wire shape. Add canViewAdminDashboard and effective canManageRoles; the latter is ManageRoles AND ManageAccounts. A ManageRoles-only role has no account or role-option access and does not itself create a navigable destination.

API authorization policies use the same result model. Valid credentials with insufficient grants return 403; failed bearer/session authority returns 401; temporary account/role store failure returns RFC 7807 HTTP 503 with a safe unavailable code. An explicit authorization-result mapping must prevent an unavailable resolver result from becoming middleware's default 403. Missing/deleted referenced roles are denied administrative authority and diagnosed safely; missing required schema/default seed roles fail readiness rather than selecting a fallback.

Role-definition/grant changes are allowed only through controlled database migrations, not direct application writes or unattended seeders. Such a future migration must advance affected user session versions and revoke old families under the same authority serialization, or keep writers stopped for cutover. No product API in this change silently mutates grants.

### 3. Serialize account/role commands and recheck the presented session

Extend the existing account-command unit of work to accept the required permission set and the actor's presented session version obtained from the validated principal. Client fields cannot supply actor identity, session version or correlation attribution. Existing suspension/reactivation/sign-out require ManageAccounts; role assignment requires ManageAccounts AND ManageRoles.

A short account-control transaction takes the existing DPACCTRL advisory lock, then locks actor and target Users rows in deterministic database UUID order (also for self-targeted commands). Read fresh account/role state after locks; reject missing/deleted/inactive actors and a presented version different from current AuthenticationVersion before executing the command. Do not mistake a reactivated or newly re-promoted account for authority of its old token. Then validate target existence, expected AdminControlVersion, new role eligibility and transition/protection rules.

The protected-admin minimum counts Active, nondeleted users in the protected Admin seed role, with the required system grants intact; a role merely named Admin or holding ManageAccounts alone is not the recovery anchor. Suspension and role demotion share the same lock and count. Self-suspension and all self-role changes are rejected. Assignment to a Suspended target is allowed but never reactivates it. No-op/same-role and stale-version requests return conflict and do not repeat revocation.

Successful assignment changes RoleId, both versions and all unrevoked target refresh families, then stages one User.AssignRole event and commits once. Use the same scoped context/transaction; a helper that calls SaveChanges must not independently commit or expose partial success. Audit failure rolls everything back, including refresh changes. The acting account's session is unaffected because self-role assignment is prohibited.

Other privileged catalog/document mutations must not rely solely on an earlier middleware check. At their final short admission/persistence boundary, acquire a shared actor Users lock before content/category database row locks and verify current state, session version and exact grants. Keep any existing outer document mutation lease outside these database locks. Account role/status changes take the conflicting actor row lock, so either the privileged mutation commits first or it observes revoked authority. Provider work stays outside these short transactions; repeat admission before persisting/dispatching the privileged result. Do not hold a database lock while contacting AI or downloading sources.

This boundary governs authenticated request admission, durable queue acceptance and interactive result persistence. Already accepted background jobs retain their existing durable processing contract; role reassignment does not replay an expired actor JWT in a worker or silently cancel accepted ingestion. Publication and new privileged commands still require their own current request admission.

Alternative rejected: check capabilities once in the UI or before network work. It allows a command to commit using authority removed while work was in flight.

### 4. Session issuance, refresh and rejection persistence

Keep the existing JWT signature, issuer/audience, lifetime, cookie persistence and session_version contract. Do not add authority-bearing role claims. On a real role change increment the target version and revoke every refresh family in the same transaction. Post-commit old bearer tokens are rejected by AccountSessionAuthority, and old refresh/grace successors cannot renew the session.

Inspect and align SessionIssuer/RefreshTokenService's authoritative reads and short issuance/rotation transaction with the Users row boundary. A token derived from pre-change state must retain that old version and fail after commit; never combine a freshly reread version with stale role/user state or return a grace successor without current-version validation. Fresh sign-in after commit uses the current role descriptor. Do not redesign the unrelated family rotation/reuse algorithm.

For recoverable authorized command rejection, roll back and dispose the command transaction before creating a fresh dependency scope/DbContext for only the bounded Rejected event. The clean context must contain no tracked actor/target/session mutations from the failed command. This corrects the existing account helper so role commands and account controls use one safe path; the category successor reuses it. If rejection persistence fails, return unavailable and emit safe correlated diagnostics, not a swallowed exception or a claim that history exists. Authentication/policy denial is not attributed as an authorized role command.

### 5. Bounded API and existing account workspace

| Endpoint | Policy and contract |
| --- | --- |
| GET `/api/v1/admin/roles` | ManageAccounts AND ManageRoles; search length ≤200; page default 1; pageSize 20/max100; stable Key/Id ordering; descriptors plus recognized permissionKeys and totalCount |
| PUT `/api/v1/admin/users/{userId}/role` | Same conjunction; `{ roleId, reason, expectedVersion }`; 200 updated AdminAccountDetail; exact UUID/positive version and trimmed 1–500 reason validation |
| Existing account reads/actions | Preserve ManageAccounts and current routes; add role descriptor to summaries/details; apply shared lockout/audit changes |
| Existing capabilities | Authenticated, current DB authority, no-store; explicit six flags |
| Dashboard overview/status/ai-probe | Dedicated ViewAdminDashboard policy; preserve existing explicit-probe limiter and no automatic AI calls |

Return 400 for malformed fields, 404 for missing/deleted target or selected role, 409 for ACCOUNT_CONCURRENCY_CONFLICT, ACCOUNT_SELF_ROLE_CHANGE_BLOCKED, ACCOUNT_LAST_ADMIN_PROTECTED or ACCOUNT_ROLE_UNCHANGED, 401/403 for authority rejection and 503 for authority/persistence/audit unavailability. Preserve existing account command codes where their meaning is unchanged. New-role options and assignment use named per-user limits of 120 reads and 30 writes per minute with 429/Retry-After. Mark admin/capability responses and errors no-store. Document real HTTP statuses and schemas in OpenAPI; update the canonical `frontend/app/types/api.generated.ts` when consumed without creating a second generated file.

Reuse `/admin/users`, useAdminUsersStore and the existing confirmation pattern. Account inspectors always see the role descriptor, but only effective canManageRoles fetches role options or enables assignment. Require explicit new-role selection and explain loss/gain of administrative scope plus all-device sign-out; do not preselect a different role. Preserve selection/reason on failure. After a version conflict, gate submission until explicit successful refresh, show current server values without overwriting the draft, then require deliberate reconfirmation. Failed refresh preserves the draft. Discard late responses using the existing session/admin-generation fences.

Use AppSelect/AppModal, not native selects/dialogs. The actual focusable combobox needs a localized accessible name, selected value, expanded/required/invalid states and associated errors. Caller-owned dialog focus containment must include the teleported role list and prevent background focus; first Escape closes the list only, later Escape cancels the dialog. Restore a surviving trigger on cancel/success. Announce error/result states; keep full role names available without hover, project typography, no-wrap actions/badges and responsive gaps. Verify English/Vietnamese at desktop 1440x900 and mobile 390x844 with authentic sessions, plus effective 320 CSS-pixel reflow and keyboard interaction. This change does not claim to remediate every pre-existing modal globally.

### 6. Permission-aware landing and shared shell

Maintain one registered destination-to-capability mapping used by admin middleware, login landing, sidebar/mobile navigation, shell home links and dashboard drill-down links. Initially order Dashboard, Catalog, Documents, Accounts, Audit. A return path must first pass existing local-path safety checks and then destination authorization. If forbidden/unsafe, select the first permitted destination; if none, remain at admin login with the learner session intact. Unknown destinations are default-deny rather than `any capability => allow`.

Dashboard requires canViewAdminDashboard, not isAuthorized or canManageCatalog. Accounts requires canManageAccounts, not ManageRoles OR ManageAccounts. The role action is separately gated inside Accounts. Background re-verification can retain the same session's mounted shell, but a newly navigated outlet cannot fetch/render before its destination check. A definitive denial clears privileged state; 503 retains credentials with a neutral retry boundary. Account/session replacement discards all previous privileged responses.

The category successor adds Categories and canManageCategories to this same mapping; it must not restore an unconditional Dashboard landing or erase restricted-role scenarios when modifying shared requirements.

### 7. Explicit one-time initialization, not startup elevation

Implement an operator-only `authorization initialize --admin-user-id <uuid>` command in the existing backend host, dispatched before web/worker startup. It uses normal secret configuration and a trusted database connection, not an HTTP endpoint or fake authenticated user. Repeated options form the proposed administrator set; the operator copies/reviews the legacy designation explicitly rather than the application reading it for authority. Do not log credentials or connection strings.

Schema migration creates role/grant seeds and backfills all existing Users, including deleted users, to Learner so the required FK can be installed without dropping account history. This schema step never guesses administrators. The explicit command takes DPACCTRL, checks the durable initialization scope is absent, validates every supplied UUID and existing nondeleted identity, locks the selected accounts deterministically and requires at least one Active target. Suspended selected identities may receive Admin but do not satisfy the minimum. Invalid/deleted/missing identities fail the entire command; none are silently ignored.

In one transaction promote the selected users, advance versions/revoke sessions for actual membership changes, and insert the immutable completion receipt. Verify protected role/grant seed integrity before commit. No user-attributed admin event is fabricated for this operational action. Refuse repeat initialization even if there are later zero active administrators; no `count == 0 => restore allowlist` heuristic. Recovery from out-of-band database damage requires a separately approved operator recovery procedure, not an automatic application backdoor.

On a fresh database, valid schema and incomplete initialization allow normal real Learner registration/sign-in. Capability/admin APIs report authorization initialization unavailable until completion; no protected endpoint is admitted. Existing deployments keep general writes closed until operator initialization passes. Required schema/default role/grant integrity is checked outside Program's current warning-and-continue migration catch, before API/workers become ready. Missing schema is not confused with intentionally incomplete bootstrap.

Remove the production startup E2EAccountSeeder call and obsolete committed-default seeding path; any needed QA identity provisioning is an explicit isolated test fixture with no production credential writer and no implicit Admin grant. Do not delete existing user rows. Before a real deployment, operators must inspect whether that former default identity exists outside isolated test databases, reset exposed credentials/revoke sessions if applicable, and select only intended administrator IDs. This is a cutover safety prerequisite, not a claim that a deployment was compromised.

## Risks / Trade-offs

- **One role initially** -> Simple audit/revocation semantics; additive roles use current schema. Multiple concurrent roles require a separately specified composition policy.
- **No role editor** -> Only operator-reviewed migrations define new roles/grants for now; assignment is admin-managed. This avoids granting the ability to rewrite one's own authority.
- **A role change ends every target session** -> Deliberate security boundary; communicate before confirmation and prove fresh sign-in after commit.
- **Permission checks during an outage** -> Return 503 with no protected data, preserve browser credentials and offer retry; never silently interpret failure as a role.
- **Concurrent actor revocation** -> Shared/exclusive actor-row protocol and presented-version recheck, exercised with independent PostgreSQL connections.
- **Role/bootstrap schema on old binaries** -> Coordinated maintenance cutover; no mixed writable deployment or permanent dual authorization.
- **Known startup QA writer** -> Eliminate the production path before opening RBAC administration; deployment operators handle any existing exposed account separately without deleting history.
- **Manual direct SQL changes** -> Outside the supported runtime management contract; controlled future migrations must preserve system roles, invalidate affected sessions and retain the recovery anchor.

## Migration Plan

1. Inventory existing legacy configured IDs, account status/deletion state, users, versions, refresh families and audit counts. Do not emit credential values. Verify intended administrator identities and take a restorable database backup; rehearse on an isolated copy.
2. Build matched backend/frontend artifacts, remove the startup identity writer and complete local security/behavior/browser gates. Stop old writers/workers and deploy the new schema under the approved maintenance window; old binaries must not write through their allowlist path after cutover begins.
3. Apply schema/seeds/required Learner backfill transactionally; verify every account has a valid role, existing content/status/credentials/history remain unchanged, and required role/grant integrity is ready. Schema failure prevents API/worker readiness.
4. Run the explicit operator initialization command with reviewed existing IDs. Verify versions/token-family revocation for promoted identities, protected active-admin minimum and durable completion. Remove CatalogAdministration deployment examples and variables; restarting with leftover legacy settings must never reimport authority.
5. Launch the matched application while general writes remain closed. Sign in freshly as protected Admin and Learner; verify role options/assignment, per-destination denials, all-session revocation, stale/protection conflicts, atomic audit and restricted-role fixtures on the isolated smoke environment. Regenerate consumed API contracts from the actual OpenAPI output and verify the matched frontend.
6. Confirm existing startup no longer creates accounts/resets credentials or overwrites memberships, inspect English/Vietnamese desktop/mobile screenshots, then open normal writes. The category change can proceed only after this authority boundary and shared spec deltas are accepted.
7. Before general writes reopen, rollback is a full verified pre-cutover database restore plus previous matched assets, with explicit acceptance that this restores the old authorization model. After live role changes or other new writes, do not roll back by dropping Roles or ignoring recorded assignments; prefer forward repair or separately approved backup recovery acknowledging data/access-state loss. Restart nginx after rebuilt containers where applicable.

This planning workflow does not execute the command, change accounts, run migrations, rotate credentials or deploy services.
