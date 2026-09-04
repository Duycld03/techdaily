# Reader Specification

## Purpose
Provides an immersive technical reading experience with chapter table of contents, progress indicators, keyboard shortcuts, local bookmarking, active recall quizzes, and scoped text highlight tools.

## Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`.

#### Scenario: User opens a book from the library
- **WHEN** user clicks "Read Book" on a book card in `/library`
- **THEN** application navigates to `/read/[bookId]` loading the first slice or bookmarked slice.

---

### Requirement: Table of Contents & Chapter Sidebar
The reader SHALL include a collapsible Table of Contents sidebar displaying all slices/chapters with estimated reading time, order, and completion indicators.

#### Scenario: User selects a chapter from table of contents
- **WHEN** user clicks a chapter item in the reader sidebar
- **THEN** reader pane smoothly switches to the selected slice and resets scroll position to the top.

---

### Requirement: Seamless Next / Previous Slice Navigation
At the bottom of each slice, the reader SHALL render prominent navigation buttons (`← Previous: #{order-1}` and `Next Slice: #{order+1} →` or finish button) and support keyboard shortcuts (`Shift + ArrowRight` / `Shift + ArrowLeft`).

#### Scenario: User navigates to next slice via button
- **WHEN** user clicks "Next Slice" button at the bottom of a chapter
- **THEN** reader pane transitions to the next sequential slice and scrolls to top.

#### Scenario: User navigates using keyboard shortcuts
- **WHEN** user presses `Shift + ArrowRight` while reading
- **THEN** reader navigates to the next slice.

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
The reader SHALL support highlighting text inside the markdown container to show a discreet floating action bar (`✨ Explain with Gemini` and `📋 Copy`), and optionally render an interactive Micro Quiz check at the end of the chapter.

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text inside the reader markdown container
- **THEN** floating toolbar appears with Gemini Explainer and Copy actions.
