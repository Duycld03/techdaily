# Spec Delta: Markdown Formatting

## ADDED Requirements

### Requirement: Multi-Language Syntax Highlighting & Code Block Resilience
The syntax highlighting subsystem SHALL support resilient multi-language tokenization across both standalone code blocks (`CommonShikiCodeBlock.vue`) and markdown fences (`useMarkdownRenderer.ts`), preventing monochromatic unstyled fallbacks:

1. **Headless Vue Script Highlighting Fallback:** When a code block is identified or tagged as `vue`, if the snippet content lacks `<template>` and `<script>` tags (such as Composition API snippets, reactivity primitives, or store definitions), the highlighter SHALL route tokenization to `typescript` (or `javascript`) while maintaining the high-tech `Vue 3 / SFC` or `Vue 3 / TypeScript` label in the header bar.
2. **Extended Language Support:** The syntax engine SHALL natively bundle and support languages including `csharp`, `typescript`, `javascript`, `sql`, `json`, `vue`, `bash`, `html`, `css`, `yaml`, `rust`, `go`, `python`, `markdown`, `dockerfile`, `diff`, `nginx`, `powershell`, and `xml`. Unrecognized languages SHALL gracefully fall back to `text` or `csharp` with safe HTML escaping rather than breaking page rendering.
3. **Inline Backtick Normalization:** The markdown rendering pipeline SHALL automatically unescape double-escaped backticks (`\\``) so that inline code fragments render as `<code>...</code>` elements rather than printing literal backtick characters.

#### Scenario: Headless Vue script highlighting produces rich tokenization
- **WHEN** a code block tagged with `vue` containing TypeScript reactivity code without `<template>` or `<script>` tags is rendered
- **THEN** Shiki tokenizes keywords, identifiers, and comments with distinct theme colors instead of rendering monochromatic template text
- **AND** the code block header displays the appropriate Vue language badge.

#### Scenario: Extended languages highlight without fallback to C#
- **WHEN** a code fence specifies an extended language such as `diff`, `nginx`, `powershell`, or `xml`
- **THEN** Shiki highlights the snippet using the corresponding language grammar
- **AND** does not fall back to C# keywords or unhighlighted plain text.

#### Scenario: Escaped backticks render as inline code elements
- **WHEN** markdown content contains double-escaped backtick sequences (`\\`...\\``)
- **THEN** the markdown parser converts them into HTML `<code>...</code>` tags
- **AND** does not render literal backtick characters in body paragraphs.
