# Curated Catalog Spec Delta

## MODIFIED Requirements

### Requirement: Explicit preparation and recovery
Administrators SHALL see processing progress and safe errors and explicitly initiate preparation or retry. Publication SHALL require all extracted slices to be readable and the first min(3, slice count) slices successfully formatted. Remaining slices SHALL retain readable source content for existing on-demand curation. Failed source ingestion SHALL use the controlled retry and retention contract; initial-formatting-only recovery SHALL remain a separate catalog preparation action, without granting access to personal content.

#### Scenario: Preparation fails
- **WHEN** initial formatting fails, including missing AI configuration
- **THEN** the draft remains unpublished and shows a recoverable safe error
- **AND** no fabricated fallback is treated as successful preparation.

#### Scenario: Retrying interrupted work
- **WHEN** an administrator retries failed or interrupted catalog source ingestion
- **THEN** the system prevents concurrent duplicate processing and duplicate slice orders through the current attempt and control version
- **AND** it uses the retained validated snapshot when available, or requests a validated replacement through the catalog source endpoint when input is missing or expired.

#### Scenario: Viewing a draft does not generate content
- **WHEN** an administrator opens a draft or changes filters
- **THEN** no new AI generation begins without an explicit preparation action.

#### Scenario: Readable slices need initial formatting only
- **WHEN** a catalog entry already has readable source slices but its required initial formatting is incomplete
- **THEN** the existing explicit catalog preparation action remains available under catalog permission and publication safeguards
- **AND** the document-operations workspace does not expose this as a standalone AI, embedding or audio retry.

#### Scenario: Published catalog entry targeted for source retry
- **WHEN** a source retry targets a published entry
- **THEN** the server returns a conflict without modifying source, slices or visibility
- **AND** catalog changes still require hiding and explicit review before republication.

## ADDED Requirements

### Requirement: Validated catalog source replacement
Catalog administrators SHALL replace missing or expired input for eligible Failed or Interrupted, nonpublished catalog source ingestion through validated source-specific APIs. Replacement SHALL require the current control version and replay protection, preserve target identity, metadata, language and attribution, enforce shared quotas, and record administrative acceptance. Client-supplied backend paths SHALL be rejected.

#### Scenario: Replace missing catalog PDF
- **WHEN** an authorized catalog administrator confirms a validated replacement PDF for an eligible draft
- **THEN** one durable recovery attempt is accepted under the existing catalog identifier
- **AND** adopted personal copies remain unchanged.

#### Scenario: Replace web snapshot
- **WHEN** an administrator explicitly confirms accepted replacement content for an eligible web-source failure
- **THEN** URL validation, sanitization, source-language and bounded-storage rules apply
- **AND** opening a preview alone starts no recovery.

#### Scenario: Legacy temporary-path input
- **WHEN** a client supplies `ReplacementTempFilePath` or another backend path to preparation or source replacement
- **THEN** the API returns 400 with safe migration guidance and reads no caller-selected server file
- **AND** the supported replacement flow accepts validated upload/import input rather than a filesystem location.

#### Scenario: Denied or conflicting replacement
- **WHEN** a learner, stale command or published/active target requests catalog source replacement
- **THEN** the request is rejected without source acceptance, mutation or worker dispatch.

### Requirement: Catalog ingestion history integration
Catalog draft admission and source recovery SHALL participate in persisted ingestion history and the existing administrative audit contract. Queued acceptance SHALL remain distinct from terminal processing outcome. Catalogue editing and publication SHALL continue to use their own authoritative concurrency and readiness checks, and source recovery SHALL never republish a document automatically.

#### Scenario: Catalog URL fails after admission
- **WHEN** an explicitly admitted catalog URL import fails during fetch or source slicing
- **THEN** its draft and safe failed attempt remain visible for recovery
- **AND** no publishable or learner-discoverable entry is fabricated.

#### Scenario: Source recovery succeeds
- **WHEN** a catalog source recovery reaches successful processing
- **THEN** its attempt and linked administrative completion are recorded
- **AND** visibility remains nonpublished until an authorized administrator explicitly publishes it.
