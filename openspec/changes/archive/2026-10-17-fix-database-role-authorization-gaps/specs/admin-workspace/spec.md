# Spec Delta

## MODIFIED Requirements

### Requirement: Administrator route admission
Every `/admin` route except `/admin/login` SHALL require current server-confirmed permission for its registered destination. Dashboard SHALL retain canViewAdminDashboard and Accounts its independent capability. Categories SHALL require canManageCategories, never role names, browser flags or another grant. Invalid authentication or definitive denial SHALL redirect to admin login without protected SSR/client data. Backend APIs SHALL independently enforce permission. Renewal/retry SHALL NOT admit an unpermitted destination merely because another destination is allowed.

#### Scenario: Guest or learner opens an admin deep link
- **WHEN** a guest or non-admin requests `/admin/catalog` directly or through client navigation
- **THEN** admin login opens with a safe return destination
- **AND** no protected data appears before or during hydration.

#### Scenario: Permission revoked while open
- **WHEN** a privileged request returns definitive permission denial after access is removed
- **THEN** privileged state and pending requests are cleared and the user is redirected once to admin login.

#### Scenario: Permission check temporarily unavailable
- **WHEN** verification fails because of a network/server error
- **THEN** protected content remains behind a neutral retry state
- **AND** failure is not interpreted as approval or a reason to destroy a valid session.

#### Scenario: Account or audit deep link
- **WHEN** a guest or learner requests Accounts or Audit, including SSR
- **THEN** admin login receives a validated local return destination
- **AND** account identities and audit events are neither fetched nor rendered before admission.

#### Scenario: New privileged request resolves after account replacement
- **WHEN** an account, role-option, role-assignment, audit, document-operations or category request resolves after logout, denial or account replacement
- **THEN** its result is discarded and cannot populate the next session's workspace.

#### Scenario: Document operations deep link
- **WHEN** a guest or learner requests document operations with a selected document/filter
- **THEN** no document metadata or attempt history is fetched/rendered before admission
- **AND** only a validated local return path is retained.

#### Scenario: Restricted role accesses only its destination
- **WHEN** an account has ReadAudit but lacks dashboard, catalog, account, category and document-operations grants
- **THEN** Audit is admitted and the other destinations are denied independently
- **AND** the shell does not require catalog authority as an umbrella check.

#### Scenario: Category destination permission is distinct
- **WHEN** an account without `canManageCategories` requests `/admin/categories`, including through SSR
- **THEN** no category administration data or privileged controls are fetched/rendered
- **AND** adding a sidebar link or forging a capability in the browser does not authorize the server API.

#### Scenario: Category-only role has a permitted landing
- **WHEN** a category-only role signs in without a permitted return destination
- **THEN** the registered landing resolver selects Categories without requesting or flashing Dashboard
- **AND** unsafe or forbidden return paths cannot create a login loop.

#### Scenario: Refresh-only session returns capabilities for a different destination
- **GIVEN** an audit-only account has a valid refresh session but needs renewed access credentials
- **WHEN** it opens Catalog and renewal/capability discovery succeeds
- **THEN** Catalog is never mounted, its privileged controls are not rendered and its administrative document request is not issued, including during hydration or the transition to confirmed capabilities
- **AND** the browser resolves the denied destination through the safe admin-login flow while retaining the valid learner session and independently permitted Audit access.

#### Scenario: Retry confirms only another destination
- **GIVEN** a destination is behind the temporary verification-failure boundary
- **WHEN** retry obtains current capabilities that permit another destination but not the requested one
- **THEN** the requested outlet stays unmounted and unfetched until navigation resolves its definitive destination denial
- **AND** the interface does not become stuck in a checking state or destroy an otherwise valid session.

#### Scenario: Retry confirms the current destination
- **WHEN** verification retry confirms the current destination for the current acting session
- **THEN** only that admitted destination may mount and load protected data
- **AND** another transient failure retains the neutral retry state and credentials; a completion belonging to a replaced account or superseded route cannot admit the new outlet.

#### Scenario: Same-session navigation retains the shell but not an unadmitted outlet
- **WHEN** a permitted account navigates within the mounted admin shell while background verification is pending
- **THEN** the existing shell remains mounted
- **AND** a new destination does not fetch or render protected content before its own permission is confirmed.

## ADDED Requirements

### Requirement: Safe administrator return context
Administrator return URLs SHALL pass existing local-path safety validation and be authorized by pathname against the registered destination permissions. Safe query and fragment context SHALL survive sign-in and already-authenticated entry. Query/fragment text SHALL NOT grant permission. Unsafe, unknown or forbidden destinations SHALL use the existing permitted fallback without a redirect loop or unconditional dashboard admission.

#### Scenario: Permitted return preserves query and fragment
- **WHEN** a permitted account signs in with a safe Catalog, Documents or Categories return URL containing filters, a selected item or a fragment
- **THEN** the confirmed destination opens with its sanitized query and fragment intact
- **AND** access is decided using the pathname, not by comparing the entire return URL to a route literal.

#### Scenario: Existing session follows a contextual deep link
- **WHEN** an already-authenticated account enters a permitted admin pathname with safe query and fragment context
- **THEN** route admission preserves that context instead of discarding it for a default landing.

#### Scenario: Context cannot grant another destination
- **WHEN** a safe return pathname is forbidden but its query or fragment mentions a permitted route
- **THEN** the pathname remains forbidden and the existing permitted fallback is selected without fetching the denied destination.

#### Scenario: Unsafe or ambiguous return remains rejected
- **WHEN** a return value is external, protocol-relative, a login destination, outside the protected area, or violates the existing control-character, backslash, traversal or encoded-path safety rules
- **THEN** query/fragment preservation does not bypass rejection or cause external navigation
- **AND** the existing safe permitted fallback is used.

#### Scenario: Unregistered local pathname remains denied
- **WHEN** an otherwise local safe return pathname has no registered administrative destination
- **THEN** it does not inherit permission from a query, fragment or similarly prefixed route
- **AND** fallback follows the existing registered permissions, including independent Categories access, without changing dashboard aliases or landing precedence.
