# Spec Delta

## MODIFIED Requirements

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
