# Tasks

## 1. Frontend Library Card Badge Modernization

- [x] 1.1 Refactor the ready status badge in `frontend/pages/library.vue` from emerald styling to Dev-Learning Studio neutral Obsidian tokens (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 text-xs sm:text-sm font-semibold`), replace `CheckCircle2` with `BookOpen` icon, and eliminate `(book.status as any)`.

## 2. Automated Verification & Testing

- [x] 2.1 Add unit test in `frontend/tests/pages/library.spec.ts` asserting that unread ready book cards render with the neutral Obsidian `BookOpen` badge and no emerald styling classes.
- [x] 2.2 Run full frontend test suite (`npm --prefix frontend test`) and validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
