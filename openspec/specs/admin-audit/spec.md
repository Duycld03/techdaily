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
The system SHALL record suspension, reactivation and all-device sign-out, including successful results and recoverable validation, protection or concurrency rejections from authorized administrators. Recording SHALL distinguish Rejected from Succeeded and SHALL NOT expose sensitive failure details.

#### Scenario: Reactivation follows suspension
- **WHEN** an administrator suspends an account and later reactivates it
- **THEN** both actions remain separately inspectable with their actors, times and reasons.

#### Scenario: Protection or concurrency rejects an action
- **WHEN** an authorized account action is rejected for self-suspension or a stale version
- **THEN** a Rejected event contains the safe reason code and target identifier
- **AND** it does not claim that status or sessions changed.

#### Scenario: Validation rejects sensitive or excessive input
- **WHEN** an account action fails input validation
- **THEN** the rejection contains only bounded, approved fields and a validation code
- **AND** invalid request bodies and overlong reason text are not copied into history.

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
A committed account or catalog mutation SHALL persist its successful or accepted audit event in the same transaction. Audit persistence failure SHALL prevent that mutation from committing. Rejected actions SHALL NOT persist a successful event. Database outages SHALL return failure without claiming an action or its audit record succeeded.

#### Scenario: Audit write fails during suspension
- **WHEN** audit persistence fails while applying suspension
- **THEN** account status, session revocation and the successful event all roll back
- **AND** the response does not report suspension success.

#### Scenario: Catalog mutation rolls back
- **WHEN** a catalog metadata or publication transaction fails before commit
- **THEN** neither the catalog change nor a Succeeded event for that change remains committed.

#### Scenario: Database is unavailable
- **WHEN** an administrative command cannot persist because the database is unavailable
- **THEN** the caller receives an unavailable/failure response without fabricated audit entries or a success notification.

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
Events SHALL retain only approved identifiers, action/result codes, bounded administrative reasons, changed-field names and necessary access/catalog state values. Credentials, hashes, cookies, raw request/response bodies, document text, private learning records, provider payloads and permission-evidence text SHALL NOT be copied into events or audit responses.

#### Scenario: Inspect a catalog metadata event
- **WHEN** an administrator opens the event for a metadata update
- **THEN** it shows approved changed-field names and relevant revision/state information
- **AND** it omits source content, permission-evidence text and a serialized document DTO.

#### Scenario: Inspect a session revocation event
- **WHEN** an administrator opens an all-device sign-out event
- **THEN** it identifies the account and result without exposing access tokens, refresh-token values/hashes or password material.

#### Scenario: Unexpected error contains sensitive data
- **WHEN** an operation fails with an exception containing credentials or source content
- **THEN** the persisted outcome uses a safe error code instead of copying the exception or provider message.

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
