# Reader Specification

## Purpose
Provides an immersive technical reading experience with chapter table of contents, progress indicators, keyboard shortcuts, local bookmarking, active recall quizzes, and scoped text highlight tools.

## Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, novel-style typography settings toggle (`Aa`), theme toggle, and 1-click quiz launcher. The reader page shell, sticky header, popovers, and backdrop elements SHALL render with Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) and hairline translucent borders (`border-slate-200/80 dark:border-white/[0.08]`). All interactive controls, typography adjusters, navigation hints, and loading indicators SHALL be fully localized through i18n message catalogs in both English and Vietnamese.

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

#### Scenario: Reader interface 100% localization coverage
- **WHEN** user views the dedicated reader route in any supported locale (English or Vietnamese)
- **THEN** all typography controls (font families "Sans", "Serif", "Mono", size adjusters "Smaller Font" / "Larger Font"), slice navigation hints ("Previous Slice (Shift + ←)" / "Next Slice (Shift + →)"), Table of Contents close buttons, and chapter loading spinners render with localized text from the i18n message catalog without raw English literals.
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
The reader floating selection toolbar SHALL render exactly 3 streamlined action buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`. The direct flashcard creation button SHALL be removed from the reader selection tooltip to protect reading immersion and avoid premature card generation. The `Highlight/Note` action SHALL unify text highlighting and note-taking into a single continuous action: clicking the button immediately creates and persists a highlight record (`POST /api/v1/notes/highlights`), while smoothly opening an attached reflection popover where users can optionally add personal reflection notes and technical tags.

The floating toolbar container and attached note popover SHALL render with **Dev-Learning Studio** elevated obsidian panels (`bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/60 dark:border-white/[0.12] rounded-2xl shadow-2xl`) and adhere to the following design system invariants:
1. **Toolbar Button Hierarchy**:
   - `Explain with Gemini`: Styled as a primary Iris Violet accent button (`bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs shadow rounded-xl`).
   - `Highlight/Note`: Styled with Iris Violet brand-tinted glass (`bg-brand-500/10 text-brand-300 border border-brand-500/20 hover:bg-brand-500/20 hover:text-white rounded-xl`), transitioning to an elevated active state when the note popover is open. Harsh amber/orange button styling (`bg-amber-500`) is strictly prohibited.
   - `Copy`: Styled as a refined neutral translucent glass button (`text-slate-300 hover:text-white hover:bg-white/[0.08] rounded-xl`).
2. **Input Placeholders & Guidance**:
   - The reflection note textarea SHALL display the localized placeholder text (`reader.note_placeholder`) to guide architectural reflection and prevent blank void states.
   - The tag input field SHALL display the localized placeholder text (`reader.tags_placeholder`).
3. **Refined Focus Outlines & Quote Display**:
   - Inputs SHALL render with subtle hairline borders (`border-slate-700/80 dark:border-white/[0.10]`) and soft focus rings (`focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60`), eliminating harsh, thick neon borders.
   - Excerpt quotes SHALL display subtle Iris Violet accent borders (`border-l-2 border-brand-500/60 pl-2.5 py-0.5 text-xs text-slate-300 dark:text-slate-300 italic line-clamp-2`).

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
- **AND** quote preview renders with a primary brand border indicator (`border-brand-500/60`).

#### Scenario: Reflection textarea and tag inputs display localized placeholders
- **WHEN** user opens the reflection note popover from the floating selection toolbar
- **THEN** the reflection textarea displays the placeholder "Viết đúc kết hoặc suy ngẫm kiến trúc của bạn..." in Vietnamese or "Write your reflection or architectural takeaway..." in English
- **AND** the tag input displays "Thẻ phân loại (vd: storage, concurrency)" in Vietnamese or "Tags (e.g. storage, concurrency)" in English
- **AND** focusing the textarea activates a soft Iris Violet focus ring (`focus:ring-1 focus:ring-brand-500/40`) without glaring thick outlines.

#### Scenario: Unified Dev-Learning Studio button hierarchy in floating toolbar
- **WHEN** the floating selection toolbar appears
- **THEN** the `Highlight/Note` button renders using Iris Violet brand glass tokens (`bg-brand-500/10 text-brand-300 border-brand-500/20`)
- **AND** zero amber or orange background styling is rendered.

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, suppressing redundant trailing Key Takeaways sections from the markdown body when structured takeaways are present, formatting inline code (`code:not(pre code)`) with Dev-Learning Studio tokens (neutral pill in light mode with `bg-slate-100 text-slate-800 border-slate-200/90`, and Obsidian pill in dark mode with `dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08] font-medium`, eliminating legacy green/emerald styling across both `/read/[bookId]` and `/today`), formatting markdown links with system primary brand tokens (`prose-a:text-brand-600 dark:prose-a:text-brand-400 hover:prose-a:underline`), parsing technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`), and providing reliable 1-click syntax-highlighted code block copying with localized confirmation feedback.

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
- **THEN** the badge above the title displays "Slice {current} of {total}" in English and "Lát cắt {current} / {total}" in Vietnamese instead of day drill labels
- **AND** the badge renders via valid Vue template interpolation without outputting unparsed JavaScript expressions or raw `$t(...)` code text.

#### Scenario: Key Takeaways deduplication in article body
- **WHEN** the slice contains structured key takeaways (`hasValidTakeaways` is true)
- **THEN** the markdown renderer suppresses any trailing `### Key Takeaways` or `## Key Takeaways` heading and bullet list from the article markdown body
- **AND** key takeaways render exclusively inside the dedicated callout card below the article body, eliminating duplicate content and avoiding untranslated English headings in the Vietnamese locale.

#### Scenario: Key Takeaways callout card brand palette
- **WHEN** the Key Takeaways callout card renders below the reading article body
- **THEN** it renders with Dev-Learning Studio system primary brand violet tokens (`bg-brand-50/50 dark:bg-brand-500/10`, `border-brand-200/80 dark:border-brand-500/20`, `text-brand-900 dark:text-brand-300`, `Sparkles text-brand-600 dark:text-brand-400`, bullet dots `bg-brand-500`)
- **AND** no warning amber colors (`amber-50`, `amber-950`, `amber-200`, `amber-500`) are applied to the key takeaways card.

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

---

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

### Requirement: Code Block Horizontal Alignment and Indentation Bounds

Code blocks rendered within reading slices SHALL preserve visual alignment between the code header controls and code body text, preventing flush-left alignment across initial cold navigations, subsequent client-side slice transitions, and hard reloads.

#### Scenario: Cold navigation to reading slice displays indented code block

- **WHEN** a user navigates directly or via client router to `/read/[bookId]?slice={order}`
- **THEN** the code block content is indented with horizontal padding matching or exceeding the header window buttons, without touching the left card border.

### Requirement: Lightweight TOC and Lazy Slice Loading
The reader SHALL decouple Table of Contents metadata from slice body content. When navigating to `/read/[bookId]`, the reader SHALL immediately load the book metadata and lightweight TOC items (`id`, `chunkOrder`, `chapterTitle`, `estimatedReadMinutes`, `isAiFormatted`), rendering the reader shell and TOC in `<100ms`. The reader SHALL load the active slice content on demand and cache retrieved slice content in memory for instant switching.

#### Scenario: User opens a multi-slice book in the reader
- **WHEN** user opens `/read/[bookId]` for a book with multiple slices
- **THEN** reader receives lightweight TOC metadata without transferring full markdown bodies for all slices
- **AND** reader loads the active slice content on demand, rendering the reading pane without multi-second delays.

#### Scenario: User navigates between previously visited slices
- **WHEN** user navigates back to a slice that was already loaded during the current reading session
- **THEN** reader renders the slice immediately from in-memory cache without an additional network request.

### Requirement: Two-Tier Semantic Cache for AI Term Explainer
The `TermExplanationService` SHALL evaluate term lookup requests using a two-tier caching strategy combining exact string matching and pgvector approximate nearest neighbor search against `TermExplanationCaches`. The service SHALL enforce strict database column length safety by clamping normalized term strings to a maximum of 200 characters (`varchar(200)`) prior to cache querying, embedding generation, or database persistence. Auxiliary cache write operations SHALL execute within a non-blocking fault-tolerant boundary such that database persistence failures do not abort or discard the primary AI-generated term explanation.

#### Scenario: Exact match cache hit (Tier 1)
- **WHEN** user selects a technical term whose normalized text matches an existing cache entry (`t.Term == normalizedTerm && t.Locale == locale`)
- **THEN** system immediately returns the cached explanation within <5ms, increments `HitCount`, and bypasses both the vector search and external LLM.

#### Scenario: Semantic vector cache hit (Tier 2)
- **WHEN** exact match misses, but the generated embedding of the selected term has a cosine distance `<= 0.08` (similarity `>= 92%`) with an existing cached term in the same category and locale
- **THEN** system returns the semantically equivalent cached explanation within <150ms, increments `HitCount`, and avoids calling Gemini Flash completion.

#### Scenario: Full cache miss fallback
- **WHEN** neither exact nor semantic cache match is found
- **THEN** system invokes Gemini Flash to generate a fresh explanation, generates the term's embedding vector, persists the new entry into `TermExplanationCaches` with its vector, and returns the explanation.

#### Scenario: Term length clamping for safe cache persistence
- **WHEN** a user highlights a multi-line technical sentence or phrase exceeding 200 characters (e.g. 237 characters)
- **THEN** `TermExplanationService` clamps the normalized term to 200 characters (`normalizedTerm[..200]`) before checking the cache, generating embeddings, or inserting into `TermExplanationCaches`
- **AND** the operation completes successfully without throwing a PostgreSQL `22001 (value too long for type character varying(200))` exception.

#### Scenario: Non-blocking resilience during cache persistence failure
- **WHEN** Google Gemini generates a valid explanation but secondary database persistence to `TermExplanationCaches` fails due to a transient database lock, unique constraint collision, or timeout
- **THEN** `TermExplanationService` logs a warning without throwing an unhandled exception
- **AND** returns the valid LLM explanation to the client with `IsFromCache = false`.

---

### Requirement: In-Context "Ask AI About This Book" (RAG)
The reader SHALL provide an interactive slide-over chat drawer allowing readers to ask technical questions grounded exclusively in the active document book.

#### Scenario: User asks a question about the active book
- **WHEN** user submits a technical query in the reader's Ask Book drawer
- **THEN** backend vectorizes the question, retrieves the top 3 most relevant `DocumentChunk` records from the current book (`DocumentBookId == id`) ordered by cosine distance, and synthesizes a grounded answer using `gemini-3.5-flash-lite`.

#### Scenario: Grounded citations and source jumping
- **WHEN** the AI response is delivered to the reader
- **THEN** response includes citation metadata (`chunkOrder`, `chapterTitle`, `relevanceScore`), rendered in the UI as clickable badges. Clicking a badge immediately navigates the reader to that specific chapter slice and highlights the excerpt.

#### Scenario: Information not present in book
- **WHEN** user asks a question whose answer is not covered in the book's slices
- **THEN** the AI explicitly states that the active document does not cover the requested topic, preventing speculative or out-of-context hallucinations.

---

### Requirement: Multi-Layer Anti-Spam & Rate Limiting Defense
All AI generation and RAG endpoints (`/ask`, `/explain-term`, `/quiz/generate`) SHALL enforce multi-layer anti-spam protection across reverse proxy, API rate limiting, and client-side UI guarding.

#### Scenario: User exceeds rate limit threshold
- **WHEN** a client submits more than 10 AI requests within a rolling 60-second window
- **THEN** the system rejects subsequent requests with `HTTP 429 Too Many Requests` problem details containing a `Retry-After` header without invoking external Gemini services.

#### Scenario: Rapid multi-click prevention in reader UI
- **WHEN** user clicks "Ask Book" or triggers generation while an existing request is pending
- **THEN** the frontend locks the action with an active mutex (`isAsking = true`), disables buttons with loading spinners, and ignores subsequent clicks or keyboard submissions until completed.

#### Scenario: Request input bounds validation
- **WHEN** a user submits an Ask Book question exceeding 300 characters or under 3 characters
- **THEN** the request fails validation immediately with `HTTP 400 Bad Request`, preventing prompt injection and excessive token consumption.

### Requirement: Responsive Term Explainer Modal Layout
The `TermExplainerModal` SHALL provide a responsive, overflow-resistant header layout that cleanly displays category metadata, term title, instant cache indicators, and modal dismiss controls across all screen sizes without text wrapping or button collision.

#### Scenario: Long book category title with cache hit
- **WHEN** user opens the term explainer modal for a term whose category/book title exceeds 30 characters and the term was resolved from the cache (`isFromCache == true`)
- **THEN** the category text is truncated cleanly with an ellipsis (`truncate max-w-[180px] sm:max-w-xs`), the `⚡ Instant Cache` badge maintains a single line with `whitespace-nowrap shrink-0`, and the modal close button remains unclipped and interactive at `shrink-0`.

#### Scenario: Mobile viewport header containment
- **WHEN** user views the term explainer modal on a mobile device (<640px viewport width)
- **THEN** the header elements respect flex boundaries (`min-w-0 flex-1`), preventing horizontal scroll or content leaking outside the modal container.

---

### Requirement: Comprehensive i18n Localization in Reader Term Explainer
All copy inside `TermExplainerModal` SHALL be fully localized through the platform's internationalization framework (`useI18n`), dynamically rendering translated labels for cache status, loading animations, branding footers, and copy-to-clipboard interactions based on the active locale (`en` or `vi`).

#### Scenario: Explainer modal rendered in Vietnamese locale
- **WHEN** a user with locale set to Vietnamese (`vi`) opens the term explainer modal
- **THEN** the cache badge displays "⚡ Bộ nhớ tức thì", the loading state displays "Đang phân tích thuật ngữ với Google Gemini...", the footer displays "Được hỗ trợ bởi Google Gemini", and the action button displays "Sao chép giải thích" (transitioning to "Đã sao chép" upon click).

#### Scenario: Explainer modal rendered in English locale
- **WHEN** a user with locale set to English (`en`) opens the term explainer modal
- **THEN** all modal copy displays corresponding English translations without missing key warnings or raw localization fallback keys.

---

### Requirement: Surrounding Document Context Extraction for In-Reader Term Explanations
When invoking the AI term explainer from an active text selection in `/read/[bookId]`, the reader SHALL extract the surrounding document context from the enclosing DOM element (up to 500 characters enclosing the selected term) and pass this text to `currentContext`, falling back to the chapter title only when surrounding DOM text is unavailable.

#### Scenario: User selects a term inside a narrative paragraph
- **WHEN** user selects a technical term (such as "Write-Ahead Log") within a paragraph in the reader pane
- **THEN** the reader captures up to 500 characters of the surrounding paragraph text encompassing the selection and sends it as the `context` parameter to the `/explain-term` API request.

#### Scenario: Context extraction within dense code or list elements
- **WHEN** user selects a term within an inline code block or list item
- **THEN** the reader traverses to the nearest parent block container (`p`, `li`, `blockquote`, `div`), retrieves up to 500 characters of surrounding text, and supplies it as contextual grounding.

#### Scenario: Fallback when selection container text is inaccessible
- **WHEN** the selected text DOM node cannot be resolved or contains no additional textual context
- **THEN** the reader gracefully defaults `currentContext` to the active slice chapter title (`currentChunk.chapterTitle`).

---

### Requirement: 1-Click Active Recall Flashcard Generation from Highlights
The system SHALL provide an automated bridge (`POST /api/v1/review/cards/from-highlight`) converting any highlighted quote and attached reflection note into an SM-2 spaced repetition flashcard via Google Gemini.

#### Scenario: User transforms a highlight into an active recall card
- **WHEN** user clicks `⚡ Turn into Flashcard` from the reader note popover or from `/notes`
- **THEN** the backend fetches the highlight and chapter context, prompts Google Gemini to synthesize a conceptual challenge question (`Front`) and architectural explanation (`Back`), saves a new `SpacedRepetitionCard` with `SourceType = CardSourceType.Highlight`, and schedules it for immediate review in `/review`.

#### Scenario: Duplicate flashcard creation prevention
- **WHEN** user attempts to turn the same highlight into a flashcard multiple times
- **THEN** the system detects the existing card by `SourceHighlightId`, returns the existing card details without creating redundant duplicate database records, and notifies the user.

#### Scenario: Gemini synthesis failure fallback
- **WHEN** the AI service is unavailable or rate-limited during flashcard generation
- **THEN** the system generates a structured fallback flashcard using the highlighted quote as the prompt context and the attached note or chapter summary as the answer, ensuring the review card is successfully provisioned.

---

### Requirement: Obsidian and Notion Markdown Book Exporter
The system SHALL expose an automated export endpoint (`GET /api/v1/library/books/{id}/export-markdown`) compiling a book's metadata, chapter summaries, key takeaways, and user highlights/notes into a standardized Markdown file with YAML frontmatter suitable for Obsidian, Logseq, and Notion vaults.

#### Scenario: User downloads book notes as Obsidian Markdown
- **WHEN** user clicks `Export to Obsidian / Markdown` in the reader drawer or book details modal
- **THEN** the browser downloads `{book-slug}-notes.md` with `Content-Type: text/markdown; charset=utf-8`.

#### Scenario: Exported file YAML frontmatter structure
- **WHEN** the export file is generated
- **THEN** the file begins with a valid YAML frontmatter block enclosed by `---` containing `title`, `author`, `category`, `source_url`, `exported_at`, `total_chapters`, `total_highlights`, and array of `tags`.

#### Scenario: Chapter highlights formatting with personal notes
- **WHEN** the export compiles a chapter containing user highlights
- **THEN** each highlight renders as a Markdown blockquote (`> "Quote text..."`), followed immediately by `**Personal Note:** {note}` and formatted hashtag chips (`#tag1 #tag2`), correctly linking the author's words with the engineer's reflections.

### Requirement: Highlight Notes System & Flashcard Generation
The Notes management interface (`/notes`) SHALL support converting reading highlights into active recall SM-2 flashcards via `POST /api/v1/review/cards/from-highlight`. Composable utilities (such as `useI18n`, `useToast`, `useReviewStore`, `useApiError`) SHALL be initialized and destructured exclusively at the synchronous top level of the `<script setup>` block in accordance with Vue 3 Composition API injection lifecycle constraints. Asynchronous event callbacks SHALL NOT invoke dependency-injecting composables inline.

#### Scenario: User generates flashcard from highlight in notes view
- **WHEN** an authenticated user clicks "Flashcard SM-2" on any saved reading highlight in `/notes`
- **THEN** the handler reads the synchronously captured `locale` ref without triggering a `[vue-i18n] Not found injection "vue-i18n"` runtime exception
- **AND** the client invokes `POST /api/v1/review/cards/from-highlight` passing the highlight ID and resolved locale string
- **AND** upon successful generation, displays the localized success toast (`notes.toast_flashcard_success`) and marks the highlight card as generated.

#### Scenario: Error handling during flashcard generation in notes view
- **WHEN** the backend returns an error or network connection fails during flashcard creation from `/notes`
- **THEN** the handler catches the error, releases the loading lock (`creatingCardHighlightId = null`), and displays a localized error toast (`notes.toast_flashcard_error`) without unhandled client crashes.

### Requirement: Reading Highlight Deduplication and Note Upsert
The highlight creation endpoint (`POST /api/v1/notes/highlights`) SHALL perform an idempotent query against `UserHighlights` using `(UserId, DocumentChunkId, SelectedText.Trim())` before persisting new records. If an identical text selection already exists for the user in the specified document chunk, the system SHALL update existing reflection notes and tags if provided and return the existing highlight entity rather than creating redundant database rows.

#### Scenario: User highlights previously highlighted text
- **WHEN** an authenticated user selects text in `/read/[bookId]` or `/today` that was previously saved as a highlight in the same chunk
- **THEN** `CreateHighlightHandler` detects the existing highlight record
- **AND** returns `HTTP 200 OK` with the existing `HighlightId` without inserting a new database row.

#### Scenario: User attaches note to existing highlight
- **WHEN** user opens the note popover on previously highlighted text, types a personal reflection note, and clicks `Save Note`
- **THEN** the handler updates `UserHighlight.Note` and `UserHighlight.UpdatedAt` on the existing entity and returns the updated DTO.

#### Scenario: Flashcard creation reuses deduplicated highlight
- **WHEN** user initiates "Turn into Flashcard" from a text selection that already exists as a highlight
- **THEN** the client receives the stable existing `HighlightId`
- **AND** dispatches `POST /api/v1/review/cards/from-highlight` with the existing ID, allowing card idempotency checks to prevent duplicate flashcards.

---

### Requirement: Resilient Term Explainer Error Handling and Retry Action
The `TermExplainerModal` component SHALL maintain a distinct error state (`errorMessage`) completely separated from the Markdown explanation content. When an explanation request fails due to network or server issues, the modal SHALL render a dedicated error banner with a localized message and a "Retry" button rather than rendering raw error strings inside the Markdown prose box.

#### Scenario: Server error displays dedicated error banner with retry button
- **WHEN** an error occurs during term explanation (such as HTTP 500 or network failure)
- **THEN** `TermExplainerModal` renders an error alert banner with an `AlertCircle` icon, localized error description, and a `Retry` action button
- **AND** does not render raw error strings inside the Markdown explanation container.

#### Scenario: Successful retry clears error banner and renders markdown explanation
- **WHEN** user clicks the `Retry` button in the error alert banner
- **THEN** the modal re-invokes `loadExplanation()`, shows the loading spinner, and upon success clears the error state and renders the Markdown explanation.

#### Scenario: Copy button disabled or hidden during error state
- **WHEN** the modal is displaying an error state
- **THEN** the "Copy" action button in the footer is disabled or hidden to prevent copying error messages to the clipboard.

### Requirement: Novel-Style Reader Typography Controls and Layout Customization
The reader page (`/read/[bookId]`) SHALL provide a comprehensive typography customization popover accessible via an `Aa` action button in the reader top navigation bar immediately adjacent to the `ThemeToggle` component. The typography system SHALL allow readers to adjust font size, font family, line spacing, and reading column width, immediately applying changes to the reading pane and persisting preferences in browser storage.

The typography engine SHALL support the following configuration dimensions:
1. **Font Size Scaling:**
   - Five distinct size scale presets: `sm` (14px), `base` (16px, default), `lg` (18px), `xl` (20px), and `2xl` (22px).
   - Stepped decrement (`A-`) and increment (`A+`) buttons with a visual indicator showing the active scale step.
2. **Font Family Selection:**
   - **Sans-Serif (Default):** Modern UI sans-serif stack (`Inter`, `system-ui`, `-apple-system`, `sans-serif`).
   - **Serif (Novel Standard):** High-readability editorial serif stack (`Merriweather`, `Georgia`, `Lora`, `serif`).
   - **Monospace (Technical):** Fixed-width programming stack (`JetBrains Mono`, `ui-monospace`, `monospace`).
3. **Line Spacing (Leading):**
   - **Standard:** `leading-normal` (line height 1.5).
   - **Relaxed (Default):** `leading-relaxed` (line height 1.625).
   - **Loose:** `leading-loose` (line height 2.0).
4. **Reading Column Width:**
   - **Standard (Default):** `max-w-3xl` (~768px, optimal for focused novel reading).
   - **Wide:** `max-w-4xl` (~896px, optimal for multi-column code comparisons).
   - **Full:** `max-w-full` (fluid edge-to-edge reading container).
5. **Preference Persistence:**
   - Settings SHALL automatically save to `localStorage` under key `techdaily_reader_typography` as a serialized JSON object.
   - Upon mounting `/read/[bookId]`, the reader SHALL hydrate typography state from `localStorage`, gracefully falling back to defaults (`{ fontSize: 'base', fontFamily: 'sans', lineSpacing: 'relaxed', readingWidth: 'standard' }`) if no saved preferences exist.
6. **Paragraph Formatting & Rhythm:**
   - Prose text inside the reader container SHALL render cleanly separated `<p>` tags with vertical margins (`my-4` to `my-5`) rather than merging into a continuous wall of text.

#### Scenario: User opens typography controls popover from reader topbar
- **GIVEN** an active reading session on `/read/[bookId]`
- **WHEN** user clicks the `Aa` button in the top navigation bar
- **THEN** a floating typography popover appears directly beneath the button
- **AND** the popover displays controls for Font Size (`A-` / `A+`), Font Family (Sans, Serif, Mono), Line Spacing (Standard, Relaxed, Loose), and Reading Width (Standard, Wide, Full).

#### Scenario: User adjusts font size across 5 scale presets
- **GIVEN** the typography popover is open with default font size `base` (16px)
- **WHEN** user clicks the `A+` button
- **THEN** font size increases to `lg` (18px)
- **AND** the reader article prose updates reactively in real time
- **WHEN** user clicks `A+` again until reaching `2xl` (22px)
- **THEN** the `A+` button becomes visually disabled at the maximum scale boundary.

#### Scenario: User switches font family between Sans, Serif, and Monospace
- **GIVEN** the reader is displaying prose in the default Sans-Serif font family
- **WHEN** user selects the "Serif" font family button in the typography popover
- **THEN** the reader article element dynamically applies the serif font family stack
- **AND** user observes high-readability literary serif typography suitable for book reading.

#### Scenario: User adjusts line spacing between Standard, Relaxed, and Loose
- **GIVEN** the typography popover is open
- **WHEN** user selects "Loose" line spacing
- **THEN** the article element switches class to `leading-loose`
- **AND** vertical spacing between text lines increases to 2.0em for relaxed reading.

#### Scenario: User adjusts reading width between Standard, Wide, and Full
- **GIVEN** the reader view is displayed on a wide desktop screen ($\ge 1280\text{px}$)
- **WHEN** user selects "Wide" reading width in the typography popover
- **THEN** the article container width expands from `max-w-3xl` to `max-w-4xl`
- **WHEN** user selects "Full" reading width
- **THEN** the article container width expands to `max-w-full`.

#### Scenario: Typography preferences persist across sessions and books via LocalStorage
- **GIVEN** a user configures font size `lg`, font family `serif`, line spacing `loose`, and reading width `wide`
- **WHEN** the user navigates away to `/library` or reloads the browser tab
- **THEN** `localStorage.getItem('techdaily_reader_typography')` contains the updated preferences
- **AND** opening any book at `/read/[bookId]` automatically hydrates and applies the saved typography settings.

#### Scenario: Paragraph breaks render cleanly as distinct HTML <p> tags with vertical margins
- **GIVEN** a book slice containing multiple paragraphs separated by double newlines (`\n\n`)
- **WHEN** `useMarkdownRenderer` renders the content inside `/read/[bookId]`
- **THEN** each paragraph is contained within an individual `<p>` element
- **AND** paragraphs are separated by distinct vertical margins without run-on text collapse.

### Requirement: Immersive Reader Studio Mobile Height and Typography Controls
The dedicated Reader Studio (`pages/read/[bookId].vue`) and document reading surfaces (`DocReaderPane.vue`) SHALL utilize dynamic viewport units (`h-dvh`), safe area padding, and responsive segmented typography controls to prevent layout clipping, text wrapping, and event listener leaks on mobile devices.

#### Scenario: Dynamic Viewport Height on Mobile Reader
- **WHEN** user reads technical documentation on a mobile device
- **THEN** the reader container and table-of-contents drawer SHALL use `h-dvh` to ensure reading position and bottom navigation bars stay visible when browser address bars auto-hide.

#### Scenario: Typography Settings Segmented Control on 320px Screens
- **WHEN** user opens typography settings popover on a $320\text{px}$ to $375\text{px}$ screen
- **THEN** the font selection chips ("Sans", "Serif", "Mono") and line-height buttons SHALL preserve `whitespace-nowrap shrink-0` and scale without button clipping.

#### Scenario: Storage Synchronization Event Hygiene
- **WHEN** reader typography preferences are updated in `useReaderTypography.ts`
- **THEN** cross-tab synchronization SHALL utilize VueUse `useEventListener(window, 'storage', ...)` ensuring automatic cleanup on unmount.

### Requirement: Modular Reader Chrome Architecture
The reader studio at `/read/[bookId]` SHALL decompose its monolithic interface into dedicated, single-responsibility sub-components located in `frontend/components/reader/`:
1. **ReaderHeaderBar (`ReaderHeaderBar.vue`)**: Renders top navigation bar containing library return link, responsive Table of Contents trigger, document title, chapter label, slice progress bar, novel-style typography settings popover (`Aa`), theme toggle, and 1-click active recall quiz button.
2. **ReaderTocSidebar (`ReaderTocSidebar.vue`)**: Renders collapsible left TOC sidebar on desktop (>= 768px) and off-canvas slide-over drawer with backdrop blur on mobile (< 768px), featuring chapter search filter, slice reading time estimates, active slice highlight, and completion status indicators.
3. **ReaderNavigationCards (`ReaderNavigationCards.vue`)**: Renders bottom navigation containing two symmetric glass cards for preceding slice and succeeding slice (or return to library on completion), respecting minimum touch targets and preventing text clipping or wrapping across both English and Vietnamese locales.

#### Scenario: User navigates reading interface on desktop
- **WHEN** user opens `/read/[bookId]` on a desktop screen
- **THEN** the modular header, collapsible TOC sidebar, and bottom navigation cards render coherently with shared typography and dark mode theme state without template duplication.

#### Scenario: User toggles mobile TOC drawer
- **WHEN** user taps contents button on a mobile viewport (< 768px)
- **THEN** `ReaderTocSidebar` renders as an off-canvas drawer over backdrop blur and dismisses upon chapter selection or backdrop touch.

### Requirement: Centered Reading Canvas & High-DPI Visual Density
The reading article body at `/read/[bookId]` SHALL render inside a centered container (`#main`) constrained by the active reading width preset (`max-w-3xl` for standard, `max-w-4xl` for wide, `max-w-full` for full width) with symmetric gutters (`px-4 sm:px-8 md:px-12`):
1. **Symmetric Margin Alignment**: On wide screens and high-DPI displays (including Windows 11 1080p/2K viewports), reading prose SHALL remain centered without left-biased drift or uneven whitespace when the TOC sidebar collapses or expands.
2. **Typography Scaling Integrity**: Paragraphs, lists, blockquotes, and headings within the reading prose container SHALL reactively scale according to the active `useReaderTypography` scale factor without overflowing or clipping container boundaries.

#### Scenario: User toggles wide reading width
- **WHEN** user selects "Wide" reading width in the typography settings popover
- **THEN** the `#main` reading article container expands smoothly to `max-w-4xl mx-auto` while preserving symmetric horizontal padding.
