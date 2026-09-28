# Tasks

## 1. Frontend Component Redesign

- [ ] 1.1 Redesign `frontend/components/dashboard/HomeBentoDashboard.vue` to split `action-stage` into Card A (Active Reading Hero) and Card B (Today's Practice Session Cockpit).
- [ ] 1.2 Remove daily drill completion badge and score from Card A header in `HomeBentoDashboard.vue`, retaining only book title, slice badge, and reading time estimate.
- [ ] 1.3 Implement semantic drill status badges on Card B in `HomeBentoDashboard.vue` supporting three distinct states: pending (`+10 Points`, amber target icon), passed (`✓ Completed: 10/10`, emerald checkmark icon), and failed (`✗ Needs Review: 0/10`, rose alert icon).
- [ ] 1.4 Redesign `frontend/components/dashboard/DomainConstellationCard.vue` to replace the hardcoded static SVG constellation with a live personal knowledge breakdown table displaying SM-2 flashcard count, saved highlights count, learned slices count, and connected concepts/relations telemetry.
- [ ] 1.5 Add bilingual i18n localization keys in `frontend/locales/en-US.json` and `frontend/locales/vi-VN.json` for knowledge breakdown telemetry items and failed drill status.

## 2. Frontend Tests & Verification

- [ ] 2.1 Update and add unit tests for `HomeBentoDashboard` in `frontend/tests/` verifying the 2-card action stage rendering and semantic drill badge states (pending, passed, failed).
- [ ] 2.2 Update and add unit tests for `DomainConstellationCard` in `frontend/tests/` asserting dynamic telemetry rendering from store data without static SVG nodes.
- [ ] 2.3 Run full test suites (`npm test` and `dotnet test`) to verify 100% test pass rate for Gate 1.
- [ ] 2.4 Programmatically drive headless Chromium via `browser` in `eval` across Desktop (1440x900) and Mobile (390x844) viewports to visually inspect and verify elimination of the center void and correct badge appearance (Gate 2).
