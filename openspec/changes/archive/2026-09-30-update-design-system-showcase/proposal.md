# Proposal

## Why

The TechDaily design system showcase page (`/showcase`) has drifted significantly from production standards. While core features across the platform (`/library`, `/notes`, `/review`, `/today`) now strictly adopt standardized page header banners, responsive bilingual typography, and modern micro-components, `/showcase` still displays an unstyled text header, misses key production components (`BasePagination`, `Sm2GradingButtons`, `FlashcardBentoCard`, retention analytics cards), and omits the mandatory `BentoDashboardLayout` archetype. Updating `/showcase` bridges this gap, establishing a true living styleguide that developers and agents can inspect and verify against design system invariants.

## What Changes

- **Standardize Showcase Page Header**: Replace the raw text header in `frontend/pages/showcase.vue` with the platform-standard page header banner featuring a `w-10 h-10 rounded-2xl bg-brand-500/10` icon badge (`Component` icon), primary title (`text-xl sm:text-2xl font-black`), localized subtitle, and responsive quick-jump section anchor pills.
- **Showcase Core Pagination (`BasePagination.vue`)**: Introduce an interactive demonstration of `BasePagination.vue` showing 1-based page navigation, total pages, record counts, and responsive layouts.
- **Showcase SM-2 Assessment & Recall Buttons (`Sm2GradingButtons.vue`)**: Add an interactive section demonstrating the four SM-2 rating buttons (Again [1], Hard [2], Good [3], Easy [4]) with semantic colors, interval descriptions, hotkey indicators, and click events.
- **Showcase Flashcard Inventory Cards (`FlashcardBentoCard.vue`)**: Render interactive flashcard item cards with urgency badges (`Due Today`, `Mastered`, `Learning`), SM-2 metrics (Ease Factor, Interval), 2-line answer summaries, and action controls.
- **Showcase Retention Analytics 5-Card Bento Suite**: Incorporate the newly implemented retention analytics cards (`MasteryGaugeCard`, `ReviewForecastChart`, `AtRiskLeechCard`, `SourceChannelRetentionCard`, `EaseFactorDistributionCard`) to visualize memory health metrics.
- **Complete System Layout Archetypes Coverage**: Add the missing `BentoDashboardLayout` archetype to `LayoutArchetypesShowcase.vue`, ensuring all four mandatory layout archetypes defined in `AGENTS.md` (Pillar 2) are demonstrated.
- **Bilingual i18n Standardization**: Add all necessary translation keys under the `showcase` namespace in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, eliminating hardcoded strings.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `system-layout-archetypes`: Expand design system showcase specifications to mandate standard page header banner adoption, full layout archetype coverage (including `BentoDashboardLayout`), and production parity for pagination, SM-2 grading, and retention analytics micro-components on `/showcase`.

## Impact

- **Frontend Routes**: `frontend/pages/showcase.vue`.
- **Showcase Components**: `frontend/components/showcase/` (`PrimitivesShowcase.vue`, `LayoutArchetypesShowcase.vue`, new or updated showcase section components).
- **Localization**: `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
- **Tests**: `frontend/tests/` unit tests for showcase components and dual-gate headless browser verification.
