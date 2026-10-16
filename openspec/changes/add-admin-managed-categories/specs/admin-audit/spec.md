# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Atomic mutation and audit persistence
A committed account (including role assignment), catalog or category mutation SHALL persist its successful/accepted event in the same transaction. Audit failure SHALL prevent mutation commit. Rejection history SHALL not resave failed category mutations and SHALL be recorded only after the failed command is rolled back and released. Database or rejection-audit failure SHALL return unavailable without claiming success or durable history.

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
