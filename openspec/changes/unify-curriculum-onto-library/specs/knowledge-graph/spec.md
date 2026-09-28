## MODIFIED Requirements

### Requirement: Knowledge Graph Relational Extraction API
The system SHALL expose a protected HTTP GET endpoint `GET /api/v1/graph` that extracts and returns an associative **personal** knowledge graph for the authenticated user, derived strictly from that user's own learning artifacts in relational PostgreSQL 17 without introducing auxiliary graph databases or continuous server-side vector calculations. The graph SHALL represent only knowledge the user has actually engaged with — books the user imported, the chunks within those books, the user's personal highlights, and the user's spaced-repetition cards — and SHALL NOT project content owned by other users.

The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and SHALL return `HTTP 401 Unauthorized` with RFC 7807 problem details when invoked without a valid token.

All node source queries SHALL honor the global soft-delete query filter (`IsDeleted == false`), so that any book, highlight, or flashcard the user has deleted is absent from the graph without special handling.

The returned response payload (`KnowledgeGraphResponse`) SHALL consist of:
1. `nodes`: An array of `GraphNodeDto` items representing:
   - **Card Nodes:** Spaced repetition flashcards from `SpacedRepetitionCards` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label`, `documentChunkId`, `status` (`Learning`, `Reviewing`, `Mastered`), `intervalDays`, `easeFactor`, and `repetitionCount`.
   - **Highlight Nodes:** Personal reading highlights from `UserHighlights` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label` (truncated quote), `documentChunkId`, `bookId`, `note`, `tags`, and `createdAt`.
   - **Book Nodes:** Only books the authenticated user imported — from `DocumentBooks` where `CreatedByUserId == currentUser.Id` and the book is published and not deleted — containing `id`, `label`, `category`, `totalChunks`, and `authorOrSourceUrl`. Books created by other accounts SHALL NOT appear (parity with the Library page `GET /api/v1/library/books`).
   - **Chunk Nodes:** Reading slices from `DocumentChunks` belonging to the user's imported books, containing `id`, `label` (chunk/chapter title), `bookId`, `chunkOrder`, and `summary`. A chunk SHALL be emitted if and only if it belongs to a surviving user book and is referenced by at least one of the user's own cards or highlights, avoiding a blind projection of every slice.
   - **Pillar Hub Nodes:** Canonical architectural pillar anchors (id pattern `pillar-{Category}` for the five pillars Frontend & Web, Backend & Runtime, Database & Storage, Distributed Systems, Engineering Craft), containing `id`, `label`, `category`, and `type: "pillar"`. A pillar hub SHALL be emitted only when it anchors at least one surviving user node — a user book or a user card of that category. Pillars with no user activity SHALL be omitted; the graph therefore contains between zero and five pillar hubs depending on the breadth of the user's learning.

2. `edges`: An array of `GraphEdgeDto` items connecting only nodes present in `nodes`:
   - `BookToPillar`: Connecting each user book to the pillar hub for the book's own category (`Book.Id -> pillar-{Category}`).
   - `ChunkToBook`: Connecting each emitted chunk to its parent book (`Chunk.Id -> Book.Id`).
   - `CardToChunk`: Connecting a user flashcard to its source `DocumentChunk` (`Card.DocumentChunkId -> Chunk.Id`) when that chunk is present.
   - `CardToHighlight`: Connecting a highlight-sourced flashcard (`SourceType = Highlight`, `SourceHighlightId != null`) to its source highlight.
   - `CardToPillar`: Connecting a flashcard with `DocumentChunkId == null` and `SourceHighlightId == null` to `pillar-{card.Category}`.
   - `HighlightToChunk`: Connecting a user highlight to its source `DocumentChunk` (`Highlight.DocumentChunkId -> Chunk.Id`) when that chunk is present.
   - `HighlightToBook`: Connecting a user highlight to its source book via `DocumentChunk.DocumentBookId` when that book is present.
   - `SharedTag`: Connecting user highlights that share one or more normalized tag keywords.

3. `stats`: Metadata containing `totalNodes`, `totalEdges`, `nodeTypeCounts` (including counts for `pillar`, `book`, `chunk`, `card`, `highlight`), and `pillarCounts`.

The query execution SHALL execute in a single consolidated read pass using EF Core `AsNoTracking()`, utilizing indexes on foreign keys (`UserId`, `DocumentChunkId`, `CreatedByUserId`) to keep database query time low on production PostgreSQL 17. Because the projection is scoped to a single user's artifacts, the uncompressed JSON payload SHALL remain well under 100 KB.

#### Scenario: Unauthenticated request to knowledge graph endpoint
- **WHEN** an unauthenticated client sends `GET /api/v1/graph` without a JWT Bearer token
- **THEN** the system returns `HTTP 401 Unauthorized` with RFC 7807 problem details.

#### Scenario: Authenticated user requests knowledge graph
- **WHEN** an authenticated user who has imported books and created highlights and flashcards sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the `nodes` array contains the user's own book nodes, the chunks referenced by the user's cards or highlights, and the user's card and highlight nodes
- **AND** the `edges` array links cards to chunks, highlights to books and chunks, and highlights sharing common tags
- **AND** the payload excludes books, cards, and highlights belonging to other users.

#### Scenario: New user with zero flashcards or highlights requests graph
- **WHEN** an authenticated user who has not imported any book and has no highlights or flashcards sends `GET /api/v1/graph`
- **THEN** the system returns `HTTP 200 OK`
- **AND** the `nodes` array contains no book, chunk, card, or highlight nodes and no pillar hub nodes (all node collections are empty arrays without causing null reference errors)
- **AND** the client renders the graph empty state rather than a populated canvas.

#### Scenario: Flashcard created from highlight links to highlight node
- **WHEN** an authenticated user has a flashcard created from a reading highlight (`SourceType = Highlight` and `SourceHighlightId != null`)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToHighlight"` connecting `card.Id` to `card.SourceHighlightId`
- **AND** the card node is positioned relative to its source highlight cluster.

#### Scenario: Flashcard sourced from a document chunk links to chunk node
- **WHEN** an authenticated user has a flashcard whose `DocumentChunkId` references a surviving chunk of an imported book
- **THEN** the backend graph projection derives an edge with `relationType: "CardToChunk"` connecting `card.Id` to `chunk.Id`
- **AND** the chunk node is emitted and connected to its parent book via a `ChunkToBook` edge.

#### Scenario: Flashcard with no linked topic or highlight links to pillar hub
- **WHEN** an authenticated user has a flashcard with `DocumentChunkId == null` and `SourceHighlightId == null` (e.g. quiz mistake card)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToPillar"` connecting `card.Id` to `pillar-{card.Category}`
- **AND** the pillar hub for that category is emitted so the card node does not become an isolated degree-0 node.

#### Scenario: Shared tag associative edge generation
- **WHEN** an authenticated user has two highlights that both contain the tag `"mvcc"`
- **THEN** the backend graph projection derives a bidirectional or directed edge between the two highlight nodes with `relationType: "SharedTag"` and `label: "mvcc"`.

#### Scenario: Authenticated user receives pillar hub nodes and guaranteed connected topics
- **WHEN** an authenticated user whose artifacts span only some technical domains sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the `nodes` array contains a `type: "pillar"` hub only for each category that has at least one user book or card (between 0 and 5 hubs), and omits pillars with no user activity
- **AND** the `edges` array contains a `BookToPillar` edge for every emitted book, connecting it to its respective pillar hub
- **AND** no emitted book or chunk node has an edge degree of 0.

#### Scenario: Universal multi-disciplinary book connections
- **WHEN** a user-imported book spans multiple technical domains
- **THEN** the backend graph projection generates a `BookToPillar` edge only to the pillar hub of the book's own category, plus `ChunkToBook` edges from the book's referenced chunks
- **AND** no automatic fan-out to all four core technical pillars is generated for that book.

#### Scenario: Specific book-to-topic linking without Cartesian blowout
- **WHEN** a user-imported book belongs to `Category.BackendRuntime` and covers GC and memory allocation across several chunks
- **THEN** direct `ChunkToBook` edges are generated only for that book's own emitted chunks referenced by the user's cards or highlights
- **AND** no automatic Cartesian product edges are created to unrelated books or chunks solely because they share `Category.BackendRuntime`.

#### Scenario: Only user-imported books appear as book nodes
- **WHEN** an authenticated user has imported exactly one book while a different account has published a separate book
- **THEN** the graph's book nodes contain only the user's imported book
- **AND** the other account's published book is absent from the response.

#### Scenario: Only touched curriculum topics appear
- **WHEN** an authenticated user has a flashcard linked to Chunk A but has no card or highlight referencing Chunk B
- **THEN** Chunk A is emitted as a chunk node with a `ChunkToBook` edge to its parent book
- **AND** Chunk B is absent from the graph.

#### Scenario: Deleted highlight or flashcard is excluded from the graph
- **WHEN** an authenticated user soft-deletes a highlight or flashcard and then requests `GET /api/v1/graph`
- **THEN** the deleted node and all of its edges are absent from the response
- **AND** any chunk or pillar hub that no longer anchors a surviving user node is also omitted.

### Requirement: Client-Side Canvas 2D Force Layout Visualization
The client application SHALL provide an interactive graph visualization at `/graph` using Canvas 2D rendering powered by Cytoscape.js, executing physics layout calculations entirely inside the user's browser without placing computational load on the VPS.

The visualization SHALL execute an asynchronous force-directed layout (such as CoSE or Web Worker-driven simulation) upon mounting, configured with:
- `randomize: true` to avoid initial deterministic node stacking.
- `componentSpacing: 120` to guarantee visible separation between disparate pillar constellations.
- `nodeRepulsion: () => 500000` to prevent adjacent nodes from overlapping.
- `idealEdgeLength: () => 120` to balance visual density and readability.
- A physics cooling parameter (`coolingFactor: 0.95`, `numIter: 300`) that settles and halts all movement within 1.5 seconds. Once settled, the canvas SHALL transition to static interactive mode, consuming 0% ongoing CPU or GPU cycles when idle.

The canvas SHALL visually differentiate node types and retention status:
- **Pillar Hub Nodes:** Prominent circular nodes ($54\times 54\text{px}$) with a $3.5\text{px}$ neon halo ring (blur $18\text{px}$), bold typography ($13\text{px}$ font weight 700), the highest stacking rank (z 50), and color-coded backgrounds matching their respective domain palette.
- **Book Nodes:** Rounded-rectangle nodes ($34\times 26\text{px}$, corner radius $6\text{px}$) in an indigo tone (`#6366f1`) with an internal bookmark stroke, displaying book source emblems.
- **Chunk Nodes:** Small circular nodes ($30\times 30\text{px}$) color-coded by their parent book's engineering pillar, representing individual reading slices/chapters:
  - Backend Runtime: Cyan/Sky (`#0284c7` / `#38bdf8`)
  - Data Storage: Cyan/Teal (`#0891b2` / `#22d3ee`)
  - Distributed Systems: Violet/Purple (`#7c3aed` / `#a78bfa`)
  - Frontend Engineering: Amber/Orange (`#f59e0b` / `#fbbf24`)
  - Engineering Craft: Pink/Rose (`#ec4899` / `#fb7185`)
- **Card Nodes:** Diamond-shaped nodes ($26\times 26\text{px}$) color-coded by SM-2 retention status:
  - Learning: Amber (`#f59e0b`)
  - Reviewing: Blue (`#3b82f6`)
  - Mastered: Primary Brand Violet (`#7c3aed` fill with `#c4b5fd` border)
- **Highlight Nodes:** Diamond-cut nodes ($22\times 22\text{px}$) in cyan (`#06b6d4`) representing personal notes.

The canvas viewport wrapper SHALL render on neutral dark obsidian `#09090b` (`dark:bg-canvas`), completely eliminating legacy `dark:bg-slate-950`. In dark mode, node borders and connecting edges SHALL employ translucent hairline styling `#27272a`. The canvas stylesheet SHALL dynamically synchronize with `@nuxtjs/color-mode`, updating node fills, borders, labels, and edge opacities when the user switches between dark and light themes without requiring a page reload.

The canvas SHALL support smooth mouse and touch pan, zoom (bounded between 0.2x and 3.0x), box selection, drag repositioning, and double-click / button-triggered viewport fitting.

#### Scenario: Graph initialization and physics auto-settle
- **WHEN** a user navigates to `/graph`
- **THEN** the canvas initializes Cytoscape.js with the user's nodes and edges
- **AND** the force layout animates node positions into natural clusters
- **AND** the physics simulation settles and completely stops within 1.5 seconds.

#### Scenario: Level-of-Detail label decluttering at overview zoom
- **WHEN** the user views the graph canvas at default overview zoom ($zoom < 1.1\times$)
- **THEN** text labels for Card (diamond) and Highlight (diamond-cut) nodes are hidden to avoid label collision
- **AND** text labels for Pillar hubs, Books, and Chunks remain legible.

#### Scenario: Card label reveals on hover or selection
- **WHEN** the user hovers over or taps a Card node whose label is hidden
- **THEN** the canvas immediately reveals the Card node's label and highlights its connecting edge
- **AND** closing or unselecting restores the clean overview state.

#### Scenario: Viewport pan and zoom controls
- **WHEN** a user scrolls the mouse wheel or pinches the touch screen on the canvas
- **THEN** the canvas smoothly zooms within bounds ($0.2\times$ to $3.0\times$) centered on the cursor position
- **WHEN** the user clicks the "Fit to Screen" button
- **THEN** the canvas animates viewport bounds to display all visible nodes with comfortable padding.

#### Scenario: Dark/light mode theme synchronization
- **WHEN** the user switches application theme from dark to light mode via `ThemeToggle.vue`
- **THEN** the graph canvas immediately updates node labels, background grid contrast, and edge line colors to match the light mode palette without re-fetching graph data or resetting node coordinates.

#### Scenario: Pillar node rendering and visual prominence
- **WHEN** the graph canvas renders in the browser
- **THEN** each pillar hub node is rendered at $54\times 54\text{px}$, larger than book ($34\times 26\text{px}$), chunk ($30\times 30\text{px}$), card ($26\times 26\text{px}$), and highlight ($22\times 22\text{px}$) nodes
- **AND** the pillar hub node displays a bold label and distinct accent halo
- **AND** connected books form a surrounding orbital constellation around their parent pillar hub.

#### Scenario: Highlight and Book node colors unified across renderers
- **WHEN** the 2D canvas renders Highlight and Book nodes
- **THEN** Highlight nodes render cyan `#06b6d4` and Book nodes render indigo `#6366f1`, matching the legend swatches and the 3D renderer
- **AND** no Highlight node renders the legacy violet fill and no Book node renders the legacy slate fill.

#### Scenario: Stable CoSE layout without collapsed horizontal stacking
- **WHEN** a user navigates to `/graph` with a book that has chunks but zero user highlights
- **THEN** the chunks belonging to that book remain anchored to it via `ChunkToBook` edges and to their pillar via the book's `BookToPillar` edge
- **AND** the CoSE simulation settles without stacking unconnected nodes into a horizontal line at the viewport perimeter.

#### Scenario: Obsidian 2D canvas background and hairline border styling
- **WHEN** the 2D canvas renders in dark mode
- **THEN** the canvas container background renders with neutral obsidian `#09090b` (`dark:bg-canvas`)
- **AND** node borders and edges in dark mode utilize `#27272a` hairline styling rather than legacy opaque slate colors.

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `techdaily_graph_view_mode`.
2. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`). The filter container SHALL employ a responsive wrapping layout (`flex-wrap gap-1.5`) without hidden scrollbars or box-model clipping across both English and Vietnamese locales, ensuring that all 6 pill options remain 100% visible and discoverable. All category pills SHALL resolve explicit localization keys without falling back to raw untranslated strings.
3. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Books`, `Chunks`, `Flashcards`, `Highlights`).
4. **Mastery Status Filter:** Dropdown or pill selector to filter flashcard nodes by SM-2 status (`All`, `Learning`, `Reviewing`, `Mastered`). When `Mastered` is selected, the active indicator SHALL display primary brand violet styling (`bg-brand-600 text-white`) instead of emerald green.
5. **Live Search Input:** Text input that dynamically matches node titles, tags, and summary keywords. Matching nodes SHALL remain fully opaque and highlighted, while non-matching nodes SHALL fade to 15% opacity with edges dimmed in both 2D and 3D modes.
6. **Reset Filters CTA:** A button to immediately reset all filters, search inputs, and node opacities back to the default global view.

All control bar action buttons, mode switches, and filter chips SHALL utilize `.glass-panel`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, and `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-800`, `dark:bg-slate-900`, and `dark:border-slate-700`.

#### Scenario: Switching between 2D and 3D view modes
- **WHEN** the user clicks the "3D Cosmos" mode button in `GraphControlBar.vue`
- **THEN** the 2D canvas is smoothly unmounted or hidden
- **AND** the 3D WebGL Galaxy canvas mounts, preserving active category and type filters
- **AND** the choice is stored in `localStorage` so subsequent visits default to the chosen engine.

#### Scenario: Filtering by pillar category
- **WHEN** the user clicks the "Data Storage & Persistence" pillar filter chip
- **THEN** all book, chunk, card, and highlight nodes associated with other categories are hidden from the canvas
- **AND** the viewport smoothly animates to focus on the Data Storage cluster.

#### Scenario: Toggling node types via type filters
- **WHEN** the user disables the `Books`, `Chunks`, `Flashcards`, or `Highlights` toggle
- **THEN** nodes of that type and their connecting edges are hidden from the canvas
- **AND** no legacy `Topics` toggle is present in the control bar.

#### Scenario: Filtering by flashcard mastery level
- **WHEN** the user selects "Mastered" in the mastery status filter
- **THEN** card nodes with status `Learning` or `Reviewing` are hidden from the canvas
- **AND** only cards with SM-2 interval $\ge 21$ days (`Mastered`) remain visible alongside their connected chunk nodes.

#### Scenario: Live search node focus
- **WHEN** the user types `"MVCC"` into the search input
- **THEN** nodes containing `"MVCC"` in their title, summary, or tags remain fully highlighted with a glowing border
- **AND** all unrelated nodes fade to 15% opacity
- **AND** pressing Enter or clicking a match centers the viewport onto that node at 1.5x zoom.

#### Scenario: Resetting filters
- **WHEN** the user clicks the "Reset" button after applying multiple filters
- **THEN** all node type toggles, category chips, mastery filters, and search queries return to default
- **AND** all nodes and edges return to 100% visibility.

#### Scenario: Bilingual responsive category pills wrapping and complete localization
- **GIVEN** a user views `/graph` in Vietnamese locale (`vi-VN`)
- **WHEN** inspecting the Category Pillars filter row in `GraphControlBar.vue`
- **THEN** all 6 category pills ("Tất Cả", "Backend & Runtime", "Database & Storage", "Distributed Systems", "Frontend & Web", "Engineering Craft") are fully visible without horizontal clipping or truncation
- **AND** each pill resolves its translated label rather than falling back to raw untranslated English strings
- **AND** on viewports narrower than the combined pill width, the container wraps naturally into multiple clean rows.

### Requirement: Node Detail Slide-Over Drawer & 1-Click Action Bridges
Selecting any node on the graph canvas SHALL open a responsive slide-over drawer (`GraphDetailDrawer.vue`) on desktop ($\ge 768\text{px}$) or bottom sheet on mobile ($< 768\text{px}$) displaying contextual details and 1-click action bridges into the corresponding platform feature.

The drawer container and internal metrics/takeaways containers SHALL utilize `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated` with translucent hairline borders `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-800` and `dark:bg-slate-900`.

The detail drawer SHALL present:
1. **Node Header:** Node type badge with icon, pillar category tag, node title, and creation/review timestamp.
2. **Body Content:**
   - For Book nodes: Book cover/emblem, total chapters/slices, source URL, and reading progress.
   - For Chunk nodes: Chunk/chapter title, chunk order, summary, and parent book reference.
   - For Card nodes: Spaced repetition metrics including current interval, ease factor, repetition count, and next due date.
   - For Highlight nodes: Verbatim quote block with quotation styling, chapter source reference, and personal reflection note.
3. **Action Bridges (1-Click CTAs):**
   - Book node: "Browse in Library" (`/library`) and "Read Slices" (`/read/{bookId}`).
   - Chunk node: "Read Slice" (`/read/{bookId}?slice={chunkOrder}`).
   - Card node: "Review Flashcard" (`/review?cardId={id}`).
   - Highlight node: "Read Chapter" (`/read/{bookId}#slice-{chunkOrder}`) and "View in Notes" (`/notes?highlightId={id}`).
4. **Connected Relations List:** A list of adjacent connected nodes (e.g. connected flashcards, source book, related chunks) with clickable chips that select and center that node on the canvas.

All action buttons, badges, and status pills SHALL enforce the Bilingual Responsive Layout Invariant (`whitespace-nowrap shrink-0`) and responsive padding to eliminate text wrapping, truncation, or layout breaking in both English and Vietnamese.

The drawer SHALL support closing via an explicit close button, pressing the `Escape` keyboard key, or clicking the canvas backdrop outside the drawer.

#### Scenario: User selects a book node
- **WHEN** the user clicks a book node on the canvas
- **THEN** the node becomes visually highlighted with an active outline
- **AND** the detail drawer slides in from the right edge
- **AND** the drawer displays the book's title, pillar badge, total chapters/slices, and action buttons for "Browse in Library" and "Read Slices" bridging to `/read/{bookId}`.

#### Scenario: User selects a topic node
- **WHEN** the user clicks a chunk node on the canvas
- **THEN** the detail drawer displays the chunk/chapter title, chunk order, summary, and parent book reference
- **AND** displays an action button "Read Slice" linking to `/read/{bookId}?slice={chunkOrder}`.

#### Scenario: User selects a flashcard node
- **WHEN** the user clicks a flashcard node
- **THEN** the detail drawer displays the card's question/prompt, current SM-2 interval, ease factor, and repetition count
- **AND** displays an action button "Review Flashcard" that links directly to `/review?cardId={id}`.

#### Scenario: User selects a highlight node
- **WHEN** the user clicks a highlight node
- **THEN** the detail drawer renders the highlighted quote text, source chapter title, parent book title, and user note
- **AND** displays an action button "Read Chapter" linking to `/read/{bookId}#slice-{chunkOrder}`.

#### Scenario: User dismisses detail drawer
- **WHEN** the user presses the `Escape` key while the detail drawer is open
- **THEN** the drawer smoothly transitions off-screen
- **AND** the active node selection on the canvas is cleared.

### Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization
The client application SHALL provide an alternative 3D interactive knowledge graph visualization at `/graph` rendered with WebGL (via Three.js / `3d-force-graph`), executing calculations entirely on the client-side GPU without placing computational or memory load on the backend server.

The 3D WebGL cosmos canvas background SHALL strictly render with neutral dark obsidian `#09090b` in dark mode, completely eliminating bluish slate backgrounds (`#020617`). 3D sprite billboard label backgrounds in dark mode SHALL utilize elevated obsidian `rgba(18, 18, 21, 0.85)` (`dark:bg-canvas-elevated`), eliminating legacy slate boxes (`rgba(15, 23, 42, 0.85)`).

The 3D visualization SHALL represent architectural entities in an interactive spherical cosmos:
1. **Pillar Hub Nodes:** Rendered as glowing primary celestial bodies with large radii and pillar-specific emissive glow colors.
2. **Book Nodes:** Rendered as textured or emblem-accented spherical bodies orbiting their parent pillar hubs.
3. **Chunk Nodes:** Rendered as medium planetary spheres color-coded by their parent book's engineering pillar category, orbiting their parent book.
4. **Card Nodes:** Rendered as compact glowing spheres color-coded by SM-2 retention status (Learning: amber `#f59e0b`, Reviewing: blue `#3b82f6`, Mastered: primary brand violet `#7c3aed`).
5. **Highlight Nodes:** Rendered as crystalline or accent-colored satellites orbiting source books and chunks.
6. **Relational Edges:** Rendered as glowing 3D vector splines or translucent beams linking interconnected nodes across $(x, y, z)$ space.

The 3D visualization SHALL provide 360-degree OrbitControls supporting rotation around arbitrary axes, smooth pan, pinch-zoom, and a camera reset button. The 3D visualization SHALL provide a functional 360-degree Auto-Rotate mode driven by an active orbital camera trajectory, rotating the camera smoothly around the constellation center at the current altitude and distance, and pausing automatically upon user drag interaction. The 3D engine SHALL implement strict distance-based Level-of-Detail (LOD) label culling:
- At default galaxy overview camera distances, text labels SHALL be strictly restricted to the 5 primary Pillar Hubs, preventing overlapping text clusters from obscuring the constellation.
- Book, Chunk, Card, and Highlight labels SHALL be culled at overview distance, and SHALL dynamically reveal when the camera zooms within close range ($d < 250$), when the user hovers over or taps the node, or when the user toggles the HUD label switch.
- The floating HUD SHALL provide a 1-click label visibility toggle button allowing users to switch between Clean Cosmos mode (Hubs only) and Full Inspection mode (All labels).
To conserve user device battery and eliminate main-thread lag:
- The 3D force simulation SHALL settle node positions within a bounded warmup tick threshold (max 120 ticks) and halt physics calculations.
- The rendering loop SHALL throttle or pause when the camera is static and no animations are active (render-on-demand).
- Clicking any 3D node SHALL smoothly interpolate the camera toward the selected node and open `GraphDetailDrawer.vue`.

#### Scenario: 3D Galaxy visualization initialization
- **WHEN** the user activates 3D Cosmos mode on `/graph`
- **THEN** the application lazily loads the 3D WebGL engine without blocking the initial route transition
- **AND** initializes a 3D force-directed cosmos displaying nodes as glowing spheres in $(x, y, z)$ coordinates
- **AND** the physics simulation settles within 120 ticks without freezing the UI thread.

#### Scenario: 360-degree camera orbital navigation
- **WHEN** the user clicks and drags on the 3D canvas
- **THEN** the camera rotates smoothly around the orbital focus center at 60 FPS
- **AND** zooming with the mouse wheel smoothly scales the camera distance along the line of sight.

#### Scenario: 360-degree camera auto-orbit rotation
- **GIVEN** a user is on `/graph` in `3D Cosmos` mode
- **WHEN** the user clicks the "Auto Rotate" button in the floating HUD
- **THEN** the active indicator lights up with an accent highlight
- **AND** the camera begins continuous circular rotation around the constellation center $(0,0,0)$ along the $(x, z)$ orbital plane at consistent velocity
- **WHEN** the user clicks the button again
- **THEN** camera auto-rotation ceases immediately, leaving the camera positioned at its current vantage angle.

#### Scenario: Distance-based Level-of-Detail label culling
- **WHEN** the camera is positioned at a wide galaxy overview distance
- **THEN** text labels for Book, Chunk, Card, and Highlight nodes are culled to prevent visual clutter
- **AND** only the 5 primary Pillar Hub text billboard labels remain visible
- **WHEN** the user zooms in close to a specific cluster or hovers over a node
- **THEN** high-contrast billboard labels for the targeted nodes dynamically appear facing the camera.

#### Scenario: Toggling all labels visibility via HUD control
- **WHEN** the user clicks the label toggle button in the floating HUD
- **THEN** the canvas toggles between Clean Cosmos mode (Pillar Hubs only) and Full Inspection mode (All visible nodes display labels).

#### Scenario: 3D node selection and detail drawer sync
- **WHEN** the user clicks on a 3D node sphere
- **THEN** the camera smoothly animates to center on the selected node
- **AND** `GraphDetailDrawer.vue` opens with complete node details and 1-click action bridges matching the 2D view.

#### Scenario: Render-on-demand battery preservation
- **WHEN** the 3D layout has settled and the user is not actively rotating or zooming the camera
- **THEN** the WebGL frame loop halts active re-renders until interaction resumes, consuming negligible CPU and GPU cycles when idle.

#### Scenario: Obsidian 3D galaxy canvas background and elevated billboard styling
- **WHEN** the 3D canvas renders in dark mode
- **THEN** the WebGL renderer background renders with `#09090b` matching the studio obsidian shell
- **AND** sprite text billboard labels utilize elevated background `rgba(18, 18, 21, 0.85)` with hairline borders.

### Requirement: Interactive Visual Graph Legend & Entity Guide
The knowledge graph view SHALL feature a floating, collapsible visual legend panel (`GraphLegend.vue`) positioned in the bottom-left viewport corner (`bottom-5 left-5`), providing an intuitive visual key for all node geometries, relative scales, category colors, and SM-2 retention metrics across both 2D and 3D view modes.

The legend panel SHALL present:
1. **Header:** A title labeled "Visual Legend" (`en-US`) / "Bảng Chú Thích" (`vi-VN`) with a layers glyph, and a collapse control.
2. **Entity Hierarchy Section:**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering unclipped unabridged text labels for every entity type without using text truncation or ellipsis characters across both English and Vietnamese locales.
   - Each entity row SHALL display the count for that node type sourced from the graph payload `stats.nodeTypeCounts`. When a count is unavailable the row SHALL omit the number rather than render `undefined` or `NaN`.
   - Each entity row's swatch SHALL mirror the on-canvas node geometry for that type:
     - **Pillar Hubs:** Ringed filled circle (2D) / Luminous cosmic hubs (3D) color-coded by the 5 canonical engineering pillars.
     - **Books:** Rounded-rectangle swatch (2D) / Indigo spheres (3D) representing ingested documentation ("Tech Book" / "Sách kỹ thuật").
     - **Chunks:** Smaller filled circle (2D) / Planetary spheres (3D) color-coded by parent book pillar, representing reading slices ("Reading Slice" / "Phân đoạn đọc").
     - **Highlights:** Diamond (rotated-square) swatch (2D) / Cyan satellites (3D) representing personal quotes and notes ("Personal Note / Highlight" / "Ghi chú & Trích đoạn").
3. **Flashcard Retention Status Section (SM-2):**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering complete status labels and duration intervals without text wrapping or clipping.
   - **Learning ($< 6$ days):** Amber indicator (`#f59e0b`).
   - **Reviewing ($6–20$ days):** Blue indicator (`#3b82f6`).
   - **Mastered ($\ge 21$ days):** Brand violet indicator (`#7c3aed`), matching the mastered-flashcard fill rendered on the canvas (the legacy emerald `#10b981` swatch is retired).
4. **Interactive Hover Dimming:**
   - Hovering over any entity or retention row in the legend SHALL keep matching nodes fully opaque and dim non-matching nodes and their edges to 15% opacity (`0.15`) in both the 2D and 3D renderers.
   - Leaving the hover area SHALL immediately restore full standard node and edge opacities.
5. **Footer:** The legend footer SHALL display an isolate hint ("Hover item to isolate" / "Di chuột để tách biệt") and an `L` keyboard-shortcut chip.
6. **Collapsible Header, Safe Clearance & Persistence:**
   - The expanded card container width SHALL be standardized to 224 pixels (`w-56`), guaranteeing at least 80 pixels of horizontal clearance between the legend card and the 2D Minimap on 768px tablet viewports with the navigation sidebar open.
   - On viewports narrower than 1024 pixels (tablet and mobile devices), the legend panel SHALL default to a collapsed pill trigger button (`[ ? Visual Legend ^ ]` / `[ ? Bảng Chú Thích ^ ]`) on initial page load when no user preference is stored in `localStorage`.
   - The collapsed state SHALL persist in `localStorage` under `techdaily_graph_legend_collapsed`.

#### Scenario: Legend displays entity hierarchy and SM-2 status color keys
- **GIVEN** a user is on `/graph` in either 2D or 3D view mode
- **WHEN** the user views the bottom-left corner of the screen
- **THEN** `GraphLegend.vue` displays geometry-matched swatches and descriptions for Pillar Hubs, Books, Chunks, Highlights, and SM-2 Flashcard retention states (Learning, Reviewing, Mastered)
- **AND** each entity row shows its live node-type count sourced from `stats.nodeTypeCounts`.

#### Scenario: Entity rows show node-type counts from the graph payload
- **GIVEN** the loaded graph payload reports `stats.nodeTypeCounts` of `{ pillar: 5, book: 19, chunk: 120, highlight: 184 }`
- **THEN** the Pillar, Book, Chunk, and Highlight entity rows display `5`, `19`, `120`, and `184` respectively
- **AND** a node type whose count is missing renders no number rather than `undefined` or `NaN`.

#### Scenario: Mastered retention swatch matches canvas violet fill
- **GIVEN** a user views the expanded legend
- **WHEN** inspecting the SM-2 retention section
- **THEN** the "Mastered (≥ 21 days)" indicator renders brand violet `#7c3aed`, identical to the mastered-flashcard fill on the canvas
- **AND** no emerald `#10b981` mastered swatch is present.

#### Scenario: Complete unclipped legend text in English and Vietnamese
- **GIVEN** a user views `/graph` with the visual legend expanded
- **WHEN** inspecting the entity hierarchy items and retention status items
- **THEN** all item labels (including "Reading Slice", "Personal Note / Highlight", "Phân đoạn đọc", and "Ghi chú & Trích đoạn") are rendered in their entirety without text truncation or ellipsis characters (`...`)
- **AND** no legacy "Curriculum Topic" / "Chủ đề giáo trình" legend item is present.

#### Scenario: Tablet clearance between Visual Legend and Minimap
- **GIVEN** a user views `/graph` on a 768px tablet viewport in 2D mode with the sidebar open
- **WHEN** the user expands the Visual Legend panel
- **THEN** the horizontal clearance gap between the right edge of the Visual Legend and the left edge of the Minimap is at least 80 pixels
- **AND** neither component overlaps or occludes the other.

#### Scenario: Interactive legend hover dims non-matching graph nodes
- **GIVEN** the legend panel is expanded
- **WHEN** the user hovers over the "Highlights" or "Mastered" entry in the legend
- **THEN** all non-matching nodes and edges on the active graph canvas dim to 15% opacity (`0.15`)
- **AND** matching nodes remain fully opaque with prominent glowing accents
- **WHEN** the cursor leaves the legend item
- **THEN** all nodes and edges return to their standard opacity.

#### Scenario: Collapsing and expanding the legend panel with state persistence
- **WHEN** the user clicks the collapse button on the legend header
- **THEN** the legend smoothly transitions into a minimal floating pill button labeled "Visual Legend" (or "Bảng Chú Thích")
- **AND** the preference is saved in `localStorage`
- **WHEN** the user reloads the page or navigates back to `/graph`
- **THEN** the collapsed state is automatically preserved.

#### Scenario: Default collapsed state on tablet and mobile viewports
- **GIVEN** a user on a tablet ($768\text{px}$) or mobile device ($< 1024\text{px}$) with no prior `techdaily_graph_legend_collapsed` preference in `localStorage`
- **WHEN** the user navigates to `/graph`
- **THEN** the visual legend initializes in the collapsed state as a floating pill button (`[ ? Visual Legend ^ ]` / `[ ? Bảng Chú Thích ^ ]`).

### Requirement: Knowledge Graph Synapse Edge Styling
The knowledge graph SHALL render relational edges with a distinct visual style per relation-type family, applied consistently across the 2D Cytoscape canvas (`GraphCanvas.vue`) and the 3D WebGL cosmos (`GraphCanvas3D.vue`), so users can distinguish structural, atomic, and associative links at a glance.

Every edge relation type SHALL resolve to exactly one of three style families:
1. **Solid Constellation** — structural anchor links `BookToPillar` and `ChunkToBook`:
   - Rendered as a smooth Bézier curve (curve-style bezier) with curvature `0.35`, line width `2.0px`, solid line style, and active opacity `0.85`.
2. **Dotted Synapse** — atomic card links `CardToHighlight`, `CardToPillar`, and `CardToChunk`:
   - Rendered as a dotted line with dash pattern `[3, 4]`, line width `1.2px`, and active opacity `0.60`.
3. **Shared-Tag Dashed** — associative links `SharedTag` (and, as the default family for any relation type not named above — `HighlightToBook`, `HighlightToChunk`):
   - Rendered as a bidirectional dashed line with dash pattern `[4, 4]`, line width `1.5px`, and a directional particle-pulse rate of `0.007`.

In the 3D renderer, active edges SHALL animate directional particles at pulse rate `0.007`, and idle edges SHALL carry none. In the 2D Cytoscape renderer, families are distinguished by line style, width, dash pattern, and curvature; any particle-flow affordance SHALL run only while an edge is active, selected, or hovered and SHALL cease when the canvas is idle, preserving the 0% idle-CPU guarantee. When any filter, live-search, or legend-hover dimming is active, non-matching edges SHALL dim to 15% opacity (`0.15`).

#### Scenario: Solid constellation edges anchor structural links
- **WHEN** the canvas renders a `BookToPillar` or `ChunkToBook` edge
- **THEN** the edge is drawn as a Bézier curve with curvature `0.35`, line width `2.0px`, solid style, and opacity `0.85` when active.

#### Scenario: Dotted synapse edges connect atomic card links
- **WHEN** the canvas renders a `CardToHighlight`, `CardToPillar`, or `CardToChunk` edge
- **THEN** the edge is drawn dotted with dash pattern `[3, 4]`, line width `1.2px`, and opacity `0.60`.

#### Scenario: Shared-tag dashed edges connect cross-cutting taxonomy
- **WHEN** the canvas renders a `SharedTag` edge between two highlights that share a tag
- **THEN** the edge is drawn as a bidirectional dashed line with dash pattern `[4, 4]`, line width `1.5px`
- **AND** in the 3D renderer, directional particles animate along it at pulse rate `0.007`.

#### Scenario: Unlisted relation types fall back to the associative dashed family
- **WHEN** the canvas renders a relation type not explicitly assigned to the Solid or Dotted family (for example `HighlightToBook`)
- **THEN** the edge resolves to the Shared-Tag Dashed associative family style rather than rendering unstyled.

#### Scenario: Edge dimming and particle pause during isolation
- **GIVEN** the user hovers a legend row, applies a filter, or runs a live search
- **WHEN** an edge does not connect to any matching node
- **THEN** that edge dims to 15% opacity (`0.15`) and any active particle flow pauses
- **WHEN** the dimming state is cleared
- **THEN** the edge returns to its family's active opacity.

#### Scenario: Consistent edge families across 2D and 3D renderers
- **WHEN** the user switches between the 2D Cytoscape canvas and the 3D WebGL cosmos
- **THEN** each relation type keeps its family assignment (Solid Constellation, Dotted Synapse, or Shared-Tag Dashed)
- **AND** the 3D renderer applies directional particle pulse rate `0.007` to active edges of every family.
