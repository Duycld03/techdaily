# Design: Practice Studio & Core Assessment Modernization

## Context

Following the establishment of the neutral obsidian canvas (`#09090b`), Deep Iris Violet (`#7c3aed`), and hairline border utilities in Phase 5 and the palette refinement change, the assessment pages (`/quiz`, `/review`), error handling (`error.vue`), and authentication (`/login`) need to be upgraded to match the design language. These views contain complex internal states including multiple tabs, question arenas, flashcard flip cards, and modal dialogs. See `proposal.md - Why` for motivation.

## Goals / Non-Goals

**Goals:**
- Replace legacy dark slate (`dark:bg-slate-950`, `dark:bg-slate-900/60`, `dark:border-slate-800`) across all 5 quiz tabs and 2 review tabs with `.glass-card` and `.glass-panel`.
- Unify interactive option card states in `/quiz` (default, selected, correct, incorrect) with clean hairline borders and subtle tinting.
- Modernize all modals (`cardToEdit`, `cardToReset`, `cardToDelete`) in `/review` with glassmorphic backdrop blur and dark inputs.
- Replace legacy emerald green in `error.vue` and `login.vue` with Deep Iris Violet.
- Maintain full functional reactivity, keyboard navigation (1-4, Enter, Arrow keys), and zero test regressions.

**Non-Goals:**
- Modifying backend APIs, SM-2 algorithms, or quiz generation logic.
- Redesigning routes outside of Phase 6 scope (Library, Notes, Insights, Profile, Settings are scoped for Phase 7 and Phase 8).

## Decisions

### Decision 1: Consistent Glassmorphic Tab Switchers
- **Choice**: Standardize the top tab bar in both `/quiz` and `/review` with:
  - Wrapper: `bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06] p-1 rounded-2xl`
  - Active Tab: `bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border border-transparent dark:border-white/[0.06]`
  - Inactive Tab: `text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white`
- **Rationale**: Provides visual cohesion with the existing `/today` view mode switcher and eliminates harsh contrasts.

### Decision 2: Arena Multiple-Choice Card Visual States
- **Choice**:
  - Unselected: `border-slate-200 dark:border-white/[0.06] bg-white dark:bg-white/[0.03] text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-white/[0.15]`
  - User Selected: `border-brand-500 bg-brand-50/50 dark:bg-brand-500/10 text-brand-900 dark:text-white ring-1 ring-brand-500/30`
  - Correct Answer: `border-emerald-500/80 bg-emerald-50/70 dark:bg-emerald-500/10 text-emerald-900 dark:text-emerald-300 ring-1 ring-emerald-500/30`
  - Incorrect Answer: `border-rose-500/80 bg-rose-50/70 dark:bg-rose-500/10 text-rose-900 dark:text-rose-300 ring-1 ring-rose-500/30`
- **Rationale**: Replaces heavy opaque colors with translucent dark glass, matching the dark studio aesthetic.

### Decision 3: Review Modal Elevation & Sub-Tabs
- **Choice**: Modals in `review.vue` will use `glass-panel glow-subtle border border-slate-200 dark:border-white/[0.08]` with dark inputs `dark:bg-canvas-subtle dark:border-white/[0.08]`. The Markdown Edit/Preview tab switcher inside `cardToEdit` uses the standard studio pill layout.
- **Rationale**: Eliminates jarring opaque white/black dialogs.

### Decision 4: Error Page Refinement
- **Choice**: Update `error.vue` to use `dark:bg-canvas`, a centered `.glass-panel` container, and Deep Iris Violet action buttons (`bg-brand-600 hover:bg-brand-500`).
- **Rationale**: Removes the last remnant of the old emerald theme seen during system errors or 404s.

## Risks / Trade-offs

- **Risk**: Test assertions in `quiz.spec.ts` or `review.spec.ts` that check for exact class names (e.g. `dark:bg-slate-900`) may fail.
  - **Mitigation**: Checked existing specs; tests primarily assert `data-testid`, text labels, and user event reactions. Any brittle class checks will be updated to reflect the new tokens.
