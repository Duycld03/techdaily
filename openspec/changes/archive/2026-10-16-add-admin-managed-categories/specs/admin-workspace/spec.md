# Spec Delta

## MODIFIED Requirements

### Requirement: Administrator route admission
Every `/admin` route except `/admin/login` SHALL require current server-confirmed permission for its registered destination. Dashboard SHALL retain canViewAdminDashboard and Accounts its independent capability. Categories SHALL require canManageCategories, never role names, browser flags or another grant. Invalid authentication or definitive denial SHALL redirect to admin login without protected SSR/client data. Backend APIs SHALL independently enforce permission.

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

### Requirement: Distinct administrator navigation
Authorized administration SHALL retain dedicated navigation, active-route indication, locale/theme controls, logout and Switch to learning. Dashboard, Catalog, Documents, Accounts, Audit and Categories SHALL appear only when their destination capabilities are confirmed. Learner sidebar, telemetry and command palette SHALL remain absent from the admin shell; admin login SHALL show neither protected shell.

Client-side transitions SHALL preserve the mounted shell during same-session background re-verification, but a new outlet SHALL not fetch/render before its destination check. Compact header locale/theme controls SHALL retain alignment. Dashboard telemetry SHALL remain focused on API, PostgreSQL, generation and embeddings without duplicate learner navigation. Categories SHALL extend the existing destination registry and master-detail/custom-control patterns; no competing theme or unconditional dashboard landing SHALL be introduced.

#### Scenario: Navigate from dashboard to catalog
- **WHEN** an administrator opens Catalog
- **THEN** `/admin/catalog` uses the existing functionality inside the admin shell without duplicate learner navigation.

#### Scenario: Switch to learning
- **WHEN** Switch to learning is selected
- **THEN** `/today` opens in the learner shell with the same session and no new login, token issuance or privilege change.

#### Scenario: Switching between Dashboard and Documents via Sidebar
- **WHEN** an authenticated administrator switches existing sidebar destinations
- **THEN** routing remains SPA-based without unmounting the shell or flashing authorization UI.

#### Scenario: Admin header controls alignment
- **WHEN** the header renders
- **THEN** locale controls remain compact and theme icons optically centered.

#### Scenario: Navigate to new destinations
- **WHEN** Accounts, Audit or Categories is selected with the matching permission
- **THEN** the corresponding route opens in the same shell with correct active indication
- **AND** the mobile menu closes without duplicate navigation or trapped focus.

#### Scenario: Navigation reflects permission
- **WHEN** a destination capability is unconfirmed
- **THEN** its protected navigation entry is unavailable
- **AND** menu manipulation cannot grant backend access.

#### Scenario: Editing and operations remain distinct
- **WHEN** Catalog or Documents is selected
- **THEN** Catalog opens publication/editing and Documents opens metadata/ingestion operations
- **AND** personal document operations expose no personal-content preview/edit action.

#### Scenario: Dashboard entry is independently hidden
- **WHEN** an admitted role lacks canViewAdminDashboard
- **THEN** Dashboard is absent from desktop/mobile navigation and is not an unconditional logo/home destination
- **AND** the account retains its other confirmed destinations and Switch to learning.

#### Scenario: Category management is not document generation
- **WHEN** an administrator opens Categories, changes filters or edits category metadata
- **THEN** it remains in the category master-detail workspace
- **AND** no document preparation, content preview, AI call or worker retry is initiated.
