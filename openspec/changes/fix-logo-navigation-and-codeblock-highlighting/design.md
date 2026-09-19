# Design: Fix Logo Home Navigation and Code Block Multi-Language Highlighting

## Context

TechDaily users navigating the web interface encounter two issues:
1. Clicking the TechDaily logo in the top application header (`frontend/components/layout/AppHeader.vue`) routes to `/today` instead of the root Home Bento Dashboard (`/`).
2. Code blocks in `/insights` (and other markdown surfaces) displaying Vue snippets that contain Composition API logic (such as `shallowRef()`, `ref()`, `reactive()`, `computed()`) without `<template>` or `<script>` tags fail to be syntax highlighted by Shiki. This occurs because Shiki's TextMate Vue grammar expects an SFC format (`<template>` or `<script>`); without those tags, it treats the entire code block as raw template text, resulting in monochromatic text (colored `#DBD7CAEE`). In addition, missing languages in `SUPPORTED_LANGS` (such as `diff`, `nginx`, `powershell`, `xml`) cause fences to fall back to plain text or C#, and double-escaped backticks (`\\``) from AI/JSON serialization print as literal backticks.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Direct brand logo clicks in `AppHeader.vue` to `/`.
- Ensure Vue snippets without `<template>` or `<script>` tags are highlighted using the `typescript` grammar while retaining the `Vue 3 / SFC` or `Vue 3 / TypeScript` label in the code block header.
- Expand `SUPPORTED_LANGS` and alias normalization in `shikiHighlighter.ts` to include `diff`, `nginx`, `powershell`, and `xml`.
- Normalize double-escaped backticks in markdown rendering (`frontend/pages/insights.vue` and `useMarkdownRenderer.ts`) so inline code displays in HTML `<code>` elements.

**Non-Goals:**
- Changing backend database seed schemas or API contracts.
- Modifying how full Vue SFCs (containing `<template>` and `<script>`) are highlighted.

## Decisions

### Decision 1: Brand Logo Navigation to Root Dashboard
- **Implementation**: Change `<NuxtLink to="/today" ...>` in `frontend/components/layout/AppHeader.vue` line 126 to `<NuxtLink to="/" ...>`.
- **Rationale**: Returning to the root Home Bento Dashboard (`/`) is standard across web applications when clicking the top-left brand logo.

### Decision 2: Headless Vue Snippet Shiki Grammar Routing
- **Implementation**: In `frontend/utils/shikiHighlighter.ts`, inside `highlightCode(code, lang, theme)`:
  ```ts
  let targetLang = SUPPORTED_LANGS.includes(normalizedLang) ? normalizedLang : 'csharp'
  if (targetLang === 'vue' && !code.includes('<template') && !code.includes('<script')) {
    targetLang = 'typescript'
  }
  ```
- **Rationale**: Vue Composition API scripts without SFC tags are syntactically TypeScript/JavaScript. Routing them to `typescript` for Shiki tokenization gives rich token styling for comments, keywords, types, and strings, while the header badge continues to display `Vue 3 / SFC` (or `Vue 3 / TypeScript`) from `formatLanguageLabel`.

### Decision 3: Extended Language Support
- **Implementation**: Add `'diff'`, `'nginx'`, `'powershell'`, and `'xml'` to `SUPPORTED_LANGS` in `shikiHighlighter.ts`. Add aliases in `normalizeLanguage`:
  - `ps`, `powershell`, `pwsh` -> `'powershell'`
  - `diff`, `patch` -> `'diff'`
  - `nginx`, `conf` -> `'nginx'`
  - `xml`, `svg` -> `'xml'`
- **Rationale**: Shiki 2.x bundles 303 languages out of the box. Adding these 4 languages covers infrastructure configurations, command line scripts, and code differences without increasing bundle overhead.

### Decision 4: Inline Backtick Unescaping
- **Implementation**: In `frontend/pages/insights.vue`, update `renderMarkdown`:
  ```ts
  function renderMarkdown(raw: string | undefined | null): string {
    if (!raw) return ''
    const clean = raw.replace(/\\n/g, '\n').replace(/\\`/g, '`')
    return md.render(clean)
  }
  ```
- **Rationale**: JSON responses from LLM generation or serialized strings frequently double-escape backticks as `\\``, which MarkdownIt parses as literal backtick characters. Unescaping restores `<code>...</code>` inline formatting.

## Risks / Trade-offs

- **Zero Breaking Risks**: Fully backwards compatible. Full `.vue` SFC files containing `<template>` or `<script>` tags continue to use the official Vue grammar, while headless scripts receive full TypeScript tokenization.
