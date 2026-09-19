# Spec Delta

## MODIFIED Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, novel-style typography settings toggle (`Aa`), theme toggle, and 1-click quiz launcher. The reader page shell, sticky header, popovers, and backdrop elements SHALL render with Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) and hairline translucent borders (`border-slate-200/80 dark:border-white/[0.08]`), replacing legacy slate-900/950 backgrounds. The 1-Click Quiz Chapter button SHALL use system primary brand tokens (`bg-brand-50 dark:bg-brand-500/10 border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 hover:bg-brand-100 dark:hover:bg-brand-500/20`) instead of hardcoded purple utility classes.

#### Scenario: User opens a book from the library
- **GIVEN** an authenticated or guest user browsing `/library`
- **WHEN** user clicks "Read Book" on a book card in `/library`
- **THEN** application navigates to `/read/[bookId]` loading the first slice or bookmarked slice without global application chrome.

#### Scenario: Mobile viewport reader header layout
- **GIVEN** user is reading on a mobile screen width (<768px)
- **WHEN** user views `/read/[bookId]`
- **THEN** reader header displays a single compact top bar with back arrow, truncated book title with slice indicator, table of contents button, typography toggle (`Aa`), and theme toggle without vertical header duplication.

#### Scenario: Dev-Learning Studio theme styling on reader shell
- **GIVEN** user views `/read/[bookId]` in dark mode
- **WHEN** the reader page and header render
- **THEN** the root container renders with `dark:bg-canvas` (`#09090b`)
- **AND** the top header renders with `dark:bg-canvas/90` with hairline border `dark:border-white/[0.08]`
- **AND** the 1-Click Quiz button renders with primary brand tokens (`bg-brand-50 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400`) instead of hardcoded purple tokens.

---

### Requirement: Table of Contents & Chapter Sidebar
The reader SHALL include a Table of Contents displaying all slices/chapters with estimated reading time, order, and completion indicators. On desktop (≥768px), the TOC SHALL render as a collapsible left sidebar styled with Dev-Learning Studio obsidian subtle background (`dark:bg-canvas-subtle/70`) and hairline border (`dark:border-white/[0.08]`). On mobile (<768px), the TOC SHALL render as an off-canvas slide-over drawer with backdrop blur and obsidian elevated canvas styling (`dark:bg-canvas-subtle`), closing automatically upon chapter selection. Completed slices across both desktop sidebar and mobile drawer SHALL render completion checkmark indicators in system primary brand tokens (`text-brand-600 dark:text-brand-400`) instead of legacy emerald green.

#### Scenario: User selects a chapter from table of contents
- **WHEN** user clicks or taps a chapter item in the table of contents
- **THEN** reader pane smoothly switches to the selected slice, highlights the active chapter, and resets scroll position to the top.

#### Scenario: User selects a chapter from table of contents on mobile
- **WHEN** user opens mobile TOC drawer and taps any chapter item
- **THEN** reader switches to selected slice, closes the drawer immediately, and resets scroll position to the top.

#### Scenario: Table of Contents completed slice visual encoding
- **WHEN** a slice has been completed by the user
- **THEN** the TOC item displays a checkmark icon in primary brand color (`text-brand-600 dark:text-brand-400`)
- **AND** no legacy emerald green (`text-emerald-500`) is rendered on completed slice indicators.

---

### Requirement: Seamless Next / Previous Slice Navigation
At the bottom of each slice, the reader SHALL render a balanced, symmetrical two-card navigation component (`Previous Slice` card on the left and `Next Slice` / `Return to Library` card on the right) on desktop and a thumb-friendly responsive layout on mobile, while supporting keyboard shortcuts (`Shift + ArrowRight` / `Shift + ArrowLeft`). Each navigation card SHALL feature an uppercase section label and a truncated chapter title, avoiding asymmetric inline button stretching or text wrapping across both English and Vietnamese locales (`whitespace-nowrap shrink-0`). All navigation cards SHALL adopt Dev-Learning Studio glass-card styling (`.glass-card` / hairline border `dark:border-white/[0.08]`). The final slice completion card ("Return to Library") SHALL render in system primary brand tokens (`border-brand-500/30 dark:border-brand-500/20`, `bg-brand-50/30 dark:bg-brand-500/10`, `text-brand-600 dark:text-brand-400`) instead of legacy emerald styling.

#### Scenario: User navigates to next slice via button
- **WHEN** user clicks or taps "Next Slice" card at the bottom of a chapter
- **THEN** reader pane transitions to the next sequential slice and scrolls to top.

#### Scenario: Symmetrical desktop card layout
- **WHEN** user reaches the bottom of a slice on desktop (≥640px)
- **THEN** reader displays a 2-column grid with Previous and Next cards balanced at equal width, with chapter titles cleanly truncated and action labels styled with `whitespace-nowrap`.

#### Scenario: First slice previous card handling
- **WHEN** user is reading the first slice (slice order = 1)
- **THEN** the Previous card is gracefully hidden or visually preserved as empty column space, and the Next card remains aligned to the right.

#### Scenario: User navigates using keyboard shortcuts
- **WHEN** user presses `Shift + ArrowRight` while reading
- **THEN** reader navigates to the next slice.

#### Scenario: Mobile thumb-reach layout
- **WHEN** user reaches bottom of slice on a mobile viewport
- **THEN** navigation cards stack cleanly with the primary Next action card accessible at full width.

#### Scenario: Final slice completion card styling
- **WHEN** user reaches the final slice of a book
- **THEN** the right navigation card renders the "Return to Library" completion card using system primary brand tokens (`border-brand-500/30`, `bg-brand-50/30 dark:bg-brand-500/10`, `text-brand-600 dark:text-brand-400`)
- **AND** no emerald green (`emerald-500`, `bg-emerald-50`, `text-emerald-600`) tokens are applied.

---

### Requirement: Scoped Floating Mini-Toolbar & Active Recall Quiz
The reader floating selection toolbar SHALL render exactly 3 streamlined action buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`. The direct flashcard creation button SHALL be removed from the reader selection tooltip to protect reading immersion and avoid premature card generation. The `Highlight/Note` action SHALL unify text highlighting and note-taking into a single continuous action: clicking the button immediately creates and persists a highlight record (`POST /api/v1/notes/highlights`), while smoothly opening an attached reflection popover where users can optionally add personal reflection notes and technical tags. The floating toolbar container and attached note popover SHALL render with Dev-Learning Studio elevated obsidian panels (`bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/80 dark:border-white/[0.12] shadow-2xl rounded-2xl`). Text input fields in the note popover SHALL use elevated studio styling (`dark:bg-canvas-subtle dark:border-white/[0.10] focus:ring-brand-500/40 focus:border-brand-500`).

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text (2 to 500 characters) inside the reader markdown container
- **THEN** floating selection toolbar appears directly above the selection displaying exactly 3 buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy` without a direct flashcard creation button.

#### Scenario: User opens floating note popover on text selection
- **WHEN** user selects text in the reader markdown pane and clicks `Highlight/Note`
- **THEN** an inline popover appears above the selection containing the quote preview, a reflection note textarea, and a tag input field with Save and Close actions.

#### Scenario: User saves a highlight with attached personal reflection
- **WHEN** user types a reflection note and tags into the reflection popover and clicks `Save Note`
- **THEN** client dispatches `PUT /api/v1/notes/highlights/{id}` with updated `note` and `tags`, persists the highlight note in PostgreSQL, displays a localized confirmation toast, and smoothly closes the popover.

#### Scenario: User saves a simple highlight without a note
- **WHEN** user clicks `Highlight/Note` and dismisses the popover without typing a note or adding tags
- **THEN** the system preserves the initially persisted highlight with `note = null` and displays a confirmation toast (`reader.toast_highlight_success`).

#### Scenario: User copies selected text or triggers Gemini explanation
- **WHEN** user clicks `Copy` on the floating toolbar
- **THEN** selected text is copied to clipboard and toolbar closes with a confirmation toast (`reader.toast_copy`)
- **WHEN** user clicks `Explain with Gemini`
- **THEN** floating toolbar closes and opens the Gemini Term Explainer modal populated with selected text and surrounding context.

#### Scenario: Floating toolbar and reflection popover obsidian styling
- **WHEN** the floating selection toolbar or note popover renders
- **THEN** container styles use elevated obsidian tokens (`dark:bg-canvas-elevated/95`, `dark:border-white/[0.12]`)
- **AND** quote preview renders with a primary brand border indicator (`border-brand-500`).

---

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, formatting inline code (`code:not(pre code)`) with Dev-Learning Studio tokens (neutral pill in light mode with `bg-slate-100 text-slate-800 border-slate-200/90`, and Obsidian pill in dark mode with `dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08] font-medium`, eliminating legacy green/emerald styling across both `/read/[bookId]` and `/today`), formatting markdown links with system primary brand tokens (`prose-a:text-brand-600 dark:prose-a:text-brand-400 hover:prose-a:underline` instead of legacy `prose-a:text-emerald-500`), parsing technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`) into distinct semantic callout boxes with dedicated icons without decorative quotation marks, and providing reliable one-click copy buttons on all code fences without throwing unhandled clipboard exceptions.

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

#### Scenario: Markdown hyperlink primary brand styling
- **WHEN** reader renders hyperlinks inside the technical article body
- **THEN** links render in primary brand accent (`prose-a:text-brand-600 dark:prose-a:text-brand-400`)
- **AND** no legacy emerald green (`prose-a:text-emerald-500`) is applied to article links.
