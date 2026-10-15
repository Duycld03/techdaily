# Spec Delta

## MODIFIED Requirements

### Requirement: Operator-controlled administrator designation
Administrative authority SHALL come from current database role membership and supported role grants for an Active, non-deleted account. The trusted operator SHALL initialize the first protected administrators explicitly; thereafter authorized role assignment SHALL control membership. Runtime configuration, registration, OAuth, profile fields and token refresh SHALL NOT grant or restore administrative authority.

#### Scenario: No configured administrator
- **GIVEN** database authority has not been initialized and no eligible initial administrator has been selected
- **WHEN** initialization is attempted without a valid administrator designation
- **THEN** initialization fails and privileged administration remains unavailable
- **AND** normal Learner registration, sign-in and published discovery remain usable without an administrator bypass.

#### Scenario: Administrator removal
- **WHEN** an authorized role change removes a user's administrative grants and commits
- **THEN** old access and refresh sessions fail authentication with 401
- **AND** after fresh sign-in, management requests without the required grant return 403 even if a legacy deployment allowlist still names the user.

#### Scenario: Spoofed administrator field
- **WHEN** a regular user supplies administrator fields during registration or profile editing or alters browser storage
- **THEN** the user gains no management permission.

#### Scenario: Designated account is suspended
- **WHEN** an account holding the protected Admin role is Suspended
- **THEN** it receives no administrator capabilities or authenticated access until reactivated and signed in again
- **AND** reactivation preserves its current database membership rather than consulting deployment configuration.

### Requirement: Administrator capability discovery and route protection
The server SHALL expose effective dashboard, catalog, account, audit, document-operations and role-assignment capabilities from current database authority. Role assignment SHALL require both ManageAccounts and ManageRoles. Every protected admin route SHALL verify its destination capability before rendering or fetching data. Denied visitors SHALL reach admin login with a validated local return path; transient verification failure SHALL preserve credentials behind a neutral retry state.

#### Scenario: Administrator opens management page
- **WHEN** an administrator whose current role grants ManageCatalog opens `/admin/catalog`
- **THEN** the server confirms that permission and the catalog workspace loads inside the administrator shell.

#### Scenario: Client capability becomes stale
- **WHEN** an administrator's access is revoked while their page is open
- **THEN** terminal authentication rejection or definitive permission denial clears privileged data, aborts pending requests, stops polling and redirects once to `/admin/login`
- **AND** cached capabilities cannot authorize another request.

#### Scenario: Non-administrator opens management page
- **WHEN** an authenticated account without the destination permission visits a protected administrator route
- **THEN** the browser redirects to `/admin/login` without protected SSR markup or draft requests
- **AND** a permission denial alone preserves the learner session.

#### Scenario: New administration destinations
- **WHEN** a user opens `/admin/users` or `/admin/audit`
- **THEN** ManageAccounts or ReadAudit is verified independently of navigation visibility and catalog authority
- **AND** browser flags or a direct URL cannot substitute for the required grant.

#### Scenario: Legacy catalog capability consumer
- **WHEN** an existing client requests the catalog-management capability through its current endpoint
- **THEN** the existing response contract remains available with its original permission meaning, resolved from database grants.

#### Scenario: Capability discovery is effective and private
- **WHEN** an authenticated client requests `/api/v1/admin/capabilities`
- **THEN** it receives explicit booleans including `canViewAdminDashboard` and `canManageRoles`, with existing four flags preserved
- **AND** `canManageRoles` is true only when both role-management and account-management grants are present
- **AND** the response and all rejections are no-store; unavailable authority returns 503 rather than a fabricated all-false success.

### Requirement: Server-verifiable session revocation
Protected API authentication SHALL verify access tokens against current server account/session state in addition to signature, issuer, audience and expiry. Suspension, administrator all-device sign-out and actual role changes SHALL invalidate previous access tokens and all refresh families after commit. Normal per-device logout SHALL retain its existing isolation. A rejected or unchanged-role command SHALL NOT invalidate sessions.

#### Scenario: Access token has not expired
- **WHEN** an account presents an unexpired, correctly signed access token issued before a committed all-device sign-out
- **THEN** authentication returns 401 without allowing a learner or administrator operation.

#### Scenario: Refresh races with revocation
- **WHEN** session issuance or refresh overlaps suspension, all-device sign-out or role assignment
- **THEN** no credentials based on pre-revocation state authorize requests after revocation commits
- **AND** refresh grace-window handling cannot revive the revoked authority.

#### Scenario: Ordinary device logout
- **WHEN** a user revokes one refresh-token family through normal logout
- **THEN** other devices' valid sessions remain usable according to existing per-family behavior.

#### Scenario: Browser receives terminal authentication rejection
- **WHEN** an old access token returns 401 and refresh is definitively rejected
- **THEN** session and privileged state are cleared and the browser redirects once to the login for its current area
- **AND** late administrative responses cannot restore account or audit data.

#### Scenario: Role change ends every old session
- **WHEN** another administrator commits a promotion or demotion
- **THEN** all pre-change access tokens and refresh families fail on subsequent learner and admin requests
- **AND** fresh sign-in returns the current role and capabilities without restoring previous sessions.

### Requirement: Account authority availability handling
When current account, session or role-grant authority cannot be verified due to temporary service failure, protected access and session issuance SHALL fail closed with an unavailable response rather than invalid-credential or permission-denied feedback. Clients SHALL preserve stored credentials while presenting retry, without treating uncertainty as authorization.

#### Scenario: Account state store is temporarily unavailable
- **WHEN** authentication cannot read authoritative account/session state due to a database outage
- **THEN** the server returns 503 without protected data or replacement tokens
- **AND** the client preserves credentials and offers retry instead of a forced logout.

#### Scenario: Permission state store is temporarily unavailable
- **WHEN** authentication succeeds but current role grants cannot be read
- **THEN** privileged APIs and capability discovery return 503 without protected data or approved access
- **AND** the browser retains its session behind a retry boundary.

### Requirement: Server-verified document operations capability
The capability response SHALL retain canManageDocumentOperations alongside existing destination flags, resolved from current database grants. Document-operations APIs SHALL independently enforce ManageDocumentOperations and current account/session authority. Invalid authentication SHALL return 401, verified insufficient permission 403, and unavailable authority 503. Catalog mutation SHALL additionally require ManageCatalog; ownership SHALL remain independent.

#### Scenario: Capability discovery remains compatible
- **WHEN** an authenticated client reads `/api/v1/admin/capabilities`
- **THEN** the current document-operations flag appears alongside other destination flags
- **AND** `/api/v1/catalog/capabilities` retains its existing catalog-management response contract.

#### Scenario: Direct unauthorized operations call
- **WHEN** a caller without a valid session requests inventory, history or retry
- **THEN** the response is 401 without protected metadata
- **WHEN** an authenticated account lacks operations permission
- **THEN** the response is 403 without metadata or processing side effects.

#### Scenario: Catalog mutation needs catalog authority too
- **WHEN** an account has document-operations permission but lacks catalog-management permission
- **THEN** catalog source retry returns 403 without changing content or dispatching work
- **AND** metadata-only operations do not become a catalog-editing grant.

#### Scenario: Operator designation or account state changes
- **WHEN** the actor's database role loses permission, the actor is suspended or their session is revoked before retry admission commits
- **THEN** current server authority rejects the command despite cached capabilities or an earlier token
- **AND** no retry is accepted under stale authority; deployment designation is not a fallback.

#### Scenario: Spoofed fields or personal ownership
- **WHEN** a caller alters browser flags or supplies actor, role or owner fields to an operations command
- **THEN** those fields cannot grant permission or change the trusted actor/owner
- **WHEN** an administrator calls another user's owner-only source replacement endpoint
- **THEN** operations permission does not satisfy ownership.
