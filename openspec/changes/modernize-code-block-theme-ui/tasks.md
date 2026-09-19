# Tasks

## 1. Shiki Highlighter & Syntax Theme (Frontend)

- [x] 1.1 Update `frontend/utils/shikiHighlighter.ts`: change `CODE_THEME` from legacy `one-dark-pro` to `vitesse-dark` (or `github-dark-default`), ensuring it is registered in `createHighlighter` themes for neutral obsidian surface compatibility.
- [x] 1.2 Update `frontend/assets/css/main.css`: update `.code-block-wrapper`, `.markdown-body pre`, and `.prose pre` to use `dark:bg-canvas-subtle` and hairline `dark:border-white/[0.08]`, enforce `background-color: transparent !important` on `pre.shiki`, and add italic styling for code comments.

## 2. Code Block Component & Markdown Renderer Unification (Frontend)

- [x] 2.1 Update `frontend/composables/useMarkdownRenderer.ts`: refactor `md.renderer.rules.fence` to render the Dev-Learning Studio container (`rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/90 dark:bg-canvas-subtle shadow-lg dark:shadow-2xl`), glassmorphic header bar (`bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md`), traffic-light window controls (`#ff5f56`, `#ffbd2e`, `#27c93f`), uppercase brand-accented language badge (`text-brand-600 dark:text-brand-400`), and glassmorphic copy button.
- [x] 2.2 Update `frontend/components/common/ShikiCodeBlock.vue`: align container, header, traffic-light dots, language badge, and copy button to match the exact same Dev-Learning Studio tokens, unifying styling across all code rendering surfaces.

## 3. Automated Verification & Testing

- [x] 3.1 Update unit tests in `frontend/tests/composables/markdownRenderer.spec.ts` to assert against the new Dev-Learning Studio container classes, window controls, and copy button structure.
- [x] 3.2 Run frontend unit test suite (`npm --prefix frontend test`) to ensure 100% test pass rate across all suites.
- [x] 3.3 Validate OpenSpec change specifications and sync requirements with `openspec validate --changes` and `openspec validate --specs`.
