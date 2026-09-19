# Design: Modernize Code Block Theme to Dev-Learning Studio UI

## Context

See `proposal.md` for motivation.

The screenshot provided by the user exhibits the **legacy slate code block UI**:
- A dark blue-slate box (`bg-slate-900 border-slate-800`) with an opaque header (`bg-slate-950/80`) and blocky gray copy button (`bg-slate-800 hover:bg-slate-700`).
- Hardcoded Shiki syntax highlighting theme `one-dark-pro`, an Atom-legacy theme featuring bluish background tints (`#282c34`) and low contrast against neutral obsidian.

This widget sharply conflicts with the **Dev-Learning Studio** visual language, which is built on neutral obsidian `#09090b` canvas, `#121215` `bg-canvas-subtle` surfaces, `#18181b` `bg-canvas-elevated` panels, and translucent hairline borders `border-white/[0.08]`.

Code is rendered in two primary pathways in TechDaily:
1. **Markdown-rendered fenced code blocks** via `frontend/composables/useMarkdownRenderer.ts` using a custom MarkdownIt `md.renderer.rules.fence` rule (affecting `/read/[bookId]`, `/today`, scenario challenge feedback, and term explainers).
2. **Standalone code block components** via `frontend/components/common/ShikiCodeBlock.vue`.

This design document specifies the complete unification of both pathways and global CSS styles under the Dev-Learning Studio terminal card standard.

## Goals / Non-Goals

**Goals:**
- Replace legacy slate styling with neutral dark obsidian (`dark:bg-canvas-subtle` / `#121215`) and hairline borders (`dark:border-white/[0.08]`).
- Upgrade Shiki syntax theme to `vitesse-dark` (or `github-dark-default`), designed specifically for neutral dark surfaces.
- Modernize the terminal top rail with macOS traffic-light window controls (`#ff5f56`, `#ffbd2e`, `#27c93f`), Deep Iris Violet language telemetry (`text-brand-400`), and a translucent glassmorphic Copy button.
- Unify code styling across `useMarkdownRenderer.ts`, `ShikiCodeBlock.vue`, and `frontend/assets/css/main.css`.
- Ensure italic styling for syntax-highlighted code comments (`font-style: italic !important`).

**Non-Goals:**
- Adding interactive code execution, linting, or in-browser REPL capabilities.
- Modifying backend Markdown storage or crawler pipelines.
- Replacing the client-side Shiki highlighter engine.

## Decisions

### 1. Shiki Syntax Theme Selection & Canvas Inheritance

- **Theme Migration:**
  - Change `CODE_THEME` in `frontend/utils/shikiHighlighter.ts` from `one-dark-pro` to `vitesse-dark` (with `github-dark-default` as an alternate high-contrast neutral theme).
  - `vitesse-dark` provides a clean, neutral dark palette that eliminates the blue-gray background tint of Atom's `one-dark-pro`.
- **Canvas Inheritance (`bg-transparent`):**
  - In `frontend/assets/css/main.css`, strictly enforce `background-color: transparent !important` on `pre.shiki`, `.code-block-wrapper pre`, and `.code-content pre`.
  - This ensures syntax-highlighted code seamlessly inherits the container's `dark:bg-canvas-subtle` (`#121215`) background without awkward nested color seams.
- **Comment Typography:**
  - Add CSS rule to italicize code comments across all highlighted blocks:
    ```css
    .code-block-wrapper pre .comment,
    .code-content pre .comment,
    .shiki span[style*="color:#5c6370"],
    .shiki span[style*="color:#7f848e"],
    .shiki span[style*="color:#6272a4"],
    .shiki span[style*="color:#6a737d"] {
      font-style: italic !important;
    }
    ```

### 2. Container & Surface Architecture

- **Outer Container:**
  - Classes: `code-block-wrapper relative group my-6 sm:my-8 rounded-2xl overflow-hidden border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/90 dark:bg-canvas-subtle shadow-lg dark:shadow-2xl max-w-full w-full min-w-0 font-mono text-sm sm:text-[14.5px]`
  - Delivers a smooth, rounded card geometry with hairline borders matching `.glass-panel` and `.glass-card`.
- **Top Header Rail:**
  - Classes: `flex items-center justify-between px-4 sm:px-5 py-2.5 bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06] text-xs select-none`
  - Uses `.glass-panel` elevation with subtle translucent border.

### 3. Header Controls & Telemetry

- **Traffic-Light Window Controls:**
  - Red dot: `w-2.5 h-2.5 rounded-full bg-[#ff5f56]`
  - Amber dot: `w-2.5 h-2.5 rounded-full bg-[#ffbd2e]`
  - Green dot: `w-2.5 h-2.5 rounded-full bg-[#27c93f]`
  - Gap: `gap-2`
  - Replaces generic low-contrast dots with crisp, authentic developer terminal indicators.
- **Language Telemetry Badge:**
  - Classes: `text-brand-600 dark:text-brand-400 font-bold uppercase tracking-widest text-[11px] sm:text-xs font-mono ml-2.5`
  - Uses Deep Iris Violet brand color for high-tech aesthetic.
- **Glassmorphic Copy Button:**
  - Classes: `flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/80 dark:bg-white/[0.06] hover:bg-white dark:hover:bg-white/[0.12] border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-200 text-xs font-semibold shadow-xs transition-all active:scale-95 cursor-pointer`
  - When copied, smoothly transitions to `text-emerald-500 dark:text-emerald-400` with "Copied!" text.

### 4. Complete Component Unification

- Both `useMarkdownRenderer.ts` (markdown fence rule) and `ShikiCodeBlock.vue` (standalone component) will share identical class structures, HTML hierarchy, dot colors, language badge formatting, and copy button styling.

## Risks / Trade-offs

- **Unit Test Class Assertions:**
  - `frontend/tests/composables/markdownRenderer.spec.ts` verifies specific DOM classes on rendered code blocks (e.g. `bg-slate-900`, `border-slate-800`).
  - *Mitigation*: Update test assertions to match the new studio classes (`dark:bg-canvas-subtle`, `dark:border-white/[0.08]`, `copy-code-btn`) and ensure test suites pass with 100% success.
