# Spec Delta: Knowledge Graph

## MODIFIED Requirements

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
  - Data Storage: Emerald (`#059669` / `#34d399`)
  - Distributed Systems: Violet/Purple (`#7c3aed` / `#a78bfa`)
  - Frontend Engineering: Amber/Orange (`#d97706` / `#fbbf24`)
  - Engineering Craft: Rose (`#e11d48` / `#fb7185`)
- **Book Nodes:** Rounded rectangular nodes displaying book source emblems.
- **Card Nodes:** Diamond-shaped nodes color-coded by SM-2 retention status:
  - Learning: Amber (`#f59e0b`)
  - Reviewing: Blue (`#3b82f6`)
  - Mastered: Emerald (`#10b981`)
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

### Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization
The client application SHALL provide an alternative 3D interactive knowledge graph visualization at `/graph` rendered with WebGL (via Three.js / `3d-force-graph`), executing calculations entirely on the client-side GPU without placing computational or memory load on the backend server.

The 3D WebGL cosmos canvas background SHALL strictly render with neutral dark obsidian `#09090b` in dark mode, completely eliminating bluish slate backgrounds (`#020617`). 3D sprite billboard label backgrounds in dark mode SHALL utilize elevated obsidian `rgba(18, 18, 21, 0.85)` (`dark:bg-canvas-elevated`), eliminating legacy slate boxes (`rgba(15, 23, 42, 0.85)`).

The 3D visualization SHALL represent architectural entities in an interactive spherical cosmos:
1. **Pillar Hub Nodes:** Rendered as glowing primary celestial bodies with large radii and pillar-specific emissive glow colors.
2. **Topic Nodes:** Rendered as medium planetary spheres color-coded by their parent engineering pillar category.
3. **Book Nodes:** Rendered as textured or emblem-accented spherical bodies orbiting their parent pillar hubs.
4. **Card Nodes:** Rendered as compact glowing spheres color-coded by SM-2 retention status (Learning: amber, Reviewing: blue, Mastered: emerald).
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

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `techdaily_graph_view_mode`.
2. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`). The filter container SHALL employ a responsive wrapping layout (`flex-wrap gap-1.5`) without hidden scrollbars or box-model clipping across both English and Vietnamese locales, ensuring that all 6 pill options remain 100% visible and discoverable. All category pills SHALL resolve explicit localization keys without falling back to raw untranslated strings.
3. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Topics`, `Books`, `Flashcards`, `Highlights`).
4. **Mastery Status Filter:** Dropdown or pill selector to filter flashcard nodes by SM-2 status (`All`, `Learning`, `Reviewing`, `Mastered`).
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
