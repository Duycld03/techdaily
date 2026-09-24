# Design: Reader Shiki Syntax Highlighting & Dual-Theme Resilience

## Context

See `proposal.md - Why` for motivation.

In the reader interface (`frontend/pages/read/[bookId].vue`), markdown code fences are rendered through `useMarkdownRenderer.ts`, which initializes Shiki client-side. The stylesheet `frontend/assets/css/main.css` contains dual-theme rules:
```css
html.dark .shiki,
html.dark .shiki span,
.dark .shiki,
.dark .shiki span {
  color: var(--shiki-dark) !important;
}
```
Because `useMarkdownRenderer.ts` passed `theme: CODE_THEME` (`vitesse-dark`) instead of `themes: { light, dark }`, Shiki emitted inline styles without the `--shiki-dark` CSS variable. In dark mode, the browser resolved `var(--shiki-dark)` to undefined, falling back to the inherited text color and collapsing all syntax tokens into plain monochrome white text across all languages.

Furthermore, `shikiHighlighter.ts` lacked `proto` and `razor` in `SUPPORTED_LANGS`, resulting in Protobuf snippets falling back to Go heuristics and Razor/Blazor components failing to tokenize.

## Goals / Non-Goals

**Goals:**
- Guarantee rich, multi-color syntax highlighting for all supported languages in both Dark and Light modes.
- Preserve token colors even when `--shiki-dark` is missing or when third-party components render single-theme blocks.
- Natively support Protocol Buffers (`proto` / `protobuf`) and Razor/Blazor (`razor` / `blazor`) with high-tech badge headers.
- Prevent TOC localization regressions with complete `reader.search_placeholder` and `reader.no_chapters_found` entries.

**Non-Goals:**
- Replacing Shiki with Prism, Highlight.js, or low-fidelity regex tokenizers.
- Altering the backend ingestion pipeline or database chunk markdown data.
- Introducing client-side heavy compiler runtimes for executing code snippets.

## Decisions

### Decision 1: Standardize Markdown Renderer on Dual-Theme Shiki Output
In `frontend/composables/useMarkdownRenderer.ts`, pass `themes: { light: CODE_THEME_LIGHT, dark: CODE_THEME }` to `highlighter.codeToHtml`.
- **Rationale:** Aligns with `shikiHighlighter.ts` (`highlightCode`), generating inline styles for Light Mode and `--shiki-dark` custom properties on every token `<span>` for Dark Mode.
- **Alternative considered:** Re-rendering markdown every time the color mode changes. Rejected due to unnecessary CPU overhead, DOM thrashing, and potential scroll jump during reading sessions.

### Decision 2: Defensive Attribute Selector in Global and Scoped Styles
Update `frontend/assets/css/main.css` and `frontend/components/common/ShikiCodeBlock.vue` to use:
```css
html.dark .shiki[style*="--shiki-dark"],
html.dark .shiki span[style*="--shiki-dark"],
.dark .shiki[style*="--shiki-dark"],
.dark .shiki span[style*="--shiki-dark"] {
  color: var(--shiki-dark) !important;
}
```
- **Rationale:** If `--shiki-dark` is present, it dynamically applies the dark theme color. If `--shiki-dark` is absent (such as single-theme or custom blocks), the CSS rule will not match, preserving the token's inline color rather than resetting it to inherited white text.

### Decision 3: Native Protobuf & Razor Bundling and Disambiguation
In `frontend/utils/shikiHighlighter.ts`:
- Add `'proto'` and `'razor'` to `SUPPORTED_LANGS`.
- Add aliases: `protobuf: 'proto'`, `razor: 'razor'`, `blazor: 'razor'`, `cshtml: 'razor'`.
- Insert a Protobuf detector before the Go detector to prevent `package greet;` from triggering Go syntax detection.
- Refine Go detection regex to negative-lookahead on semicolons (`package\s+[a-zA-Z0-9_]+(?!\s*;)`).
- Add Razor detector for `@page`, `@code {`, and `@inject`.
- Map display labels: `'proto' -> 'Protobuf'`, `'razor' -> 'Razor / Blazor'`.

### Decision 4: Safe TOC Localization Catalog Keys
Ensure `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` declare:
- `reader.search_placeholder`: `"Search chapters..."` (EN) / `"Tìm kiếm chương..."` (VI)
- `reader.no_chapters_found`: `"No chapters found"` (EN) / `"Không tìm thấy chương phù hợp"` (VI)

## Risks / Trade-offs

- **Risk:** Additional grammar bundles (`proto`, `razor`) could increase client bundle size.
  - **Mitigation:** Shiki bundles grammars as lightweight JSON/WASM chunks loaded on-demand. Total overhead is under 25KB, well within Nuxt performance budgets.
- **Risk:** Vitest snapshot or unit test assertions expecting single-theme classnames (`shiki vitesse-dark`).
  - **Mitigation:** Update Vitest test cases in `markdownRenderer.spec.ts` to assert `shiki` and `vitesse-dark` classes while verifying dual-theme token output.
