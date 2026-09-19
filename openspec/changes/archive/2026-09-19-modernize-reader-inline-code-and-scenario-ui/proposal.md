# Proposal: Modernize Reader Inline Code and Scenario Challenge UI

## Why

Following the modernization of code blocks (to Shiki Vitesse-Dark and Obsidian card containers in `modernize-code-block-theme-ui`), Knowledge Graph (`modernize-knowledge-graph-cosmos-ui`), and Roadmap (`modernize-roadmap-mindmap-ui`), inline code elements and scenario challenge widgets on `/today` and `/read/[bookId]` retain legacy styling artifacts that conflict with the Dev-Learning Studio design language:
1. **Harsh Emerald Inline Code**: All inline code tokens in Markdown (`.markdown-body code:not(pre code)`) and `/read/[bookId]` are hardcoded to green/emerald tints (`bg-emerald-500/10 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-500/20`). This visual treatment creates visual confusion by mimicking positive evaluation badges rather than technical syntax tokens, and clashes with the neutral Obsidian `#09090b` canvas.
2. **Legacy Slate Tokens in Scenario Options**: In `InterviewChallengePane.vue`, unselected and non-optimal options when reviewed still use legacy slate classes (`dark:bg-slate-800`, `dark:border-slate-800/60`, `dark:bg-slate-950/20`), creating dull blue-gray seams.
3. **Over-Saturated Success & Source Badges**: The correct option card, `+10 Pts` pill, and `DocReaderPane.vue` authoritative source context box use overly opaque or saturated emerald fills rather than the refined translucent studio aesthetic with hairline borders.

Standardizing both reader surfaces (`/today` and `/read/[bookId]`) and the scenario drill interface aligns the entire reading and evaluation workflow with the Dev-Learning Studio visual standard.

## What Changes

- **Global & Scoped Inline Code Modernization**:
  - Refactor `.markdown-body code:not(pre code)` in `frontend/assets/css/main.css` from legacy green to a sleek Dev-Learning Studio terminal badge: neutral light background (`bg-slate-100 text-slate-800 border-slate-200`) in light mode, and Obsidian pill (`dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08]`) in dark mode.
  - Refactor article typography overrides in `frontend/pages/read/[bookId].vue` to eliminate `prose-code:text-emerald-600 dark:prose-code:text-emerald-400 prose-code:bg-slate-100 dark:prose-code:bg-slate-800/80`, inheriting the studio standard.
- **Authoritative Source Excerpt Modernization**:
  - Refactor the authoritative source excerpt box in `frontend/components/today/DocReaderPane.vue` from `bg-emerald-500/5 dark:bg-emerald-950/20 border-emerald-500/20` to `.glass-panel` with `dark:bg-canvas-subtle/80`, `dark:border-white/[0.08]`, and Deep Iris Violet telemetry header (`text-brand-600 dark:text-brand-400`).
- **Scenario Challenge Option & Feedback Refinement**:
  - In `frontend/components/today/InterviewChallengePane.vue`, replace all legacy slate tokens on unselected and reviewed options (`dark:bg-slate-800`, `dark:border-slate-800/60`, `dark:bg-slate-950/20`) with `dark:bg-canvas-subtle`, `dark:border-white/[0.04]`, and `text-slate-500 dark:text-slate-400`.
  - Refine the optimal choice container and letter badge to use studio-grade translucent emerald (`border-emerald-500 dark:border-emerald-500/40 bg-emerald-50/80 dark:bg-emerald-500/10 text-emerald-950 dark:text-emerald-100 ring-2 ring-emerald-500/20`), preserving essential semantic success feedback while eliminating jarring contrast against obsidian backgrounds.
  - Refactor the score badge (`+10 Pts`) to use translucent pill styling with hairline border (`dark:bg-emerald-950/40 dark:border-emerald-500/30 dark:text-emerald-300`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `reader`: Modernize inline code rendering specification across both the dedicated reader (`/read/[bookId]`) and daily focus reader (`/today`), replacing legacy emerald styling with studio-grade Obsidian badges.
- `today`: Update scenario multiple-choice option rendering and authoritative source excerpt boxes to eliminate all legacy dark slate classes, standardizing on studio canvas tokens and refined translucent semantic feedback.

## Impact

- **UI & Design System**: 100% visual consistency between reading context, inline code references, code fences, and scenario drill components.
- **API & Domain Contracts**: Zero breaking changes. No backend or database modifications.
- **Automated Tests**: Existing unit tests in `frontend/tests` continue to pass; any class assertions in component tests are verified and updated.
