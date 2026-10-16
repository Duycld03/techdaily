# Spec Delta

## MODIFIED Requirements

### Requirement: Server-verifiable session revocation
Protected API authentication SHALL verify access tokens against current server account/session state in addition to signature, issuer, audience and expiry. Suspension, administrator all-device sign-out and actual role changes SHALL invalidate previous access tokens and all refresh families after commit. Normal per-device logout SHALL retain its existing isolation. A rejected or unchanged-role command SHALL NOT invalidate sessions. Privileged command admission SHALL use the session version verified from the presented credentials, not an assumed initial version or a substituted current account version; a valid version greater than 1 SHALL remain usable with the required grants.

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

#### Scenario: Initialized administrator uses a noninitial session version
- **GIVEN** initialization or a later access-control transition has advanced an Active administrator's session version beyond 1
- **AND** the administrator signs in freshly with the permissions and eligible target required for each action
- **WHEN** the administrator publishes a catalog document, retries document ingestion or replaces a catalog PDF source
- **THEN** publication returns its existing successful 200 response and the recovery actions return their existing accepted 202 responses
- **AND** none is rejected merely because the verified presented session version is not 1.

#### Scenario: A stale command cannot adopt the account's newer version
- **WHEN** an already-admitted privileged command reaches final mutation admission after the actor's session version has advanced
- **THEN** it cannot commit protected changes or dispatch new work using the actor's newer database version
- **AND** a subsequent request with pre-change credentials fails authentication, while fresh sign-in is evaluated using its own presented version.

### Requirement: Server-enforced catalog administrator permission
Every catalog management operation SHALL require a valid authenticated identity and server-verified catalog administrator permission. Anonymous requests SHALL receive 401; authenticated non-administrators SHALL receive 403 when administrative authority is initialized and available. Authenticated administrative access with pending initialization or unavailable authority SHALL instead receive 503 before grant denial. Client state and client-supplied role fields SHALL NOT grant permission.

#### Scenario: Direct unauthorized management request
- **GIVEN** administrative authority is initialized and available
- **WHEN** a regular user calls draft creation, upload, preview, preparation, publication, hiding or editing endpoints directly
- **THEN** the server returns 403 without changing data or starting ingestion or AI jobs.

#### Scenario: Anonymous management request
- **WHEN** a caller without a valid JWT accesses an administrator endpoint
- **THEN** the server returns 401.

### Requirement: Server-enforced account and audit administration permission
Account administration SHALL require a valid authenticated identity and server-verified account-management permission. Reading administrative history SHALL require server-verified audit-read permission. Anonymous requests SHALL receive 401; authenticated callers without the required permission SHALL receive 403 when administrative authority is initialized and available. Pending initialization or unavailable authority SHALL instead return 503 before authenticated grant denial. Client role fields or cached capabilities SHALL NOT grant access.

#### Scenario: Learner calls account or audit APIs directly
- **GIVEN** administrative authority is initialized and available
- **WHEN** an authenticated learner requests account details, submits suspension or reads audit events without the required permission
- **THEN** the server returns 403 without disclosing administrative data or changing state.

#### Scenario: Guest calls account or audit APIs
- **WHEN** a caller without valid authentication requests account administration or audit history
- **THEN** the server returns 401.

### Requirement: Server-verified document operations capability
The capability response SHALL retain canManageDocumentOperations alongside existing destination flags, resolved from current database grants. Document-operations APIs SHALL independently enforce ManageDocumentOperations and current account/session authority. Invalid authentication SHALL return 401; pending initialization or unavailable authority SHALL return 503 before authenticated insufficient-permission 403. Catalog mutation SHALL additionally require ManageCatalog; ownership SHALL remain independent.

#### Scenario: Capability discovery remains compatible
- **GIVEN** administrative authority is initialized and available
- **WHEN** an authenticated client reads `/api/v1/admin/capabilities`
- **THEN** the current document-operations flag appears alongside other destination flags
- **AND** `/api/v1/catalog/capabilities` retains its existing catalog-management response contract.

#### Scenario: Direct unauthorized operations call
- **GIVEN** administrative authority is initialized and available
- **WHEN** a caller without a valid session requests inventory, history or retry
- **THEN** the response is 401 without protected metadata
- **WHEN** an authenticated account lacks operations permission
- **THEN** the response is 403 without metadata or processing side effects.

#### Scenario: Catalog mutation needs catalog authority too
- **GIVEN** administrative authority is initialized and available
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
