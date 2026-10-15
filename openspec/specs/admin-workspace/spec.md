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

### Requirement: Shared session and admin logout
Admin and learner interfaces SHALL use the same account/session system. Explicit administrator logout SHALL revoke the refresh session, clear local credentials and privileged state, and return to `/admin/login`. Switching interfaces SHALL preserve the session; switching accounts SHALL discard previous-user privileged state.

#### Scenario: Logout from administration
- **WHEN** an administrator logs out
- **THEN** admin login appears and subsequent protected learner or admin requests require authentication again.

#### Scenario: Account changes during a request
- **WHEN** an old admin request resolves after logout or account replacement
- **THEN** its result is discarded and cannot populate the new account's state.

### Requirement: DeepPace design system parity
Administrator UI SHALL reuse DeepPace colors, typography, surface styles, icons and accessible controls. Login SHALL use StudioLayout, dashboard BentoDashboardLayout, catalog and accounts MasterDetailLayout, audit BoardLayout, and document operations MasterDetailLayout. The separate shell SHALL NOT introduce a competing theme, CSS framework or unconstrained page wrappers. On `/admin/login`, the StudioLayout main stage (`#main`) and context dock (`#dock`) SHALL align to a common top baseline on desktop viewports ($\ge 1024\text{px}$) with structured hairline-bordered overview cards in the dock, preventing vertical drifting or bottom-weighted misalignments.

1. **Accessible Control Parity**:
- Native HTML form select elements (`<select><option>`) are strictly prohibited across all administration pages.
- The outcome filter on `/admin/audit` SHALL use `AppSelect.vue` with accessible listbox keyboard navigation, dark theme glass styling, and viewport-aware floating positioning.

2. **Complete Bilingual Localization Parity**:
- All visible text, placeholders, status pills, pagination buttons, metadata labels, attempt states, and ineligible reason notices on `/admin/audit` and `/admin/documents` SHALL resolve from locale dictionaries (`en.json` and `vi.json`) via `$t()` and `t()`.
- On `/admin/audit`:
  - Date filter placeholders SHALL render localized strings (`"Từ ngày (YYYY-MM-DD)"` / `"From (YYYY-MM-DD)"` and `"Đến ngày (YYYY-MM-DD)"` / `"To (YYYY-MM-DD)"`).
  - Event outcome badges in both the event list and detail modal SHALL display localized outcome labels (`"Thành công"`, `"Đã tiếp nhận"`, `"Bị từ chối"`, `"Thất bại"` in Vietnamese, and `"Succeeded"`, `"Accepted"`, `"Rejected"`, `"Failed"` in English).
- On `/admin/documents`:
  - Document status pills in both master card items and the detail header SHALL render localized labels (`"Sẵn sàng"`, `"Thất bại"`, `"Đang xử lý"`, `"Trong hàng đợi"` in Vietnamese, and `"Ready"`, `"Failed"`, `"Processing"`, `"Queued"` in English).
  - Scope badges SHALL display localized strings (`"Cá nhân"` / `"Personal"`, `"Catalog"` / `"Catalog"`).
  - Navigation list pagination buttons SHALL display localized labels (`"Trước"` / `"Prev"` and `"Sau"` / `"Next"`).
  - Ineligible retry warning banners SHALL map `ineligibleReasonCode` to user-facing localized guidance instead of displaying raw English backend messages.
  - Source availability indicators SHALL render localized labels (`"Thiếu nguồn"` / `"Missing"`, `"Khả dụng"` / `"Available"`).
  - Metadata Timestamps cards and attempt execution records SHALL display localized prefix labels (`"Mốc thời gian"`, `"Tạo lúc:"`, `"Ghi nhận:"`, `"Hàng đợi:"`, `"Bắt đầu:"`, `"Kết thúc:"`).

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

#### Scenario: Accessible account and audit controls
- **WHEN** an administrator uses keyboard or touch to filter accounts/history or confirm an account action
- **THEN** custom select controls and confirmation dialogs provide labels, focus management and clear localized feedback
- **AND** Escape closes a cancelable dialog without submitting the action.

#### Scenario: Accessible document history and retry
- **WHEN** an administrator filters document operations, opens attempt history or confirms a retry
- **THEN** reusable custom selects, paginated detail and teleported dialogs expose labels, focus management, safe localized errors and explicit cancellation
- **AND** long titles, identifiers and reasons remain readable on mobile and desktop.

#### Scenario: Audit log outcome filter uses accessible AppSelect
- **WHEN** an administrator opens `/admin/audit`
- **THEN** the outcome filter control is an `AppSelect` component rather than a native HTML `<select>`
- **AND** opening the dropdown presents styled options without native browser popovers.

#### Scenario: Audit log viewed in Vietnamese
- **WHEN** an administrator views `/admin/audit` with locale set to Vietnamese (`vi`)
- **THEN** the date filter inputs display placeholders `"Từ ngày (YYYY-MM-DD)"` and `"Đến ngày (YYYY-MM-DD)"`
- **AND** all event outcome badges in the list and detail modal display Vietnamese labels (`"Thành công"`, `"Bị từ chối"`, `"Đã tiếp nhận"`, `"Thất bại"`).

#### Scenario: Document operations viewed in Vietnamese
- **WHEN** an administrator views `/admin/documents` with locale set to Vietnamese (`vi`)
- **THEN** document status badges in the master rail and detail view display `"Sẵn sàng"`, `"Thất bại"`, `"Đang xử lý"`, or `"Trong hàng đợi"`
- **AND** pagination controls display `"Trước"` and `"Sau"`
- **AND** metadata timestamp cards and attempt history render Vietnamese headers and timestamp prefixes
- **AND** any ineligible retry message displays localized guidance.
