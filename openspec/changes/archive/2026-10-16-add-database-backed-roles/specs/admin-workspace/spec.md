# Spec Delta

## MODIFIED Requirements

### Requirement: Dedicated administrator login
The system SHALL provide `/admin/login` with administrator-specific identity and feedback. Successful sign-in SHALL use a validated local protected return path only when its destination permission is confirmed; otherwise it SHALL choose the first permitted registered admin destination. Protected Admin SHALL default to `/admin`. An account with no permitted destination SHALL remain at admin login without protected content, preserving its learner session.

#### Scenario: Administrator signs in
- **WHEN** a protected Admin submits valid credentials without a return destination
- **THEN** the dashboard at `/admin` opens after server permission confirmation.

#### Scenario: Regular account signs in
- **WHEN** valid credentials belong to an account without a permitted administrative destination
- **THEN** the account remains on `/admin/login` with localized permission feedback
- **AND** no admin shell, dashboard or catalog data loads.

#### Scenario: Existing non-admin session
- **WHEN** a signed-in learner visits `/admin/login`
- **THEN** login remains accessible for another account's credentials without bouncing to `/today` or `/admin`.

#### Scenario: Unsafe return path
- **WHEN** a return parameter is external, protocol-relative, a login route or outside the protected admin area
- **THEN** it is discarded and sign-in uses the first permitted destination, `/admin` for protected Admin.

#### Scenario: Restricted role has no dashboard permission
- **WHEN** an account with an authorized destination but no dashboard permission signs in
- **THEN** it lands on its first permitted destination without requesting dashboard data or flashing the dashboard
- **AND** landing selection follows the registered deterministic order, initially Dashboard, Catalog, Documents, Accounts and Audit.

#### Scenario: Return path is safe but forbidden
- **WHEN** the local return destination is valid but its capability is denied
- **THEN** successful sign-in uses another permitted destination rather than looping between that destination and admin login.

### Requirement: Administrator route admission
Every `/admin` route except `/admin/login` SHALL require current server-confirmed permission for its registered destination. Dashboard admission SHALL require canViewAdminDashboard; other destinations SHALL retain their independent capabilities. Missing authentication or definitive denial SHALL redirect to admin login without protected SSR or client data. Backend APIs SHALL independently enforce permission. A role label or unrelated grant SHALL NOT authorize a destination.

#### Scenario: Guest or learner opens an admin deep link
- **WHEN** a guest or non-admin requests `/admin/catalog` directly or by client navigation
- **THEN** admin login receives a safe return destination
- **AND** no protected data appears before or during hydration.

#### Scenario: Permission revoked while open
- **WHEN** an admin request definitively denies access after permission removal
- **THEN** privileged state and pending requests are cleared and the user is redirected once to `/admin/login`.

#### Scenario: Permission check temporarily unavailable
- **WHEN** permission verification fails due to network or service failure
- **THEN** protected content stays behind a neutral retry boundary
- **AND** uncertainty neither approves access nor destroys a valid session.

#### Scenario: Account or audit deep link
- **WHEN** a guest or learner requests `/admin/users` or `/admin/audit`, including SSR
- **THEN** admin login receives a validated local return destination
- **AND** identities and audit events are neither fetched nor rendered before admission.

#### Scenario: New privileged request resolves after account replacement
- **WHEN** an account, role-option, role-assignment, audit or document-operations request resolves after logout, denial or account replacement
- **THEN** its result is discarded and cannot populate the new session's workspace.

#### Scenario: Document operations deep link
- **WHEN** a guest or learner requests `/admin/documents` with a selected document or processing filter
- **THEN** no metadata or attempt history is fetched or rendered before destination admission
- **AND** only a validated local return path is preserved.

#### Scenario: Restricted role accesses only its destination
- **WHEN** an account has ReadAudit but lacks dashboard, catalog, account and document-operations grants
- **THEN** `/admin/audit` is admitted and the other destinations are denied independently
- **AND** the shell does not require catalog authority as an umbrella administrator check.

### Requirement: Distinct administrator navigation
Authorized routes SHALL retain dedicated navigation, active-route indication, locale/theme controls, logout and Switch to learning. Dashboard, Catalog, Documents, Accounts and Audit SHALL appear only when their destination capabilities are confirmed. Learner sidebar, telemetry and command palette SHALL remain absent; admin login SHALL show neither protected shell. Navigation SHALL NOT use role-name comparisons or assume every admitted account may view Dashboard.

Client transitions within the same verified session SHALL retain the mounted shell during background capability verification without admitting the new protected outlet before its destination check. Compact locale/theme controls SHALL retain alignment. Dashboard telemetry SHALL remain focused on API, PostgreSQL, generation and embedding health, without duplicate learner navigation cards.

#### Scenario: Navigate from dashboard to catalog
- **WHEN** an administrator opens Catalog with the required capability
- **THEN** `/admin/catalog` retains existing functionality inside the admin shell without duplicate learner navigation.

#### Scenario: Switch to learning
- **WHEN** Switch to learning is selected
- **THEN** `/today` opens in the learner shell with the same session
- **AND** no login, token issuance or privilege change occurs.

#### Scenario: Switching between Dashboard and Documents via Sidebar
- **WHEN** an administrator navigates between confirmed sidebar destinations
- **THEN** routing remains SPA-based without unmounting the shell or flashing a full-screen boundary
- **AND** each destination's protected data remains gated by its own capability.

#### Scenario: Admin header controls alignment
- **WHEN** the header renders
- **THEN** locale controls remain compact and theme icons optically centered.

#### Scenario: Navigate to new destinations
- **WHEN** a permitted administrator selects Accounts or Administration log
- **THEN** `/admin/users` or `/admin/audit` opens in the same shell with correct active indication
- **AND** the mobile menu closes without duplicate navigation or trapped focus.

#### Scenario: Navigation reflects permission
- **WHEN** a destination's capability is not confirmed
- **THEN** its navigation entry is unavailable
- **AND** menu manipulation cannot grant backend access.

#### Scenario: Editing and operations remain distinct
- **WHEN** Catalog or Documents is selected
- **THEN** Catalog opens publication/editing and Documents opens metadata/ingestion operations
- **AND** personal document operations offer no personal-content preview or edit action.

#### Scenario: Dashboard entry is independently hidden
- **WHEN** an admitted role lacks canViewAdminDashboard
- **THEN** Dashboard is absent from both desktop and mobile navigation and is not used as an unconditional logo/home destination
- **AND** the account can still use its other confirmed destinations and Switch to learning.
