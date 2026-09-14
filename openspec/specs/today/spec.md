# today Specification

## Purpose
TBD - created by archiving change all-in-one-quiz-and-lazy-prefetch. Update Purpose after archive.

## Requirements

### Requirement: All-in-One Generation on Today Page
When a user opens `/today` on an uncurated slice, the system SHALL curate both the reading context and the Senior Scenario Drill simultaneously in a single AI invocation.

#### Scenario: User visits /today on an uncurated slice
- **GIVEN** a book pacer set to an uncurated slice (e.g. Slice 4)
- **WHEN** the user opens `/today`
- **THEN** a loading indicator indicates that AI is preparing today's focus and challenge
- **AND** both the formatted reading markdown and the Senior Scenario Drill populate simultaneously upon completion.

### Requirement: Next-Day Prefetching on Today Page
When the user is active on `/today` viewing Slice $N$, the system SHALL trigger a background prefetch for Slice $N+1$.

#### Scenario: Silent prefetch for tomorrow's reading
- **GIVEN** the user is viewing Slice 4 on `/today`
- **AND** Slice 5 is not yet AI-curated
- **WHEN** the `/today` page finishes mounting
- **THEN** an asynchronous background request curates Slice 5
- **AND** navigation to Slice 5 on the following day renders instantly with zero wait.

### Requirement: Centered Padded Loading Banner on Mobile
The `/today` focus loading state SHALL have at least 24px (`p-6`) padding, centered alignment, and a constrained width (`max-w-sm sm:max-w-md`) on mobile viewports.

#### Scenario: User visits /today on mobile during synthesis
- **GIVEN** a user on a mobile viewport (<640px wide) opens `/today`
- **WHEN** the daily focus or scenario challenge is loading
- **THEN** the loading icon and explanatory text are vertically and horizontally centered with comfortable margins
- **AND** the explanatory text does not collide with or touch the device edges.
