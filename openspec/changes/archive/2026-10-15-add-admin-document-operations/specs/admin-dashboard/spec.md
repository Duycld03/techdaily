# Admin Dashboard Spec Delta

## MODIFIED Requirements

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
