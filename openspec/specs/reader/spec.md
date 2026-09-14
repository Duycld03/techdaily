# Reader Specification

## Purpose
Provides an immersive technical reading experience with chapter table of contents, progress indicators, keyboard shortcuts, local bookmarking, active recall quizzes, and scoped text highlight tools.

## Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, theme toggle, and 1-click quiz launcher.

#### Scenario: User opens a book from the library
- **WHEN** user clicks "Read Book" on a book card in `/library`
- **THEN** application navigates to `/read/[bookId]` loading the first slice or bookmarked slice without global application chrome.

#### Scenario: Mobile viewport reader header layout
- **WHEN** user views `/read/[bookId]` on a mobile screen width (<768px)
- **THEN** reader header displays a single compact top bar with back arrow, truncated book title with slice indicator, table of contents button, and theme toggle without vertical header duplication.

---

### Requirement: Table of Contents & Chapter Sidebar
The reader SHALL include a Table of Contents displaying all slices/chapters with estimated reading time, order, and completion indicators. On desktop (≥768px), the TOC SHALL render as a collapsible left sidebar. On mobile (<768px), the TOC SHALL render as an off-canvas slide-over drawer with backdrop blur, closing automatically upon chapter selection.

#### Scenario: User selects a chapter from table of contents
- **WHEN** user clicks or taps a chapter item in the table of contents
- **THEN** reader pane smoothly switches to the selected slice, highlights the active chapter, and resets scroll position to the top.

#### Scenario: User selects a chapter from table of contents on mobile
- **WHEN** user opens mobile TOC drawer and taps any chapter item
- **THEN** reader switches to selected slice, closes the drawer immediately, and resets scroll position to the top.

---

### Requirement: Seamless Next / Previous Slice Navigation
At the bottom of each slice, the reader SHALL render a balanced, symmetrical two-card navigation component (`Previous Slice` card on the left and `Next Slice` / `Return to Library` card on the right) on desktop and a thumb-friendly responsive layout on mobile, while supporting keyboard shortcuts (`Shift + ArrowRight` / `Shift + ArrowLeft`). Each navigation card SHALL feature an uppercase section label and a truncated chapter title, avoiding asymmetric inline button stretching or text wrapping across both English and Vietnamese locales.

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

### Requirement: Reading Progress Bar & Statistics
The top navigation bar SHALL display a persistent progress bar showing percentage of slices completed (e.g. `Slice 3 of 12 (25%)`).

#### Scenario: User progresses through book chapters
- **WHEN** user advances to slice 3 of 12
- **THEN** top progress bar reflects 25% completion with current slice counter.

---

### Requirement: Local Bookmark & Progress Persistence
The system SHALL automatically record the user's latest read slice for each book in `localStorage` (`techdaily_bookmark_{bookId}`) so navigating back to `/read/[bookId]` automatically resumes at the bookmarked slice.

#### Scenario: User returns to a previously read book
- **WHEN** user navigates back to `/read/[bookId]` without explicit `?slice=` parameter
- **THEN** reader retrieves stored bookmark and resumes display at the bookmarked slice.

---

### Requirement: Scoped Floating Mini-Toolbar & Active Recall Quiz
The reader SHALL support highlighting text inside the markdown container to show a discreet floating action bar (`✨ Explain with Gemini` and `📋 Copy`), and optionally render an interactive Micro Quiz check at the end of the chapter.

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text inside the reader markdown container
- **THEN** floating toolbar appears with Gemini Explainer and Copy actions.

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, formatting inline code without intrusive default pseudo-element backticks, parsing technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`) into distinct semantic callout boxes with dedicated icons without decorative quotation marks, and providing reliable one-click copy buttons on all code fences without throwing unhandled exceptions.

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

### Requirement: Document Relative Link Resolution
During technical article crawling and import, the crawler SHALL resolve all relative anchor hyperlinks (`<a href="...">`) and images (`<img src="...">`) against the document's canonical source URL into absolute URLs, with the exception of same-page fragment bookmarks (`#...`).

#### Scenario: Crawler processes relative document link
- **WHEN** the crawler encounters an anchor tag with a relative path such as `href="dependency-injection?view=aspnetcore-10.0"` while crawling `https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0`
- **THEN** the crawler resolves the link to `https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0` before generating Markdown.

#### Scenario: Crawler processes root-relative API reference link
- **WHEN** the crawler encounters `href="/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointdatasource"`
- **THEN** the crawler resolves the link to `https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointdatasource`.

#### Scenario: Crawler resolves in-page section bookmarks
- **WHEN** the crawler encounters an in-page fragment bookmark such as `href="#routing-basics"`
- **THEN** the crawler resolves the bookmark against the canonical source URL so the markdown anchor preserves the target destination on the original documentation.

---

### Requirement: Isolated External Link Navigation
The reader markdown renderer SHALL configure anchor rendering such that all external documentation links automatically include `target="_blank"` and `rel="noopener noreferrer"`. Clicking any external link inside an article slice SHALL open the external reference in a new browser tab without replacing or disrupting the active TechDaily reading session.

#### Scenario: User clicks external reference link in reader pane
- **WHEN** user clicks on an external documentation link rendered in markdown (e.g. `[DI](https://learn.microsoft.com/...)`)
- **THEN** the link opens in a separate browser tab with secure `noopener noreferrer` attributes, and the TechDaily reading route `/read/[bookId]` remains open at the current slice.

#### Scenario: In-page anchor navigation
- **WHEN** user clicks a table-of-contents or anchor link targeting a heading on the same page (e.g. `[Basics](#routing-basics)`)
- **THEN** the link does not open a new tab and instead scrolls smoothly to the target element within the active reading pane.

### Requirement: Active Book Pacer Navigation Bar on /today
The `/today` top navigation bar SHALL display the user's active document book pacer status, replacing the fixed 30-day selector. The pacer bar SHALL display the active book title, current chapter title, slice order, total slices, completion percentage, and previous/next navigation buttons.

#### Scenario: User navigates to /today with an active book
- **WHEN** user loads `/today`
- **THEN** top bar displays `[Book Title] • [Chapter Title] • Slice [X] of [Total] ([Y]%)`.
- **THEN** `DocReaderPane` loads slice $X$ of the active book.
- **THEN** `InterviewChallengePane` loads the trade-off challenge associated with slice $X$.

---

### Requirement: 1-Click Book Switcher & Featured Default Book
The system SHALL ensure the `/today` page is always populated with an active document book. Unauthenticated visitors or new users without an active book SHALL be seamlessly assigned the system's primary featured book (e.g. *PostgreSQL 17 Internals*). The pacer bar SHALL provide an integrated 1-click book switcher dropdown.

#### Scenario: Guest or first-time user visits /today
- **WHEN** an unauthenticated visitor or new user visits `/today`
- **THEN** system automatically loads the primary featured book starting at Slice 1 without displaying empty states or blocking onboarding dialogs.

#### Scenario: User switches active book from top bar
- **WHEN** user clicks the active book title in the pacer bar
- **THEN** a dropdown menu displays the user's in-progress books with their respective progress bars and a button to choose books from `/library`.
- **WHEN** user selects an alternative book from the dropdown
- **THEN** system sets `IsActive = true` for the selected book and refreshes `/today` with its current reading slice.

---

### Requirement: Chapter-Based Roadmap Synchronization on /roadmap
The `/roadmap` page SHALL dynamically reflect the chapter milestone progress of the user's active book rather than a static 30-day timeline. Each chapter milestone SHALL indicate completed slices, active slice, and upcoming slices.

#### Scenario: User views roadmap of active book
- **WHEN** user navigates to `/roadmap`
- **THEN** roadmap displays the active book title, total progress percentage, estimated days remaining at current pace, and sequential chapter milestone cards.
- **WHEN** user clicks the active chapter milestone
- **THEN** application navigates to `/today` positioned at the current reading slice.

### Requirement: Debounced Lookahead Prefetching

The reader view (`/read/[bookId]`) and the daily focus view (`/today`) SHALL apply a 2.5-second debounce delay to lookahead prefetch triggers (`triggerLookaheadPrefetch` / `triggerNextDayPrefetch`). If the user changes slices or navigates away before the 2.5-second timer elapses, the pending prefetch timer SHALL be immediately canceled.

#### Scenario: User pauses on a slice to read

- **GIVEN** user is viewing slice 4
- **WHEN** user remains on slice 4 for at least 2.5 seconds
- **THEN** system triggers lookahead prefetch for slice 5 in the background.

#### Scenario: User rapidly clicks through table of contents

- **GIVEN** user is browsing the chapter list
- **WHEN** user clicks slice 1, then slice 2, then slice 3 within 1.5 seconds
- **THEN** intermediate prefetch timers for slice 2 and slice 3 are canceled before any HTTP request is dispatched
- **AND** only slice 3 schedules a prefetch timer once the user pauses.

#### Scenario: Component unmount cancels pending prefetch

- **WHEN** user leaves the reader route or closes the page while a prefetch timer is pending
- **THEN** the timer is cleared and no background request is dispatched after navigation.

### Requirement: 1-Step Lookahead Prefetching
When a user views Slice $N$ in `/read/[bookId]`, the client SHALL automatically initiate a background curation request for Slice $N+1$ if it is not yet curated.

#### Scenario: Background prefetch during reading
- **GIVEN** a user is reading Slice 3
- **AND** Slice 4 has `IsAiFormatted == false`
- **WHEN** the reader view is active on Slice 3
- **THEN** a background request curates Slice 4
- **AND** when the user clicks "Next Slice", Slice 4 renders immediately.

### Requirement: Ephemeral Raw Text Fallback
When AI curation fails in the reader view, the system SHALL display a retry interface with an option to view raw text temporarily without altering persistent database state.

#### Scenario: Temporary view of raw text
- **GIVEN** AI curation fails for an uncurated slice
- **WHEN** the user clicks "View raw text temporarily"
- **THEN** the raw markdown text renders with an amber notice banner
- **AND** `DocumentChunk.IsAiFormatted` remains `false` in the database
- **AND** reloading the page (F5) re-initiates the AI curation workflow.

### Requirement: Standardized TechInsight Markdown Schema
The AI markdown formatting pipeline SHALL transform raw technical document text into a standardized TechInsight reading structure conforming to the following layout:
1. **Document Heading:** Level-1 `# Title` matching the slice chapter name.
2. **Context Callout:** Immediate `> [!NOTE]` blockquote containing a 2–3 sentence executive summary of the architectural context.
3. **Clean Narrative Prose:** Flowing paragraphs with merged sentence fragments and zero unformatted line-breaks. Explanatory sentences MUST never be trapped inside monospace code fences.
4. **Universal Syntax-Tagged Code Blocks:** Every code snippet MUST be enclosed in fenced blocks with its correct language identifier (e.g. `csharp`, `python`, `typescript`, `sql`, `go`, `rust`, `bash`, `yaml`, `dockerfile`).
5. **Architectural Callouts:** Dedicated `> [!TIP]` or `> [!IMPORTANT]` alert boxes for caveats and best practices.
6. **Key Takeaways:** Exactly three bullet points summarizing actionable takeaways at the end of the slice.

#### Scenario: Raw slice converted by AI formatter
- **WHEN** raw extracted text contains code snippets mixed with explanatory prose instructions
- **THEN** AI formatter emits standard markdown with code cleanly segregated into language-tagged fences, prose formatted as body text, and extraneous publication boilerplate removed.

---

### Requirement: On-Demand Just-In-Time (JIT) Slice Formatting
When a user navigates to a slice in `/read/[bookId]` or `/today` that has not yet been processed by the Tier 2 background queue, the reading service SHALL perform on-demand JIT AI formatting in real time, persist the formatted Markdown to the database, and return the curated content seamlessly.

#### Scenario: User navigates ahead to an unformatted slice
- **WHEN** user opens a slice whose `IsAiFormatted` flag is `false`
- **THEN** reader endpoint transparently invokes `IAiMarkdownFormatter.FormatSliceAsync`, saves the resulting Markdown to `DocumentChunk.OriginalTextMarkdown` and sets `IsAiFormatted = true`, and returns the formatted content to the client within ~1.5s.

#### Scenario: User revisits an already formatted slice
- **WHEN** user opens a slice whose `IsAiFormatted` flag is `true`
- **THEN** reader endpoint immediately serves the persisted Markdown from the database without invoking the AI model.

### Requirement: Distraction-Free Daily Reader Pane
The daily reader pane on `/today` (`DocReaderPane.vue`) SHALL present authoritative technical content, summary, key takeaways, and source context without inline micro-quizzes or superficial interruption components. Reading flow ends cleanly after the content or source context, leaving the right pane as the sole evaluation venue.

#### Scenario: User views the daily reader pane on /today
- **WHEN** user navigates to `/today` or selects a curriculum day
- **THEN** `DocReaderPane` renders the document header (title, summary, estimated read time, key takeaway pills), the deep-dive architectural markdown, optional authoritative source context, and optional benchmark snippets.
- **THEN** no inline micro-quiz card or redundant quick-check questions appear at the bottom of the reading column.

#### Scenario: Text selection floating toolbar remains fully functional
- **WHEN** user selects text (2 to 500 characters) inside `.doc-reader-content`
- **THEN** the floating selection toolbar appears above the selection offering "Explain with Gemini", "Highlight", and "Copy".
- **THEN** mouse selection is not blocked or corrupted by deleted quiz selectors.
