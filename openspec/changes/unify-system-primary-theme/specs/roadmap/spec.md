# Spec Delta

## MODIFIED Requirements

### Requirement: Curriculum Roadmap Progression & Macro View
The system SHALL provide an interactive curriculum overview endpoint and interactive timeline page at `/roadmap` allowing users to visualize curriculum progression across core technical modules (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`). The `/roadmap` page SHALL render this progression within a single, unified timeline container without competing view tabs, presenting module progress bars, completed node highlights, and drill score badges.

The 30-day curriculum track SHALL be explicitly designated in the user interface as the **"Starter Pack (Senior Fullstack Demo Track)"**, serving as seed onboarding material while clearly communicating that users can ingest arbitrary technical books and documentation via `/library` to generate custom personalized learning roadmaps.

#### Scenario: User queries curriculum roadmap progression
- **WHEN** user sends `GET /api/v1/curriculum/roadmap`
- **THEN** the system returns a structured response containing days grouped by technical module (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`) with each day's completion status, drill score, and active indicator for today.

#### Scenario: User navigates roadmap visual skill tree on frontend
- **WHEN** user visits `/roadmap`
- **THEN** the application renders an interactive skill tree displaying completed nodes in primary brand violet (`bg-brand-600` / `text-brand-400`), current day highlighted in gold, and upcoming nodes in locked state with overall module completion percentages.

#### Scenario: 30-day curriculum fallback
- **WHEN** user has no active document book pacer or explicitly selects the 30-day senior curriculum track in the track switcher dropdown
- **THEN** the application renders the 4 core curriculum technical modules labeled as the Starter Pack demo track (`FrontendWeb`, `BackendRuntime`, `DatabaseStorage`, `SystemDesign`), with clear call-to-action prompts to upload custom materials in `/library`.

---

### Requirement: Hierarchical Mindmap Interactive View
The system SHALL provide an interactive, client-side hierarchical tree mindmap visualization on `/roadmap` when `mindmap` view mode is active. The mindmap SHALL render the active book or curriculum as a root node, chapters or modules as intermediate expandable and collapsible branches, and slices or daily challenges as leaf nodes. Each leaf node SHALL visually indicate completion state (`completed`, `active_today`, `upcoming`) and provide a 1-click action bridge to `/today` or `/read/[bookId]`. The view SHALL provide controls for zoom in, zoom out, pan, fit to screen, and batch expand and collapse all branches, operating with zero backend API overhead. Completed chapter branch nodes, slice leaf nodes, check indicators, and connecting SVG edges SHALL strictly render in primary brand violet tokens (`stroke-brand-500`, `bg-brand-500`, `text-brand-500`, `border-brand-400/60`), completely eliminating disparate emerald green styling.

For documents with large chapter counts (greater than 12 chapters), the mindmap SHALL employ scalable layout heuristics including windowed root node anchoring, smart single-chapter auto-accordion expansion, and default viewport centering focused directly on the user's active chapter and today's slice at 100% scale (`scale = 1.0`). The canvas SHALL provide an in-toolbar search input that dynamically highlights matching nodes and auto-expands relevant branches without triggering full-document layout blowout.

Canvas panning and dragging interactions SHALL capture mouse and touch events globally on `window` upon pointerdown to prevent sticky dragging cursor states when moving outside the container boundary, suppress native drag-selection hitches via `preventDefault()`, and disable visual CSS transitions during active drag for zero-latency 1:1 pointer tracking.

The mindmap viewport and controls SHALL strictly employ the Dev-Learning Studio design language: the canvas container SHALL utilize neutral obsidian background `dark:bg-canvas` with translucent hairline borders `dark:border-white/[0.08]`; floating search and toolbar controls SHALL render as glassmorphic panels (`.glass-panel`); chapter branch and slice leaf cards SHALL utilize `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated` with translucent hairline borders, eliminating all legacy `dark:bg-slate-900`, `dark:bg-slate-950`, and `dark:border-slate-800` styling.

#### Scenario: Mindmap tree rendering from active track
- **GIVEN** an active book pacer or curriculum track is loaded
- **WHEN** user activates the `mindmap` view
- **THEN** the canvas renders a root node containing the track title and completion badge, connecting via visual bezier curve edges to chapter branch nodes, which branch into individual slice leaf nodes.

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
- **THEN** the application navigates to `/today?bookId={bookId}&chunkOrder={chunkOrder}` (or `/today?day={dayOrder}` for curriculum track)
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

---

### Requirement: Continuous Milestone Timeline Spine & Active Telemetry
The `/roadmap` timeline view SHALL render an illuminated continuous vertical timeline spine connecting sequential milestones (chapters, days, and slices). The spine SHALL visually connect module and chapter milestone cards to daily slice nodes with subtle progress gradients and connector indicators. Completed chapter nodes, completed day milestones, completed slice cards, and progress bar fills SHALL strictly render in primary brand violet tokens (`bg-brand-600`, `text-brand-400`, `border-brand-500/30`, `from-brand-600 to-brand-400`), completely eliminating emerald green styling.

Today's active learning milestone (active day or active chunk slice) SHALL be visually accented along the spine with an active telemetry treatment including an amber flame or electric violet pulsing ring, glowing status pill, and a direct 1-click launch button to start or resume today's session.

All timeline milestone surfaces, accordion cards, slice items, progress tracks, and status badges SHALL strictly employ the Dev-Learning Studio design language (`dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, `dark:border-white/[0.08]`), eliminating all legacy `dark:bg-slate-800`, `dark:bg-slate-900`, and `dark:border-slate-700` styling.

#### Scenario: Visual connector spine rendering
- **WHEN** user views the `/roadmap` page in `timeline` view mode
- **THEN** chapter headers and daily slice nodes are vertically interconnected by a continuous visual connector spine indicating sequential progression.

#### Scenario: Completed timeline milestones render with primary brand violet
- **WHEN** a user inspects completed chapters, days, or slices on the `/roadmap` timeline
- **THEN** the completed node circles render with `bg-brand-600 text-white`, completed badges render with primary brand violet accents, and milestone progress bars render with primary violet gradients rather than emerald green.

#### Scenario: Active milestone node telemetry
- **WHEN** an active day or active document slice exists for today
- **THEN** that milestone node displays an illuminated pulsing ring, prominent active badge, and quick action button to start or resume learning without manual searching.

#### Scenario: Modernized studio surface styling
- **WHEN** milestone cards and chapter accordions are rendered in dark mode
- **THEN** they utilize elevated canvas tokens (`bg-canvas-subtle`, `bg-canvas-elevated`) and hairline borders (`border-white/[0.08]`) consistent with the core studio design system.
