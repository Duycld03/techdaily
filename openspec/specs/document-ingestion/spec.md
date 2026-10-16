# Document Ingestion Specification

## Purpose

Make accepted document imports recoverable and observable through durable attempts, bounded private source retention and safe processing across failures and restarts.

## Requirements

### Requirement: Durable initial-ingestion admission
Validated, explicitly submitted PDF, web and Markdown imports SHALL receive a stable document and attempt identity before source processing starts. Accepted work and its dispatch obligation SHALL persist together. Successful synchronous import and asynchronous PDF response contracts SHALL remain compatible; post-admission failures SHALL identify the persisted document and attempt without exposing source content.

#### Scenario: Process exits after acceptance
- **WHEN** an accepted PDF import commits but the process exits before worker delivery
- **THEN** another worker discovers the same queued attempt from persisted state
- **AND** the accepted document is not silently abandoned or created again.

#### Scenario: Synchronous Markdown processing fails
- **WHEN** a valid, admitted Markdown import fails during slicing or output persistence
- **THEN** its document and Failed attempt remain identifiable for owner status and authorized operations
- **AND** a successful synchronous import continues to return its existing created-book response.

#### Scenario: Preview or invalid request
- **WHEN** a user previews a URL without confirming import, or input validation rejects a submission
- **THEN** no runnable ingestion attempt is admitted
- **AND** rejected source bytes and preview contents are not retained as failed-import history.

### Requirement: Persisted attempt lifecycle
Each attempt SHALL record origin, target scope/ID, source reference, processing stage, state, UTC timing, safe error and generation. States SHALL distinguish Queued, Running, Succeeded, Failed, Interrupted and Cancelled. A retry SHALL create a distinct attempt linked to its predecessor. Existing document readiness and catalog publication criteria SHALL remain authoritative.

#### Scenario: Attempt changes state
- **WHEN** accepted work is claimed, advances stages and reaches a terminal outcome
- **THEN** history exposes its actual state transitions and timing rather than inferred completion from a document count.

#### Scenario: Ready without complete enrichment
- **WHEN** existing personal import behavior makes a book readable despite an optional enrichment failure
- **THEN** the book remains Ready under that behavior
- **AND** no source retry is inferred from missing AI, embedding or audio output.

#### Scenario: Catalog initial formatting fails
- **WHEN** source slices are readable but the catalog's required initial formatting fails
- **THEN** the existing catalog preparation failure remains observable and publication remains blocked
- **AND** operations distinguish that failure from a missing or failed source extraction.

### Requirement: Stable accepted source snapshots
Retryable input SHALL be the validated PDF bytes or accepted UTF-8 Markdown snapshot used for initial ingestion, with length, integrity identity and source language. Web snapshots SHALL reflect the content explicitly accepted for import. Retrying an available source SHALL NOT silently recrawl, redownload or change source revision. Missing pre-fetch inputs SHALL require explicit source resupply. Declared language SHALL follow an explicit owner correction.

#### Scenario: Web article changes after failure
- **WHEN** an administrator retries an available accepted web snapshot after the remote article changes
- **THEN** processing uses the originally accepted snapshot and source language.

#### Scenario: Remote download fails before a snapshot exists
- **WHEN** an admitted URL import fails before obtaining valid source bytes
- **THEN** history records the fetch failure and source availability as Missing
- **AND** recovery requires an explicit, validated source replacement rather than an automatic remote retry.

#### Scenario: Stored source is corrupted
- **WHEN** stored bytes no longer match the recorded integrity identity
- **THEN** processing stops with a safe source-integrity error and does not commit output from those bytes.

#### Scenario: Owner explicitly corrects source language
- **WHEN** an owner changes the failed document's declared language before a newly confirmed recovery attempt
- **THEN** the new attempt uses that explicit language choice while source bytes remain unchanged
- **AND** an interface-locale change alone cannot change source language.

### Requirement: Backend-only source storage
Retained sources SHALL be accessible only to authorized backend processing and cleanup. Browser APIs SHALL accept validated inputs or opaque server-managed identities, never arbitrary storage locations. Metadata, logs and audit SHALL exclude source bytes and storage paths. Retention SHALL grant neither administrators nor other users a source view/download permission.

#### Scenario: Source reference requested through an API
- **WHEN** a caller tries to obtain a retained source download or supply a server filesystem path
- **THEN** no source bytes are served and no caller-selected backend file is read.

#### Scenario: Persistent storage is unavailable
- **WHEN** configured source storage is missing, inaccessible or unsuitable for restart recovery
- **THEN** new source admission fails with 503 rather than claiming acceptance or silently using ephemeral storage.

### Requirement: Failed-source retention deadline
A validated source used by a failed or interrupted attempt SHALL expire no later than seven days after its first failure or interruption. Retrying the same source SHALL NOT extend that deadline. Successful processing SHALL immediately make the source unavailable and delete it. Expiry SHALL prevent further processing access and initiate deletion; missing and expired sources SHALL remain distinguishable in metadata.

#### Scenario: Repeated failures share one deadline
- **WHEN** the same source fails again after an explicit retry
- **THEN** its original expiry remains unchanged.

#### Scenario: Retry completes successfully
- **WHEN** source processing succeeds
- **THEN** retained input becomes unavailable immediately and is deleted without waiting for the seven-day deadline
- **AND** the document and safe attempt history remain available.

#### Scenario: Source expires during recovery
- **WHEN** the source reaches its deadline while a retry is queued or running
- **THEN** no further source access or claim renewal is allowed and expired input is deleted
- **AND** the attempt records a safe source-expired outcome without destructive output changes.

### Requirement: Global and per-account source quotas
Source storage SHALL enforce configurable positive global and per-account byte limits, including uploads in progress, queued/running inputs and failed retained sources. Concurrent admissions SHALL NOT exceed either limit. Capacity exhaustion SHALL produce an explicit recoverable error; existing nonexpired processing inputs SHALL NOT be evicted to admit another source. Retry of the same retained input SHALL NOT duplicate its quota charge.

#### Scenario: Concurrent uploads reach capacity
- **WHEN** two uploads compete for the last available global or owner capacity
- **THEN** only admissions fitting both limits succeed
- **AND** the other upload returns a quota error and its partial bytes are removed.

#### Scenario: Unknown content length
- **WHEN** a streamed remote source has no trustworthy content length
- **THEN** admission reserves or accounts for incoming bytes within the existing source-size boundary
- **AND** streaming stops before storage exceeds either quota.

#### Scenario: Quota is released after deletion
- **WHEN** retained source deletion completes after success, expiry or rejected staging
- **THEN** its charged bytes are released exactly once.
- **WHEN** physical deletion fails
- **THEN** charged capacity remains reserved until cleanup confirms deletion.

### Requirement: Single active attempt and stale-worker fencing
A target SHALL have at most one accepted active ingestion attempt. Claims and progress/output writes SHALL require the current attempt generation and an unexpired claim. Duplicate delivery, delayed callbacks and expired workers SHALL NOT alter newer target state or append duplicate output. Claim expiry SHALL NOT exceed a retained source's expiry deadline.

#### Scenario: Two workers receive one attempt
- **WHEN** concurrent workers attempt to claim the same queued work
- **THEN** only one obtains processing authority and the other starts no source processing.

#### Scenario: Old worker returns after retry
- **WHEN** an older generation submits progress, failure or output after another attempt was accepted
- **THEN** its write is discarded without changing the newer attempt or document.

#### Scenario: Metadata changes after confirmation
- **WHEN** source-relevant target metadata or source availability changes after a retry form was loaded
- **THEN** its control version becomes stale and the server requires a fresh eligibility check.

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

### Requirement: Atomic safe output completion
Processing SHALL commit source-derived output and terminal attempt state atomically after checking the target's current generation, existence and output safety. Failed replacement SHALL preserve prior output. Ready personal documents, published catalog entries and learning-referenced personal output SHALL NOT be destructively replaced. Retry SHALL preserve source language and original-text preservation rules.

#### Scenario: Output transaction fails
- **WHEN** processing prepares new slices but completion persistence fails
- **THEN** neither a partial replacement nor a false Succeeded outcome is committed.

#### Scenario: Owner deletes the target while processing
- **WHEN** deletion commits before output completion
- **THEN** the attempt cannot restore the document or its chunks
- **AND** processing authority is invalidated and retained input is removed safely.

#### Scenario: Publication or learning activity races with completion
- **WHEN** the catalog is published or personal learning activity makes replacement unsafe before completion
- **THEN** completion refuses the replacement and preserves existing content and learning data.

### Requirement: Source cleanup and legacy recovery
Cleanup SHALL remove expired sources, invalid/uncommitted staging, successful inputs and inputs for deleted targets without deleting documents or safe attempt history. It SHALL respect current processing authority and reconcile abandoned reservations after restarts. Legacy documents without recoverable inputs SHALL remain inspectable, with unavailable history/source and blocked retry rather than fabricated recoverability.

#### Scenario: Abandoned upload
- **WHEN** a process stops while staging an upload that never becomes an accepted source
- **THEN** reconciliation removes its orphaned bytes and releases its reservation safely.

#### Scenario: Expired source after downtime
- **WHEN** processing starts after downtime with expired retained sources
- **THEN** those sources are made inaccessible and cleanup is initiated before any worker can claim them.

#### Scenario: Legacy failure without original file
- **WHEN** a pre-migration failed document has no validated retained source
- **THEN** operations shows unavailable source/history and requests authorized source resupply
- **AND** migration does not invent a historical successful or failed attempt.

### Requirement: Sanitized operational failures
Attempt errors SHALL use stable, bounded codes and safe stage information for validation, fetch, extraction, persistence, interruption, source availability, quota and concurrency failures. Responses SHALL preserve the existing structured problem-details contract. Processing and audit output SHALL NOT expose provider exceptions, private content, credentials or internal storage references.

#### Scenario: Extraction exception contains source text
- **WHEN** extraction throws an exception containing an excerpt or storage location
- **THEN** attempt history and problem details expose an approved extraction-failed code only
- **AND** English and Vietnamese UI resolve localized guidance from that code.

#### Scenario: Database outage during terminal persistence
- **WHEN** persistence cannot record a terminal transition
- **THEN** no successful completion is reported
- **AND** recovery later reconciles the expired claim without trusting an uncommitted in-memory outcome.
