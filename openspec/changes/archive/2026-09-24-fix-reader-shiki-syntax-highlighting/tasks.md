# Tasks

## 1. Frontend - Shiki Highlighter & Language Support

- [x] 1.1 Add `'proto'` and `'razor'` to `SUPPORTED_LANGS` and configure language normalization aliases (`protobuf: 'proto'`, `razor: 'razor'`, `blazor: 'razor'`, `cshtml: 'razor'`) in `frontend/utils/shikiHighlighter.ts`
- [x] 1.2 Implement dedicated Protobuf syntax detector (`syntax = "proto3"`, `service`, `rpc`) and refine Go regex (`package\s+[a-zA-Z0-9_]+(?!\s*;)`) to avoid false-positive Go detection on Protobuf package statements
- [x] 1.3 Implement Razor/Blazor syntax detector (`@page`, `@code {`, `@inject`) and format display labels (`'proto' -> 'Protobuf'`, `'razor' -> 'Razor / Blazor'`) in `frontend/utils/shikiHighlighter.ts`

## 2. Frontend - Markdown Renderer & Dual-Theme Generation

- [x] 2.1 Update `frontend/composables/useMarkdownRenderer.ts` to import `CODE_THEME_LIGHT` and invoke `highlighter.codeToHtml` with `themes: { light: CODE_THEME_LIGHT, dark: CODE_THEME }`
- [x] 2.2 Update `frontend/assets/css/main.css` to scope dark-mode color overrides using `span[style*="--shiki-dark"]`, preventing destructive overrides on tokens lacking `--shiki-dark`
- [x] 2.3 Update `frontend/components/common/ShikiCodeBlock.vue` scoped styles to match the defensive `span[style*="--shiki-dark"]` selector

## 3. Frontend - Localization & TOC Defense

- [x] 3.1 Verify `reader.search_placeholder` and `reader.no_chapters_found` are present in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`
- [x] 3.2 Update `frontend/components/reader/ReaderTocSidebar.vue` to bind `$t('reader.search_placeholder')` and `$t('reader.no_chapters_found')`

## 4. Testing & Verification

- [x] 4.1 Update `frontend/tests/composables/markdownRenderer.spec.ts` with assertions verifying dual-theme token output (`--shiki-dark`), Protobuf tokenization, and Razor highlighting
- [x] 4.2 Execute unit test suites (`npm test`) and run production build (`npm run build`) to ensure zero regressions across all frontend components
