# Delta Spec: Immersive Book & Document Reader Capability

## Requirements

### REQ-1: Dedicated Reading Route
- The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`.
- Clicking "Read Book" on any book card in `/library` SHALL navigate to `/read/[bookId]`.

### REQ-2: Table of Contents & Chapter Sidebar
- The reader SHALL include a collapsible Table of Contents sidebar displaying all slices/chapters with estimated reading time, order, and completion indicators.
- Clicking any chapter in the sidebar SHALL smoothly switch the active slice and scroll the reader pane to the top.

### REQ-3: Seamless Next / Previous Slice Navigation
- At the bottom of each slice, the reader SHALL render prominent navigation buttons:
  - `← Previous: #{order-1} {Title}` (disabled on slice 1).
  - `Next Slice: #{order+1} {Title} →` (or `🎉 Finish Book` on the last slice).
- The reader SHALL support keyboard navigation (`Shift + ArrowRight` for next, `Shift + ArrowLeft` for previous).

### REQ-4: Reading Progress Bar & Statistics
- The top navigation bar SHALL display a persistent progress bar showing percentage of slices completed (e.g. `Slice 3 of 12 (25%)`).

### REQ-5: Local Bookmark & Progress Persistence
- The system SHALL automatically record the user's latest read slice for each book in `localStorage` (`techdaily_bookmark_{bookId}`) so navigating back to `/read/[bookId]` automatically resumes at the bookmarked slice.

### REQ-6: Scoped Floating Mini-Toolbar & Active Recall Quiz
- The reader SHALL support highlighting text inside the markdown container to show a discreet floating action bar (`✨ Explain with Gemini` and `📋 Copy`).
- Each slice SHALL optionally render its interactive Micro Quiz check at the end of the chapter.
