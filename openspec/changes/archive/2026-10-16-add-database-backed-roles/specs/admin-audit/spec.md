# Spec Delta

## MODIFIED Requirements

### Requirement: Account action history coverage
The system SHALL record suspension, reactivation, all-device sign-out and role assignment, including success and recoverable validation, protection or concurrency rejection from authorized administrators. Rejected SHALL remain distinct from Succeeded and SHALL NOT expose sensitive failure details. Role history SHALL use the attributed User.AssignRole action rather than a catalog action.

#### Scenario: Reactivation follows suspension
- **WHEN** an administrator suspends an account and later reactivates it
- **THEN** both actions remain separately inspectable with their actors, times and reasons.

#### Scenario: Protection or concurrency rejects an action
- **WHEN** an authorized account action is rejected for self-suspension, self-role change, last-admin protection or a stale version
- **THEN** a Rejected event contains its safe code and target identifier
- **AND** it does not claim that status, membership or sessions changed.

#### Scenario: Validation rejects sensitive or excessive input
- **WHEN** an account action fails input validation
- **THEN** rejection history contains only bounded approved fields and a validation code
- **AND** invalid request bodies and overlong reasons are not copied into history.

#### Scenario: Administrator assigns a role
- **WHEN** a permitted administrator changes another account's role successfully
- **THEN** one User.AssignRole event identifies the verified actor, target account, previous/new role IDs, bounded reason, UTC time, result and correlation ID
- **AND** membership, account/session versions, refresh revocation and the event commit together.

## ADDED Requirements

### Requirement: Role audit privacy and inspection
Role events SHALL retain only approved identifiers, changed-field names, previous/resulting role IDs and necessary version values, in addition to existing safe attribution and bounded reasons. ReadAudit SHALL remain required to inspect them. ManageRoles SHALL NOT grant audit access, permit history editing, expose credentials or private learning content, or fabricate user attribution for operator bootstrap.

#### Scenario: Inspect a role assignment event
- **WHEN** an audit-authorized account opens a role assignment event or filters by its action/target
- **THEN** bounded results expose the approved transition and localized outcome without tokens, hashes, raw requests or account learning data.

#### Scenario: Role manager lacks audit permission
- **WHEN** an account has role-assignment authority but lacks ReadAudit
- **THEN** role assignment can succeed while audit queries remain forbidden.

#### Scenario: Bootstrap is an operator action
- **WHEN** the one-time operator command initializes authorization
- **THEN** its durable operational receipt identifies the import and completion without claiming that a fabricated application user performed it
- **AND** ordinary subsequent assignments use actual authenticated-user attribution.

### Requirement: Rejected access-control command isolation
Rejected account and role commands SHALL leave membership, account/session versions and refresh state unchanged, even if changes were staged before rejection. A Rejected event SHALL be persisted independently only after the failed transaction is rolled back and released. Rejection-audit failure SHALL return unavailable feedback without claiming history was saved; it SHALL NOT be silently swallowed.

#### Scenario: Rejection follows staged mutation
- **WHEN** an authorized command stages a role/version/session change and then fails a protection or validation check
- **THEN** no staged mutation or successful event survives
- **AND** only the independently persisted Rejected event remains.

#### Scenario: Transaction fails at the database boundary
- **WHEN** a role command fails during persistence and the failed transaction is rolled back
- **THEN** recording an eligible rejection cannot resave tracked mutations from that failed command or attempt a nested transaction on its failed connection.

#### Scenario: Rejection history cannot be saved
- **WHEN** the independent rejection write is unavailable
- **THEN** the command returns 503, safe correlated diagnostics identify the failure, and no success or durable-rejection claim is made
- **AND** account, membership and session state remain unchanged.
