# admin-dashboard Specification

## Purpose

Give authorized administrators a factual overview of accounts, document inventory, processing state and service availability without exposing private learning content.

## Requirements

### Requirement: Administrator overview metrics
The dashboard SHALL show non-deleted account count, personal document count, catalog total with Draft/Published/Hidden breakdown, and Pending/Processing/Failed document counts separated by personal and catalog scope. Counts SHALL exclude soft-deleted records and include an observation timestamp. Personal copies SHALL count as personal documents, not additional catalog titles.

#### Scenario: Counts distinguish inventory scopes
- **GIVEN** one published catalog entry adopted by two users and one independently imported personal book
- **WHEN** an admin loads the dashboard
- **THEN** catalog total is one and personal document total is three
- **AND** labels make this distinction explicit.

#### Scenario: Empty and deleted records
- **WHEN** a scope contains no non-deleted records
- **THEN** its successful aggregate count is zero
- **AND** deleted accounts and documents do not inflate counts.

### Requirement: Restricted aggregate visibility
Dashboard overview, status and explicit AI-probe APIs SHALL require current server-verified ViewAdminDashboard, independently of catalog or other administrative grants. Responses SHALL expose aggregates and sanitized operational observations, not emails, document text, private notes, tokens or secrets. Invalid authentication SHALL receive 401, insufficient permission 403, and unavailable authority 503.

#### Scenario: Direct unauthorized request
- **WHEN** a Learner calls the overview API with a valid token
- **THEN** the response is 403 and contains no aggregate metrics.

#### Scenario: Catalog-only role cannot probe infrastructure
- **WHEN** an account has ManageCatalog but lacks ViewAdminDashboard and requests overview, status or an AI probe
- **THEN** the API returns 403 without metrics or operational details
- **AND** no external AI probe is dispatched.

#### Scenario: Dashboard-only role is admitted independently
- **WHEN** an account has ViewAdminDashboard but lacks ManageCatalog and document-operations permission
- **THEN** dashboard reads and an explicitly requested rate-limited probe are permitted
- **AND** catalog actions and document drill-downs remain unavailable.

#### Scenario: Dashboard authority is temporarily unavailable
- **WHEN** current role authority cannot be verified
- **THEN** the response is 503 rather than 403 or invented healthy/zero values
- **AND** no protected observation or external probe is produced.

### Requirement: Measured system status
The dashboard SHALL show API responsiveness, database connectivity and text-generation/embedding probe status with observation times. AI statuses SHALL be Not checked until an explicit probe succeeds or fails. Missing or stale observations SHALL NOT be presented as current healthy status; raw provider errors and secrets SHALL NOT be displayed.

#### Scenario: Initial dashboard load
- **WHEN** an administrator opens the dashboard
- **THEN** a bounded database check and overview load occur without external AI generation
- **AND** AI status shows the last explicitly measured result with its age, or Not checked.

#### Scenario: Explicit AI probe
- **WHEN** an administrator selects Check AI services
- **THEN** a protected rate-limited probe checks text generation and embeddings and returns sanitized statuses and measured latencies
- **AND** missing configuration, timeout or provider failure is shown as failure or unavailable rather than healthy.

### Requirement: Refresh and partial failure semantics
Administrators SHALL be able to refresh the overview and retry failed sections. Refresh SHALL show a loading state and the last successful observation as stale when available. An unavailable count SHALL display unavailable rather than zero. No automatic polling SHALL initiate external AI probes.

#### Scenario: Metrics load fails
- **WHEN** metrics cannot be read but system status remains available
- **THEN** the available status remains visible and metrics show unavailable or visibly stale values with a retry action.

#### Scenario: Concurrent refresh clicks
- **WHEN** the administrator repeatedly clicks refresh while a request is pending
- **THEN** duplicate in-flight refreshes are suppressed and older responses cannot replace newer data.

### Requirement: Dashboard navigation and honest scope
Dashboard SHALL provide access to Catalog, Documents and Switch to learning according to verified destination capabilities. Eligible personal/catalog inventory and processing counts SHALL link to matching document-operations filters. Metrics SHALL NOT link to unimplemented or unauthorized pages, or present invented active-user trends, AI cost, CPU, RAM, disk or live queue-depth estimates as measured telemetry.

#### Scenario: Account total displayed
- **WHEN** the admin inspects the account total
- **THEN** it remains a read-only count and does not itself perform account editing or suspension.

#### Scenario: Processing counts displayed
- **WHEN** pending and processing document counts appear
- **THEN** they are labeled as persisted document states, not live worker queue length
- **AND** an admitted administrator can open the matching scope and document-status filter in `/admin/documents`.

#### Scenario: Failed personal documents drill-down
- **WHEN** an administrator selects the personal Failed document count
- **THEN** `/admin/documents` opens with personal scope and Failed status selected
- **AND** it lists documents rather than inflating the count by every historical failed attempt.

#### Scenario: No destination permission
- **WHEN** document-operations permission is not confirmed
- **THEN** the dashboard does not expose an actionable document-operations link
- **AND** modifying a URL or browser capability cannot grant API access.

#### Scenario: Drill-down performs no processing
- **WHEN** an administrator opens a processing drill-down or refreshes its results
- **THEN** only authorized read requests occur
- **AND** no source retry or AI probe is initiated automatically.
