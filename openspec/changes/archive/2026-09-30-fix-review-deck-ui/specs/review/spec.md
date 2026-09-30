# Spec Delta

## ADDED Requirements

### Requirement: Deck Management Tab Persistence Across Pagination
The `/review` page SHALL keep the user on the Deck Management tab ("Deck Management" / "Kho thẻ của tôi") when navigating between deck pages. The active tab SHALL be encoded in the URL so that any navigation that recreates the page — including a full remount triggered by a `route.fullPath` change from a pagination query update — restores the Deck Management tab rather than defaulting to the Review Session tab.

#### Scenario: Paginating the deck keeps the Deck Management tab active
- **GIVEN** the user is on the Deck Management tab with more than one page of cards
- **WHEN** the user clicks "Next", "Previous", or a numbered page button
- **THEN** the requested page renders within the Deck Management tab
- **AND** the view does NOT switch to the Review Session tab.

#### Scenario: Reloading a deck page URL restores the Deck Management tab
- **GIVEN** a URL that encodes the Deck Management tab and a page index (e.g. `?tab=management&page=3`)
- **WHEN** the user loads or reloads that URL
- **THEN** the page opens on the Deck Management tab showing the requested page.

### Requirement: Deck Management Pagination Page-Size Isolation
The Deck Management card list SHALL always paginate at its own standard configured page size. A lightweight count or preview fetch performed elsewhere in the application (for example, a dashboard requesting a single card only to read the total count) SHALL NOT alter the page size used by the Deck Management list. When the user opens the Deck Management tab, the list SHALL request its standard page size regardless of any prior fetch.

#### Scenario: Deck list ignores a prior single-card count probe
- **GIVEN** another view has already fetched review cards with a page size of 1 to read the total count
- **WHEN** the user opens the Deck Management tab
- **THEN** the deck list requests its standard page size (more than one card per page)
- **AND** renders multiple cards per page with a pagination bar whose page count reflects the standard page size, not one card per page.

## MODIFIED Requirements

### Requirement: Deck Management Retention Analytics Panel

The `/review` Deck Management tab SHALL surface the retention analytics from `GET /api/v1/review/analytics` alongside the existing `MasteryGaugeCard` and `ReviewForecastChart`, without duplicating those components elsewhere. The panel SHALL present:

1. **At-Risk & Leech Card**: displays `overdueCount`, `leechCount`, and `atRiskCount`, and provides a 1-click action that opens the Deck Management card list ordered by soonest due date so overdue and low-ease at-risk cards surface first, letting the user act on them immediately.
2. **Source-Channel Retention Card**: compares retention across the three recall sources (reader highlights, quiz mistakes, daily drills), showing per-source total, maturity split, and average ease factor so the user can see which channel retains best. Each source present in the deck SHALL render with its own distinct localized label derived from its `CardSourceType` (`Highlight`, `QuizMistake`, `DocumentChunk` each mapped to a separate string). No two source rows SHALL display the same label, and a recognized source type MUST NOT fall back to a shared generic label.
3. **Ease-Factor Distribution Card**: visualizes the difficulty distribution from the analytics response across the bounded SM-2 `[1.30, 2.50]` range — `strugglingCount` (`[1.30, 1.70]`), `developingCount` (`(1.70, 2.10)`), and `comfortableCount` (`[2.10, 2.50]`) — so the user can see how the deck's ease factors are spread, reusing values already returned by `GET /api/v1/review/analytics` without new API surface.

The three analytics cards SHALL lay out in a three-column row on large viewports (`lg` and above), eliminating the wide empty gaps of a two-card row, and SHALL stack into a single column on mobile viewports without horizontal scrolling.

All labels, counts, and action controls SHALL be fully localized in English (`en`) and Vietnamese (`vi`), and action controls SHALL use `whitespace-nowrap shrink-0` to prevent text wrapping collisions across locales.

#### Scenario: User views retention analytics in Deck Management
- **GIVEN** an authenticated user with graded cards across multiple sources
- **WHEN** the user opens the Deck Management tab on `/review`
- **THEN** the At-Risk & Leech card, Source-Channel Retention card, and Ease-Factor Distribution card render with data from `GET /api/v1/review/analytics`
- **AND** the existing Mastery Gauge and Review Forecast remain the sole instances of those components.

#### Scenario: User jumps from the at-risk card to the deck list
- **GIVEN** the At-Risk & Leech card reports a non-zero `atRiskCount`
- **WHEN** the user activates its review action
- **THEN** the Deck Management card list opens ordered by soonest due date so overdue at-risk cards surface first, without a full page reload.

#### Scenario: Source-channel rows render distinct labels
- **GIVEN** a deck with cards from more than one recall source (e.g. reader highlights and daily drills)
- **WHEN** the user views the Source-Channel Retention card
- **THEN** each source row shows a distinct localized label matching its `CardSourceType`
- **AND** no two rows display the same label.

#### Scenario: Ease-factor distribution card renders in a three-column analytics row
- **GIVEN** an authenticated user with graded cards
- **WHEN** the user opens the Deck Management tab on a large viewport (`\ge 1024\text{px}`)
- **THEN** the analytics row shows the At-Risk & Leech, Source-Channel Retention, and Ease-Factor Distribution cards in a three-column layout with no oversized empty gaps
- **AND** the Ease-Factor Distribution card reflects the `strugglingCount`, `developingCount`, and `comfortableCount` from the analytics response.

#### Scenario: Retention analytics render in both locales
- **WHEN** the user toggles the application locale between `en` and `vi` on the Deck Management tab
- **THEN** all retention card titles, metric labels, and action controls update reactively and render without truncation or layout overflow in either locale.
