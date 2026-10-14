# Tasks

## 1. Frontend Layout Refactoring & Studio Archetype

- [ ] 1.1 Import and integrate `StudioLayout` component in `frontend/app/pages/quiz.vue` for active Arena practice mode (`quizStore.activeTab === 'arena'`)
- [ ] 1.2 Structure Arena mode into `#main` action stage (question card, options, submit/next controls) and `#dock` telemetry dock (question count, streak, timer, hotkeys cheatsheet)
- [ ] 1.3 Ensure responsive single-column stacking on mobile viewports (< 1024px) with zero layout clipping or horizontal overflow

## 2. Vitest Test Suite Hygiene & Pillar 3 Alignment

- [ ] 2.1 Refactor `frontend/tests/components/LayoutArchetypes.spec.ts` to purge forbidden CSS class assertions (`wrapper.classes().toContain('glass-card')`, `wrapper.classes().toContain('space-y-4')`)
- [ ] 2.2 Add structural and slot rendering assertions to `LayoutArchetypes.spec.ts` verifying component contracts without styling assertions

## 3. Dual-Gate Verification (Tests & Visual Proof)

- [ ] 3.1 Run `npm test` across the full frontend Vitest test suite and ensure 100% passing tests with zero regressions
- [ ] 3.2 Launch headless Chromium via browser automation, navigate to `/quiz`, and capture visual verification screenshots at Desktop (1440x900) and Mobile (390x844) viewports
