# Proposal: Modernize Code Block Theme to Dev-Learning Studio UI

## Why

Throughout TechDaily, code snippets serve as the core technical learning artifact—rendered across documentation slices (`/read/[bookId]`), daily focus reading (`/today`), scenario challenge explanations, AI term explainer dialogs, flashcard deep dives, and standalone code blocks (`ShikiCodeBlock.vue`).

The user-provided reference screenshot depicts the **legacy slate code block UI**:
- An opaque, boxy blue-slate container (`bg-slate-900 border-slate-800`).
- A mismatched dark header (`bg-slate-950/80 border-b border-slate-800/80`) with generic, low-contrast window dots and a heavy dark-gray copy button (`bg-slate-800 hover:bg-slate-700`).
- An outdated Shiki syntax theme (`one-dark-pro`), originally designed for Atom with an intrusive bluish background (`#282c34`) that clashes sharply with the platform's neutral obsidian palette.

To achieve complete aesthetic harmony with the **Dev-Learning Studio** visual language, all code rendering surfaces must be modernized and unified:
1. **Neutral Dark Obsidian Canvas**: Transition from legacy slate-900 to neutral dark obsidian (`#09090b` canvas, `#121215` `bg-canvas-subtle`) with translucent hairline borders (`border-white/[0.08]`).
2. **Modern Neutral Shiki Syntax Theme**: Migrate syntax highlighting from legacy `one-dark-pro` to modern neutral dark themes (`vitesse-dark` or `github-dark-default`) with transparent background inheritance and italicized comments.
3. **Unified Surface Consistency**: Unify `ShikiCodeBlock.vue`, `useMarkdownRenderer.ts` (markdown fences), and `frontend/assets/css/main.css` under the same terminal design system with macOS traffic-light window controls (`#ff5f56`, `#ffbd2e`, `#27c93f`), Deep Iris Violet language badges (`text-brand-400`), and glassmorphic copy buttons.

## What Changes

- **Shiki Syntax Highlighter Engine (`frontend/utils/shikiHighlighter.ts`)**:
  - Migrate `CODE_THEME` from legacy `one-dark-pro` to `vitesse-dark` (or `github-dark-default`), designed specifically for neutral dark obsidian surfaces.
  - Register the updated theme in `getShikiHighlighter()` and preserve multi-language support.
- **Markdown Fenced Code Renderer (`frontend/composables/useMarkdownRenderer.ts`)**:
  - Update `md.renderer.rules.fence` to render the Dev-Learning Studio container (`rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/90 dark:bg-canvas-subtle shadow-lg dark:shadow-2xl`).
  - Render a glassmorphic top rail (`bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06]`).
  - Modernize window controls to authentic macOS traffic-light dots (`#ff5f56` red, `#ffbd2e` amber, `#27c93f` green).
  - Style language badge with Deep Iris Violet typography (`text-brand-600 dark:text-brand-400 font-mono font-bold uppercase tracking-widest text-[11px] sm:text-xs`).
  - Upgrade copy button to a glassmorphic interactive button (`px-2.5 py-1 rounded-lg bg-white/80 dark:bg-white/[0.06] hover:bg-white dark:hover:bg-white/[0.12] border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-200 text-xs font-semibold shadow-xs transition-all active:scale-95`).
- **Standalone Code Component (`frontend/components/common/ShikiCodeBlock.vue`)**:
  - Align container, header bar, traffic-light dots, language label, and copy button to the exact same Dev-Learning Studio tokens.
- **Global Stylesheet (`frontend/assets/css/main.css`)**:
  - Update `.markdown-body pre`, `.markdown-body .code-block-wrapper`, and `.prose pre` to use `dark:bg-canvas-subtle` and hairline `dark:border-white/[0.08]`.
  - Enforce `background-color: transparent !important` on `pre.shiki` to eliminate nested color boxes.
  - Add italic styling rule for code comments (`font-style: italic !important`).
- **Automated Tests (`frontend/tests/composables/markdownRenderer.spec.ts`)**:
  - Update DOM class assertions and copy button structures to verify the modernized output.

## Capabilities

### Modified Capabilities

- `markdown-formatting`: Update requirement `Isolated Code Block Container Styling` to mandate Dev-Learning Studio obsidian container tokens (`dark:bg-canvas-subtle`, `dark:border-white/[0.08]`), glassmorphic studio header with traffic-light dots, brand-accented language badges, translucent copy buttons, and transparent-background Shiki syntax highlighting.
- `core-platform`: Update requirement `Dev-Learning Studio Design System & Navigation Shell` to incorporate the universal code block terminal standard as an explicit component design specification.

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Consistency**: Eradicates the legacy blue-slate styling depicted in the screenshot, delivering a unified, executive developer terminal experience across the entire platform.
