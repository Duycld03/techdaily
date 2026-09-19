# markdown-formatting Specification

## Purpose
TBD - created by archiving change markdown-newline-unescaping-and-prose-extraction. Update Purpose after archive.

## Requirements

### Requirement: Strict Linebreak Normalization for AI-Generated Markdown
When parsing LLM JSON responses, the system MUST normalize any literal escaped newline sequences (`\n`, `\r\n`, `\t`) into actual control characters (`\n`, `\r\n`, `\t`) before persisting to storage or returning to API consumers.

#### Scenario 1.1: Double-escaped newlines from JSON payload
- **GIVEN** an AI service response where `formattedMarkdown` contains literal string sequences `\n\n> [!NOTE]\n>`
- **WHEN** `GeminiAiService.ParseSliceResponse` processes the response
- **THEN** the persisted `OriginalTextMarkdown` MUST contain real LF characters (`0x0A`)
- **AND** the markdown renderer MUST parse the content into separate headings, paragraphs, and alert callouts without printing literal `\n`.

### Requirement: Frontend Defense-in-Depth Normalization
The frontend Markdown renderer MUST automatically detect and normalize any literal `\n` or `\r\n` sequences prior to feeding Markdown strings into `markdown-it`.

#### Scenario 2.1: Legacy or raw strings containing literal \n
- **GIVEN** a document slice where `OriginalTextMarkdown` contains `\n` characters
- **WHEN** `useMarkdownRenderer().render()` processes the text
- **THEN** it converts `\n` into actual line breaks
- **AND** properly renders GitHub alert callouts and headers.

### Requirement: Isolated Code Block Container Styling
The Markdown renderer MUST wrap code blocks in dedicated, isolated container classes that prevent style leakage or collision with standalone code highlighting components. Rendered code blocks SHALL guarantee minimum horizontal padding of at least 16px (`1rem`) on mobile viewports (<640px) and 20px (`1.25rem` to `1.5rem`) on desktop screens (≥640px) regardless of asynchronous route chunk loading order or CSS injection sequence.

Rendered code block containers and headers SHALL strictly adhere to the Dev-Learning Studio design language across both Markdown-rendered surfaces and standalone code blocks:
1. **Container Styling:** Code blocks SHALL render in a rounded container (`rounded-2xl`) with translucent hairline borders (`border-slate-200/80 dark:border-white/[0.08]`), deep neutral obsidian canvas (`dark:bg-canvas-subtle` / `#121215`), and studio drop shadow (`shadow-lg dark:shadow-2xl`).
2. **Studio Header Bar:** The header bar SHALL render as a glassmorphic top rail (`bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06]`) featuring:
   - Three macOS traffic-light window controls (Red `#ff5f56`, Amber `#ffbd2e`, Green `#27c93f`).
   - A high-tech monospace language indicator styled with studio brand accents (`text-brand-600 dark:text-brand-400 font-bold uppercase tracking-widest`).
   - A translucent glassmorphic Copy button (`bg-white/80 dark:bg-white/[0.06] hover:bg-white dark:hover:bg-white/[0.12] border border-slate-200/80 dark:border-white/[0.08]`) that provides copy icon and "Copied!" confirmation feedback.
3. **Syntax Highlighting Theme:** Syntax highlighting SHALL render against a transparent code container background (`bg-transparent`) using a modern neutral obsidian theme (`vitesse-dark` or `github-dark-default`), seamlessly inheriting the obsidian canvas background without nested color boxes, and SHALL format code comments with italic styling (`font-style: italic`).

#### Scenario: Unscoped external styles do not override markdown code block padding
- **WHEN** route preloading or an external component injects zero-padding styles into `<head>`
- **THEN** markdown code blocks within reading slices, today pane, and explanations retain their horizontal padding and remain visually aligned under the header window controls.

#### Scenario: Asynchronous syntax highlighter readiness updates rendered code blocks
- **WHEN** the client-side syntax highlighter completes asynchronous initialization
- **THEN** all active markdown panes (including reader slices, drill explanations, and term explainer dialogs) automatically re-render to display syntax-highlighted code.

#### Scenario: Dev-Learning Studio obsidian terminal window styling
- **WHEN** a code block is rendered in dark mode
- **THEN** the container renders on neutral obsidian `dark:bg-canvas-subtle` with hairline border `dark:border-white/[0.08]`
- **AND** the top header displays traffic-light circular dots (`#ff5f56`, `#ffbd2e`, `#27c93f`) alongside the uppercase language label.

#### Scenario: Neutral syntax highlighting with transparent canvas background and italic comments
- **WHEN** syntax highlighting is applied to a code fence
- **THEN** the `<pre>` and `<code>` elements have transparent backgrounds without introducing nested gray or blue boxes
- **AND** the syntax highlighter uses a neutral dark theme (`vitesse-dark` or `github-dark-default`)
- **AND** code comments render in italic font style for enhanced readability.

#### Scenario: Interactive glassmorphic copy button feedback
- **WHEN** the user clicks the "Copy" button on a code block header
- **THEN** the code content is copied to the system clipboard
- **AND** the button temporarily displays a green checkmark with "Copied!" text before returning to the default state.

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
