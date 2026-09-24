# Proposal: Fix Reader Shiki Syntax Highlighting & Dual-Theme Resilience

## Why

Code blocks across all programming languages (C#, Razor, Protobuf, TypeScript, Go, Python, Bash, etc.) in the Reader view (`read/[bookId].vue`) currently render as plain monochrome white text without syntax coloring in Dark Mode. This occurs because the markdown renderer invokes Shiki in single-theme mode (`theme: 'vitesse-dark'`) rather than dual-theme mode (`themes: { light, dark }`), leaving the `--shiki-dark` CSS variable undefined on every token `<span>`. Consequently, the global dark-mode stylesheet rule `html.dark .shiki span { color: var(--shiki-dark) !important; }` forcibly strips inline token colors and collapses all code into inherited white text. Additionally, Protobuf definitions are misclassified as Go, and Razor/Blazor component syntax (`.razor`) lacks native grammar support and header badges.

## What Changes

- **Dual-Theme Highlighting in Markdown Renderer:** Configure `useMarkdownRenderer.ts` to invoke `highlighter.codeToHtml` using dual themes (`themes: { light: CODE_THEME_LIGHT, dark: CODE_THEME }`), ensuring that each token `<span>` receives inline colors and the `--shiki-dark` CSS variable.
- **Defensive CSS Dark-Mode Token Targeting:** Update `frontend/assets/css/main.css` and `frontend/components/common/ShikiCodeBlock.vue` to scope dark-mode color overrides using the attribute selector `span[style*="--shiki-dark"]`. This guarantees that tokens with `--shiki-dark` switch to Vitesse Dark, while any single-theme or custom tokens retain their inline styles without collapsing into monochrome text.
- **Protocol Buffers (`proto` / `protobuf`) Support & Disambiguation:** Add `'proto'` to bundled languages, normalize `'protobuf'`, introduce a dedicated Protobuf syntax detector (`syntax = "proto3"`, `service`, `rpc`), and refine Go detection regex (`package\s+[a-zA-Z0-9_]+(?!\s*;)`) to prevent false-positive Go tagging on Protobuf/Java package statements.
- **Razor / Blazor (`razor` / `blazor`) Support:** Add `'razor'` to bundled languages, support aliases (`'blazor'`, `'cshtml'`), introduce syntax detection for Razor directives (`@page`, `@code {`, `@inject`), and map display labels to `"Razor / Blazor"`.
- **Localization Defense for Reader TOC:** Ensure table-of-contents search placeholder and empty state strings (`reader.search_placeholder`, `reader.no_chapters_found`) are populated in both English and Vietnamese locale catalogs (`en.json` and `vi.json`).

## Capabilities

### New Capabilities
*None.*

### Modified Capabilities
- `markdown-formatting`: Update syntax highlighting requirements to mandate dual-theme token generation (`--shiki-dark`), non-destructive CSS scoping, and expanded grammar support for `proto` and `razor`.

## Impact

- **Frontend Core:** `frontend/composables/useMarkdownRenderer.ts`, `frontend/assets/css/main.css`, `frontend/components/common/ShikiCodeBlock.vue`, `frontend/utils/shikiHighlighter.ts`, `frontend/components/reader/ReaderTocSidebar.vue`.
- **Localization:** `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Test Suite:** `frontend/tests/composables/markdownRenderer.spec.ts`, `frontend/tests/components/reader/ReaderComponents.spec.ts`.
- **Backward Compatibility:** Zero breaking changes. All existing markdown content and code blocks will immediately regain full multi-color syntax highlighting in both Dark and Light modes.
