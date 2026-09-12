## MODIFIED Requirements

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
At the bottom of each slice, the reader SHALL render prominent navigation buttons (`← Previous Slice` and `Next Slice →` or return to library) and support keyboard shortcuts (`Shift + ArrowRight` / `Shift + ArrowLeft`). On mobile viewports, the next slice button SHALL expand to full width for comfortable thumb-reach touch interaction.

#### Scenario: User navigates to next slice via button
- **WHEN** user clicks or taps "Next Slice" button at the bottom of a chapter
- **THEN** reader pane transitions to the next sequential slice and scrolls to top.

#### Scenario: User navigates using keyboard shortcuts
- **WHEN** user presses `Shift + ArrowRight` while reading
- **THEN** reader navigates to the next slice.

#### Scenario: Mobile thumb-reach layout
- **WHEN** user reaches bottom of slice on a mobile viewport
- **THEN** primary action button displays as full-width element showing next chapter context.

---

## ADDED Requirements

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, formatting inline code without intrusive default pseudo-element backticks, and providing reliable one-click copy buttons on all code fences without throwing unhandled exceptions.

#### Scenario: Slice title duplication avoidance
- **WHEN** a markdown slice starts with a heading tag matching the slice's chapter title
- **THEN** the reader suppresses the redundant heading in the markdown body and renders body content immediately after the page H1 element.

#### Scenario: Clean inline code rendering
- **WHEN** markdown content contains inline code segments wrapped in single backticks
- **THEN** inline code elements render as styled pills without decorative backtick characters in pseudo-elements (`before`/`after`).

#### Scenario: Reliable code snippet copying
- **WHEN** user clicks the "Copy" button on any syntax-highlighted code fence
- **THEN** the code snippet is decoded and copied to the system clipboard, and the button provides temporary visual confirmation ("Copied!").

#### Scenario: Accurate slice badge display
- **WHEN** reading any document slice
- **THEN** the badge above the title displays "Slice {current} of {total}" in English and "Lát cắt {current} / {total}" in Vietnamese instead of day drill labels.
