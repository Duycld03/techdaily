# Proposal: Standardize Frontend Phase 3 — Spaced Review Studio & Quiz Arena

## Why

The Spaced Repetition Review Studio (`pages/review.vue`) and Interview Quiz Arena (`pages/quiz.vue`) represent the high-frequency interactive learning loops of TechDaily. To conform to `antfu/skills` and ensure flawless interaction on small mobile viewports:
1. SM-2 Flashcard grading buttons (`Sm2GradingButtons.vue`) must maintain comfortable touch targets ($\ge 44\text{px}$) and clean 2-column wrapping on narrow screens ($\le 360\text{px}$).
2. The 7-day Review Forecast Bar Chart (`ReviewForecastChart.vue`) and Mastery Gauge (`MasteryGaugeCard.vue`) must adapt fluidly without bar clipping or tooltip overflow.
3. The Interview Quiz interactive loop must strictly isolate option choice states (Rule 15 in `AGENTS.md`) and support keyboard shortcut triggers (1-4, Enter) via VueUse `useEventListener`.
4. Advanced filter modals (`AdvancedFilterModal.vue`) must present a clean mobile layout for multi-select chips and sorting options.

## What Changes

- **Spaced Repetition Review Studio (`pages/review.vue`, `FlashcardDeck.vue`, `Sm2GradingButtons.vue`, `FlashcardHeroCard.vue`, `FlashcardBentoCard.vue`)**:
  - Audit flashcard card flip animation and 3D perspective to ensure zero layout shift or content overflow on small screens.
  - Standardize `Sm2GradingButtons.vue`: enforce `grid-cols-2 sm:grid-cols-4 gap-2.5` with high-contrast score indicators and clean keyboard bindings.
  - Modernize `ReviewForecastChart.vue`: scale bars responsively, use `tabular-nums` for counts, and ensure popover tooltips remain within viewport boundaries.
- **Interview Quiz Arena (`pages/quiz.vue`)**:
  - Audit quiz question choices: enforce `whitespace-nowrap shrink-0` on option badges (`A`, `B`, `C`, `D`) and responsive flex layout for option text.
  - Verify clean session state reset (`selectedOptionIndex = null`) on new session initialization.
  - Replace manual keydown event listeners with VueUse `useEventListener`.
- **Review Advanced Filtering (`AdvancedFilterModal.vue`)**:
  - Ensure filter chips (source, status, urgency, sorting) wrap into 2 columns on mobile screens.
  - Provide a touch-friendly sticky bottom bar with clear and apply action buttons.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `review`: Standardize SM-2 grading button geometry, mobile flashcard flip, and forecast chart responsive scaling.
- `quiz`: Enforce option badge isolation, mobile option flex layout, and keyboard event hygiene.

## Impact

- **Affected Files**: `frontend/pages/review.vue`, `frontend/pages/quiz.vue`, `frontend/components/review/*.vue`.
- **Testing**: Unit tests in `frontend/tests/pages/review.spec.ts`, `frontend/tests/pages/quiz.spec.ts`, and Playwright interactive tests.
