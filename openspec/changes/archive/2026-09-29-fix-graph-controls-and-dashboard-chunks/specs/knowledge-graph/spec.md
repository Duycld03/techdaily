# Spec Delta

## MODIFIED Requirements

### Requirement: Multi-Dimensional Graph Control Bar

The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `techdaily_graph_view_mode`.
2. **Action Controls Visual Alignment (Fit to Screen & Reset Filters):** Dedicated action buttons ("Fit to Screen", "Reset Filters") providing 1-click viewport centering and filter clearance SHALL share a unified compact rendered height (`h-8`, 32px), typography scale (`text-xs font-bold`), icon dimensions (`w-3.5 h-3.5`), and padding (`px-2.5 py-1.5`) matching the exact visual footprint and vertical baseline of the adjacent 2D/3D view mode switcher container.
3. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`). Selecting a pillar SHALL immediately filter visible nodes and edges on the canvas.
4. **Node Type Filter:** Filter chips allowing the user to view all node types or isolate a specific type (`All`, `Topics`, `Books`, `Highlights`, `Flashcards`).
5. **SM-2 Mastery Filter:** Filter chips allowing the user to filter cards by retention stage (`All`, `Learning`, `Reviewing`, `Mastered`).
6. **Search Input:** A real-time debounce search bar filtering nodes by title, label, or summary. Matching nodes SHALL be highlighted on the canvas while non-matching nodes are dimmed.

#### Scenario: Action buttons match 2D/3D toggle height and typography
- **WHEN** a user views the knowledge graph control bar on `/graph`
- **THEN** the "Fit to Screen" and "Reset Filters" buttons have an identical height (`h-8`, 32px), typography scale (`text-xs font-bold`), and icon size (`w-3.5 h-3.5`) aligning horizontally with the 2D/3D mode switcher.
