# Proposal: Merge Profile into Settings & Eliminate Tab Border Flicker

## Why

Currently, the Settings interface (`/settings`) contains only three compact configuration cards (Language & Theme, Web Push, and Study Schedule), leaving significant dead space on 1080p desktop displays and feeling unnecessarily sparse. At the same time, account management is fragmented across two separate destinations: `/profile` (Personal Info, Password, Milestones, Domain Mastery) and `/settings` (Preferences, Notifications, Timezone). 

Furthermore, switching tabs in `MasterDetailLayout` exhibits a visual border flash/layout shift because inactive tab buttons lack a 1px `border-transparent` base. Merging Profile into Settings consolidates all user, security, and preference management into a cohesive, executive-grade Master-Detail Hub while eliminating tab flicker.

## What Changes

- **Tab Border Flicker Elimination**: All navigation rail tab buttons in `MasterDetailLayout` strictly maintain a 1px base border (`border border-transparent` when inactive, `border-brand-500/20` when active) and scoped `transition-colors` to eliminate 1px box-sizing jumps or visual flashing during tab transitions.
- **Unified Account & Settings Master-Detail Architecture**: Consolidate `/profile` sections into `/settings` across 6 organized categories:
  1. `profile` (**Profile & Identity**): Full Name, Target Role, Daily Goal Minutes selector chips, and Engineer Passport identity summary.
  2. `security` (**Security & Password**): Password change form with strength analysis and Google OAuth link status.
  3. `mastery` (**Milestones & Mastery**): Engineer Milestones card and 4-pillar Domain Mastery Goal Tracker.
  4. `appearance` (**Language & Theme**): Interface language (EN/VI) and color mode (Dark/Light) selectors.
  5. `notifications` (**Web Push Notifications**): Web push toggle switch, subscription status, and test push trigger.
  6. `schedule` (**Study Schedule & Timezone**): Preferred study time, streak warning alert time, and IANA timezone selector (`AppSelect.vue`).
- **Deep-Link Tab Query Synchronization**: Support deep-linking and state preservation using `?tab=<tab_id>` (e.g. `/settings?tab=security`), synchronizing `route.query.tab` with `activeTab`.
- **Navigation Cutover & Backward Compatibility**:
  - Update `frontend/pages/profile.vue` with an immediate redirect to `/settings?tab=profile`.
  - Update `frontend/components/layout/AppHeader.vue` so the user avatar chip links to `/settings?tab=profile`.
  - Consolidate `nav.group_account` in `frontend/composables/useNavigationMenu.ts` from separate Profile/Settings entries into a unified "Settings & Profile" destination.

## Capabilities

### Modified Capabilities
- `core-platform`: Update desktop layout and settings requirements to mandate unified Account & Settings Master-Detail architecture, deep-linked tab synchronization, and zero-flicker tab navigation.

## Impact

- **Frontend Routes**: `frontend/pages/settings.vue` (unified hub), `frontend/pages/profile.vue` (redirect).
- **Navigation & Layouts**: `frontend/composables/useNavigationMenu.ts`, `frontend/components/layout/AppHeader.vue`, `frontend/components/layout/MasterDetailLayout.vue`.
- **Testing**: `frontend/tests/pages/settings.spec.ts`, `frontend/tests/composables/useNavigationMenu.spec.ts`.
