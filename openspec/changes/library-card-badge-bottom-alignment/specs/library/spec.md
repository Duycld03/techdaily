## ADDED Requirements

### Requirement: Consistent Book Card Status Badge Baseline Alignment
The library catalog on `/library` SHALL align the bookmark resume badge, ready badge, and in-progress ingestion status indicator to a consistent bottom baseline across all book cards within each row of the library grid, regardless of variable title lengths (1-line vs 2-line) or the presence/absence of the author or source URL subtitle.

1. **Flex-Stretched Card Content Structure:**
   - The book card upper content container SHALL flex-stretch (`flex flex-col flex-1`) to occupy all available vertical space above the card action footer.
   - The category badge, total chunks count, document title (`<h3>`), and optional subtitle (`<p>`) SHALL remain anchored at the top of the upper content area.
2. **Bottom-Anchored Status Container:**
   - The status indicator elements (bookmark resume badge, ready badge, and processing ingestion indicator) SHALL reside within a bottom-anchored container (`mt-auto pt-3`) inside the upper content wrapper.
   - The status container SHALL maintain an identical vertical distance directly above the card footer divider (`pt-4 border-t`) across adjacent cards of differing content heights.

#### Scenario: Book card with 1-line title aligns status badge with 2-line title card
- **WHEN** multiple book cards with differing title line counts (e.g. a 1-line title alongside 2-line titles) are rendered in the `/library` grid
- **THEN** both cards' status badges are pinned to the bottom of the content container via `mt-auto`
- **AND** the top edges and baselines of the badges align horizontally across the grid row directly above the card footer action divider.

#### Scenario: Book card without author or source URL maintains bottom-aligned badge
- **WHEN** a book card without an author or source URL subtitle is rendered alongside cards with subtitle text
- **THEN** the status badge container uses `mt-auto` to anchor directly above the card footer divider
- **AND** the absence of the subtitle does not cause the status badge to float higher up in the card body.

#### Scenario: In-progress ingestion indicator aligns with completed and bookmarked cards
- **WHEN** a book with processing status `Processing` is rendered in a grid row alongside a `Ready` book or a bookmarked book
- **THEN** the processing ingestion indicator is bottom-anchored within the upper content container via `mt-auto pt-3`
- **AND** its bottom edge sits immediately above the card footer action divider consistent with adjacent cards.
