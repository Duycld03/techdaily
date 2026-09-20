# Proposal: Standardize Frontend Phase 6 — Engineer Profile, Settings & Authentication

## Why

The Engineer Profile (`pages/profile.vue`), System Settings (`pages/settings.vue`), and Authentication (`pages/login.vue`) surfaces house personal identity, career benchmarks, notification preferences, and account security. Under `antfu/skills` guidelines, these surfaces require standardization to:
1. Guarantee the Executive 3-Tier Bento Profile adapts seamlessly to small mobile screens ($\le 375\text{px}$ and $320\text{px}$) without layout overflow, badge collision, or cramped inputs.
2. Refactor the daily study pace selector: replace static `grid-cols-4` with responsive `grid-cols-2 sm:grid-cols-4` so chips (`5m`, `10m`, `15m`, `30m`) never get squished on narrow displays.
3. Standardize the Milestones Telemetry Strip (`EngineerMilestonesCard.vue`) and Domain Mastery Tracker (`DomainGoalTracker.vue`) to ensure clean typography, non-truncated labels in both English and Vietnamese, and smooth progress track animations.
4. Modernize the Web Push settings and Google OAuth flow with strict VueUse lifecycle hygiene and accessible form focus states.

## What Changes

- **Engineer Portfolio Profile (`pages/profile.vue`, `EngineerIdentityPassport.vue`, `EngineerMilestonesCard.vue`, `DomainGoalTracker.vue`)**:
  - Audit Tier 1 Passport: ensure large avatar, badges (Role, Google, Streak Trophy), and edit actions stack gracefully on mobile viewports.
  - Audit Tier 2 Milestones: verify 4 telemetry cells (`grid-cols-2 lg:grid-cols-4`) maintain legible typography on 320px screens.
  - Audit Tier 3 Lower Grid: ensure balanced 50/50 desktop split transitions cleanly to vertical stacking on mobile (`order-1` Domain Mastery, `order-2` Account Settings).
  - Modernize Pace chips in Account Settings: change `grid-cols-4` to `grid-cols-2 sm:grid-cols-4 gap-2`.
- **System Settings & Web Push (`pages/settings.vue`)**:
  - Verify `AppTimePicker` integrations for study time and streak alert scheduling.
  - Standardize IANA timezone selector with clean mobile dropdown positioning.
  - Audit Brave push browser guidance card for mobile responsive layout.
- **Authentication & OAuth (`pages/login.vue`)**:
  - Ensure Google GIS button container adapts responsively to mobile container widths.
  - Standardize tab switching (Sign In vs Register) with zero-shift border geometry.
  - Display RFC 7807 error problem details with clean alert cards and accessible aria attributes.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Standardize mobile login/register responsive layout and Google GIS button scaling.
- `core-platform`: Refine 3-Tier Bento Profile mobile layout, responsive daily goal pace selector, and settings notification controls.

## Impact

- **Affected Files**: `frontend/pages/profile.vue`, `frontend/pages/settings.vue`, `frontend/pages/login.vue`, `frontend/components/profile/*.vue`.
- **Testing**: Unit tests in `frontend/tests/pages/profile.spec.ts`, `frontend/tests/pages/settings.spec.ts`, and Playwright auth/profile smoke tests.
