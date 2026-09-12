## MODIFIED Requirements

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
