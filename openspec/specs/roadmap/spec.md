# Roadmap Specification

## Purpose
Provides an interactive 30-day senior curriculum roadmap visualization displaying module progression, daily status indicators, and past day drill review.

## Requirements

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

### Requirement: Past Day Drill Review
The system SHALL allow users to click any unlocked or completed day node on the roadmap to inspect its reading material and review scenario solutions.

#### Scenario: User clicks unlocked past day node
- **WHEN** user clicks on an unlocked day on `/roadmap`
- **THEN** the application navigates to that day's focus view in review mode, allowing the user to re-read the material and view the scenario challenge explanation.

### Requirement: Roadmap Dual-View Switcher
The system SHALL provide a dual-view switcher on the `/roadmap` page allowing users to toggle between a linear milestone timeline view (`timeline`) and an interactive hierarchical tree mindmap view (`mindmap`). The active view mode SHALL be stored in `localStorage` under key `techdaily_roadmap_view_mode` and restored upon subsequent page visits, defaulting to `timeline` when no prior preference exists. All switcher buttons and badges SHALL enforce `whitespace-nowrap shrink-0` to prevent text wrapping across both English and Vietnamese locales.

#### Scenario: User toggles between timeline and mindmap views
- **GIVEN** a user is on `/roadmap` in the default `timeline` view
- **WHEN** user clicks the `Mindmap View` button on the dual-view switcher
- **THEN** the timeline milestone container is replaced by the interactive hierarchical mindmap canvas without reloading the page or altering the roadmap header banner.

#### Scenario: View mode preference preserved in localStorage
- **GIVEN** a user switches the roadmap view mode to `mindmap`
- **WHEN** the user reloads the page or navigates away and returns to `/roadmap`
- **THEN** the application reads `techdaily_roadmap_view_mode` from `localStorage` and automatically renders the `mindmap` view as the active view.

#### Scenario: Responsive switch buttons layout
- **WHEN** user views the roadmap dual-view switcher on any viewport size
- **THEN** both switch buttons enforce `whitespace-nowrap shrink-0` and responsive gap layout, preventing text truncation or wrapping across English and Vietnamese locales.

### Requirement: Hierarchical Mindmap Interactive View
The system SHALL provide an interactive, client-side hierarchical tree mindmap visualization on `/roadmap` when `mindmap` view mode is active. The mindmap SHALL render the active book or curriculum as a root node, chapters or modules as intermediate expandable and collapsible branches, and slices or daily challenges as leaf nodes. Each leaf node SHALL visually indicate completion state (`completed`, `active_today`, `upcoming`) and provide a 1-click action bridge to `/today` or `/read/[bookId]`. The view SHALL provide controls for zoom in, zoom out, pan, fit to screen, and batch expand and collapse all branches, operating with zero backend API overhead.

#### Scenario: Mindmap tree rendering from active track
- **GIVEN** an active book pacer or curriculum track is loaded
- **WHEN** user activates the `mindmap` view
- **THEN** the canvas renders a root node containing the track title and completion badge, connecting via visual bezier curve edges to chapter branch nodes, which branch into individual slice leaf nodes.

#### Scenario: Expanding and collapsing mindmap chapter branches
- **GIVEN** a chapter branch node is expanded with visible slice leaves
- **WHEN** user clicks the chapter branch node's collapse/expand toggle
- **THEN** the child slice leaf nodes collapse into the chapter node, updating branch layout coordinates dynamically and adjusting the canvas view.

#### Scenario: 1-click bridge action from slice leaf node
- **GIVEN** a slice leaf node is rendered on the mindmap
- **WHEN** user clicks an active-today slice leaf node
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` (or `/today?day={dayOrder}` for curriculum track)
- **WHEN** user clicks a completed or upcoming slice leaf node
- **THEN** the application displays a node preview modal or navigates directly to `/read/{bookId}?slice={chunkOrder}`.

#### Scenario: Canvas viewport manipulation and fit to screen
- **GIVEN** the mindmap canvas is rendered
- **WHEN** user interacts with zoom buttons or mouse wheel
- **THEN** the canvas smoothly scales the tree between 0.25x and 2.0x zoom
- **WHEN** user clicks `Fit to Screen`
- **THEN** the canvas recalculates the bounding box of all visible nodes and animates the pan and zoom scale to center the entire tree within the viewport.

#### Scenario: Batch expand and collapse all branches
- **GIVEN** the mindmap view is active with mixed branch states
- **WHEN** user clicks `Expand All` or `Collapse All` in the mindmap control toolbar
- **THEN** all chapter branch nodes simultaneously expand or collapse their child slice leaves and the tree layout is recalculated dynamically.

#### Scenario: Zero VPS overhead invariant
- **WHEN** user interacts with the mindmap (toggling branches, panning, zooming)
- **THEN** all layout calculations and rendering execute 100% in the client browser without issuing backend network requests.

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
