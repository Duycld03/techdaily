# admin-workspace Specification

## Purpose

Provide a distinct administrator entry and navigation experience while retaining DeepPace identity, shared authentication and existing layout conventions.

## Requirements

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
1. **Uninterrupted SPA Transitions**: Client-side navigation across admin routes (`/admin` $\leftrightarrow$ `/admin/catalog`) SHALL maintain `isAuthorized = true` during background capability re-verification, ensuring `AdminShell` remains mounted without visual flash, unmounting, or simulated page reload.
2. **Polished Header Controls**: The admin header SHALL render a compact `LocaleSelector` matching the learner header dimensions and an optically centered `ThemeToggle` button.
3. **Focused Dashboard**: The admin dashboard telemetry dock SHALL strictly display system operational services (API, PostgreSQL, AI Generation, Embeddings) and SHALL NOT render duplicate learner navigation cards.

#### Scenario: Navigate from dashboard to catalog
- **WHEN** an admin opens Documents
- **THEN** `/admin/catalog` uses the admin shell and existing catalog functionality without duplicate learner navigation.

#### Scenario: Switch to learning
- **WHEN** the admin selects Switch to learning
- **THEN** `/today` opens using the learner shell with the same session
- **AND** no login, token issuance or privilege change is required.

#### Scenario: Switching between Dashboard and Documents via Sidebar
- **WHEN** an authenticated administrator clicks Documents or Dashboard in the admin sidebar
- **THEN** the route transitions smoothly via Vue Router without unmounting `AdminShell` or flashing the full-screen authorization boundary.

#### Scenario: Admin header controls alignment
- **WHEN** an administrator views the admin header
- **THEN** the locale toggle displays compact sizing matching the user header and the color theme toggle displays its icon centered within its bounding box.

### Requirement: Shared session and admin logout
Admin and learner interfaces SHALL use the same account/session system. Explicit administrator logout SHALL revoke the refresh session, clear local credentials and privileged state, and return to `/admin/login`. Switching interfaces SHALL preserve the session; switching accounts SHALL discard previous-user privileged state.

#### Scenario: Logout from administration
- **WHEN** an administrator logs out
- **THEN** admin login appears and subsequent protected learner or admin requests require authentication again.

#### Scenario: Account changes during a request
- **WHEN** an old admin request resolves after logout or account replacement
- **THEN** its result is discarded and cannot populate the new account's state.

### Requirement: DeepPace design system parity
Administrator UI SHALL reuse DeepPace colors, typography, surface styles, icons and accessible controls. Login SHALL use StudioLayout, dashboard BentoDashboardLayout and catalog MasterDetailLayout. The separate shell SHALL NOT introduce a competing theme, CSS framework or unconstrained page wrappers. On `/admin/login`, the StudioLayout main stage (`#main`) and context dock (`#dock`) SHALL align to a common top baseline on desktop viewports ($\ge 1024\text{px}$) with structured hairline-bordered overview cards in the dock, preventing vertical drifting or bottom-weighted misalignments.

#### Scenario: Bilingual responsive admin surfaces
- **WHEN** admin login, dashboard and catalog are viewed in English and Vietnamese at 1440x900 and 390x844
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
