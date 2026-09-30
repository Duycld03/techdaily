# Design

## Context

See `proposal.md` - Why.

The TechDaily design system showcase currently lives at `/showcase` (`frontend/pages/showcase.vue`). While earlier iterations established primitives (buttons, inputs, segmented controls) and older mockups, the platform has matured with critical architectural standards:
1. Standardized page header banners across all major portals (`library.vue`, `notes.vue`, `review.vue`).
2. Robust micro-components for core user workflows: `BasePagination.vue` (1-based pagination), `Sm2GradingButtons.vue` (4-tier SM-2 rating controls), `FlashcardBentoCard.vue` (compact flashcard cards), and the 5-card retention analytics suite.
3. Four canonical system layout archetypes mandated by `AGENTS.md` Pillar 2 (`StudioLayout`, `MasterDetailLayout`, `BoardLayout`, and `BentoDashboardLayout`), whereas `LayoutArchetypesShowcase.vue` currently only implements three.

## Goals / Non-Goals

**Goals:**
- Upgrade `frontend/pages/showcase.vue` to adopt the platform standard page header banner with `<Component>` icon badge, responsive title, localized subtitle, and quick-jump navigation anchor pills.
- Add an interactive showcase for `BasePagination.vue` demonstrating 1-based indexing, page jumping, previous/next states, and summary counters.
- Add an interactive showcase for Spaced Repetition (SM-2) assessment: `Sm2GradingButtons.vue` with interactive click feedback, and `FlashcardBentoCard.vue` showcasing all urgency states (`Due Today`, `Learning`, `Mastered`).
- Showcase the retention analytics bento card suite (`MasteryGaugeCard`, `ReviewForecastChart`, `AtRiskLeechCard`, `SourceChannelRetentionCard`, `EaseFactorDistributionCard`) with mock telemetry.
- Expand `LayoutArchetypesShowcase.vue` to include the 4th mandatory layout archetype: `BentoDashboardLayout`.
- Provide 100% bilingual string localization (`en.json` and `vi.json`) for all newly introduced showcase copy and headers.

**Non-Goals:**
- Mutating production business components (`BasePagination.vue`, `Sm2GradingButtons.vue`, `FlashcardBentoCard.vue`, etc.) — this change showcases existing components without altering their proven contracts.
- Any backend API, database schema, or authentication endpoint changes — `/showcase` is purely a frontend design system demonstration route.

## Decisions

### 1. Header Banner & Quick-Jump Navigation Architecture
- Replace the legacy text header in `showcase.vue` with the canonical header banner pattern:
  - Left: `w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20` rendering `<Component class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`.
  - Heading: `text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`.
  - Subtitle: `text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-normal`.
  - Right: Horizontal scrollable quick-jump pill bar (`rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-subtle p-1`) allowing fast in-page anchor jumps to `#primitives`, `#pagination`, `#sm2`, `#retention`, `#archetypes`, and `#feedback`.

### 2. Integration of BasePagination Showcase
- Place `BasePagination` either within `PrimitivesShowcase.vue` or as a distinct subsection.
- Wire up a reactive `currentPage` and `pageSize` ref to allow clicking through pages and observing the active page indicator and pagination summary change in real-time.

### 3. Spaced Repetition (SM-2) Dedicated Section
- Create a dedicated showcase section for SM-2 learning mechanics:
  - Embed `Sm2GradingButtons.vue` connected to an active toast/notification showing: "Graded with score {score} ({label})".
  - Embed sample `FlashcardBentoCard.vue` cards with mock card props demonstrating different ease factors (1.70, 2.10, 2.50) and urgency levels.

### 4. Retention Analytics Bento Suite Section
- Render the 5 retention analytics cards in a 2-tier bento grid:
  - Tier 1: `MasteryGaugeCard` (semi-circle SVG gauge) and `ReviewForecastChart` (7-day forecast).
  - Tier 2: `AtRiskLeechCard` (Hay quên / Quá hạn), `SourceChannelRetentionCard` (Nguồn tài liệu), and `EaseFactorDistributionCard` (Chưa vững, Đang củng cố, Vững vàng).

### 5. BentoDashboardLayout in LayoutArchetypesShowcase
- Add `'bento-dashboard'` to `archetypes` list alongside `'studio'`, `'master-detail'`, and `'board'`.
- Provide a clean demonstration of `BentoDashboardLayout.vue`:
  - `#header`: Daily cockpit banner.
  - `#action-stage`: 2-column primary learning tile.
  - `#telemetry-dock`: 1-column streak & metric summary.

## Risks / Trade-offs

- **Risk:** Additional components imported into `showcase.vue` may slightly increase its bundle size.
  - **Mitigation:** Nuxt 3 automatically code-splits routes; `/showcase` is never loaded during primary learner sessions (`/today`, `/review`, `/library`).
- **Risk:** Mock data in showcase components becoming stale if component props change in the future.
  - **Mitigation:** Showcase components will directly bind to existing TypeScript prop interfaces exported by the production components.
