# Spec Delta: Reader

## MODIFIED Requirements

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, formatting inline code (`code:not(pre code)`) with Dev-Learning Studio tokens (neutral pill in light mode with `bg-slate-100 text-slate-800 border-slate-200/90`, and Obsidian pill in dark mode with `dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08] font-medium`, eliminating legacy green/emerald styling across both `/read/[bookId]` and `/today`), parsing technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`) into distinct semantic callout boxes with dedicated icons without decorative quotation marks, and providing reliable one-click copy buttons on all code fences without throwing unhandled exceptions.

#### Scenario: Slice title duplication avoidance
- **WHEN** a markdown slice starts with a heading tag matching the slice's chapter title
- **THEN** the reader suppresses the redundant heading in the markdown body and renders body content immediately after the page H1 element.

#### Scenario: Clean inline code rendering
- **WHEN** markdown content contains inline code segments wrapped in single backticks
- **THEN** inline code elements render as styled pills without decorative backtick characters in pseudo-elements (`before`/`after`).

#### Scenario: Semantic alert callout rendering
- **WHEN** markdown content contains a blockquote starting with an alert marker (such as `[!WARNING]`, `[!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!CAUTION]` or legacy bracketed formats)
- **THEN** the reader renders a styled alert container with an alert icon, uppercase title label, semantic border and background color (Amber for Warning, Sky for Note, Emerald for Tip, Indigo for Important, Rose for Caution), without decorative quotation marks (`“... ”`) and without duplicate title repetition.

#### Scenario: Reliable code snippet copying
- **WHEN** user clicks the "Copy" button on any syntax-highlighted code fence
- **THEN** the code snippet is decoded and copied to the system clipboard, and the button provides temporary visual confirmation ("Copied!").

#### Scenario: Accurate slice badge display
- **WHEN** reading any document slice
- **THEN** the badge above the title displays "Slice {current} of {total}" in English and "Lát cắt {current} / {total}" in Vietnamese instead of day drill labels.

#### Scenario: Inline code rendering in dark mode on daily focus reader
- **WHEN** user views an architectural deep dive containing inline code syntax (e.g. `reactive()`, `shallowRef`) on `/today` in dark mode
- **THEN** inline code elements render with an elevated obsidian container background (`dark:bg-canvas-elevated`)
- **AND** inline code text renders with syntax telemetry violet accent (`dark:text-brand-300`)
- **AND** inline code borders render with translucent hairline border (`dark:border-white/[0.08]`)
- **AND** no green (`bg-emerald-500/10` or `text-emerald-700` or `text-emerald-300`) styles are applied to inline code.

#### Scenario: Inline code rendering in dark mode on dedicated book reader
- **WHEN** user reads a technical book chapter on `/read/[bookId]` in dark mode
- **THEN** inline code in the article body inherits the Dev-Learning Studio obsidian badge styling
- **AND** typography overrides in the book reader container do not force green text (`prose-code:text-emerald-600` or `dark:prose-code:text-emerald-400`) or legacy slate backgrounds (`dark:prose-code:bg-slate-800/80`).

#### Scenario: Inline code contrast in light mode
- **WHEN** user views reading content in light mode on either `/today` or `/read/[bookId]`
- **THEN** inline code elements render with clean neutral slate styling (`bg-slate-100 text-slate-800 border-slate-200`) providing WCAG 2.1 AA compliant contrast.
