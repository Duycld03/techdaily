# Spec Delta

## MODIFIED Requirements

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
