# Authentication Delta

## ADDED Requirements

### Requirement: Server-enforced catalog administrator permission
Every catalog management operation SHALL require a valid authenticated identity and server-verified catalog administrator permission. Anonymous requests SHALL receive 401 and authenticated non-administrators SHALL receive 403. Client state and client-supplied role fields SHALL NOT grant permission.

#### Scenario: Direct unauthorized management request
- **WHEN** a regular user calls draft creation, upload, preview, preparation, publication, hiding or editing endpoints directly
- **THEN** the server returns 403 without changing data or starting ingestion or AI jobs.

#### Scenario: Anonymous management request
- **WHEN** a caller without a valid JWT accesses an administrator endpoint
- **THEN** the server returns 401.

### Requirement: Operator-controlled administrator designation
Catalog administrator access SHALL be assigned only by trusted deployment configuration referencing existing active user identifiers. An empty designation SHALL grant no administrator access. Registration, OAuth, profile updates and token refresh SHALL NOT let users choose or acquire administrator permission.

#### Scenario: No configured administrator
- **WHEN** no valid administrator identifiers are configured
- **THEN** all authenticated users are denied catalog management while published learner discovery remains usable.

#### Scenario: Administrator removal
- **WHEN** an operator removes a user from the designation and the updated configuration is loaded
- **THEN** that user's subsequent management requests return 403 even with an unexpired access token.

#### Scenario: Spoofed administrator field
- **WHEN** a regular user supplies administrator fields during registration or profile editing or alters browser storage
- **THEN** the user gains no management permission.

### Requirement: Administrator capability discovery and route protection
The server SHALL expose the current user's catalog management capability to support navigation. The admin page SHALL redirect anonymous visitors to login with the requested destination and show an access-denied state for authenticated non-administrators without loading draft data.

#### Scenario: Administrator opens management page
- **WHEN** a designated administrator opens `/admin/catalog`
- **THEN** the server confirms permission and the catalog workspace loads.

#### Scenario: Client capability becomes stale
- **WHEN** an administrator's access is revoked while their page is open
- **THEN** a denied request presents an access-denied state, clears privileged data and stops polling
- **AND** the browser does not treat its cached capability as authorization.
