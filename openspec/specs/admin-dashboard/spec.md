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
Dashboard APIs SHALL require server-verified administrator permission and return aggregates rather than account emails, document text, private notes, tokens or configuration secrets. Anonymous callers SHALL receive 401; authenticated non-admin callers SHALL receive 403.

#### Scenario: Direct unauthorized request
- **WHEN** a learner calls the overview API with a valid learner token
- **THEN** the response is 403 and contains no aggregate metrics.

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
Dashboard SHALL provide access to Documents and Switch to learning. Metrics SHALL NOT link to unimplemented management pages. The first release SHALL NOT present invented active-user trends, AI cost, CPU, RAM, disk or queue-depth estimates as measured system telemetry.

#### Scenario: Account total displayed
- **WHEN** the admin inspects the account total
- **THEN** it is a read-only count and does not imply account editing or suspension functionality.

#### Scenario: Processing counts displayed
- **WHEN** pending and processing document counts appear
- **THEN** they are labeled as persisted document states, not live worker queue length.
