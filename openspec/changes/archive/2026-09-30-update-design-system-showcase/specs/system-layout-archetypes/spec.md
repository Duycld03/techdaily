# Spec Delta

## ADDED Requirements

### Requirement: Design System Showcase Header and Section Architecture
The `/showcase` page SHALL provide a top-level standardized page header banner and organized section architecture matching the platform layout archetype standards:

1. **Standardized Page Header Banner**:
   - **Left**: Icon badge (`w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20`) rendering `<Component class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`, primary title (`$t('showcase.title')`), and subtitle (`$t('showcase.subtitle')`).
   - **Right**: Responsive quick-jump navigation pills enclosed in a refined container, linking to key showcase sections (Primitives, SM-2 Assessment, Retention Metrics, Layout Archetypes, Feedback, Iconography).
   - **Container Bound**: Enforces `max-w-7xl mx-auto space-y-8 px-4 sm:px-6 py-6`, standardizing outer margins across all viewport sizes.

2. **Bilingual Completeness**:
   - All section headers, descriptions, sample card content, and anchor controls SHALL resolve through `$t('showcase.*')` in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, prohibiting hardcoded UI strings.

#### Scenario: User navigates showcase using header quick-jump pills
- **WHEN** an engineer visits `/showcase` on desktop or mobile
- **THEN** the standardized page header renders with the Component icon badge, title, subtitle, and anchor pills
- **AND** clicking an anchor pill smoothly scrolls the viewport to the targeted showcase section.

---

### Requirement: Production Micro-Component Showcase Coverage
The `/showcase` page SHALL provide interactive demonstrations of production micro-components used across the core platform:

1. **Base Pagination (`BasePagination.vue`)**:
   - SHALL render interactive pagination with 1-based indexing, active page pill, previous/next chevron buttons, and total item summary.
   - SHALL update active page state reactively upon button interaction and emit `@change` events.
   - SHALL demonstrate both compact and verbose summary configurations (`showSummary`, `showItemSummary`).

2. **SM-2 Assessment & Recall Buttons (`Sm2GradingButtons.vue`)**:
   - SHALL render all four SM-2 rating buttons: `Again` (Score 1, Rose), `Hard` (Score 3, Amber), `Good` (Score 4, Sky), and `Easy` (Score 5, Brand).
   - SHALL display keyboard shortcut tags (`[1]`, `[2]`, `[3]`, `[4]`) and localized interval descriptions.
   - SHALL emit `@grade` with the corresponding score upon click and surface interactive user feedback.

3. **Flashcard Inventory Cards (`FlashcardBentoCard.vue`)**:
   - SHALL render flashcard cards demonstrating all three urgency tiers (`Due Today`, `Learning`, `Mastered`).
   - SHALL display SM-2 metrics (`EF: x.xx • Interval: xd`), question text, 2-line answer preview, source citations, and action controls.

4. **Retention Analytics Bento Suite**:
   - SHALL showcase the production retention analytics cards: `MasteryGaugeCard`, `ReviewForecastChart`, `AtRiskLeechCard`, `SourceChannelRetentionCard`, and `EaseFactorDistributionCard`.
   - Cards SHALL organize into responsive bento grid rows matching the visual rhythm of the `/review` analytics hub.

#### Scenario: Interacting with BasePagination in showcase
- **WHEN** a user clicks page 3 or the next-chevron in the BasePagination showcase block
- **THEN** the pagination component emits `@change` with page 3 and updates its active state display without page reload.

#### Scenario: Testing SM-2 grading buttons in showcase
- **WHEN** a user clicks the "Easy" button or presses key "4"
- **THEN** the button triggers the grade event with score 5 and displays an interactive confirmation toast or banner.

---

### Requirement: Complete Layout Archetypes Demonstration
The `LayoutArchetypesShowcase` component SHALL demonstrate all four canonical layout archetypes defined in the design system governance standard:

1. **Four-Way Archetype Switcher**:
   - The archetype selector SHALL provide tabs for: `StudioLayout` (Flashcards Studio), `MasterDetailLayout` (Settings Master-Detail), `BoardLayout` (Notes Board), and `BentoDashboardLayout` (Executive Bento Dashboard).

2. **Bento Dashboard Layout Archetype Demo**:
   - SHALL render `BentoDashboardLayout` with:
     - Header slot with greeting and status.
     - Action Stage (`#action-stage`) occupying 2 columns on desktop (`lg:col-span-2`) displaying primary practice cards.
     - Telemetry Dock (`#telemetry-dock`) occupying 1 column on desktop (`lg:col-span-1`) displaying consistency metrics and mini charts.
   - SHALL automatically collapse into a single vertical column on tablet and mobile viewports (< 1024px) without horizontal overflow.

#### Scenario: Switching to Bento Dashboard archetype in showcase
- **WHEN** a user selects "Bento Dashboard" in the layout archetypes switcher
- **THEN** the view renders the live `BentoDashboardLayout` with a 2-column action stage and 1-column telemetry dock on desktop viewports.
