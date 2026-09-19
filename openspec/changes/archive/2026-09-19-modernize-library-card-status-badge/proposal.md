# Proposal: Modernize Library Card Status Badge

## Why

In the document catalog (`frontend/pages/library.vue`), books ready to read without an active bookmark display a bright green badge (`✓ Sẵn sàng đọc` / `Ready to Read`) using `bg-emerald-50 dark:bg-emerald-500/10 border-emerald-200/80 dark:border-emerald-500/20 text-emerald-700 dark:text-emerald-400`. This visual treatment clashes with the surrounding Deep Iris Violet and neutral Obsidian Dev-Learning Studio theme, and violates the semantic color standard where emerald is strictly reserved for positive evaluation results (correct quiz/scenario solutions) rather than static catalog availability.

Replacing this badge with a neutral Dev-Learning Studio Obsidian badge (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400` with `BookOpen` icon) establishes visual harmony across the catalog and enforces semantic color discipline.

## What Changes

- **Ready to Read Badge Modernization (`frontend/pages/library.vue`)**:
  - Refactor the ready status badge from harsh emerald (`bg-emerald-50 dark:bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 border-emerald-200/80 dark:border-emerald-500/20`) to Dev-Learning Studio neutral badge tokens: `bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 text-xs sm:text-sm font-semibold`.
  - Replace the `CheckCircle2` icon with `BookOpen` (`class="w-3.5 h-3.5 text-slate-500 dark:text-slate-400"`), signaling reading readiness without mimicking quiz evaluation success.
  - Clean up type assertion `(book.status as any) === 2` to `book.status === 'Ready' || (book.status as unknown as number) === 2` to comply with the project's `ts-no-any` rule.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `library`: Update `Requirement: Clean Library Card Presentation` to specify that "Ready to Read" status badges render as neutral studio badges rather than emerald green indicators.

## Impact

- **API & Domain Contracts**: Zero breaking changes. Purely frontend styling and template presentation.
- **Frontend Components**: `frontend/pages/library.vue`.
- **Unit Tests**: Existing tests in `frontend/tests/pages/library.spec.ts` continue to pass.
