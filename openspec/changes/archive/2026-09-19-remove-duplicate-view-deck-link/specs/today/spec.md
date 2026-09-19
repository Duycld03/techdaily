# Spec Delta: Today

## ADDED Requirements

### Requirement: Concentric Metric Card Review Deck Navigation
The concentric metric card (`ConcentricMetricCard.vue`) SHALL provide spaced repetition review deck navigation exclusively through its bottom action row banner. The card header SHALL NOT display duplicate review deck navigation links, maintaining a focused header layout consisting of the icon, title, and subtitle.

#### Scenario: User views concentric metric card with zero cards due
- **WHEN** user views the concentric metric card and `dueCards === 0`
- **THEN** the card header does NOT render any "View Deck" / "Xem Bộ Thẻ" link
- **AND** the bottom action banner renders a single button displaying `dashboard.view_deck` ("Xem Bộ Thẻ ↗") linking to `/review`.

#### Scenario: User views concentric metric card with cards due
- **WHEN** user views the concentric metric card and `dueCards > 0`
- **THEN** the card header does NOT render any "View Deck" / "Xem Bộ Thẻ" link
- **AND** the bottom action banner renders a primary brand button displaying `dashboard.review_now` ("Ôn Luyện Ngay (Space) ↗") linking to `/review`.
