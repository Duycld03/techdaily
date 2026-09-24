# knowledge-graph Specification

## Purpose
TBD - created by archiving change interactive-architecture-knowledge-graph. Update Purpose after archive.

## Requirements

### Requirement: Knowledge Graph Relational Extraction API
The system SHALL expose a protected HTTP GET endpoint `GET /api/v1/graph` that extracts and returns an associative architecture knowledge graph for the authenticated user, derived strictly from relational entities in PostgreSQL 17 without introducing auxiliary graph databases or continuous server-side vector calculations.

The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and SHALL return `HTTP 401 Unauthorized` with RFC 7807 problem details when invoked without a valid token.

The returned response payload (`KnowledgeGraphResponse`) SHALL consist of:
1. `nodes`: An array of `GraphNodeDto` items representing:
   - **Pillar Hub Nodes:** Five canonical architectural pillar anchors (`pillar-FrontendWeb`, `pillar-BackendDotNet`, `pillar-DatabaseStorage`, `pillar-SystemDesign`, `pillar-EngineeringCraft`), containing `id`, `label`, `category`, and `type: "pillar"`.
   - **Topic Nodes:** Master curriculum topics from `Topics`, containing `id`, `label`, `category` (Pillar category), `dayOrder`, `summary`, and `difficulty`.
   - **Book Nodes:** Published engineering books from `DocumentBooks`, containing `id`, `label`, `category`, `totalChunks`, and `authorOrSourceUrl`.
   - **Card Nodes:** Spaced repetition flashcards from `SpacedRepetitionCards` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label`, `topicId`, `status` (`Learning`, `Reviewing`, `Mastered`), `intervalDays`, `easeFactor`, and `repetitionCount`.
   - **Highlight Nodes:** Personal reading highlights from `UserHighlights` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label` (truncated quote), `documentChunkId`, `bookId`, `note`, `tags`, and `createdAt`.
2. `edges`: An array of `GraphEdgeDto` items representing relational and associative connections:
   - `TopicToPillar`: Connecting every curriculum topic to its parent architectural Pillar Hub node (`Topic.Id -> pillar-{Category}`).
   - `BookToPillar`: Connecting published document books to their respective architectural Pillar Hub node (`Book.Id -> pillar-{Category}`), or to all four core technical pillars if the book represents a universal multi-pillar curriculum.
   - `CardToTopic`: Connecting active user flashcards to their target curriculum topic (`Card.TopicId -> Topic.Id`).
   - `BookToTopic`: Connecting document books to specific curriculum topics when explicitly referenced in book chapter titles, chunk summaries, or matching topic slugs/titles, rather than through a blind Cartesian product of all topics within the category.
   - `HighlightToBook`: Connecting user highlights to their source document book via `DocumentChunk.DocumentBookId`.
   - `HighlightToTopic`: Connecting user highlights to curriculum topics when highlight tags match topic slugs or titles.
   - `SharedTag`: Connecting user highlights to other highlights that share one or more normalized tag keywords.
3. `stats`: Metadata containing `totalNodes`, `totalEdges`, `nodeTypeCounts` (including counts for `pillar`, `topic`, `book`, `card`, `highlight`), and `pillarCounts`.

The query execution SHALL execute in a single consolidated read transaction using EF Core `AsNoTracking()`, utilizing database indexes on foreign keys (`UserId`, `TopicId`, `DocumentBookId`, `DocumentChunkId`) to achieve a sub-5ms database query time on production PostgreSQL 17. The uncompressed JSON payload size SHALL be strictly bounded under 100 KB (typical size: 30–60 KB for 150–300 nodes).

#### Scenario: Unauthenticated request to knowledge graph endpoint
- **WHEN** an unauthenticated client sends `GET /api/v1/graph` without a JWT Bearer token
- **THEN** the system returns `HTTP 401 Unauthorized` with RFC 7807 problem details.

#### Scenario: Authenticated user requests knowledge graph
- **WHEN** an authenticated user sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the payload contains topic nodes, published book nodes, and user-owned card and highlight nodes
- **AND** the payload contains edges linking cards to topics, highlights to books, and highlights sharing common tags
- **AND** the response excludes cards and highlights belonging to other users.

#### Scenario: New user with zero flashcards or highlights requests graph
- **WHEN** an authenticated user who has not created any highlights or flashcards sends `GET /api/v1/graph`
- **THEN** the system returns `HTTP 200 OK`
- **AND** the `nodes` array contains all standard curriculum topic nodes and published book nodes
- **AND** the `edges` array contains topic-to-pillar and book-to-topic edges
- **AND** the `card` and `highlight` node collections are empty arrays without causing null reference errors.

#### Scenario: Flashcard created from highlight links to highlight node
- **WHEN** an authenticated user has a flashcard created from a reading highlight (`SourceType = Highlight` and `SourceHighlightId != null`)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToHighlight"` connecting `card.Id` to `card.SourceHighlightId`
- **AND** the card node is positioned relative to its source highlight cluster.

#### Scenario: Flashcard with no linked topic or highlight links to pillar hub
- **WHEN** an authenticated user has a flashcard with `TopicId == null` and `SourceHighlightId == null` (e.g. quiz mistake card)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToPillar"` connecting `card.Id` to `pillar-{card.Category}`
- **AND** the card node does not become an isolated degree-0 node.

#### Scenario: Shared tag associative edge generation
- **WHEN** an authenticated user has two highlights that both contain the tag `"mvcc"`
- **THEN** the backend graph projection derives a bidirectional or directed edge between the two highlight nodes with `relationType: "SharedTag"` and `label: "mvcc"`.

#### Scenario: Authenticated user receives pillar hub nodes and guaranteed connected topics
- **WHEN** an authenticated user sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the `nodes` array contains exactly 5 Pillar Hub nodes with `type: "pillar"` corresponding to `FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`, and `EngineeringCraft`
- **AND** the `edges` array contains a `TopicToPillar` edge for every returned curriculum topic, connecting the topic to its respective pillar hub
- **AND** no curriculum topic node has an edge degree of 0.

#### Scenario: Universal multi-disciplinary book connections
- **WHEN** the library contains the 30-Day Master Curriculum book spanning all technical domains
- **THEN** the backend graph projection generates `BookToPillar` edges connecting the book node to each of the four core technical pillars (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`).

#### Scenario: Specific book-to-topic linking without Cartesian blowout
- **WHEN** a book belongs to `Category.BackendDotNet` and covers topics on GC and memory allocation
- **THEN** direct `BookToTopic` edges are generated only for topics whose titles or slugs match the book's contents
- **AND** no automatic Cartesian product edges are created to unrelated topics solely because they share `Category.BackendDotNet`.

---

### Requirement: Client-Side Canvas 2D Force Layout Visualization
The client application SHALL provide an interactive graph visualization at `/graph` using Canvas 2D rendering powered by Cytoscape.js, executing physics layout calculations entirely inside the user's browser without placing computational load on the VPS.

The visualization SHALL execute an asynchronous force-directed layout (such as CoSE or Web Worker-driven simulation) upon mounting, configured with:
- `randomize: true` to avoid initial deterministic node stacking.
- `componentSpacing: 120` to guarantee visible separation between disparate pillar constellations.
- `nodeRepulsion: () => 500000` to prevent adjacent nodes from overlapping.
- `idealEdgeLength: () => 120` to balance visual density and readability.
- A physics cooling parameter (`coolingFactor: 0.95`, `numIter: 300`) that settles and halts all movement within 1.5 seconds. Once settled, the canvas SHALL transition to static interactive mode, consuming 0% ongoing CPU or GPU cycles when idle.

The canvas SHALL visually differentiate node types and retention status:
- **Pillar Hub Nodes:** Prominent circular nodes ($56\times 56\text{px}$) with thick glowing borders ($3\text{px}$), bold typography ($13\text{px}$ font weight 700), and color-coded backgrounds matching their respective domain palette.
- **Topic Nodes:** Elliptical nodes ($36\times 36\text{px}$) color-coded by their engineering pillar:
  - Backend Runtime: Cyan/Sky (`#0284c7` / `#38bdf8`)
  - Data Storage: Cyan/Teal (`#0891b2` / `#22d3ee`)
  - Distributed Systems: Violet/Purple (`#7c3aed` / `#a78bfa`)
  - Frontend Engineering: Amber/Orange (`#d97706` / `#fbbf24`)
  - Engineering Craft: Rose (`#e11d48` / `#fb7185`)
- **Book Nodes:** Rounded rectangular nodes displaying book source emblems.
- **Card Nodes:** Diamond-shaped nodes color-coded by SM-2 retention status:
  - Learning: Amber (`#f59e0b`)
  - Reviewing: Blue (`#3b82f6`)
  - Mastered: Primary Brand Violet (`#7c3aed` fill with `#c4b5fd` border)
- **Highlight Nodes:** Hexagonal or compact accent nodes representing personal notes.

The canvas viewport wrapper SHALL render on neutral dark obsidian `#09090b` (`dark:bg-canvas`), completely eliminating legacy `dark:bg-slate-950`. In dark mode, node borders and connecting edges SHALL employ translucent hairline styling `#27272a`. The canvas stylesheet SHALL dynamically synchronize with `@nuxtjs/color-mode`, updating node fills, borders, labels, and edge opacities when the user switches between dark and light themes without requiring a page reload.

The canvas SHALL support smooth mouse and touch pan, zoom (bounded between 0.2x and 3.0x), box selection, drag repositioning, and double-click / button-triggered viewport fitting.

#### Scenario: Graph initialization and physics auto-settle
- **WHEN** a user navigates to `/graph`
- **THEN** the canvas initializes Cytoscape.js with the user's nodes and edges
- **AND** the force layout animates node positions into natural clusters
- **AND** the physics simulation settles and completely stops within 1.5 seconds.

#### Scenario: Level-of-Detail label decluttering at overview zoom
- **WHEN** the user views the graph canvas at default overview zoom ($zoom < 1.1\times$)
- **THEN** text labels for Card (diamond) and Highlight (hexagon) nodes are hidden to avoid label collision
- **AND** text labels for Pillar hubs, Books, and Topics remain legible.

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
- **THEN** each pillar hub node is rendered with a diameter of $56\text{px}$, larger than topic ($36\text{px}$), book ($44\text{px}$), card ($28\text{px}$), and highlight ($24\text{px}$) nodes
- **AND** the pillar hub node displays a bold label and distinct accent border
- **AND** connected topics form a surrounding orbital constellation around their parent pillar hub.

#### Scenario: Stable CoSE layout without collapsed horizontal stacking
- **WHEN** a user navigates to `/graph` with categories that contain zero book nodes and zero user highlights
- **THEN** the topics belonging to those categories remain anchored to their respective Pillar Hub via `TopicToPillar` edges
- **AND** the CoSE simulation settles without stacking unconnected nodes into a horizontal line at the viewport perimeter.

#### Scenario: Obsidian 2D canvas background and hairline border styling
- **WHEN** the 2D canvas renders in dark mode
- **THEN** the canvas container background renders with neutral obsidian `#09090b` (`dark:bg-canvas`)
- **AND** node borders and edges in dark mode utilize `#27272a` hairline styling rather than legacy opaque slate colors.

---

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `techdaily_graph_view_mode`.
2. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`). The filter container SHALL employ a responsive wrapping layout (`flex-wrap gap-1.5`) without hidden scrollbars or box-model clipping across both English and Vietnamese locales, ensuring that all 6 pill options remain 100% visible and discoverable. All category pills SHALL resolve explicit localization keys without falling back to raw untranslated strings.
3. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Topics`, `Books`, `Flashcards`, `Highlights`).
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
- **THEN** all topic, book, card, and highlight nodes associated with other categories are hidden from the canvas
- **AND** the viewport smoothly animates to focus on the Data Storage cluster.

#### Scenario: Filtering by flashcard mastery level
- **WHEN** the user selects "Mastered" in the mastery status filter
- **THEN** card nodes with status `Learning` or `Reviewing` are hidden from the canvas
- **AND** only cards with SM-2 interval $\ge 21$ days (`Mastered`) remain visible alongside their connected topic nodes.

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

---

### Requirement: Node Detail Slide-Over Drawer & 1-Click Action Bridges
Selecting any node on the graph canvas SHALL open a responsive slide-over drawer (`GraphDetailDrawer.vue`) on desktop ($\ge 768\text{px}$) or bottom sheet on mobile ($< 768\text{px}$) displaying contextual details and 1-click action bridges into the corresponding platform feature.

The drawer container and internal metrics/takeaways containers SHALL utilize `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated` with translucent hairline borders `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-800` and `dark:bg-slate-900`.

The detail drawer SHALL present:
1. **Node Header:** Node type badge with icon, pillar category tag, node title, and creation/review timestamp.
2. **Body Content:**
   - For Topic nodes: Key takeaways, curriculum day order, and difficulty level.
   - For Book nodes: Book cover/emblem, total chapters/slices, source URL, and reading progress.
   - For Card nodes: Spaced repetition metrics including current interval, ease factor, repetition count, and next due date.
   - For Highlight nodes: Verbatim quote block with quotation styling, chapter source reference, and personal reflection note.
3. **Action Bridges (1-Click CTAs):**
   - Topic node: "Practice Quiz" (`/quiz?topic={slug}`) and "View Roadmap" (`/roadmap#{dayOrder}`).
   - Book node: "Browse in Library" (`/library`) and "Read Slices" (`/read/{bookId}`).
   - Card node: "Review Flashcard" (`/review?cardId={id}`).
   - Highlight node: "Read Chapter" (`/read/{bookId}#slice-{chunkOrder}`) and "View in Notes" (`/notes?highlightId={id}`).
4. **Connected Relations List:** A list of adjacent connected nodes (e.g. connected flashcards, source book, related topics) with clickable chips that select and center that node on the canvas.

All action buttons, badges, and status pills SHALL enforce the Bilingual Responsive Layout Invariant (`whitespace-nowrap shrink-0`) and responsive padding to eliminate text wrapping, truncation, or layout breaking in both English and Vietnamese.

The drawer SHALL support closing via an explicit close button, pressing the `Escape` keyboard key, or clicking the canvas backdrop outside the drawer.

#### Scenario: User selects a topic node
- **WHEN** the user clicks a topic node on the canvas
- **THEN** the node becomes visually highlighted with an active outline
- **AND** the detail drawer slides in from the right edge
- **AND** the drawer displays the topic's title, pillar badge, summary markdown, and action buttons for "Practice Quiz" and "View Roadmap".

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

### Requirement: Application Navigation & Mobile Responsive Placement
The knowledge graph SHALL be accessible as a first-class navigation item under the **"Knowledge"** navigation section (`nav.group_knowledge`) across both desktop and mobile layouts.

In `frontend/components/layout/AppSidebar.vue`:
- A link with label `nav.graph`, route `/graph`, and icon `Network` from `lucide-vue-next` SHALL be rendered in the Knowledge navigation group alongside Insights, Library, and Notes.
- The link SHALL be highlighted with the active sidebar style when the current route is `/graph`.

In `frontend/components/layout/AppHeader.vue`:
- The mobile slide-out navigation drawer SHALL include the `/graph` link with icon `Network` under the Knowledge section.
- Selecting the link on mobile SHALL close the mobile navigation drawer and navigate to `/graph`.

On mobile viewports ($< 768\text{px}$):
- The graph canvas SHALL occupy 100% of the available screen height below the top header.
- The control bar SHALL collapse into a compact floating filter pill button that expands into a mobile filter bottom sheet on tap.
- The node detail drawer SHALL present as a swipeable bottom sheet rather than a wide side drawer, ensuring comfortable thumb reachability.

#### Scenario: Desktop sidebar navigation
- **WHEN** an authenticated user clicks "Knowledge Graph" in the desktop sidebar
- **THEN** the browser navigates to `/graph`
- **AND** the sidebar marks the "Knowledge Graph" link as active with the primary accent background.

#### Scenario: Mobile header navigation
- **WHEN** a user on a mobile device opens the header hamburger menu and taps "Knowledge Graph"
- **THEN** the mobile menu drawer closes
- **AND** the browser navigates to `/graph`
- **AND** the full-height mobile graph canvas initializes.

#### Scenario: Responsive multi-viewport and bilingual smoke verification
- **WHEN** an automated E2E test runs across Desktop ($1920\times 1080$), Tablet ($768\times 1024$), and Mobile ($375\times 812$) viewports
- **THEN** the knowledge graph canvas initializes successfully in both 2D and 3D engine modes without console errors
- **AND** all HUD buttons, visual legend cards, and category filter pills render without horizontal clipping across both English and Vietnamese locales.

#### Scenario: Live smoke test execution and defect resolution
- **WHEN** the live responsive E2E smoke test is executed against the application runtime
- **THEN** 100% of assertion checks pass across Desktop, Tablet, and Mobile viewports
- **AND** visual snapshots confirm zero label collisions, proper HUD elevation, and unclipped legend cards.

### Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization
The client application SHALL provide an alternative 3D interactive knowledge graph visualization at `/graph` rendered with WebGL (via Three.js / `3d-force-graph`), executing calculations entirely on the client-side GPU without placing computational or memory load on the backend server.

The 3D WebGL cosmos canvas background SHALL strictly render with neutral dark obsidian `#09090b` in dark mode, completely eliminating bluish slate backgrounds (`#020617`). 3D sprite billboard label backgrounds in dark mode SHALL utilize elevated obsidian `rgba(18, 18, 21, 0.85)` (`dark:bg-canvas-elevated`), eliminating legacy slate boxes (`rgba(15, 23, 42, 0.85)`).

The 3D visualization SHALL represent architectural entities in an interactive spherical cosmos:
1. **Pillar Hub Nodes:** Rendered as glowing primary celestial bodies with large radii and pillar-specific emissive glow colors.
2. **Topic Nodes:** Rendered as medium planetary spheres color-coded by their parent engineering pillar category.
3. **Book Nodes:** Rendered as textured or emblem-accented spherical bodies orbiting their parent pillar hubs.
4. **Card Nodes:** Rendered as compact glowing spheres color-coded by SM-2 retention status (Learning: amber `#f59e0b`, Reviewing: blue `#3b82f6`, Mastered: primary brand violet `#7c3aed`).
5. **Highlight Nodes:** Rendered as crystalline or accent-colored satellites orbiting source books and topics.
6. **Relational Edges:** Rendered as glowing 3D vector splines or translucent beams linking interconnected nodes across $(x, y, z)$ space.

The 3D visualization SHALL provide 360-degree OrbitControls supporting rotation around arbitrary axes, smooth pan, pinch-zoom, and a camera reset button. The 3D visualization SHALL provide a functional 360-degree Auto-Rotate mode driven by an active orbital camera trajectory, rotating the camera smoothly around the constellation center at the current altitude and distance, and pausing automatically upon user drag interaction. The 3D engine SHALL implement strict distance-based Level-of-Detail (LOD) label culling:
- At default galaxy overview camera distances, text labels SHALL be strictly restricted to the 5 primary Pillar Hubs, preventing overlapping text clusters from obscuring the constellation.
- Topic, Book, Card, and Highlight labels SHALL be culled at overview distance, and SHALL dynamically reveal when the camera zooms within close range ($d < 250$), when the user hovers over or taps the node, or when the user toggles the HUD label switch.
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
- **THEN** text labels for Topic, Book, Card, and Highlight nodes are culled to prevent visual clutter
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

---

### Requirement: Interactive Visual Graph Legend & Entity Guide
The knowledge graph view SHALL feature a floating, collapsible visual legend panel (`GraphLegend.vue`) positioned in the bottom-left viewport corner (`bottom-5 left-5`), providing an intuitive visual key for all node geometries, relative scales, category colors, and SM-2 retention metrics across both 2D and 3D view modes.

The legend panel SHALL present:
1. **Entity Hierarchy Section:**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering unclipped unabridged text labels for every entity type without using text truncation or ellipsis characters across both English and Vietnamese locales.
   - **Pillar Hubs:** Large circles (2D) / Luminous cosmic hubs (3D) color-coded by the 5 canonical engineering pillars.
   - **Topics:** Elliptical nodes (2D) / Planetary spheres (3D) color-coded by parent pillar ("Curriculum Topic" / "Chủ đề giáo trình").
   - **Books:** Rounded rectangles (2D) / Indigo spheres (3D) representing ingested documentation ("Tech Book" / "Sách kỹ thuật").
   - **Highlights:** Compact hexagons (2D) / Cyan satellites (3D) representing personal quotes and notes ("Personal Note / Highlight" / "Ghi chú & Trích đoạn").
2. **Flashcard Retention Status Section (SM-2):**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering complete status labels and duration intervals without text wrapping or clipping.
   - **Learning ($< 6$ days):** Amber indicator (`#f59e0b`).
   - **Reviewing ($6–20$ days):** Blue indicator (`#3b82f6`).
   - **Mastered ($\ge 21$ days):** Emerald indicator (`#10b981`).
3. **Interactive Hover Dimming:**
   - Hovering over any entity row in the legend SHALL highlight matching nodes across the canvas and dim non-matching nodes to 20% opacity.
   - Leaving the hover area SHALL immediately restore full standard node opacities.
4. **Collapsible Header, Safe Clearance & Persistence:**
   - The expanded card container width SHALL be standardized to 224 pixels (`w-56`), guaranteeing at least 80 pixels of horizontal clearance between the legend card and the 2D Minimap on 768px tablet viewports with the navigation sidebar open.
   - On viewports narrower than 1024 pixels (tablet and mobile devices), the legend panel SHALL default to a collapsed pill trigger button (`[ ? Visual Legend ^ ]`) on initial page load when no user preference is stored in `localStorage`.
   - The collapsed state SHALL persist in `localStorage` under `techdaily_graph_legend_collapsed`.

#### Scenario: Legend displays entity hierarchy and SM-2 status color keys
- **GIVEN** a user is on `/graph` in either 2D or 3D view mode
- **WHEN** the user views the bottom-left corner of the screen
- **THEN** `GraphLegend.vue` displays color-coded badges and descriptions for Pillar Hubs, Topics, Books, Highlights, and SM-2 Flashcard retention states (Learning, Reviewing, Mastered).

#### Scenario: Complete unclipped legend text in English and Vietnamese
- **GIVEN** a user views `/graph` with the visual legend expanded
- **WHEN** inspecting the entity hierarchy items and retention status items
- **THEN** all item labels (including "Curriculum Topic", "Personal Note / Highlight", "Chủ đề giáo trình", and "Ghi chú & Trích đoạn") are rendered in their entirety without text truncation or ellipsis characters (`...`).

#### Scenario: Tablet clearance between Visual Legend and Minimap
- **GIVEN** a user views `/graph` on a 768px tablet viewport in 2D mode with the sidebar open
- **WHEN** the user expands the Visual Legend panel
- **THEN** the horizontal clearance gap between the right edge of the Visual Legend and the left edge of the Minimap is at least 80 pixels
- **AND** neither component overlaps or occludes the other.

#### Scenario: Interactive legend hover dims non-matching graph nodes
- **GIVEN** the legend panel is expanded
- **WHEN** the user hovers over the "Flashcards" or "Mastered" entry in the legend
- **THEN** all non-matching nodes and edges on the active graph canvas dim to 20% opacity
- **AND** matching flashcard nodes remain fully opaque with prominent glowing accents
- **WHEN** the cursor leaves the legend item
- **THEN** all nodes and edges return to their standard opacity.

#### Scenario: Collapsing and expanding the legend panel with state persistence
- **WHEN** the user clicks the collapse button on the legend header
- **THEN** the legend smoothly transitions into a minimal floating pill button labeled "Legend" (or "Chú Thích")
- **AND** the preference is saved in `localStorage`
- **WHEN** the user reloads the page or navigates back to `/graph`
- **THEN** the collapsed state is automatically preserved.

#### Scenario: Default collapsed state on tablet and mobile viewports
- **GIVEN** a user on a tablet ($768\text{px}$) or mobile device ($< 1024\text{px}$) with no prior `techdaily_graph_legend_collapsed` preference in `localStorage`
- **WHEN** the user navigates to `/graph`
- **THEN** the visual legend initializes in the collapsed state as a floating pill button `[ ? Visual Legend ^ ]`.

### Requirement: Cyber Neon Telemetry HUD & Animated Knowledge Radar
The knowledge graph visualization surfaces SHALL provide an animated knowledge radar telemetry display on the dashboard overview (`/today`). In the dedicated graph studio (`/graph`), the interface SHALL maintain a clean, uncluttered canvas where essential controls (mode switch, search, filters) reside exclusively in `GraphControlBar.vue`, omitting redundant floating telemetry ribbons or duplicate status counters from the viewport.

In the dashboard Bento Grid, the Knowledge Graph Radar card SHALL render an animated cyber radar display featuring concentric polar coordinate rings, a rotating telemetry sweep indicator, reactive cyber cyan (`#22d3ee`) node blips, and status badges (`ONLINE`, `SYNCED`, node and edge density counts).

#### Scenario: Animated knowledge radar display on dashboard
- **WHEN** user views the Bento Dashboard on `/today`
- **THEN** the Knowledge Radar card displays an animated SVG polar radar with rotating sweep needle, concentric coordinate rings, and cyber cyan node blips representing graph density.

#### Scenario: Telemetry HUD overlay in graph studio
- **WHEN** user navigates to `/graph`
- **THEN** the canvas viewport renders cleanly without floating telemetry HUD ribbons or redundant node/edge counters, keeping the upper screen area uncluttered.
#### Scenario: Studio glassmorphic surface polish
- **WHEN** control bars and legends are rendered in the graph studio
- **THEN** they utilize `.glass-panel` styling, hairline borders (`border-white/[0.08]`), and cyber glow highlights (`glow-subtle`) adhering to the dev studio design tokens.

### Requirement: WebGL Shallow Reactivity, OrbitControls Hygiene and Mobile Bottom-Sheet Drawer
The Knowledge Graph 3D and 2D Studio (`pages/graph.vue`, `GraphCanvas3D.vue`, `GraphCanvas.vue`, `GraphDetailDrawer.vue`, `GraphControlBar.vue`) SHALL store WebGL and canvas instances strictly in `shallowRef` to prevent reactive memory overhead, manage OrbitControls event listeners via VueUse hygiene, provide an adaptive touch-swipeable bottom-sheet drawer on mobile viewports $< 768\text{px}$, and clear safe area boundaries.

#### Scenario: Three.js and ForceGraph3D Shallow Reactivity
- **WHEN** user loads the 3D Cosmos view in `GraphCanvas3D.vue`
- **THEN** the Three.js scene, camera, renderer, ForceGraph3D instance, and controls SHALL be encapsulated in `shallowRef`
- **AND** Vue deep reactivity SHALL NOT create proxies over 3D meshes, geometries, or materials.

#### Scenario: OrbitControls Event Teardown
- **WHEN** user navigates away from `pages/graph.vue` or switches between 2D and 3D modes
- **THEN** all OrbitControls listeners (`start`, `end`) and animation frame loops SHALL be safely cancelled and disposed without memory leaks.

#### Scenario: Mobile Bottom-Sheet Detail Drawer
- **WHEN** user taps a graph node on a mobile viewport ($< 768\text{px}$)
- **THEN** the node details SHALL present as a sliding bottom sheet with max height `85dvh`, touch drag handle, and safe area bottom clearance (`env(safe-area-inset-bottom)`)
- **AND** connected topic chips SHALL wrap into 2 columns without horizontal overflow.

#### Scenario: Floating Control Bar Mobile Safe Area
- **WHEN** user interacts with floating canvas control tools (`GraphControlBar.vue`)
- **THEN** the bar SHALL be positioned with safe area clearance (`pb-[max(0.75rem,env(safe-area-inset-bottom))]`) preventing collision with home indicator bars.

### Requirement: Knowledge Graph Explorer Canvas & HUD Clearance Standards
The Knowledge Graph Explorer interface at `/graph` SHALL enforce full-bleed visual canvas bounding and responsive control clearance:

1. **Dynamic Viewport Height Bounding**: The root explorer container SHALL utilize `h-[calc(100dvh-4rem)]` to ensure seamless full-height visualization without vertical double-scrollbars across desktop, tablet, and mobile devices.
2. **HUD Control Bar Anchoring**: The top glassmorphic control bar (`GraphControlBar.vue`) SHALL remain anchored at `top-3 sm:top-4`, constrained to `max-w-4xl mx-auto`, with responsive button padding and touch targets preventing collision with browser edges.
3. **Legend & Minimap Clearance**:
   - The interactive graph legend (`GraphLegend.vue`) SHALL dock at the bottom-left (`bottom-[calc(1.25rem+env(safe-area-inset-bottom))] left-3 sm:left-5`) with `pointer-events-none` container and `pointer-events-auto` content.
   - The locator minimap (`GraphMinimap.vue`) SHALL dock at the bottom-right (`bottom-[calc(1rem+env(safe-area-inset-bottom))] right-4`) on desktop/tablet (>= 640px) and remain hidden on small mobile viewports to prevent viewport clutter.
4. **Slide-Over Detail Drawer Integration**: The node detail inspection drawer (`GraphDetailDrawer.vue`) SHALL render with backdrop blur and responsive docking without colliding with the top control bar or bottom legend.

#### Scenario: Full-bleed graph display on high-DPI displays
- **WHEN** user opens `/graph` on a 1080p, 2K, or high-DPI monitor
- **THEN** the Cytoscape 2D and Three.js 3D canvases fill the entire available viewport height without page scrolling
- **AND** floating control decks remain cleanly anchored above the canvas.

#### Scenario: Responsive legend docking on mobile viewports
- **WHEN** user interacts with `/graph` on a mobile device (< 640px)
- **THEN** the minimap is hidden to preserve screen real estate
- **AND** the legend bar respects safe area insets without overlapping system navigation gestures.
