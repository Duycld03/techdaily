# Proposal: Frontend UI Phase 2 - Interactive Practice & Arena (StudioLayout & Preview Verification)

## Why

TechDaily's interactive practice surfaces (`quiz.vue` Arena mode and `review.vue` Tab 1 Flashcard Session) suffer from visual geometry defects on standard Windows 11 1080p and 2K displays:
1. **Flashcard Review Session Black Void & Jitter**: The active flashcard card in `review.vue` Tab 1 (`FlashcardDeck.vue`) stretches into a cavernous empty black void without balanced vertical proportions or compact engineering density (8/10).
2. **Telemetry Dock Misalignment & i18n Leakage**: The companion telemetry dock reveals unlocalized raw keys (`review.session_progress`), lacks cohesive styling, and suffers from rigid vertical spacing.
3. **Quiz Arena Mode Jitter**: In `quiz.vue`, question prompts, syntax-highlighted code blocks, and `OptionCard.vue` choices experience layout shifts and height bouncing across `default`, `selected`, `correct`, and `incorrect` answering states.

To prevent unverified production regressions, this change mandates an isolated **Playground Preview Prototyping** step in `frontend/pages/playground/temp.vue` with headless 1080p screenshots (Light and Dark mode) for user approval prior to production cutover.

## What Changes

- **1. Isolated Playground Preview Prototyping (`frontend/pages/playground/temp.vue`)**:
  - Build mock-driven interactive prototypes covering:
    - View A: **Quiz Arena Studio** (68% stage with prompt, code block, and 4 `OptionCard` states + 32% dock with live countdown timer, streak multiplier, seniority badge, and question progress map).
    - View B: **Flashcard 3D Practice Studio** (68% stage with balanced 3D flip card, front/back content, and SM-2 grading CTA bar + 32% dock with session progress bar, SM-2 metrics card, and keyboard shortcuts guide).
  - Capture dual Light/Dark mode screenshots for user review and formal approval.

- **2. Flashcard Active Review Session (`frontend/pages/review.vue` Tab 1 & `FlashcardDeck.vue`)**:
  - Adopt strict `StudioLayout` geometry with 68% `#main` stage and 32% `#dock` telemetry.
  - Eliminate the huge empty black void: enforce balanced card height (`min-h-[320px] max-h-[520px]`), centered vertical rhythm, and smooth 3D flip transform without layout jumps.
  - Fix Telemetry Dock i18n: add and bind `$t('review.session_progress')` ("Session Progress" / "Tiến Độ Phiên Ôn Tập"), and localize all dock badges and counters.
  - Standardize SM-2 grading buttons with high-contrast tactile pills (`[1] Blackout`, `[2] Hard`, `[3] Good`, `[4] Easy`).

- **3. Quiz Arena Mode (`frontend/pages/quiz.vue` & `frontend/components/ui/OptionCard.vue`)**:
  - Standardize `quiz.vue` on `StudioLayout` (68% question stage, 32% telemetry dock).
  - Refactor `OptionCard.vue` to guarantee zero layout shift: uniform padding, hairline borders (`border-slate-200/80 dark:border-white/[0.08]`), fixed tactile option badges (`A`, `B`, `C`, `D`), and stable text heights across all answer states.
  - Streamline code block presentation within quizzes using `ShikiCodeBlock.vue`.

- **4. Complete Bilingual Internationalization (`en.json` & `vi.json`)**:
  - Add missing interactive session keys: `review.session_progress`, `quiz.streak_multiplier`, `quiz.question_map`, `quiz.time_remaining`, `review.flip_card_hint`.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Define `StudioLayout` 68/32 stage-to-dock proportion requirements and responsive mobile fallback specifications for interactive practice surfaces.
- `quiz`:
  - Enforce `OptionCard` tactile geometry invariants and zero-layout-shift state transitions.
- `review`:
  - Standardize active 3D flip-card practice player density and telemetry dock completeness.

## Impact

- **Affected Surfaces**:
  - `frontend/pages/playground/temp.vue` (prototype preview)
  - `frontend/pages/quiz.vue`
  - `frontend/pages/review.vue` (Tab 1)
  - `frontend/components/review/FlashcardDeck.vue`
  - `frontend/components/ui/OptionCard.vue`
  - `frontend/i18n/locales/en.json`
  - `frontend/i18n/locales/vi.json`
- **Dependencies**: None. Leverages existing `StudioLayout.vue`, `@vueuse/core`, and Pinia stores.
- **Breaking Changes**: None. Quiz and review store contracts and backend API endpoints remain unchanged.
