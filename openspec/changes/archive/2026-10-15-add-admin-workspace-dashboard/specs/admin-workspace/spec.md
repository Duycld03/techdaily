# Admin Workspace Delta

## Purpose

Provide a distinct administrator entry and navigation experience while retaining DeepPace identity, shared authentication and existing layout conventions.

## ADDED Requirements

### Requirement: Dedicated administrator login
The system SHALL provide `/admin/login` with administrator-specific identity and sign-in feedback. Successful administrator sign-in SHALL open `/admin` by default or a validated local protected admin return path. A regular account SHALL remain on admin login with a localized permission message and no protected content.

#### Scenario: Administrator signs in
- **WHEN** an administrator submits valid credentials without a return destination
- **THEN** the dashboard at `/admin` opens after server permission confirmation.

#### Scenario: Regular account signs in
- **WHEN** valid credentials belong to an account without administrator permission
- **THEN** the user remains on `/admin/login` with a permission message
- **AND** no admin shell, dashboard or catalog data is loaded.

#### Scenario: Existing non-admin session
- **WHEN** a signed-in learner visits `/admin/login`
- **THEN** the login page remains accessible to enter another account's credentials without bouncing to `/today` or `/admin`.

#### Scenario: Unsafe return path
- **WHEN** a return parameter is external, protocol-relative, a login route or outside the protected admin area
- **THEN** successful admin sign-in uses `/admin` instead.

### Requirement: Administrator route admission
Every route at `/admin` or beneath `/admin/`, except `/admin/login`, SHALL require server-confirmed administrator permission. Missing authentication or denied permission SHALL redirect to `/admin/login` without exposing protected SSR markup or client content. Backend administration APIs SHALL independently enforce authorization.

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

### Requirement: Distinct administrator navigation
Authorized admin routes SHALL show dedicated admin navigation with Dashboard and Documents destinations, active-route indication, locale/theme controls, logout and Switch to learning. Learner sidebar, learning telemetry and learner command palette SHALL NOT appear in the admin shell. Admin login SHALL show neither protected shell.

#### Scenario: Navigate from dashboard to catalog
- **WHEN** an admin opens Documents
- **THEN** `/admin/catalog` uses the admin shell and existing catalog functionality without duplicate learner navigation.

#### Scenario: Switch to learning
- **WHEN** the admin selects Switch to learning
- **THEN** `/today` opens using the learner shell with the same session
- **AND** no login, token issuance or privilege change is required.

### Requirement: Shared session and admin logout
Admin and learner interfaces SHALL use the same account/session system. Explicit administrator logout SHALL revoke the refresh session, clear local credentials and privileged state, and return to `/admin/login`. Switching interfaces SHALL preserve the session; switching accounts SHALL discard previous-user privileged state.

#### Scenario: Logout from administration
- **WHEN** an administrator logs out
- **THEN** admin login appears and subsequent protected learner or admin requests require authentication again.

#### Scenario: Account changes during a request
- **WHEN** an old admin request resolves after logout or account replacement
- **THEN** its result is discarded and cannot populate the new account's state.

### Requirement: DeepPace design system parity
Administrator UI SHALL reuse DeepPace colors, typography, surface styles, icons and accessible controls. Login SHALL use StudioLayout, dashboard BentoDashboardLayout and catalog MasterDetailLayout. The separate shell SHALL NOT introduce a competing theme, CSS framework or unconstrained page wrappers.

#### Scenario: Bilingual responsive admin surfaces
- **WHEN** admin login, dashboard and catalog are viewed in English and Vietnamese at 1440x900 and 390x844
- **THEN** controls and text remain legible without clipping, wrapping collisions or double navigation
- **AND** body text is at least 14px on mobile and 16px on larger screens, with no-wrap action labels and responsive gaps.

#### Scenario: Accessible mobile menu
- **WHEN** an administrator opens the mobile navigation using keyboard or touch
- **THEN** focus is managed, Escape closes it, and navigation dismisses it without trapping focus or obscuring the destination.
