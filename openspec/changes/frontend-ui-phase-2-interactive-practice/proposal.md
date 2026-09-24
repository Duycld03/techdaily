# Proposal: Frontend UI Phase 2 - Interactive Practice & Arena (StudioLayout)

## Why

Interactive practice surfaces (`quiz.vue` Arena mode, `review.vue` Tab 1 Flashcard Session) currently lack consistent stage proportions, resulting in vertical jitter, unbalanced telemetry docks, and nested scrollbars on Windows 11 desktop viewports. Standardizing on `StudioLayout` establishes a strict 68% main action stage paired with a 32% telemetry dock, stabilizing dimensions and preventing layout jumps during active learning loops.

## What Changes

- **Quiz Arena Mode (`frontend/pages/quiz.vue`)**:
  - Adopt `StudioLayout` with a 68% main stage (`#main`) for question prompt, syntax-highlighted code blocks, and option choices.
  - Dedicate the 32% companion dock (`#dock`) to live countdown timer, streak multiplier, seniority badges, and question map.
- **Flashcard Active Review Session (`frontend/pages/review.vue` Tab 1)**:
  - Adopt `StudioLayout`, centering the active card flip stage and SM-2 grading buttons in `#main` (`max-w-2xl mx-auto`).
  - Dock session progress, interval forecast preview, and keyboard shortcut legends in the 32% `#dock`.
- **Option & Card Surface Stabilization (`frontend/components/ui/OptionCard.vue`)**:
  - Ensure uniform tactile card dimensions across `default`, `selected`, `correct`, and `incorrect` states without font-weight shifting or height bouncing.
  - Apply hairline borders (`border-slate-200/80 dark:border-white/[0.08]`) and responsive mobile drawer fallback for viewports $< 1024\text{px}$.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Define `StudioLayout` 68/32 stage-to-dock proportion requirements and responsive mobile fallback specifications.

## Impact

- **Affected Surfaces**: `frontend/pages/quiz.vue`, `frontend/pages/review.vue`, `frontend/components/ui/OptionCard.vue`.
- **Dependencies**: None. Leverages existing `StudioLayout.vue` and Pinia stores.
- **Breaking Changes**: None.
