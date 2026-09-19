# Design: Modernize Reader Inline Code and Scenario Challenge UI

## Context

See `proposal.md` for motivation.

Following previous design modernization phases (`modernize-code-block-theme-ui`, `modernize-knowledge-graph-cosmos-ui`, and `modernize-roadmap-mindmap-ui`), inline code references across reading surfaces (`/today` and `/read/[bookId]`) and the scenario challenge option cards in `InterviewChallengePane.vue` retain styling remnants from earlier development sprints:
1. Hardcoded emerald inline code styling in `frontend/assets/css/main.css` and `frontend/pages/read/[bookId].vue`.
2. Legacy `dark:bg-slate-800`, `dark:border-slate-800/60`, and `dark:bg-slate-950/20` classes in `frontend/components/today/InterviewChallengePane.vue`.
3. Green-tinted container styling in the authoritative source excerpt box in `frontend/components/today/DocReaderPane.vue`.

This design document establishes the concrete styling contracts for unifying these surfaces with the Dev-Learning Studio tokens.

## Goals / Non-Goals

**Goals:**
- Replace green inline code styles with Dev-Learning Studio Obsidian badge tokens across all Markdown-rendered surfaces (`main.css`, `DocReaderPane.vue`, `[bookId].vue`).
- Eliminate all legacy dark slate classes from `InterviewChallengePane.vue`, aligning unselected and non-optimal options with `dark:bg-canvas-subtle` and `dark:border-white/[0.04]`.
- Modernize the authoritative source context box in `DocReaderPane.vue` to `.glass-panel` with hairline borders and Deep Iris Violet telemetry header.
- Preserve clear semantic success feedback for correct scenario options and point badges using refined, studio-grade translucent emerald tokens.

**Non-Goals:**
- Altering scenario drill submission logic, API payloads, or scoring algorithms.
- Changing the Shiki syntax highlighting engine or code block fence renderer.
- Modifying backend Markdown storage or crawler pipelines.

## Decisions

### 1. Global & Scoped Inline Code Styling Standardization

- **Global Markdown Inline Code (`frontend/assets/css/main.css`)**:
  - Update `.markdown-body code:not(pre code)`:
    ```css
    .markdown-body code:not(pre code) {
      @apply font-mono text-xs sm:text-sm px-2 py-0.5 rounded-lg bg-slate-100 text-slate-800 border border-slate-200/90 dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08] font-medium;
    }
    ```
  - **Rationale**: Elevates inline code into authentic developer syntax tokens using the Obsidian surface (`#18181b`) and Deep Iris Violet accent (`text-brand-300`), eliminating the visual confusion of green text that mimics success indicators or hyperlinks.

- **Dedicated Book Reader Scope (`frontend/pages/read/[bookId].vue`)**:
  - Remove conflicting Tailwind Typography prose modifiers:
    - Delete: `prose-code:text-emerald-600 dark:prose-code:text-emerald-400 prose-code:bg-slate-100 dark:prose-code:bg-slate-800/80`
    - Replace with: `prose-code:font-mono prose-code:px-2 prose-code:py-0.5 prose-code:rounded-lg prose-code:text-xs prose-code:sm:text-sm prose-code:bg-slate-100 prose-code:text-slate-800 prose-code:border prose-code:border-slate-200/90 dark:prose-code:bg-canvas-elevated dark:prose-code:text-brand-300 dark:prose-code:border-white/[0.08] prose-code:font-medium`
  - **Rationale**: Guarantees identical typography and syntax badge aesthetics whether reading in `/today` or the standalone `/read/[bookId]` view.

### 2. Authoritative Source Excerpt Box Modernization (`frontend/components/today/DocReaderPane.vue`)

- **Container Styling**:
  - Replace: `bg-emerald-500/5 dark:bg-emerald-950/20 border border-emerald-500/20`
  - With: `glass-panel dark:bg-canvas-subtle/80 border border-slate-200/80 dark:border-white/[0.08]`
- **Header Telemetry**:
  - Replace: `text-emerald-700 dark:text-emerald-400`
  - With: `text-brand-600 dark:text-brand-400 font-bold uppercase tracking-wider text-xs`
- **Rationale**: The excerpt is technical reference documentation, not a status or success indicator. Glass-panel styling with subtle violet telemetry creates high-contrast hierarchy without green visual noise.

### 3. Scenario Drill Option Cards & Feedback (`frontend/components/today/InterviewChallengePane.vue`)

- **Unselected & Reviewed Inactive Options**:
  - Replace: `border-slate-200/60 dark:border-slate-800/60 bg-slate-50/30 dark:bg-slate-950/20 text-slate-500 dark:text-slate-400 opacity-60`
  - With: `border-slate-200/60 dark:border-white/[0.04] bg-slate-50/30 dark:bg-canvas-subtle/30 text-slate-500 dark:text-slate-400 opacity-60`
  - Option letter badge (reviewed inactive): replace `bg-slate-200 dark:bg-slate-800 text-slate-400` with `bg-slate-200 dark:bg-canvas-elevated text-slate-400 border border-transparent dark:border-white/[0.06]`
- **Optimal Choice (Semantic Success)**:
  - Container: `border-emerald-500 dark:border-emerald-500/40 bg-emerald-50/80 dark:bg-emerald-500/10 text-emerald-950 dark:text-emerald-100 font-semibold ring-2 ring-emerald-500/20`
  - Option letter badge: `bg-emerald-600 text-white shadow-sm`
  - Status pill badge: `inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-bold bg-emerald-600 text-white shadow-sm whitespace-nowrap shrink-0`
- **Score Badge (`+10 Pts`)**:
  - Refactor to: `isCorrect ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-500/30' : 'bg-amber-50 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border-amber-200 dark:border-amber-500/30'`
- **Rationale**: Preserves standard UX semantic recognition for correctness while softening the harsh neon border contrast against `#09090b` obsidian backgrounds.

## Risks / Trade-offs

- **Component Tests**:
  - Tests in `frontend/tests/components/DocReaderPane.spec.ts` or related page tests might assert specific class names.
  - *Mitigation*: Run the full test suite (`npm --prefix frontend test`) and update any class assertions to match the new studio tokens.
