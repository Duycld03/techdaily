## MODIFIED Requirements

### Requirement: Curriculum Roadmap Progression & Macro View
The system SHALL provide an interactive 30-day curriculum overview endpoint and interactive timeline page at `/roadmap` allowing users to visualize full curriculum progression across all 4 technical modules (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`). The `/roadmap` page SHALL render this progression within a single, unified timeline container without competing view tabs, presenting module progress bars, completed node highlights, and drill score badges.

#### Scenario: User queries curriculum roadmap progression
- **WHEN** user sends `GET /api/v1/curriculum/roadmap`
- **THEN** the system returns a structured response containing all 30 days grouped by technical module (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`) with each day's completion status, drill score, and active indicator for today.

#### Scenario: User navigates roadmap visual skill tree on frontend
- **WHEN** user visits `/roadmap`
- **THEN** the application renders an interactive 30-day skill tree displaying completed nodes in green, current day highlighted in gold, and upcoming nodes in locked state with overall module completion percentages.

#### Scenario: 30-day curriculum fallback
- **WHEN** user has no active document book pacer or explicitly selects the 30-day senior curriculum track in the track switcher dropdown
- **THEN** the application renders the 4 core curriculum technical modules with daily challenge nodes, completion percentages, and past drill reviews within the unified layout without dual view tabs.


## ADDED Requirements

### Requirement: Active Track Synchronization & Switcher
The `/roadmap` page SHALL present a single, cohesive timeline view directly synchronized with the user's active learning track on `/today` (Active Book Pacer or 30-Day Senior Curriculum). The page SHALL provide a unified header containing an integrated Track Switcher dropdown that displays the active track, allows switching between in-progress library books, viewing the 30-day curriculum, and linking directly to `/library`. The page SHALL render chapter milestones and slices with search filtering and provide direct 1-click action bridges to `/today` and `/read/[bookId]`.

#### Scenario: Active book pacer on /today reflected on /roadmap
- **GIVEN** an authenticated user has an active document book pacer configured on `/today`
- **WHEN** user navigates to `/roadmap`
- **THEN** the roadmap page automatically displays the active book's title, chapter milestones, slice progress, and estimated days remaining in the primary header without showing dual view tabs.

#### Scenario: Switching track via dropdown
- **WHEN** user clicks the Track Switcher dropdown button in the `/roadmap` header
- **THEN** the application displays a popover menu listing the active track with an `Active` badge, other in-progress library books with progress bars, the 30-Day Senior Curriculum track option, and a link to browse `/library`
- **WHEN** user selects an alternative document book from the dropdown
- **THEN** the roadmap view immediately transitions to display the selected book's chapter milestones and slices, and synchronizes the active book pacer on `/today`.

#### Scenario: 30-day curriculum fallback
- **WHEN** a user with no active book pacer visits `/roadmap` or selects the 30-Day Senior Curriculum track from the dropdown
- **THEN** the roadmap view smoothly transitions to display the 30-day curriculum modules with day nodes and completion percentages while preserving the unified header and track switcher.

#### Scenario: 1-click bridge actions
- **WHEN** user clicks `Start Today's Drill` on the active slice card or active chapter header
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` (or `/today?day={dayOrder}` for curriculum track)
- **WHEN** user clicks a completed slice card
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` for scenario challenge review or `/read/{bookId}?slice={chunkOrder}` for full-text reading.
