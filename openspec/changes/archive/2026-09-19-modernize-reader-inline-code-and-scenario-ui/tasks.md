# Tasks

## 1. Frontend (Inline Code Modernization & Book Reader)

- [x] 1.1 Refactor `.markdown-body code:not(pre code)` in `frontend/assets/css/main.css` to use Dev-Learning Studio Obsidian tokens (`bg-slate-100 dark:bg-canvas-elevated text-slate-800 dark:text-brand-300 border border-slate-200/90 dark:border-white/[0.08] rounded-lg font-medium`).
- [x] 1.2 Refactor typography prose classes in `frontend/pages/read/[bookId].vue` to remove green code overrides (`prose-code:text-emerald-600 dark:prose-code:text-emerald-400 prose-code:bg-slate-100 dark:prose-code:bg-slate-800/80`) and align with studio tokens.

## 2. Frontend (Today Studio Authoritative Excerpt & Scenario Options)

- [x] 2.1 Refactor authoritative source excerpt container in `frontend/components/today/DocReaderPane.vue` from green tint to `.glass-panel` with `dark:bg-canvas-subtle/80`, `dark:border-white/[0.08]`, and Deep Iris Violet telemetry header (`text-brand-600 dark:text-brand-400`).
- [x] 2.2 Refactor unselected and reviewed inactive option cards in `frontend/components/today/InterviewChallengePane.vue` to eliminate all legacy dark slate classes (`dark:bg-slate-800`, `dark:border-slate-800/60`, `dark:bg-slate-950/20`), replacing them with `dark:bg-canvas-subtle`, `dark:border-white/[0.04]`, and studio letter badge tokens.
- [x] 2.3 Refine optimal choice card, checkmark pill badge, and score indicator badge in `frontend/components/today/InterviewChallengePane.vue` to studio-grade translucent emerald styling (`dark:border-emerald-500/40`, `dark:bg-emerald-500/10`, `dark:ring-emerald-500/20`, and `dark:bg-emerald-950/40`).

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure all test files pass without regression.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
