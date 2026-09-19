# Design: Profile Layout Bento Rebalance & Typography Optimization

## Context

See `proposal.md` for motivation. The `/profile` route currently stacks both cumulative telemetry widgets (`EngineerMilestonesCard` and `DomainGoalTracker`) in the right column, leaving the left column with only the short Account & Security Hub. This results in a 1:3 vertical height imbalance, leaving over 500px of dead space on desktop. Additionally, long Vietnamese phrases (`"Độ chính xác trắc nghiệm"`) truncate in narrow bento cells, and the page subtitle duplicates the domain mastery subtitle.

This design establishes a **3-Tier Bento Dashboard** architecture that eliminates dead space, creates a dedicated full-width telemetry strip, balances the lower columns, and guarantees crisp bilingual typography.

## Goals / Non-Goals

**Goals:**
- Provide a clean 3-tier visual hierarchy on desktop ($\ge 1024\text{px}$):
  1. Tier 1: Full-width Engineer Identity Passport.
  2. Tier 2: Full-width 4-column horizontal Milestones Telemetry Strip.
  3. Tier 3: Balanced 50/50 2-column split between Account Hub (~360px) and Domain Mastery (~380px).
- Eliminate all empty vertical void space on desktop viewports.
- Optimize component padding and density in `DomainGoalTracker.vue` to achieve visual height parity with the Account & Security Hub.
- Shorten Vietnamese milestone titles to eliminate text truncation (`ĐỘ CHÍNH XÁC TRẮC N...` $\rightarrow$ `ĐỘ CHÍNH XÁC QUIZ`).
- Decouple the profile page header subtitle from the domain goal tracker subtitle.

**Non-Goals:**
- Modifying backend API contracts, database schemas, or store interfaces.
- Altering the business logic of Spaced Repetition (SM-2), Quiz scoring, or Domain Pillar keyword mapping.
- Moving notification preferences into profile (they remain strictly isolated in `/settings`).

## Decisions

### 1. 3-Tier Bento Flow Architecture
- **Decision**: Structure `frontend/pages/profile.vue` into three distinct horizontal tiers:
  ```
  +-------------------------------------------------------------------------------+
  | Tier 1: EngineerIdentityPassport.vue (Full Width, ~140px)                     |
  +-------------------------------------------------------------------------------+
  | Tier 2: EngineerMilestonesCard.vue (Full Width Strip: 4 Columns, ~140px)      |
  | [ Drills (35) ]  [ Quiz Acc (75%) ]  [ Memory Vault (80) ]  [ Highlights (20) ]
  +---------------------------------------+---------------------------------------+
  | Tier 3 - Left Column (50%):           | Tier 3 - Right Column (50%):          |
  | Account & Security Hub                | Domain Mastery Goal Tracker           |
  | (Personal Info & Password Tabs)       | (4 Universal Engineering Pillars)     |
  | Height: ~360px                        | Height: ~380px                        |
  +---------------------------------------+---------------------------------------+
  ```
- **Rationale**:
  - Distributing the 4 milestone metrics horizontally across Tier 2 gives each card ample horizontal width (~260px on 1280px screen), completely preventing text truncation even for longer locale phrases.
  - Positioning the Account Hub and Domain Mastery Goal Tracker side-by-side in Tier 3 achieves near-perfect vertical alignment (~360px vs ~380px), eliminating the 500px dead void.

### 2. Full-Width 4-Column Strip in `EngineerMilestonesCard.vue`
- **Decision**: Refactor `EngineerMilestonesCard.vue` to render a full-width container with a responsive 4-column grid (`grid grid-cols-2 lg:grid-cols-4 gap-4`).
- **Rationale**:
  - On mobile/tablet ($< 1024\text{px}$), it forms a balanced 2x2 bento grid.
  - On desktop ($\ge 1024\text{px}$), it spans across 4 columns in a single sleek telemetry row.

### 3. Compact Height Tuning for `DomainGoalTracker.vue`
- **Decision**: Tighten padding and vertical spacing in `DomainGoalTracker.vue`:
  - Outer card spacing: `p-5 sm:p-6 space-y-4` (down from `p-6 sm:p-7 space-y-6`).
  - Pillar row cards: `p-3 rounded-xl space-y-2` (down from `p-4 space-y-3`).
  - Pillar icon size: `w-7 h-7` (down from `w-8 h-8`).
- **Rationale**: Reduces total height of the 4-pillar list from ~550px down to ~380px, creating exact visual parity with the ~360px Account & Security Hub card.

### 4. Bilingual Typography & Subtitle Decoupling
- **Decision**:
  - Update `vi.json`: change `profile.quiz_accuracy` from `"Độ chính xác trắc nghiệm"` to `"Độ chính xác Quiz"`.
  - Add `profile.subtitle` to `en.json` (*"Manage your senior engineer profile, career telemetry, and security credentials"*) and `vi.json` (*"Quản lý hồ sơ kỹ sư, năng lực chuyên môn và bảo mật tài khoản"*).
  - Bind the page header subtitle in `frontend/pages/profile.vue` to `profile.subtitle`.
- **Rationale**: Shortens the longest Vietnamese title from 26 to 18 characters, eliminating truncation while preserving precise technical meaning. Eliminates redundant copy between page header and section header.

## Risks / Trade-offs

- **Risk**: Stacking order on mobile viewports ($< 1024\text{px}$).
  - *Mitigation*: In `profile.vue`, mobile viewport uses CSS ordering to preserve an intuitive user flow:
    1. Identity Passport (Who I am)
    2. Milestones Strip 2x2 (My achievements)
    3. Domain Mastery Tracker (My skills)
    4. Account & Security Form (My settings)
- **Risk**: Test assertions in `profile.spec.ts`.
  - *Mitigation*: Text assertions (`Taylor TechLead`, `profile.subtitle`, `35`, `80`, `20`, etc.) are independent of grid layout, and tests will assert `profile.subtitle` instead of `profile.domain_mastery_subtitle`.
