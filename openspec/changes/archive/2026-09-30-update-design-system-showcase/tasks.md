# Tasks

## 1. Frontend - Localization & i18n
- [x] 1.1 Add comprehensive `showcase` namespace translation keys in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` covering page headers, section titles, quick-jump anchor labels, and micro-component descriptions.
## 2. Frontend - Page Header & Section Architecture

- [x] 2.1 Refactor container styling in `frontend/pages/showcase.vue` to adopt `max-w-7xl mx-auto space-y-8 px-4 sm:px-6 py-6`.
- [x] 2.2 Replace legacy text header with the platform-standard page header banner featuring `w-10 h-10 rounded-2xl bg-brand-500/10` icon badge (`<Component>`), primary title, localized subtitle, and responsive quick-jump anchor pills.

## 3. Frontend - Production Micro-Components Integration

- [x] 3.1 Add an interactive `BasePagination.vue` showcase section with reactive page tracking, page-change events, and summary badge variations.
- [x] 3.2 Add an interactive Spaced Repetition (SM-2) section demonstrating `Sm2GradingButtons.vue` with click feedback and `FlashcardBentoCard.vue` covering all urgency tiers.
- [x] 3.3 Add a Retention Analytics section showcasing the 5-card bento suite (`MasteryGaugeCard`, `ReviewForecastChart`, `AtRiskLeechCard`, `SourceChannelRetentionCard`, `EaseFactorDistributionCard`) with mock telemetry.

## 4. Frontend - System Layout Archetypes Expansion

- [x] 4.1 Update `frontend/components/showcase/LayoutArchetypesShowcase.vue` to include `'bento-dashboard'` in the archetype switcher.
- [x] 4.2 Implement live `BentoDashboardLayout.vue` demonstration featuring an asymmetric 2-column action stage and 1-column telemetry dock with responsive mobile degradation.

## 5. Tests & Verification (Dual-Gate)

- [x] 5.1 Run all frontend unit tests (`npm test`) to verify component contracts, i18n resolution, and absence of regressions.
- [x] 5.2 Execute dual-gate automated visual verification via headless Chromium driving `/showcase`, capturing and inspecting screenshots on both Desktop (1440x900) and Mobile (390x844).
