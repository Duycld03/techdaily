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

#### Scenario: Unscoped external styles do not override markdown code block padding

- **WHEN** route preloading or an external component injects zero-padding styles into `<head>`
- **THEN** markdown code blocks within reading slices, today pane, and explanations retain their horizontal padding and remain visually aligned under the header window controls.

#### Scenario: Asynchronous syntax highlighter readiness updates rendered code blocks

- **WHEN** the client-side syntax highlighter completes asynchronous initialization
- **THEN** all active markdown panes (including reader slices, drill explanations, and term explainer dialogs) automatically re-render to display syntax-highlighted code.
