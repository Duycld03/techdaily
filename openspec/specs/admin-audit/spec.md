# Admin Audit Specification

## Purpose

Provide durable, attributable administrative history for account access control and catalog changes, with bounded read-only inspection that preserves user privacy.

## Requirements

### Requirement: Server-attributed administrative events
Administrative events SHALL contain an immutable event identifier, server-derived actor identifier, action, target type/identifier, server UTC time, outcome and correlation identifier. Events SHALL include the account-action reason and safe error code when applicable. A client SHALL NOT choose the recorded actor, timestamp or outcome.

#### Scenario: Successful account action
- **WHEN** an administrator suspends an account successfully
- **THEN** history identifies the actual authenticated actor, target account, suspension action, reason, UTC time and Succeeded outcome.

#### Scenario: Spoofed attribution
- **WHEN** an administrator submits another actor identifier or a fabricated outcome with a command
- **THEN** those fields do not control the persisted event
- **AND** attribution uses the server's verified request identity and actual result.

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

### Requirement: Catalog action history coverage
History SHALL record catalog draft upload/import, metadata editing, explicit preparation/retry, review, publication and hiding. Asynchronous requests SHALL be recorded as Accepted rather than completed processing. Reads, previews and automatic worker updates SHALL NOT be presented as new administrator mutations. Existing catalog lifecycle rules SHALL continue to apply.

#### Scenario: Publish and hide a catalog document
- **WHEN** administrators publish and later hide the same entry
- **THEN** history retains both events with their respective actor, target, time and outcome
- **AND** existing catalog actor fields do not replace the historical records.

#### Scenario: Preparation is accepted
- **WHEN** an administrator explicitly queues preparation or retry
- **THEN** an Accepted event identifies the document and processing generation
- **AND** the event does not assert that extraction or AI processing has finished.

#### Scenario: View an existing document
- **WHEN** an administrator opens document details or previews a URL without submitting a mutation
- **THEN** history does not invent a catalog change or trigger processing.

#### Scenario: Publication is rejected
- **WHEN** an authorized administrator attempts publication without satisfying catalog requirements
- **THEN** history records a Rejected result with a safe catalog error code
- **AND** the entry remains unpublished.

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

### Requirement: Append-only history
Administrative history SHALL be append-only through application interfaces. No administrator endpoint or control SHALL edit or delete events. Account suspension, soft deletion or later identity changes SHALL NOT remove earlier events or prevent inspection of their stored actor and target identifiers.

#### Scenario: Attempt history modification
- **WHEN** an administrator attempts to update or delete an event through an application API
- **THEN** no supported operation modifies or deletes it.

#### Scenario: Actor is no longer available
- **WHEN** an event's actor or target is later soft-deleted or renamed
- **THEN** the event remains readable with its original identifiers and recorded action/time
- **AND** the interface handles an unavailable current identity without hiding the event.

### Requirement: Allowlisted audit payloads
Events SHALL retain only approved identifiers, action/result codes, bounded reasons, changed-field names and necessary access/catalog/category state values. Credentials, hashes, cookies, raw request/response bodies, document text, private learning records, provider payloads and permission-evidence text SHALL NOT be copied into events or audit responses. Historical payloads SHALL remain append-only through the category migration.

#### Scenario: Inspect a catalog metadata event
- **WHEN** an administrator opens a catalog metadata event
- **THEN** approved changed-field names and relevant revision/state values appear
- **AND** source content, permission evidence and serialized document DTOs are absent.

#### Scenario: Inspect a session revocation event
- **WHEN** an administrator opens an all-device sign-out event
- **THEN** it identifies the account/result without tokens, token hashes or password material.

#### Scenario: Unexpected error contains sensitive data
- **WHEN** an exception contains credentials or source content
- **THEN** history stores a safe error code, not the exception/provider message.

#### Scenario: Inspect category mutation payload
- **WHEN** an administrator opens a category event
- **THEN** it exposes only category UUID, approved changed-field names, previous/resulting active state, and version/order values needed to explain the action
- **AND** it excludes raw form bodies, private usage lists, source excerpts and client-supplied actor/timestamp values.

### Requirement: Bounded audit queries
Authorized administrators SHALL browse bounded audit pages and filter by actor identifier, action, target type/identifier, outcome and UTC time range. Results SHALL use a stable newest-first timestamp/identifier ordering. Time boundaries SHALL be explicit, and invalid ranges or unsupported filters SHALL be rejected.

#### Scenario: Filter account history
- **WHEN** an administrator filters for a target account and suspension actions in a time range
- **THEN** only matching events are returned with pagination metadata.

#### Scenario: Events share a timestamp
- **WHEN** several events have the same UTC timestamp
- **THEN** their identifiers break the ordering tie so page boundaries are deterministic.

#### Scenario: Invalid query
- **WHEN** a query has an inverted time range, unsupported action or excessive page size
- **THEN** the server returns a validation error without performing an unbounded scan.

### Requirement: Read-only audit workspace states
The audit workspace SHALL provide localized loading, empty, failed and selected-event states with clear filtering and reset actions. Event details SHALL distinguish outcomes and display timestamps with an explicit timezone. Failed reads SHALL preserve filters and offer retry without mutation controls.

#### Scenario: No events match
- **WHEN** the applied filters produce no events
- **THEN** the workspace explains the empty state and offers filter reset without sample or fabricated events.

#### Scenario: Read fails temporarily
- **WHEN** an audit query fails due to a temporary service error
- **THEN** the active filters remain available, localized feedback offers retry, and no event is reported as newly successful.

#### Scenario: Inspect an accepted command
- **WHEN** an administrator selects an Accepted preparation event
- **THEN** event details explain the recorded command outcome separately from current document processing status.

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

### Requirement: Category action history coverage
History SHALL record category creation, metadata/order changes, activation, deactivation and deletion with server-derived attribution. Authorized validation, in-use and concurrency rejections SHALL be recorded as Rejected, never Succeeded. Category reads SHALL not create mutation events. Existing ReadAudit permission and bounded action/target filters SHALL govern inspection; ManageCategories alone SHALL not grant audit access.

#### Scenario: Administrator changes category lifecycle
- **WHEN** an administrator creates, renames, reorders, activates, deactivates or deletes a category
- **THEN** its committed action has one event identifying actor, category UUID, UTC time, outcome and correlation ID
- **AND** the event uses an allowlisted `Category.Create`, `Category.UpdateMetadata`, `Category.Reorder`, `Category.Activate`, `Category.Deactivate` or `Category.Delete` action rather than a catalog publication action.

#### Scenario: Category mutation is rejected
- **WHEN** an authorized mutation is rejected because of invalid metadata, a stale version or retained references
- **THEN** a Rejected event contains only the safe code, valid target identifier when available and bounded approved fields
- **AND** no successful mutation event or raw rejected request body is retained.

#### Scenario: Category history remains readable after deletion
- **WHEN** an unused category is deleted or its name changes
- **THEN** earlier events remain readable by audit-authorized users with their original identifiers/actions/times
- **AND** current metadata lookup failure does not hide history or require rewriting historical events.

#### Scenario: Category events use the existing audit workspace
- **WHEN** an audit-authorized administrator filters by a category action or target UUID
- **THEN** the bounded, deterministically ordered results and selected-event details show those events with localized actions/outcomes
- **AND** an account with category-management permission but without audit-read permission cannot inspect the log.
