# role-authorization Specification

## Purpose

Provide explicit database-backed role membership and least-privilege administrative grants, with protected assignment and a controlled transition from deployment allowlists.

## Requirements

### Requirement: Persistent single-role membership
Every account SHALL have exactly one persistent role identity, separate from its learning goal. Roles SHALL have stable identifiers, immutable keys and English/Vietnamese names. Initial roles SHALL be Learner and protected Admin. Role definitions and grants SHALL be read-only through product APIs; this change SHALL NOT expose role creation, deletion, multiple memberships or per-user grants.

#### Scenario: Role metadata is distinct from a learning goal
- **WHEN** an account changes its target engineering role or other learning preferences
- **THEN** its authorization role and permission grants remain unchanged.

#### Scenario: Role names do not authorize actions
- **WHEN** an account holds a role with a name resembling Admin but lacks an action's grant
- **THEN** the action remains forbidden.

#### Scenario: A new role uses the same membership contract
- **GIVEN** a role and supported grants introduced through a controlled database change
- **WHEN** an authorized operator assigns that role through the supported account command
- **THEN** the same membership and capability contracts apply without a new user-schema field or role-name switch.

### Requirement: Code-defined administrative permission grants
Only supported action keys SHALL grant permission. Initial keys SHALL be ViewAdminDashboard, ManageCatalog, ManageAccounts, ReadAudit, ManageDocumentOperations and ManageRoles. Learner SHALL have no administrative grants; protected Admin SHALL receive these six explicit grants. Unknown keys and wildcard-like values SHALL grant no action. New permissions SHALL NOT automatically propagate to other roles.

#### Scenario: Catalog-only grant does not expose accounts or metrics
- **WHEN** a role has ManageCatalog but lacks ViewAdminDashboard, ManageAccounts and ReadAudit
- **THEN** catalog actions are available while dashboard, account and audit APIs return 403.

#### Scenario: Unsupported grant cannot bypass a policy
- **WHEN** a role contains an unknown key, a differently cased key or a wildcard-like grant
- **THEN** that value authorizes no supported action and is reported through safe operational diagnostics.

#### Scenario: Later category permission is independent
- **WHEN** the category change introduces ManageCategories
- **THEN** its controlled migration explicitly grants it to protected Admin
- **AND** other roles do not acquire category administration merely by having another administrative grant.

### Requirement: Least-privilege account provisioning
Email registration and first-time Google provisioning SHALL assign Learner on the server. Existing-account sign-in, OAuth linkage, password recovery, profile updates and refresh SHALL preserve membership. Public account requests SHALL NOT bind role or grant fields. Failure to resolve the required default role SHALL fail account creation rather than choose another role or create a partially provisioned account.

#### Scenario: Registration attempts role injection
- **WHEN** a visitor supplies Admin, a role identifier or permission fields during registration and later verifies the OTP
- **THEN** the created account has Learner membership and no administrative grant.

#### Scenario: Google links an existing administrator
- **WHEN** Google sign-in links to an eligible existing account
- **THEN** its database role is preserved rather than reset to Learner or inferred from provider claims.

#### Scenario: Default role is unavailable
- **WHEN** provisioning cannot resolve the valid Learner role
- **THEN** no new account or authenticated session is committed and unavailable feedback is returned.

#### Scenario: Startup does not provision an alternate identity
- **WHEN** the production service starts or restarts
- **THEN** it neither creates a QA/default account nor resets account credentials or membership from seeder configuration
- **AND** test fixtures cannot implicitly grant production Admin authority.

### Requirement: Current database authorization authority
Privileged requests SHALL use the current active, non-deleted account and role grants. Browser metadata, access-token role labels and deployment allowlists SHALL NOT authorize requests. Invalid authentication SHALL return 401; verified insufficient grants SHALL return 403; unavailable authority SHALL return 503 without protected data or mutation. Mutation admission SHALL recheck current authority before commit.

#### Scenario: Cached administrator metadata is forged
- **WHEN** a Learner changes stored user metadata or presents a role label not supported by database grants
- **THEN** privileged APIs remain forbidden.

#### Scenario: Permission is removed before a command commits
- **WHEN** a competing protected command changes the actor's role or suspends the actor before mutation admission
- **THEN** the stale command cannot commit under its previous authority.

#### Scenario: Role authority cannot be verified
- **WHEN** database authority is temporarily unavailable
- **THEN** the response is 503 without protected data
- **AND** the browser retains valid credentials and offers retry rather than treating uncertainty as approval or a terminal logout.

### Requirement: Bounded role option discovery
GET /api/v1/admin/roles SHALL require both ManageAccounts and ManageRoles. It SHALL return stable-ID role descriptors and supported permission keys in bounded searchable pages, excluding deleted roles. Other authenticated users SHALL not obtain this administrative list. Own-account responses SHALL expose only that account's role descriptor; role metadata SHALL never act as permission evidence.

#### Scenario: Administrator loads assignment options
- **WHEN** a permitted administrator searches the role list with a valid page request
- **THEN** matching roles, pagination metadata and supported grants are returned with no-store caching
- **AND** no user list, credentials or private learning content is included.

#### Scenario: Account administrator lacks role-management authority
- **WHEN** an account administrator without ManageRoles requests role options
- **THEN** the response is 403 even though ordinary account inspection remains available.

### Requirement: Protected user-role assignment
PUT /api/v1/admin/users/{userId}/role SHALL require ManageAccounts and ManageRoles, a valid role ID, a trimmed reason of 1–500 characters, and the inspected account version. It SHALL reject self-role changes, missing/deleted targets or roles, stale versions and unchanged assignments. Success SHALL atomically change membership, advance account/session versions, revoke all target refresh families and persist attributed history.

#### Scenario: Administrator promotes another active account
- **WHEN** a permitted administrator assigns Admin to another eligible Learner with a valid reason and current version
- **THEN** the new role and audit event commit together, the updated account is returned, and all pre-change target sessions are invalid.

#### Scenario: Ordinary account control does not permit promotion
- **WHEN** an actor has ManageAccounts but lacks ManageRoles and submits a role change directly
- **THEN** the response is 403 and membership, sessions and account version are unchanged.

#### Scenario: Role management alone does not disclose accounts
- **WHEN** a role has ManageRoles but lacks ManageAccounts
- **THEN** account inspection, role options and role assignment remain forbidden.

#### Scenario: Actor attempts self-role change
- **WHEN** an administrator submits a role change targeting their own account
- **THEN** a protection conflict is returned without changing their role, sessions or account version.

#### Scenario: Repeated assignment cannot revoke later sessions
- **WHEN** a successful role command is repeated with its old version, or the current role is submitted unchanged
- **THEN** a conflict is returned without another membership change, version increment or session revocation.

#### Scenario: Suspended target remains suspended
- **WHEN** an authorized role change targets a Suspended account
- **THEN** its membership changes but its status, suspension metadata and inability to sign in remain unchanged.

### Requirement: One-time operator-controlled authorization initialization
An explicit operator command SHALL initialize database authority from validated existing account IDs and persist a completion receipt. It SHALL require at least one active, non-deleted protected Admin, preserve account status and learning data, and reject invalid or missing identities without partial elevation. Startup SHALL NOT import or restore privileges. After completion, configuration SHALL never participate in runtime authorization. Required readiness SHALL include readable receipt schema and every required exact-case protected grant, not a grant count. Invalid required readiness SHALL prevent web/worker host startup with a nonzero outcome. Successful receipt lookup with no receipt SHALL instead permit Learner use and return 503 for authenticated administrative access before insufficient-grant evaluation; invalid authentication SHALL remain 401.

#### Scenario: Existing designation is imported once
- **WHEN** the operator explicitly imports a valid legacy designation during the coordinated cutover
- **THEN** designated non-deleted accounts receive protected Admin, other accounts retain Learner, changed memberships invalidate old sessions, and completion is recorded atomically.

#### Scenario: Designated account is suspended
- **WHEN** the import includes a Suspended, non-deleted account and at least one other designated active account
- **THEN** its intended Admin membership is retained but suspension still blocks authentication
- **AND** it does not count toward the active-administrator minimum.

#### Scenario: Initialization would lock out administration
- **WHEN** all proposed administrators are missing, deleted or suspended, or any supplied identity is invalid
- **THEN** initialization fails without committing elevated memberships or a completion receipt.

#### Scenario: Restart or leftover configuration cannot promote users
- **WHEN** initialization has completed and the service restarts with a changed legacy allowlist
- **THEN** persisted roles remain unchanged and the explicit initialization command refuses a second import.

#### Scenario: Fresh deployment has no verified account yet
- **WHEN** a fresh database has role schema but initialization is incomplete
- **THEN** the system permits normal registration and sign-in to establish real Learner accounts
- **AND** privileged administration remains unavailable until the operator explicitly initializes eligible identities; no default account or bypass is created.

#### Scenario: Required authorization schema is missing
- **WHEN** required role schema or protected seed integrity cannot be established
- **THEN** API and worker readiness fail before admitting work rather than continuing after a migration warning.

#### Scenario: Valid schema is awaiting explicit initialization
- **WHEN** a real Learner signs in before the initialization receipt exists
- **THEN** learner operations remain available but capability discovery and privileged APIs return 503 without protected data
- **AND** the browser retains credentials behind the administration retry boundary.

#### Scenario: Initialization receipt cannot be read
- **WHEN** the initialization receipt table is missing or its lookup fails rather than successfully returning no receipt
- **THEN** required readiness fails instead of reporting a healthy uninitialized deployment
- **AND** startup fails, or an already-running host reports unavailable readiness and refuses affected work, without inventing a receipt or elevating an account.

#### Scenario: An equal grant count hides a missing required permission
- **WHEN** protected Admin lacks a required exact-case permission such as ManageRoles but additional category, unsupported or differently cased rows keep the total grant count at or above the former threshold
- **THEN** protected seed readiness fails
- **AND** additional grants cannot substitute for a missing required permission or cause automatic privilege repair.

#### Scenario: Valid additional category grant remains supported
- **WHEN** protected Admin holds every required installed grant, including its explicitly migrated category permission
- **THEN** readiness succeeds without requiring the grant count to equal the original six
- **AND** other roles receive no additional permission from this readiness check.

#### Scenario: Required authority is temporarily unavailable at startup
- **WHEN** a web/worker host cannot establish required authorization readiness during startup
- **THEN** startup terminates unsuccessfully before accepting HTTP or ingestion work
- **AND** a later database recovery does not leave a healthy admitted host with a permanently stopped ingestion consumer; a successful restart is required.

#### Scenario: Authenticated permission denial cannot hide pending initialization
- **GIVEN** required schema and seeds are valid but no initialization receipt exists
- **WHEN** a valid Learner directly requests administrative role or account APIs, or administrative/catalog capability discovery
- **THEN** the response is 503 rather than 403 or an all-false capability success
- **AND** no protected handler runs and the response is no-store.

#### Scenario: Initialization availability does not replace authentication or grants
- **WHEN** a guest or invalid-session caller requests a protected administrative endpoint on a valid uninitialized deployment
- **THEN** the response remains 401 without disclosing administrative data.
- **WHEN** initialization is complete and a valid account lacks the required permission
- **THEN** the response is 403 without protected handler execution.
