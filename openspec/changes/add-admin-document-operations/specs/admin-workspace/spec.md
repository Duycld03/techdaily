# Admin Workspace Spec Delta

## MODIFIED Requirements

### Requirement: Administrator route admission
Every route at `/admin` or beneath `/admin/`, except `/admin/login`, SHALL require server-confirmed permission for its destination. `/admin/users` SHALL require account-management permission, `/admin/audit` audit-read permission and `/admin/documents` document-operations permission. Missing authentication or denied permission SHALL redirect to `/admin/login` without exposing protected SSR markup or client content. Backend administration APIs SHALL independently enforce authorization.

#### Scenario: Guest or learner opens an admin deep link
- **WHEN** a guest or non-admin requests `/admin/catalog` directly or through client navigation
- **THEN** the browser reaches `/admin/login` with a safe return destination
- **AND** no protected data appears before or during hydration.

#### Scenario: Permission revoked while open
- **WHEN** an admin request returns a definitive permission denial after access is removed
- **THEN** privileged state and pending privileged requests are cleared and the user is redirected once to `/admin/login`.

#### Scenario: Permission check temporarily unavailable
- **WHEN** permission verification fails due to network or server failure
- **THEN** protected content stays hidden behind a neutral retry state
- **AND** the failure is not treated as permission approval or grounds to destroy a valid session.

#### Scenario: Account or audit deep link
- **WHEN** a guest or learner requests `/admin/users` or `/admin/audit`, including through SSR
- **THEN** the browser reaches admin login with a validated local return destination
- **AND** account identities and audit events are never rendered or fetched before admission.

#### Scenario: New privileged request resolves after account replacement
- **WHEN** an account, audit or document-operations request resolves after logout, permission denial or a change of signed-in account
- **THEN** its result is discarded and cannot appear in the next session's workspace.

#### Scenario: Document operations deep link
- **WHEN** a guest or learner requests `/admin/documents` with a selected document or processing filter
- **THEN** no document metadata or attempt history is fetched or rendered before destination admission
- **AND** only a validated local return path is preserved for admin sign-in.

### Requirement: Distinct administrator navigation
Authorized admin routes SHALL show dedicated navigation with Dashboard, Catalog, Documents, Accounts and Administration log destinations according to verified capabilities, active-route indication, locale/theme controls, logout and Switch to learning. Learner sidebar, learning telemetry and learner command palette SHALL NOT appear in the admin shell. Admin login SHALL show neither protected shell.
1. **Uninterrupted SPA Transitions**: Client-side navigation across admitted admin routes SHALL maintain `isAuthorized = true` during background capability re-verification, ensuring `AdminShell` remains mounted without visual flash, unmounting, or simulated page reload.
2. **Polished Header Controls**: The admin header SHALL render a compact `LocaleSelector` matching the learner header dimensions and an optically centered `ThemeToggle` button.
3. **Focused Dashboard**: The admin dashboard telemetry dock SHALL strictly display system operational services (API, PostgreSQL, AI Generation, Embeddings) and SHALL NOT render duplicate learner navigation cards.

#### Scenario: Navigate from dashboard to catalog
- **WHEN** an admin opens Catalog
- **THEN** `/admin/catalog` uses the admin shell and existing catalog functionality without duplicate learner navigation.

#### Scenario: Switch to learning
- **WHEN** the admin selects Switch to learning
- **THEN** `/today` opens using the learner shell with the same session
- **AND** no login, token issuance or privilege change is required.

#### Scenario: Switching between Dashboard and Documents via Sidebar
- **WHEN** an authenticated administrator clicks Documents or Dashboard in the admin sidebar
- **THEN** `/admin/documents` or `/admin` opens through Vue Router without unmounting `AdminShell` or flashing the full-screen authorization boundary.

#### Scenario: Admin header controls alignment
- **WHEN** an administrator views the admin header
- **THEN** the locale toggle displays compact sizing matching the user header and the color theme toggle displays its icon centered within its bounding box.

#### Scenario: Navigate to new destinations
- **WHEN** an administrator selects Accounts or Administration log
- **THEN** `/admin/users` or `/admin/audit` opens within the same admin shell with the correct active destination
- **AND** the mobile menu dismisses without leaving duplicate navigation or trapped focus.

#### Scenario: Navigation reflects permission
- **WHEN** the server does not confirm a destination's required capability
- **THEN** its protected navigation entry is unavailable
- **AND** removing or adding a browser menu entry cannot grant backend access.

#### Scenario: Editing and operations remain distinct
- **WHEN** an administrator selects Catalog or Documents
- **THEN** Catalog opens publication/editing at `/admin/catalog` and Documents opens metadata/ingestion operations at `/admin/documents`
- **AND** personal document operations show no personal-content preview or edit action.

### Requirement: DeepPace design system parity
Administrator UI SHALL reuse DeepPace colors, typography, surface styles, icons and accessible controls. Login SHALL use StudioLayout, dashboard BentoDashboardLayout, catalog/accounts/document operations MasterDetailLayout, and audit BoardLayout. The separate shell SHALL NOT introduce a competing theme, CSS framework or unconstrained page wrappers. On `/admin/login`, the StudioLayout main stage (`#main`) and context dock (`#dock`) SHALL align to a common top baseline on desktop viewports ($\ge 1024\text{px}$) with structured hairline-bordered overview cards in the dock, preventing vertical drifting or bottom-weighted misalignments.

#### Scenario: Bilingual responsive admin surfaces
- **WHEN** admin login, dashboard, catalog, accounts, audit and document operations are viewed in English and Vietnamese at 1440x900 and 390x844 in light and dark themes
- **THEN** controls and text remain legible without clipping, wrapping collisions or double navigation
- **AND** body text is at least 14px on mobile and 16px on larger screens, with no-wrap action labels and responsive gaps.

#### Scenario: Accessible mobile menu
- **WHEN** an administrator opens the mobile navigation using keyboard or touch
- **THEN** focus is managed, Escape closes it, and navigation dismisses it without trapping focus or obscuring the destination.

#### Scenario: Desktop StudioLayout baseline alignment on admin login
- **WHEN** a user or administrator opens `/admin/login` on desktop viewports ($\ge 1024\text{px}$)
- **THEN** the main action stage heading ("Administrator sign in") and the context dock heading ("Your system overview") align horizontally on a synchronized top baseline
- **AND** the workspace overview items in the dock render within elevated hairline-bordered cards (`border-slate-200 dark:border-white/[0.08]`) with icon badges, preventing content from being pushed down into the bottom-right half of the screen.

#### Scenario: Mobile viewport responsive stacking
- **WHEN** a user accesses `/admin/login` on a mobile viewport ($< 1024\text{px}$)
- **THEN** the sign-in form fills 100% of the stage width and the context overview cards stack cleanly below the form without clipping or horizontal overflow.

#### Scenario: Accessible account and audit controls
- **WHEN** an administrator uses keyboard or touch to filter accounts/history or confirm an account action
- **THEN** custom select controls and confirmation dialogs provide labels, focus management and clear localized feedback
- **AND** Escape closes a cancelable dialog without submitting the action.

#### Scenario: Accessible document history and retry
- **WHEN** an administrator filters document operations, opens attempt history or confirms a retry
- **THEN** reusable custom selects, paginated detail and teleported dialogs expose labels, focus management, safe localized errors and explicit cancellation
- **AND** long titles, identifiers and reasons remain readable on mobile and desktop.
