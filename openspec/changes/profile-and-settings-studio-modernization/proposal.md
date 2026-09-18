# Proposal

## Why

`frontend/pages/profile.vue`, `frontend/pages/settings.vue`, and their associated identity and goal tracking components (`EngineerProfileHero.vue`, `DomainGoalTracker.vue`, `LocaleSelector.vue`, `ThemeToggle.vue`) represent the final surfaces in TechDaily still using legacy slate palettes (`bg-slate-900`, `border-slate-800`). Modernizing them brings user identity, engineering domain mastery, and system preferences into full visual harmony with the Dev-Learning Studio aesthetic (OLED Obsidian Canvas `#09080e`, Electric Violet `#8b5cf6`, `.glass-card`, and `.glass-panel`).

## What Changes

- **Settings Studio Modernization (`frontend/pages/settings.vue`)**:
  - Migrate page container to Obsidian Canvas (`dark:bg-canvas`, `dark:bg-canvas-subtle`).
  - Upgrade Appearance & Language card and Web Push Notifications card to `.glass-card` with hairline borders (`dark:border-white/[0.08]`).
  - Refine notification study schedule time inputs, timezone dropdown, and test push trigger button with Studio styling and Electric Violet active states.
  - Upgrade toggle switches and active push status banners to Electric Violet accents.

- **Profile Studio Modernization (`frontend/pages/profile.vue`)**:
  - Upgrade the Asymmetric 2-Column Engineer Portfolio container to Obsidian Canvas.
  - Modernize Tab Switcher (`[ Personal Info | Security & Password ]`) with `.glass-panel` container and elevated active pill.
  - Upgrade personal identity inputs (Name, Target Role) and Daily Goal Pace chips (5m/10m/15m/30m) to Studio glass tokens.
  - Upgrade Security & Password form (Current Password, New Password, Confirm Password, dynamic strength bar) to Studio glass tokens.

- **Profile Hero & Milestones Modernization (`frontend/components/profile/EngineerProfileHero.vue`)**:
  - Upgrade Engineer Identity Card to `.glass-card` with subtle violet ambient back-glow.
  - Modernize milestone statistics cards (Active Streak, Longest Streak, Freeze Credits, Drills Completed, Quiz Accuracy) with elevated dark glass surfaces.

- **Domain Mastery Goal Tracker Modernization (`frontend/components/profile/DomainGoalTracker.vue`)**:
  - Upgrade Domain Mastery card to `.glass-card`.
  - Modernize the 4 universal engineering pillar progress bars (Backend Runtime, Data Storage, Distributed Systems, Frontend & Browser) with gradient tracks (`from-brand-600 to-brand-500`) and high-contrast telemetry indicators.

- **System UI Controls Modernization (`ThemeToggle.vue`, `LocaleSelector.vue`)**:
  - Upgrade theme toggle button and bilingual locale selector to hairline borders and subtle glass elevation.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Refine user profile management and settings presentation requirements to mandate Dev-Learning Studio design tokens, glass containers, and responsive hairline styling across `profile.vue` and `settings.vue`.

## Impact

- **API & Domain Contracts**: Zero breaking changes. All `GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`, and Web Push endpoints remain 100% untouched.
- **State & Stores**: `useProfileStore`, `useWebPush`, and `useApiClient` maintain identical reactive contracts.
- **Testing**: All existing tests in `tests/pages/settings.spec.ts` and `tests/pages/profile.spec.ts` will continue to pass.
