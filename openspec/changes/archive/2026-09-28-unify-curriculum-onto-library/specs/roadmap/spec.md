## ADDED Requirements

### Requirement: Library Book Roadmap Progression & Macro View
The system SHALL provide an interactive timeline page at `/roadmap` allowing users to visualize their reading progression through the active or selected `DocumentBook` and its ordered `DocumentChunk` slices. Progress SHALL be derived entirely from the user's library book, its chunks, and the associated `UserBookPacer`, with no fixed day count and no hardcoded pillar modules. The `/roadmap` page SHALL render this progression within a single, unified timeline container without competing view tabs, presenting chapter progress bars, completed slice highlights, and drill score badges.

New accounts start with an EMPTY library; when the user has ingested no books, the roadmap SHALL present an encouraging empty state directing the user to `/library` to import technical books and documentation, rather than falling back to any pre-built demo track.

The `/roadmap` page SHALL inherit exactly one mandatory system layout archetype (`StudioLayout`, `MasterDetailLayout`, `BentoDashboardLayout`, or `BoardLayout`) and SHALL NOT wrap its content in an unconstrained `max-w-* mx-auto` container, so the page fills the desktop viewport without an empty background void at 1080p.

#### Scenario: User views roadmap progression for the active book
- **WHEN** user visits `/roadmap` with an active `DocumentBook` and `UserBookPacer`
- **THEN** the system derives progression from the book's ordered `DocumentChunk` slices and the pacer's completion state, presenting chapter progress bars, completed slice highlights, and drill score badges without invoking any curriculum roadmap endpoint.

#### Scenario: Roadmap visual skill tree renders on the frontend
- **WHEN** user visits `/roadmap`
- **THEN** the application renders an interactive skill tree displaying completed slice nodes in primary brand violet (`bg-brand-600` / `text-brand-400`), the current active slice highlighted in gold, and upcoming slices in locked state with overall book completion percentages.

#### Scenario: Empty library shows an encouraging import prompt
- **WHEN** user has ingested no `DocumentBook` and has no active `UserBookPacer`
- **THEN** the application renders an encouraging empty state with a clear call-to-action to upload custom materials in `/library`, rather than rendering any pre-built demo track.

#### Scenario: Roadmap page inherits a mandatory layout archetype without desktop void
- **WHEN** the `/roadmap` page renders on a 1080p desktop viewport
- **THEN** the page content is hosted inside one of the mandatory system layout archetypes (`StudioLayout`, `MasterDetailLayout`, `BentoDashboardLayout`, or `BoardLayout`)
- **AND** the page does not use an unconstrained `max-w-* mx-auto` wrapper, leaving no empty background void beside the content in either the timeline or mindmap view.

## REMOVED Requirements

### Requirement: Curriculum Roadmap Progression & Macro View
**Reason**: TechDaily becomes pure bring-your-own-docs; the fixed 30-day senior curriculum track, its `GET /api/v1/curriculum/roadmap` endpoint, and the "Starter Pack (Senior Fullstack Demo Track)" modules are retired.
**Migration**: Replaced by "Requirement: Library Book Roadmap Progression & Macro View" — roadmap progression derives from the active `DocumentBook`'s ordered `DocumentChunk` slices and `UserBookPacer`; users import materials via `/library`.

## MODIFIED Requirements

### Requirement: Past Day Drill Review
The system SHALL allow users to click any unlocked or completed reading slice (`DocumentChunk`) of the active book on the roadmap to inspect its reading material and review scenario solutions.

#### Scenario: User clicks unlocked past day node
- **WHEN** user clicks on an unlocked or completed reading slice on `/roadmap`
- **THEN** the application navigates to that slice's focus view in review mode, allowing the user to re-read the material and view the scenario challenge explanation.

### Requirement: Hierarchical Mindmap Interactive View
The system SHALL provide an interactive, client-side hierarchical tree mindmap visualization on `/roadmap` when `mindmap` view mode is active. The mindmap SHALL render the active `DocumentBook` as the root node, chapters or chunk groups as intermediate expandable and collapsible branches, and `DocumentChunk` slices as leaf nodes. Each leaf node SHALL visually indicate completion state (`completed`, `active_today`, `upcoming`) and provide a 1-click action bridge to `/today?bookId={bookId}&chunkOrder={chunkOrder}` or `/read/[bookId]`. The view SHALL provide controls for zoom in, zoom out, pan, fit to screen, and batch expand and collapse all branches, operating with zero backend API overhead. Completed chapter branch nodes, slice leaf nodes, check indicators, and connecting SVG edges SHALL strictly render in primary brand violet tokens (`stroke-brand-500 dark:stroke-brand-400`, `bg-brand-600 text-white`, `border-brand-400/60`, `text-brand-500`), completely replacing disparate emerald green styling.

For documents with large chapter counts (greater than 12 chapters), the mindmap SHALL employ scalable layout heuristics including windowed root node anchoring, smart single-chapter auto-accordion expansion, and default viewport centering focused directly on the user's active chapter and today's slice at 100% scale (`scale = 1.0`). The canvas SHALL provide an in-toolbar search input that dynamically highlights matching nodes and auto-expands relevant branches without triggering full-document layout blowout.

Canvas panning and dragging interactions SHALL capture mouse and touch events globally on `window` upon pointerdown to prevent sticky dragging cursor states when moving outside the container boundary, suppress native drag-selection hitches via `preventDefault()`, and disable visual CSS transitions during active drag for zero-latency 1:1 pointer tracking.

The mindmap viewport and controls SHALL strictly employ the Dev-Learning Studio design language: the canvas container SHALL utilize neutral obsidian background `dark:bg-canvas` with translucent hairline borders `dark:border-white/[0.08]`; floating search bar and control toolbar render with translucent backdrop-blur `.glass-panel` styling; chapter branch and slice leaf cards SHALL utilize `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated` with translucent hairline borders, eliminating all legacy `dark:bg-slate-900`, `dark:bg-slate-950`, and `dark:border-slate-800` styling.

Chapter branch node index indicators SHALL strictly be sequential integer numbers (`1`, `2`, `3`, `4`, ...) representing chapter order, and SHALL NOT display category string identifiers or concatenated strings. The 32px chapter index badge (`w-8 h-8 rounded-xl`) SHALL hold only the numeric chapter index without text overflow, ensuring zero visual overlap with the chapter title or slice completion counts across all screen densities.

#### Scenario: Mindmap tree rendering from active track
- **GIVEN** an active `DocumentBook` pacer is loaded
- **WHEN** user activates the `mindmap` view
- **THEN** the canvas renders a root node containing the book title and completion badge, connecting via visual bezier curve edges to chapter branch nodes, which branch into individual `DocumentChunk` slice leaf nodes.

#### Scenario: Completed nodes render with primary brand violet styling
- **GIVEN** a chapter or slice has been completed by the user
- **WHEN** the user inspects the mindmap canvas
- **THEN** completed slice leaf cards, chapter branch badges, check icons, and connected bezier edges render with primary brand violet styling (`stroke-brand-500`, `bg-brand-500/20`, `text-brand-500`, `border-brand-400/60`) instead of emerald green.

#### Scenario: Large document chapter grouping and de-fragmentation
- **GIVEN** an active book contains more than 15 document chunks whose titles lack explicit delimiter prefixes (such as colons or hyphens)
- **WHEN** the roadmap transforms chunks into chapter milestones
- **THEN** the system groups adjacent standalone chunks into cohesive module chapters of 3 to 6 slices each rather than creating dozens of single-slice chapters
- **AND** chapters with existing explicit prefix categories remain cleanly grouped under their respective module titles.

#### Scenario: Windowed root node anchoring
- **GIVEN** a document contains more than 10 chapters resulting in a vertical layout height exceeding 1,200 pixels
- **WHEN** the mindmap tree layout coordinates are calculated
- **THEN** the root node's vertical position (`rootY`) SHALL NOT be placed at the naive global midpoint of all chapters
- **AND** the root node's vertical position SHALL be dynamically anchored relative to the active chapter's vertical position, keeping the root node in comfortable visual proximity to the currently focused study section.

#### Scenario: Expanding and collapsing mindmap chapter branches
- **GIVEN** a chapter branch node is expanded with visible slice leaves
- **WHEN** user clicks the chapter branch node's collapse/expand toggle
- **THEN** the child slice leaf nodes collapse into the chapter node, updating branch layout coordinates dynamically and adjusting the canvas view.

#### Scenario: Smart initial viewport and 1-click active focus
- **GIVEN** an active learning track has an in-progress or active-today slice
- **WHEN** the user mounts or switches to the mindmap view
- **THEN** the canvas viewport SHALL automatically pan and center directly on the active chapter and today's designated slice at 1.0x scale (100% zoom) rather than downscaling the entire document to illegible micro-text
- **WHEN** user clicks the `Focus Active` (🎯) button in the floating toolbar
- **THEN** the canvas smoothly re-centers the viewport on today's active slice at 1.0x scale.

#### Scenario: Auto-accordion mode for large documents
- **GIVEN** a document has more than 12 chapters and the mindmap view is active
- **WHEN** the user clicks to expand a collapsed chapter branch
- **THEN** previously expanded chapter branches automatically collapse (auto-accordion mode) unless user explicitly clicks `Expand All`
- **AND** the canvas recalculates the bounding box to maintain a compact, high-performance canvas height between 800px and 1,600px.

#### Scenario: In-canvas mindmap search and branch highlighting
- **GIVEN** the mindmap canvas is active
- **WHEN** the user enters a query (e.g. "kestrel", "middleware", "blazor") into the in-canvas search input
- **THEN** all chapter branches containing matching slices or matching chapter titles automatically expand
- **AND** matching slice leaf nodes and chapter headers are highlighted with prominent accent borders
- **AND** non-matching nodes and connecting edges are rendered with reduced opacity (0.25)
- **WHEN** the user clears the search input
- **THEN** all nodes return to standard opacity and the previous expansion state is restored.

#### Scenario: Smooth window-level pan capture and cursor release guarantee
- **GIVEN** the user initiates a drag-to-pan gesture on the canvas background
- **WHEN** the mouse button is pressed down (`mousedown`)
- **THEN** native browser drag-selection is prevented, the cursor transitions to `cursor-grabbing`, and pointer tracking listeners are attached to `window`
- **WHEN** the user drags the mouse outside the canvas boundary and releases the mouse button anywhere on the screen
- **THEN** the `mouseup` event on `window` successfully fires, `isPanning` resets to `false`, and the cursor immediately returns to `cursor-grab` without requiring any subsequent corrective click
- **AND** during the active drag, SVG edges and transform layers disable CSS transitions for jitter-free 1:1 movement.

#### Scenario: 1-click bridge action from slice leaf node
- **GIVEN** a slice leaf node is rendered on the mindmap
- **WHEN** user clicks an active-today slice leaf node
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}`
- **WHEN** user clicks a completed or upcoming slice leaf node
- **THEN** the application displays a node preview modal or navigates directly to `/read/{bookId}?slice={chunkOrder}`.

#### Scenario: Canvas viewport manipulation and fit to screen
- **GIVEN** the mindmap canvas is rendered
- **WHEN** user interacts with zoom buttons or mouse wheel
- **THEN** the canvas smoothly scales the tree between 0.15x and 2.0x zoom
- **WHEN** user clicks `Fit to Screen`
- **THEN** the canvas recalculates the bounding box of all visible nodes and animates the pan and zoom scale to center the entire tree within the viewport, clamping the minimum scale to 0.15x for extreme layouts.

#### Scenario: Batch expand and collapse all branches
- **GIVEN** the mindmap view is active with mixed branch states
- **WHEN** user clicks `Expand All` or `Collapse All` in the mindmap control toolbar
- **THEN** all chapter branch nodes simultaneously expand or collapse their child slice leaves and the tree layout is recalculated dynamically.

#### Scenario: Zero VPS overhead invariant
- **WHEN** user interacts with the mindmap (toggling branches, panning, zooming)
- **THEN** all layout calculations and rendering execute 100% in the client browser without issuing backend network requests.

#### Scenario: Obsidian studio canvas surface and glassmorphic control styling
- **WHEN** the mindmap view is mounted in dark mode
- **THEN** the canvas viewport renders on neutral obsidian `dark:bg-canvas` with translucent hairline borders `dark:border-white/[0.08]`
- **AND** floating search bar and control toolbar render with translucent backdrop-blur `.glass-panel` styling
- **AND** chapter branch cards and slice leaf cards render on `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated`, with active nodes illuminated by amber and violet accent rings.

#### Scenario: User opens mindmap view for multi-chapter document
- **WHEN** user toggles to the Mindmap View on `/roadmap`
- **THEN** all chapter branch nodes display numeric badges (`1`, `2`, `3`, ...) cleanly separated from chapter titles
- **AND** no category strings (such as `FrontendWeb` or `BackendRuntime`) overflow or overlap with node text.

### Requirement: Active Track Synchronization & Switcher
The `/roadmap` page SHALL present an interactive roadmap progression view that operates in a document-first paradigm, prioritizing the user's active learning materials from `/today` (Active Book Pacer). The page SHALL provide a unified header containing an integrated Track Switcher dropdown that automatically selects the user's currently active document book (`focusStore.data.pacer.bookId` or first item in `availableBookTracks`). The dropdown popover SHALL list only the user's library `DocumentBook` entries — in-progress and available — with completion counts and percentage bars, and provide a direct "+ Khám phá Thư Viện" link to `/library`. When the user has zero reading books in progress, the roadmap SHALL present an encouraging empty state with a 1-click CTA leading to `/library`.

The Track Switcher dropdown popover SHALL render fully without box-model clipping or truncation by parent containers, cleanly floating above subsequent page controls, view switchers, and in-canvas mindmap search and toolbar elements, ensuring 100% visibility of all in-progress books and library action links.

#### Scenario: Active book pacer on /today reflected on /roadmap
- **GIVEN** an authenticated user has an active document book pacer configured on `/today`
- **WHEN** user navigates to `/roadmap`
- **THEN** the roadmap page automatically displays the active book's title, chapter milestones, slice progress, and estimated slices remaining in the primary header without showing dual view tabs.

#### Scenario: Switching track via dropdown
- **WHEN** user clicks the Track Switcher dropdown button in the `/roadmap` header
- **THEN** the application displays a popover menu listing only the user's library books — the active track with an `Active` badge, other in-progress library books with progress bars, and a link to browse `/library`
- **WHEN** user selects an alternative document book from the dropdown
- **THEN** the roadmap view immediately transitions to display the selected book's chapter milestones and slices, and synchronizes the active book pacer on `/today`.

#### Scenario: Unclipped dropdown popover rendering
- **GIVEN** an authenticated user is on `/roadmap` with an active learning track
- **WHEN** the user clicks the Track Switcher dropdown button to open the track popover menu
- **THEN** the dropdown popover menu SHALL NOT be clipped by the header banner's boundary or overflow constraints
- **AND** all in-progress library book tracks and the "+ Browse Library" action link SHALL be fully visible and interactable above subsequent page sections.

#### Scenario: Dropdown popover renders above mindmap search bar without overlap
- **GIVEN** an authenticated user is on `/roadmap` with the `mindmap` view active
- **WHEN** the user clicks the Track Switcher dropdown button to open the track popover menu
- **THEN** the dropdown popover menu SHALL float strictly on top of the mindmap canvas and in-canvas search bar
- **AND** the in-canvas search bar SHALL NOT overlap, slice through, or obscure any track options in the open dropdown.

#### Scenario: Empty state guidance when no active book exists
- **WHEN** a user with no active book pacer and no in-progress library books visits `/roadmap`
- **THEN** the roadmap view renders an encouraging empty state prompting the user to explore `/library` and begin reading a book, rather than falling back to any pre-built demo track.

#### Scenario: 1-click bridge actions
- **WHEN** user clicks `Start Today's Drill` on the active slice card or active chapter header
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}`
- **WHEN** user clicks a completed slice card
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` for scenario challenge review or `/read/{bookId}?slice={chunkOrder}` for full-text reading.

### Requirement: Continuous Milestone Timeline Spine & Active Telemetry
The `/roadmap` timeline view SHALL render an illuminated continuous vertical timeline spine connecting the active book's sequential milestones (chapters and `DocumentChunk` slices). The spine SHALL visually connect chapter milestone cards to slice nodes with subtle progress gradients and connector indicators. Completed chapter nodes, completed slice cards, and progress bar fills SHALL strictly render in primary brand violet tokens (`bg-brand-600`, `text-brand-400`, `border-brand-500/30`, `from-brand-600 to-brand-400`), completely eliminating emerald green styling.

Today's active learning milestone (active chunk slice) SHALL be visually accented along the spine with an active telemetry treatment including an amber flame or electric violet pulsing ring, glowing status pill, and a direct 1-click launch button to start or resume today's session.

All timeline milestone surfaces, accordion cards, slice items, progress tracks, and status badges SHALL strictly employ the Dev-Learning Studio design language (`dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, `dark:border-white/[0.08]`), eliminating all legacy `dark:bg-slate-800`, `dark:bg-slate-900`, and `dark:border-slate-700` styling.

#### Scenario: Visual connector spine rendering
- **WHEN** user views the `/roadmap` page in `timeline` view mode
- **THEN** chapter headers and slice nodes are vertically interconnected by a continuous visual connector spine indicating sequential progression.

#### Scenario: Completed timeline milestones render with primary brand violet
- **WHEN** a user inspects completed chapters or slices on the `/roadmap` timeline
- **THEN** the completed node circles render with `bg-brand-600 text-white`, completed badges render with primary brand violet accents, and milestone progress bars render with primary violet gradients rather than emerald green.

#### Scenario: Active milestone node telemetry
- **WHEN** an active document slice exists for today
- **THEN** that milestone node displays an illuminated pulsing ring, prominent active badge, and quick action button to start or resume learning without manual searching.

#### Scenario: Modernized studio surface styling
- **WHEN** milestone cards and chapter accordions are rendered in dark mode
- **THEN** they utilize elevated canvas tokens (`bg-canvas-subtle`, `bg-canvas-elevated`) and hairline borders (`border-white/[0.08]`) consistent with the core studio design system.

