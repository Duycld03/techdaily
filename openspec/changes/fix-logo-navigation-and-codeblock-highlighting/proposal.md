# Proposal: Fix Logo Home Navigation and Code Block Multi-Language Highlighting

## Why

Two usability and rendering defects currently affect the user interface:
1. **Logo Navigation**: Clicking the TechDaily brand logo in the application header (`AppHeader.vue`) navigates to `/today` instead of the root Home Bento Dashboard (`/`), violating standard web expectations where the primary brand logo returns users to their home overview.
2. **Code Block Highlighting Failures**: In code showcases (such as `/insights` and reading slices), code snippets labeled or tagged as `vue` that contain pure TypeScript/JavaScript Composition API logic without `<template>` or `<script>` tags fail to be highlighted by Shiki, because Shiki's TextMate Vue grammar treats untagged content as raw template text and renders monochromatic unstyled text. Furthermore, missing languages in `SUPPORTED_LANGS` (such as `diff`, `nginx`, `powershell`, `xml`) cause code fences to fall back to plain text or mismatched languages, and escaped backtick sequences (`\\``) from AI JSON responses display as literal backticks instead of inline code.

## What Changes

- **Brand Logo Navigation**: Update the primary brand logo link in `frontend/components/layout/AppHeader.vue` from `to="/today"` to `to="/"`.
- **Vue Snippet Syntax Highlighting Fallback**: In `frontend/utils/shikiHighlighter.ts` (and `ShikiCodeBlock.vue` / `useMarkdownRenderer.ts`), detect when a `vue` code snippet lacks `<template>` and `<script>` enclosing tags and route the tokenization to `typescript` for rich syntax coloring while preserving the high-tech `Vue 3 / SFC` or `Vue 3 / TypeScript` display label.
- **Extended Language Support**: Expand `SUPPORTED_LANGS` and alias normalization in `shikiHighlighter.ts` to include `diff`, `nginx`, `powershell`, `xml`, and common script aliases so code blocks across all curriculum slices and insights highlight accurately.
- **Inline Backtick Unescaping**: In `frontend/pages/insights.vue` and markdown rendering routines, unescape double-escaped backticks (`\\`` -> `` ` ``) so inline code segments (`<code>...</code>`) render correctly without raw backticks appearing in the UI.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Specifies that clicking the primary brand logo in the top application header routes to the root Home Bento Dashboard (`/`).
- `markdown-formatting`: Specifies resilient multi-language syntax highlighting across standalone code blocks and markdown fences, including headless Vue script tokenization fallback and extended language support.

## Impact

- **Frontend Navigation**: `frontend/components/layout/AppHeader.vue` directs brand logo clicks to `/`.
- **Frontend Highlighting**: `frontend/utils/shikiHighlighter.ts`, `frontend/components/common/ShikiCodeBlock.vue`, and `frontend/pages/insights.vue` provide full syntax highlighting across all supported languages and clean inline code formatting.
- **Zero Breaking Changes**: Fully backwards compatible with existing routes, markdown files, and API endpoints.
