# Spec Delta

## MODIFIED Requirements

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
  - Document status pills in both master card items and the detail header SHALL render localized labels (`"Sẵn sàng"`, `"Thất bại"`, `"Đang xử lý"`, `"Trong hàng đợi"` in Vietnamese, and `"Ready"`, `"Failed"`, `"Processing"`, `"Queued"` in English) and identify them as processing status rather than publication status.
  - Scope filters and badges SHALL display localized strings (`"Cá nhân"` / `"Personal"`, `"Tuyển chọn"` / `"Catalog"`). Expanded curated-document labels SHALL use `"Tài liệu tuyển chọn"` in Vietnamese, distinct from taxonomy category labels such as `"Chuyên mục"`.
  - System-curated ownership and source-type labels SHALL be localized rather than displaying internal English ownership labels or enum names. Source document titles, author names and source excerpts SHALL retain their original language.
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
- **THEN** document processing badges in the master rail and detail view display `"Sẵn sàng"`, `"Thất bại"`, `"Đang xử lý"`, or `"Trong hàng đợi"`
- **AND** curated-document filters and badges display `"Tuyển chọn"`, while system ownership and source-type metadata use Vietnamese labels
- **AND** pagination controls display `"Trước"` and `"Sau"`
- **AND** metadata timestamp cards and attempt history render Vietnamese headers and timestamp prefixes
- **AND** any ineligible retry message displays localized guidance without equating completed processing with publication.

## ADDED Requirements

### Requirement: Learner navigation isolation
Learner desktop and mobile navigation SHALL contain no administrative destinations, including catalog administration, regardless of the signed-in account's permissions. Returning to learning or loading Discover SHALL NOT insert administrative links. This separation SHALL NOT change shared-session behavior, administrator route authorization or access through the dedicated administrator workspace.

#### Scenario: Privileged account opens Discover
- **WHEN** an account with catalog-management permission opens `/library?tab=discover`
- **THEN** neither the learner desktop sidebar nor the mobile menu contains catalog administration or another administrative destination
- **AND** permission discovery does not insert one afterward.

#### Scenario: Return from administration to learning
- **WHEN** an administrator uses Switch to learning and subsequently navigates between learner pages
- **THEN** the learner navigation remains free of administrative links
- **AND** the same session retains its existing authorized administrator access through the dedicated workspace.

#### Scenario: Direct administration entry still enforces permission
- **WHEN** an account requests a protected administrator URL directly
- **THEN** existing destination-specific server-confirmed admission applies independently of learner menu contents
- **AND** removing learner links neither grants new permission nor bypasses authentication.
