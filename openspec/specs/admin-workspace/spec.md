# admin-workspace Specification

## Purpose

Provide a distinct administrator entry and navigation experience while retaining DeepPace identity, shared authentication and existing layout conventions.

## Requirements

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
