# Tasks: Bento Dashboard & Concentric Learning Metrics

## 1. Composables & State Management

- [x] 1.1 In `frontend/composables/useTodayViewMode.ts`, implement view mode state (`bento` vs `split`) with `localStorage` persistence (`techdaily_today_view_mode`), defaulting to `'bento'`.

## 2. Concentric Ring Component (Image #3)

- [x] 2.1 In `frontend/components/today/ConcentricMetricCard.vue`, build the dual concentric SVG ring component calculating `stroke-dashoffset` for daily study pace and SM-2 retention rate, with center percentage readout and bilingual tooltips.

## 3. Bento Dashboard Layout & Modular Cards (Image #1, #3, #5)

- [x] 3.1 In `frontend/components/today/TodayBentoDashboard.vue`, scaffold the responsive Bento Grid container (3-column on desktop, 2-column on tablet, single-column stack on mobile).
- [x] 3.2 In `TodayBentoDashboard.vue`, implement the Welcome & Orientation Banner with dynamic greeting, active curriculum Day badge, and "Ask AI Explainer" modal trigger.
- [x] 3.3 In `TodayBentoDashboard.vue`, implement the Today's Focus Bento Hero Card with active book badge, slice title, estimated reading duration, progress bar, and "Continue Reading" CTA.
- [x] 3.4 In `TodayBentoDashboard.vue`, implement the Senior Scenario Drill Bento Card displaying the challenge teaser, points reward badge, and instant solve action.
- [x] 3.5 In `TodayBentoDashboard.vue`, implement the 7-Day Consistency Matrix Card rendering weekly study minutes, streak flame indicator, and freeze protection credits.
- [x] 3.6 In `TodayBentoDashboard.vue`, implement the Knowledge Graph Radar Card previewing connected node/edge counts and navigation link to 3D Cosmos.

## 4. Page Integration & Localization

- [x] 4.1 In `frontend/pages/today.vue`, integrate `useTodayViewMode`, add the header view mode segmented toggle (`[ ⊞ Dashboard | ◫ Focus Studio ]`), and conditionally render `TodayBentoDashboard.vue` or the split-pane reader.
- [x] 4.2 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add all localization keys for Bento dashboard titles, metrics, concentric tooltips, and consistency labels.

## 5. Automated Tests & Verification

- [x] 5.1 In `frontend/tests/components/today/ConcentricMetricCard.spec.ts`, write unit tests asserting SVG ring dashoffset calculations, percentage readout, and reactivity.
- [x] 5.2 In `frontend/tests/composables/useTodayViewMode.spec.ts`, write unit tests asserting default mode, mode switching, and `localStorage` persistence.
- [x] 5.3 Run `npm --prefix frontend test` to verify all frontend unit tests pass with zero regressions.
- [x] 5.4 Run `dotnet test backend/TechDaily.sln` to confirm backend test suite stability.
- [x] 5.5 Run `openspec validate --strict bento-dashboard-and-learning-metrics` to confirm OpenSpec compliance.
