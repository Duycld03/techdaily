# Spec Delta

## MODIFIED Requirements

### Requirement: Consistent Book Card Status Badge Baseline Alignment
The library catalog on `/library` SHALL align the bookmark resume badge, ready badge, and in-progress ingestion status indicator to a consistent bottom baseline across all book cards.
1. The book card status area SHALL display the primary ingestion or resume badge (`Ready to Read`, `Resumes at Slice X`, or `Processing`).
2. The card SHALL NOT render duplicate status text (`● Sẵn sàng` / `● Ready to read`) immediately above the progress bar, reserving that row strictly for numeric slice counts and reading completion percentages.

#### Scenario: Book card with 1-line title aligns status badge with 2-line title card
- **WHEN** a book with a short single-line title is displayed next to a book with a two-line title
- **THEN** both cards' bottom-anchored status elements and action rows align horizontally across the row.

#### Scenario: Book card without author or source URL maintains bottom-aligned badge
- **WHEN** a book has no author or source URL metadata
- **THEN** the upper container flexes to fill available vertical space and the status badge remains anchored to the bottom baseline.

#### Scenario: In-progress ingestion indicator aligns with completed and bookmarked cards
- **WHEN** an ingesting book is displayed alongside completed or active reading books
- **THEN** all cards maintain identical outer heights and aligned action footers.

#### Scenario: Book card rendering for unread document
- **WHEN** an unread document card is displayed in the learner library
- **THEN** it renders the top status badge and slice count without duplicating "Ready to read" or "Sẵn sàng" above the progress bar.
