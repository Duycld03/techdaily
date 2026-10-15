# Reader Spec Delta

## MODIFIED Requirements

### Requirement: Local Bookmark & Progress Persistence
The system SHALL automatically record the user's latest read slice for each book in `localStorage` (`deeppace_bookmark_{bookId}`) so navigating back to `/read/[bookId]` automatically resumes at the bookmarked slice.
The reader SHALL store its local completed-slice set at `deeppace_completed_{bookId}`. Known legacy state SHALL transfer only through the centralized client-storage migration; local markers SHALL not bypass ownership or independently alter server learning progress.

#### Scenario: User returns to a previously read book
- **WHEN** user navigates back to `/read/[bookId]` without explicit `?slice=` parameter
- **THEN** reader retrieves stored bookmark and resumes display at the bookmarked slice.


### Requirement: Isolated External Link Navigation
The reader markdown renderer SHALL configure anchor rendering such that all external documentation links automatically include `target="_blank"` and `rel="noopener noreferrer"`. Clicking any external link inside an article slice SHALL open the external reference in a new browser tab without replacing or disrupting the active DeepPace reading session.

#### Scenario: User clicks external reference link in reader pane
- **WHEN** user clicks on an external documentation link rendered in markdown (e.g. `[DI](https://learn.microsoft.com/...)`)
- **THEN** the link opens in a separate browser tab with secure `noopener noreferrer` attributes, and the DeepPace reading route `/read/[bookId]` remains open at the current slice.

#### Scenario: In-page anchor navigation
- **WHEN** user clicks a table-of-contents or anchor link targeting a heading on the same page (e.g. `[Basics](#routing-basics)`)
- **THEN** the link does not open a new tab and instead scrolls smoothly to the target element within the active reading pane.

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
   - Settings SHALL automatically save to `localStorage` under key `deeppace_reader_typography` as a serialized JSON object.
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
- **THEN** `localStorage.getItem('deeppace_reader_typography')` contains the updated preferences
- **AND** opening any book at `/read/[bookId]` automatically hydrates and applies the saved typography settings.

#### Scenario: Paragraph breaks render cleanly as distinct HTML <p> tags with vertical margins
- **GIVEN** a book slice containing multiple paragraphs separated by double newlines (`\n\n`)
- **WHEN** `useMarkdownRenderer` renders the content inside `/read/[bookId]`
- **THEN** each paragraph is contained within an individual `<p>` element
- **AND** paragraphs are separated by distinct vertical margins without run-on text collapse.

### Requirement: Audio-Driven Automated Slice Progression and Bookmark Synchronization
When reader audio auto-advances to a subsequent slice, the reader view SHALL automatically synchronize reading state, content display, and progress tracking.
1. **View Transition**: The reader SHALL update `activeChunkIndex`, switch the visible slice content, re-render markdown formatting, and reset the reader viewport to the top of the new slice.
2. **Bookmark Persistence**: Upon audio-driven progression, the reader SHALL immediately persist the new slice index to local storage (`deeppace_bookmark_{bookId}`), ensuring that reloading the reader resumes at the newly reached slice.
3. **Pacer Synchronization**: The reader SHALL trigger progress synchronization with the backend `UserBookPacer` API (`completedSlices` update), crediting reading time and streak milestones without requiring manual navigation card clicks.
4. **Active Slice Audio Isolation**: The previous slice's audio or synthesis tasks SHALL be fully detached and stopped before the new slice begins, preventing audio overlap or duplicate workers.

#### Scenario: Audio auto-advances and synchronizes reader bookmark
- **WHEN** audio narration completes on slice 2 and auto-advances to slice 3
- **THEN** the reader view displays slice 3 content, `localStorage.getItem('deeppace_bookmark_<bookId>')` is updated to index 3, and reading progress is recorded with the backend pacer.

#### Scenario: User refreshes page after audio auto-advance
- **WHEN** the user refreshes `/read/[bookId]` after audio has auto-advanced through 3 slices
- **THEN** the reader initializes at slice 3 with saved bookmark intact.
