## MODIFIED Requirements

### Requirement: Active Track Synchronization & Switcher
The `/roadmap` page SHALL present a single, cohesive timeline view directly synchronized with the user's active learning track on `/today` (Active Book Pacer or 30-Day Senior Curriculum). The page SHALL provide a unified header containing an integrated Track Switcher dropdown that displays the active track, allows switching between in-progress library books, viewing the 30-day curriculum, and linking directly to `/library`. The page SHALL render chapter milestones and slices with search filtering and provide direct 1-click action bridges to `/today` and `/read/[bookId]`.

The Track Switcher dropdown popover SHALL render fully without box-model clipping or truncation by parent containers, cleanly floating above subsequent page controls and ensuring 100% visibility of all in-progress books, curriculum options, and library action links.

#### Scenario: Active book pacer on /today reflected on /roadmap
- **GIVEN** an authenticated user has an active document book pacer configured on `/today`
- **WHEN** user navigates to `/roadmap`
- **THEN** the roadmap page automatically displays the active book's title, chapter milestones, slice progress, and estimated days remaining in the primary header without showing dual view tabs.

#### Scenario: Switching track via dropdown
- **WHEN** user clicks the Track Switcher dropdown button in the `/roadmap` header
- **THEN** the application displays a popover menu listing the active track with an `Active` badge, other in-progress library books with progress bars, the 30-Day Senior Curriculum track option, and a link to browse `/library`
- **WHEN** user selects an alternative document book from the dropdown
- **THEN** the roadmap view immediately transitions to display the selected book's chapter milestones and slices, and synchronizes the active book pacer on `/today`.

#### Scenario: Unclipped dropdown popover rendering
- **GIVEN** an authenticated user is on `/roadmap` with an active learning track
- **WHEN** the user clicks the Track Switcher dropdown button to open the track popover menu
- **THEN** the dropdown popover menu SHALL NOT be clipped by the header banner's boundary or overflow constraints
- **AND** all in-progress book tracks, the 30-Day Curriculum option, and the "+ Browse Library" action link SHALL be fully visible and interactable above subsequent page sections.

#### Scenario: 30-day curriculum fallback
- **WHEN** a user with no active book pacer visits `/roadmap` or selects the 30-Day Senior Curriculum track from the dropdown
- **THEN** the roadmap view smoothly transitions to display the 30-day curriculum modules with day nodes and completion percentages while preserving the unified header and track switcher.

#### Scenario: 1-click bridge actions
- **WHEN** user clicks `Start Today's Drill` on the active slice card or active chapter header
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` (or `/today?day={dayOrder}` for curriculum track)
- **WHEN** user clicks a completed slice card
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` for scenario challenge review or `/read/{bookId}?slice={chunkOrder}` for full-text reading.
