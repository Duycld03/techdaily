## MODIFIED Requirements

### Requirement: Hierarchical Mindmap Interactive View
The system SHALL provide an interactive, client-side hierarchical tree mindmap visualization on `/roadmap` when `mindmap` view mode is active. The mindmap SHALL render the active book or curriculum as a root node, chapters or modules as intermediate expandable and collapsible branches, and slices or daily challenges as leaf nodes. Each leaf node SHALL visually indicate completion state (`completed`, `active_today`, `upcoming`) and provide a 1-click action bridge to `/today` or `/read/[bookId]`. The view SHALL provide controls for zoom in, zoom out, pan, fit to screen, and batch expand and collapse all branches, operating with zero backend API overhead.

For documents with large chapter counts (greater than 12 chapters), the mindmap SHALL employ scalable layout heuristics including windowed root node anchoring, smart single-chapter auto-accordion expansion, and default viewport centering focused directly on the user's active chapter and today's slice at 100% scale (`scale = 1.0`). The canvas SHALL provide an in-toolbar search input that dynamically highlights matching nodes and auto-expands relevant branches without triggering full-document layout blowout.

Canvas panning and dragging interactions SHALL capture mouse and touch events globally on `window` upon pointerdown to prevent sticky dragging cursor states when moving outside the container boundary, suppress native drag-selection hitches via `preventDefault()`, and disable visual CSS transitions during active drag for zero-latency 1:1 pointer tracking.

#### Scenario: Mindmap tree rendering from active track
- **GIVEN** an active book pacer or curriculum track is loaded
- **WHEN** user activates the `mindmap` view
- **THEN** the canvas renders a root node containing the track title and completion badge, connecting via visual bezier curve edges to chapter branch nodes, which branch into individual slice leaf nodes.

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
