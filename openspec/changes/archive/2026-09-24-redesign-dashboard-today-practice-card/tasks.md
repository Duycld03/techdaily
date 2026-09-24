# Tasks: Redesign Dashboard Today's Practice Card

## 1. Frontend - Card B Template & Store Binding

- [x] 1.1 In `frontend/components/dashboard/HomeBentoDashboard.vue`, refactor Card B from a static scenario teaser into a dedicated Today's Practice Session Cockpit card bound to `topic` and `drill` from `useDailyFocusStore`
- [x] 1.2 In `frontend/components/dashboard/HomeBentoDashboard.vue`, implement computed properties for curriculum day order, drill completion status (pending vs completed with score), and session itinerary breakdown (1 Concept Reading + 1 Scenario Drill)
- [x] 1.3 In `frontend/components/dashboard/HomeBentoDashboard.vue`, update the action CTA button with dynamic copy ("Start Today's Practice →" vs "Review Today's Session →") and explicit navigation to `/today`
- [x] 1.4 In `frontend/components/dashboard/HomeBentoDashboard.vue`, ensure symmetrical card styling between Card A and Card B (`glass-card`, equalized padding, borders, and bottom action footer divider) to eliminate vertical misalignment on desktop viewports

## 2. Localization & Copy Standardization

- [x] 2.1 In `frontend/i18n/locales/en.json`, add localized keys for Today's Practice card header, curriculum day badge, drill status tags, itinerary breakdown, and action button states
- [x] 2.2 In `frontend/i18n/locales/vi.json`, add corresponding Vietnamese translations ensuring `whitespace-nowrap shrink-0` compliance to prevent badge wrapping or text clipping across viewports

## 3. Testing & Verification

- [x] 3.1 Update unit tests in `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` to assert Today's Practice card rendering, dynamic store data binding, drill status badge display, and navigation to `/today`
- [x] 3.2 Execute full frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.3 Capture browser preview screenshots at 1920x1080 (Desktop) and 390x844 (Mobile) in Dark and Light modes verifying balanced layout proportions, distinct routing semantics between Card A and Card B, and bilingual visual fidelity
