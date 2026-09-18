# Tasks

## 1. Settings Studio Modernization

- [x] 1.1 Update `frontend/pages/settings.vue` root container, header, and typography with Dev-Learning Studio tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `border-white/[0.08]`).
- [x] 1.2 Modernize Appearance & Language and Web Push Notifications cards in `frontend/pages/settings.vue` to `.glass-card` styling with Electric Violet active states.
- [x] 1.3 Upgrade study schedule time inputs, timezone dropdown, test push button, and Brave push guidance callout in `frontend/pages/settings.vue`.

## 2. Profile Studio & Identity Modernization

- [x] 2.1 Update `frontend/pages/profile.vue` container, asymmetric 2-column layout, and tab switcher (`[ Personal Info | Security & Password ]`) with Studio glass styling.
- [x] 2.2 Modernize personal identity form, target role select, daily goal pace chips, and password security form in `frontend/pages/profile.vue`.
- [x] 2.3 Modernize `frontend/components/profile/EngineerProfileHero.vue` identity card, milestone statistics, and streak badges with `.glass-card` and violet ambient glow.

## 3. Domain Mastery & System Controls Modernization

- [x] 3.1 Modernize `frontend/components/profile/DomainGoalTracker.vue` 4-pillar progress bars with Electric Violet gradients (`from-brand-600 to-brand-500`) and high-contrast telemetry counters.
- [x] 3.2 Modernize `frontend/components/common/ThemeToggle.vue` and `frontend/components/common/LocaleSelector.vue` to hairline glass panels.

## 4. Automated Testing & Verification

- [x] 4.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate across all test files including `settings.spec.ts` and `profile.spec.ts`.
- [x] 4.2 Validate OpenSpec changes and specifications (`openspec validate --changes` and `openspec validate --specs`).
