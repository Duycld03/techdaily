# Auth Spec Delta

## ADDED Requirements

### Requirement: Server-verified document operations capability
The admin capability response SHALL add `canManageDocumentOperations` alongside existing catalog, account and audit flags. Operator-designated active administrators SHALL initially receive it; learners SHALL receive false. Document-operations APIs SHALL independently enforce this permission and current account/session authority, returning 401 for invalid authentication and 403 for denied permission. Client fields SHALL NOT grant access.

#### Scenario: Capability discovery remains compatible
- **WHEN** an authenticated client reads `/api/v1/admin/capabilities`
- **THEN** it receives the current document-operations flag alongside existing destination flags
- **AND** `/api/v1/catalog/capabilities` retains its existing catalog-management response contract.

#### Scenario: Direct unauthorized operations call
- **WHEN** a caller without a valid session requests inventory, history or retry
- **THEN** the response is 401 without protected metadata.
- **WHEN** an authenticated learner or account without operations permission makes that request
- **THEN** the response is 403 without protected metadata or processing side effects.

#### Scenario: Catalog mutation needs catalog authority too
- **WHEN** an account has document-operations permission but lacks catalog-management permission
- **THEN** catalog source retry returns 403 without changing catalog content or dispatching work
- **AND** metadata-only operations do not become a catalog-editing grant.

#### Scenario: Operator designation or account state changes
- **WHEN** permission is removed, the actor is suspended or their session is revoked before a retry commits
- **THEN** current server authority rejects the command despite cached capabilities or an unexpired earlier token
- **AND** no retry is accepted under stale authority.

#### Scenario: Spoofed fields or personal ownership
- **WHEN** a caller alters browser flags or supplies actor, role or owner fields to an operations command
- **THEN** those fields cannot grant permission or change the trusted actor/owner.
- **WHEN** an administrator calls another user's owner-only source replacement endpoint
- **THEN** operations permission does not satisfy ownership.

### Requirement: Private operations admission and caching
Protected operations routes SHALL confirm their destination capability before fetching or rendering data on server or client. Operations, source-recovery and capability responses SHALL use `Cache-Control: no-store`. Definitive denial SHALL clear pending privileged work; transient authority failure SHALL return unavailable feedback without approving access or destroying valid credentials.

#### Scenario: Anonymous deep link or SSR renewal
- **WHEN** a guest opens `/admin/documents` or a session requires client-side renewal before admission
- **THEN** protected metadata is absent from SSR output and hydration until admission succeeds
- **AND** existing area-aware login/refresh behavior is retained.

#### Scenario: Authority temporarily unavailable
- **WHEN** capability or account authority cannot be verified
- **THEN** protected content remains behind the neutral retry state
- **AND** a valid learner/admin credential is not cleared merely because of that transient failure.

#### Scenario: Shared or browser cache
- **WHEN** an operations, capability or source-recovery response is returned, including rejection
- **THEN** it is marked no-store and cannot be reused as protected data or permission evidence for another session.
