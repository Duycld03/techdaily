# Spec Delta

## MODIFIED Requirements

### Requirement: Distinct administrator navigation
Authorized admin routes SHALL show dedicated admin navigation with Dashboard and Documents destinations, active-route indication, locale/theme controls, logout and Switch to learning.
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
