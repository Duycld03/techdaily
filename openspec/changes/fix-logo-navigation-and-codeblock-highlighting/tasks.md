# Tasks

## 1. Application Shell Navigation

- [x] 1.1 In `frontend/components/layout/AppHeader.vue`, update the TechDaily brand logo link from `to="/today"` to `to="/"` so clicking the logo returns to the root Home Bento Dashboard.

## 2. Multi-Language Shiki Highlighting & Markdown Formatting

- [x] 2.1 In `frontend/utils/shikiHighlighter.ts`, route `vue` language tokenization to `typescript` when code lacks `<template>` and `<script>` tags, ensuring rich syntax coloring for headless Vue Composition API snippets.
- [x] 2.2 In `frontend/utils/shikiHighlighter.ts`, expand `SUPPORTED_LANGS` with `diff`, `nginx`, `powershell`, and `xml`, and add alias normalizations (`ps`, `powershell`, `pwsh`, `diff`, `patch`, `nginx`, `conf`, `xml`, `svg`).
- [x] 2.3 In `frontend/pages/insights.vue`, update `renderMarkdown` to unescape double-escaped backticks (`\\`` -> `` ` ``) and normalize literal newlines, ensuring inline code blocks render inside HTML `<code>` tags.
- [x] 2.4 In `frontend/components/common/ShikiCodeBlock.vue` and `frontend/composables/useMarkdownRenderer.ts`, verify headless Vue script tokenization and display labels across both dark and light modes.

## 3. Verification & Automated Testing

- [x] 3.1 Validate OpenSpec change specifications and main specs with `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Run frontend unit test suite (`npm test` in `frontend/`) and verify Nuxt production build (`npm run build` in `frontend/`).
