# Spec Delta

## MODIFIED Requirements

### Requirement: Multi-Language Syntax Highlighting & Code Block Resilience
The syntax highlighting subsystem SHALL support resilient multi-language tokenization across both standalone code blocks (`CommonShikiCodeBlock.vue`) and markdown fences (`useMarkdownRenderer.ts`), preventing monochromatic unstyled fallbacks:

1. **Dual-Theme Token Generation & CSS Variable Preservation:** The syntax highlighting engine SHALL output dual-theme tokens configured with both light (`vitesse-light`) and dark (`vitesse-dark`) themes. Every token `<span>` SHALL receive inline text color alongside the `--shiki-dark` CSS variable, enabling dynamic theme switching without re-rendering or token re-computation.
2. **Defensive Dark-Mode Stylesheet Scoping:** The global application stylesheet and component scoped styles SHALL scope dark-mode color overrides using the attribute selector `span[style*="--shiki-dark"]`. Dark-mode rules SHALL NOT override tokens that lack `--shiki-dark`, preventing monochromatic white/inherited text collapse on custom tokens, single-theme blocks, or fallback snippets.
3. **Extended Language Support (Protobuf & Razor/Blazor):** The syntax engine SHALL natively bundle and support languages including `csharp`, `typescript`, `javascript`, `sql`, `json`, `vue`, `bash`, `html`, `css`, `yaml`, `rust`, `go`, `python`, `markdown`, `dockerfile`, `diff`, `nginx`, `powershell`, `xml`, `proto`, `razor`, and `text`. Unrecognized languages SHALL gracefully fall back to `text` or `csharp` with safe HTML escaping rather than breaking page rendering.
4. **Protobuf & Go Disambiguation:** The syntax detector SHALL accurately distinguish Protocol Buffer / gRPC definitions (`syntax = "proto3"`, `service`, `rpc`, `message`) from Go code without false-positive Go tagging caused by Protobuf package statements (`package greet;`).
5. **Razor / Blazor Component Detection:** The syntax detector SHALL recognize Razor/Blazor directives (`@page`, `@code {`, `@inject`) and render code blocks with the high-tech `"Razor / Blazor"` language badge and active syntax highlighting.
6. **Headless Vue Script Highlighting Fallback:** When a code block is identified or tagged as `vue`, if the snippet content lacks `<template>` and `<script>` tags, the highlighter SHALL route tokenization to `typescript` (or `javascript`) while maintaining the high-tech `Vue 3 / SFC` or `Vue 3 / TypeScript` label in the header bar.
7. **Inline Backtick Normalization:** The markdown rendering pipeline SHALL automatically unescape double-escaped backticks (`\\``) so that inline code fragments render as `<code>...</code>` elements rather than printing literal backtick characters.

#### Scenario: Code blocks in dark mode retain multi-color syntax highlighting
- **GIVEN** a markdown document containing code fences in any supported language (e.g., C#, Razor, Protobuf, TypeScript, Bash)
- **WHEN** the reader renders the document in Dark Mode (`html.dark`)
- **THEN** keywords, types, identifiers, strings, and comments SHALL display distinct colors derived from `var(--shiki-dark)`
- **AND** the code block content SHALL NOT render as plain monochrome white text.

#### Scenario: Protobuf code blocks highlight as Protobuf and not Go
- **GIVEN** a markdown code fence containing `syntax = "proto3";` and a `service` definition
- **WHEN** the markdown renderer processes the code fence
- **THEN** the code block header SHALL display the `"Protobuf"` badge
- **AND** the tokens SHALL be tokenized using the Protobuf grammar rather than Go.

#### Scenario: Razor components highlight with Razor grammar and display Razor / Blazor badge
- **GIVEN** a code fence tagged with ````razor` or containing `@page` / `@code` directives
- **WHEN** the markdown renderer processes the code fence
- **THEN** the code block header SHALL display the `"Razor / Blazor"` badge
- **AND** both HTML tags and C# blocks within the component SHALL receive active syntax coloring.
