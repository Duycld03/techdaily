# Proposal: Profile Layout Bento Rebalance & Typography Optimization

## Why

Following the initial portfolio restructure of `/profile`, visual inspection reveals three usability and aesthetic defects:
1. **Severe Vertical Asymmetry (1:3 height ratio)**: Stacking both `EngineerMilestonesCard` (~310px) and `DomainGoalTracker` (~550px) in the right column while the left column contains only the short Account & Security Hub form (~320px) leaves over 500px of dead black void on the left side of desktop viewports.
2. **Vietnamese Text Truncation**: In `EngineerMilestonesCard`, the Vietnamese label `"Độ chính xác trắc nghiệm"` (26 characters) in uppercase tracking overflows the narrow 2-column bento cell, truncating to `ĐỘ CHÍNH XÁC TRẮC N...`.
3. **Redundant Header Subtitle**: The profile page header subtitle redundantly duplicates the domain mastery subtitle (*"Tiến độ làm chủ qua 4 trụ cột kỹ thuật cốt lõi"*) verbatim.

Restructuring the profile view into a **3-Tier Bento Dashboard** eliminates the dead space, provides a dedicated full-width telemetry strip, balances the lower columns, and ensures crisp, non-truncated bilingual copy.

## What Changes

- **3-Tier Executive Bento Dashboard Layout** (`frontend/pages/profile.vue`):
  - **Tier 1 (Top Identity Banner)**: Retain `EngineerIdentityPassport.vue` full-width across the top, displaying user avatar, active status indicator, display name, email, target role badge, account type, membership tenure, and longest streak trophy badge.
  - **Tier 2 (Full-Width Milestones Telemetry Strip)**: Position `EngineerMilestonesCard.vue` as a prominent full-width 4-column bento strip (`grid-cols-2 lg:grid-cols-4 gap-4`) directly under the Identity Passport banner.
  - **Tier 3 (Balanced 2-Column Split)**: Split the lower section into balanced 50/50 columns (`lg:grid-cols-2 gap-6 items-start`):
    - *Left Column*: Account & Security Hub (`.glass-card` housing Personal Info and Security tabs, ~360px height).
    - *Right Column*: Domain Mastery Goal Tracker (`DomainGoalTracker.vue`, ~380px height), presenting the 4 universal engineering pillars with compact, high-contrast progress bars and clear topic counters.
- **Bilingual Localization & Typography Optimization**:
  - Shorten `profile.quiz_accuracy` in Vietnamese (`vi.json`) from `"Độ chính xác trắc nghiệm"` to `"Độ chính xác Quiz"` to eliminate text truncation across all responsive viewports.
  - Add dedicated `profile.subtitle` localized key ("Manage your senior engineer profile, career telemetry, and security credentials" / "Quản lý hồ sơ kỹ sư, năng lực chuyên môn và bảo mật tài khoản") to replace the duplicated `domain_mastery_subtitle`.
  - Ensure `EngineerMilestonesCard.vue` cells provide ample horizontal clearance for localized text.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update requirement `User Profile Management, Route Guards & Security` to specify the 3-Tier Bento Dashboard structure (Tier 1: Identity Passport, Tier 2: 4-Column Milestones Telemetry Strip, Tier 3: Balanced 50/50 Split for Account Hub and Domain Mastery Goal Tracker) and non-truncated bilingual typography.

## Impact

- **Backend API & Database**: 0 breaking changes. Endpoints (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`) remain unchanged.
- **State & Stores**: `useProfileStore` and `useInterviewQuizStore` contracts remain untouched.
- **Frontend Components**:
  - `frontend/pages/profile.vue` updated to 3-tier structure.
  - `frontend/components/profile/EngineerMilestonesCard.vue` adapted to 4-column full-width bento strip.
  - `frontend/components/profile/DomainGoalTracker.vue` compacted with polished spacing.
  - `frontend/i18n/locales/vi.json` and `en.json` updated with non-truncated keys.
- **Testing**: `frontend/tests/pages/profile.spec.ts` and `frontend/tests/components/profile.spec.ts` updated to verify 3-tier layout and typography.
