# Capability: Markdown Formatting & Linebreak Normalization

## ADDED REQUIREMENTS

### Requirement 1: Strict Linebreak Normalization for AI-Generated Markdown
When parsing LLM JSON responses, the system MUST normalize any literal escaped newline sequences (`\n`, `\r\n`, `\t`) into actual control characters (`\n`, `\r\n`, `\t`) before persisting to storage or returning to API consumers.

#### Scenario 1.1: Double-escaped newlines from JSON payload
- **GIVEN** an AI service response where `formattedMarkdown` contains literal string sequences `\n\n> [!NOTE]\n>`
- **WHEN** `GeminiAiService.ParseSliceResponse` processes the response
- **THEN** the persisted `OriginalTextMarkdown` MUST contain real LF characters (`0x0A`)
- **AND** the markdown renderer MUST parse the content into separate headings, paragraphs, and alert callouts without printing literal `\n`.

### Requirement 2: Frontend Defense-in-Depth Normalization
The frontend Markdown renderer MUST automatically detect and normalize any literal `\n` or `\r\n` sequences prior to feeding Markdown strings into `markdown-it`.

#### Scenario 2.1: Legacy or raw strings containing literal \n
- **GIVEN** a document slice where `OriginalTextMarkdown` contains `\n` characters
- **WHEN** `useMarkdownRenderer().render()` processes the text
- **THEN** it converts `\n` into actual line breaks
- **AND** properly renders GitHub alert callouts and headers.
