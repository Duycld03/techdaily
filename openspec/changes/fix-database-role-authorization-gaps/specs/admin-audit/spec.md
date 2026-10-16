# Spec Delta

## MODIFIED Requirements

### Requirement: Account action history coverage
The system SHALL record suspension, reactivation, all-device sign-out and role assignment, including success and recoverable validation, protection or concurrency rejection from authorized administrators. Rejected SHALL remain distinct from Succeeded and SHALL NOT expose sensitive failure details. Role history SHALL use the attributed User.AssignRole action rather than a catalog action. Rejection eligibility SHALL require current actor, presented-session and permission admission; authorized role-command rejection SHALL NOT disappear merely because validation or a lookup fails before mutation.

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

#### Scenario: Authorized role validation fails before target mutation
- **WHEN** a currently admitted role manager supplies a nonpositive expected version, missing destination-role ID or invalid reason
- **THEN** the command returns its existing safe validation code and records one eligible User.AssignRole Rejected event
- **AND** invalid version input is not reclassified as a stale-version conflict, and invalid, overlong or sensitive reasons are omitted rather than copied or truncated into history.

#### Scenario: Authorized role lookup or protection fails
- **WHEN** a currently admitted role manager encounters a missing/deleted target or destination role, self-role protection, a stale version, an unchanged assignment or last-admin protection
- **THEN** the distinct existing safe result is retained and one attributable User.AssignRole Rejected event is durably recorded
- **AND** valid target identifiers and approved transition metadata are retained only when known, without membership or session mutation.

#### Scenario: Actor admission fails before role-command eligibility
- **WHEN** the presented actor is missing, inactive, session-stale or lacks the required current grants
- **THEN** the request cannot mutate the target or create an event claiming an authorized role manager submitted a rejected command
- **AND** unavailable authority remains an availability failure rather than an invented validation or concurrency rejection.

### Requirement: Atomic mutation and audit persistence
A committed account (including role assignment), catalog or category mutation SHALL persist its successful/accepted event in the same transaction. Audit failure SHALL prevent mutation commit. Rejection history SHALL not resave failed account, role, catalog or category mutations and SHALL be recorded independently only after the failed command is rolled back and its transaction disposed. Failed mutation tracking or persistence context SHALL NOT be reused for rejection recording. Database or rejection-audit failure SHALL return unavailable without claiming success or durable history.

#### Scenario: Audit write fails during suspension
- **WHEN** audit persistence fails while applying suspension
- **THEN** account status, session revocation and the successful event all roll back
- **AND** the response does not report suspension success.

#### Scenario: Catalog mutation rolls back
- **WHEN** a catalog metadata/publication transaction fails before commit
- **THEN** neither the change nor its Succeeded event remains committed.

#### Scenario: Database is unavailable
- **WHEN** an administrative command cannot persist because the database is unavailable
- **THEN** the caller receives an unavailable/failure response without fabricated events or success notifications.

#### Scenario: Category audit cannot commit
- **WHEN** audit persistence fails during a category create/update/status/delete transaction
- **THEN** the category fields, version and successful event all roll back
- **AND** HTTP 503 is returned and the client preserves the unsaved draft.

#### Scenario: Category rejection follows a staged mutation
- **WHEN** a category command stages metadata/version changes and then fails, including a unique-slug database conflict
- **THEN** the failed changes and successful event roll back before only the eligible Rejected event is independently persisted
- **AND** no nested failed-command transaction or retained tracked mutation is reused.

#### Scenario: Category rejection history is unavailable
- **WHEN** independent rejection recording fails
- **THEN** HTTP 503 and safe correlated feedback are returned without changing category state
- **AND** the response does not claim the Rejected event was saved.

#### Scenario: Ineligible or stale publication is rejected after actor admission
- **WHEN** a permitted publication command is rejected for unmet publication requirements or a stale document revision
- **THEN** the failed publication transaction is rolled back and disposed before its eligible Rejected event is independently persisted
- **AND** the original safe catalog error is returned with unchanged visibility, revision and document state, without retaining a Succeeded event.

#### Scenario: Rejection recording fails after role or publication rollback
- **WHEN** an eligible rejected role or publication command cannot persist its independent rejection event
- **THEN** the response is 503 with safe correlated feedback rather than a durable-rejection or success claim
- **AND** no failed mutation or successful event is resaved, and exception/provider details are not exposed to the caller or copied into audit history.
