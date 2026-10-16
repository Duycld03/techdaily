# Spec Delta

## MODIFIED Requirements

### Requirement: Restart and interruption recovery
Persisted Queued work SHALL survive process restarts and be dispatched without creating a retry attempt. Only expired Running claims SHALL become Interrupted and lose processing authority. Live claims belonging to another worker SHALL be respected. Interrupted work SHALL require an explicit retry; the system SHALL NOT perform unbounded automatic failure retries. A host that cannot establish required authorization readiness at startup SHALL fail startup rather than advertise ready ingestion while its consumer has stopped. A successful restart SHALL retain the same queued identities and normal recovery rules.

#### Scenario: Worker restarts with queued work
- **WHEN** the worker starts and discovers accepted queued attempts
- **THEN** it resumes their delivery using existing attempt identities.

#### Scenario: Another instance remains active
- **WHEN** a restarting instance observes an unexpired claim held by another instance
- **THEN** it does not mark that attempt Failed or Interrupted.

#### Scenario: Expired running claim
- **WHEN** recovery observes an expired Running claim
- **THEN** it records Interrupted and invalidates further writes from that worker
- **AND** it exposes explicit retry or source replacement guidance according to source availability.

#### Scenario: Authorization readiness fails during startup
- **WHEN** required database or authorization readiness cannot be established while the host starts
- **THEN** the host terminates startup unsuccessfully instead of silently completing its PDF consumer and later accepting uploads
- **AND** no new ingestion acceptance is reported by that failed host.

#### Scenario: Database recovery is followed by a successful restart
- **GIVEN** startup failed because required readiness was unavailable and accepted queued attempts already exist
- **WHEN** the database becomes ready and the supervisor or operator restarts the host
- **THEN** the ingestion consumer starts and dispatches the previously queued attempts under their existing identities
- **AND** recovery creates no replacement retry attempts, duplicates or fabricated successful processing outcomes.

#### Scenario: Valid schema is ready before administrator initialization
- **WHEN** required schema and protected seeds are valid but authorization initialization has not yet completed
- **THEN** learner ingestion and its consumer can run under the existing learner authentication boundaries
- **AND** pending administrator initialization alone does not stop that consumer or implicitly initialize authority.
