# Spec Delta

## MODIFIED Requirements

### Requirement: Interactive Visual Graph Legend & Entity Guide
The knowledge graph view SHALL feature a floating, collapsible visual legend panel (`GraphLegend.vue`) positioned in the bottom-left viewport corner (`bottom-5 left-5`), providing an intuitive visual key for all node geometries, relative scales, category colors, and SM-2 retention metrics across both 2D and 3D view modes.

The legend panel SHALL present:
1. **Header:** A title labeled "Visual Legend" (`en-US`) / "Bảng Chú Thích" (`vi-VN`) with a layers glyph, and a collapse control.
2. **Entity Hierarchy Section:**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering unclipped unabridged text labels for every entity type without using text truncation or ellipsis characters across both English and Vietnamese locales.
   - Each entity row SHALL display the count for that node type sourced from the graph payload `stats.nodeTypeCounts`. When a count is unavailable the row SHALL omit the number rather than render `undefined` or `NaN`.
   - Each entity row's swatch SHALL mirror the on-canvas node geometry for that type:
     - **Pillar Hubs:** Ringed filled circle (2D) / Luminous cosmic hubs (3D) color-coded by the 5 canonical engineering pillars.
     - **Topics:** Smaller filled circle (2D) / Planetary spheres (3D) color-coded by parent pillar ("Curriculum Topic" / "Chủ đề giáo trình").
     - **Books:** Rounded-rectangle swatch (2D) / Indigo spheres (3D) representing ingested documentation ("Tech Book" / "Sách kỹ thuật").
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
- **THEN** `GraphLegend.vue` displays geometry-matched swatches and descriptions for Pillar Hubs, Topics, Books, Highlights, and SM-2 Flashcard retention states (Learning, Reviewing, Mastered)
- **AND** each entity row shows its live node-type count sourced from `stats.nodeTypeCounts`.

#### Scenario: Entity rows show node-type counts from the graph payload
- **GIVEN** the loaded graph payload reports `stats.nodeTypeCounts` of `{ pillar: 5, topic: 48, book: 19, highlight: 184 }`
- **THEN** the Pillar, Topic, Book, and Highlight entity rows display `5`, `48`, `19`, and `184` respectively
- **AND** a node type whose count is missing renders no number rather than `undefined` or `NaN`.

#### Scenario: Mastered retention swatch matches canvas violet fill
- **GIVEN** a user views the expanded legend
- **WHEN** inspecting the SM-2 retention section
- **THEN** the "Mastered (≥ 21 days)" indicator renders brand violet `#7c3aed`, identical to the mastered-flashcard fill on the canvas
- **AND** no emerald `#10b981` mastered swatch is present.

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
- **Topic Nodes:** Circular nodes ($36\times 36\text{px}$) carrying a `Day N` curriculum-index badge and color-coded by their engineering pillar:
  - Backend Runtime: Cyan/Sky (`#0284c7` / `#38bdf8`)
  - Data Storage: Cyan/Teal (`#0891b2` / `#22d3ee`)
  - Distributed Systems: Violet/Purple (`#7c3aed` / `#a78bfa`)
  - Frontend Engineering: Amber/Orange (`#f59e0b` / `#fbbf24`)
  - Engineering Craft: Pink/Rose (`#ec4899` / `#fb7185`)
- **Book Nodes:** Rounded-rectangle nodes ($34\times 26\text{px}$, corner radius $6\text{px}$) in an indigo tone (`#6366f1`) with an internal bookmark stroke, displaying book source emblems.
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
- **THEN** each pillar hub node is rendered at $54\times 54\text{px}$, larger than topic ($36\times 36\text{px}$), book ($34\times 26\text{px}$), card ($26\times 26\text{px}$), and highlight ($22\times 22\text{px}$) nodes
- **AND** the pillar hub node displays a bold label and distinct accent halo
- **AND** connected topics form a surrounding orbital constellation around their parent pillar hub.

#### Scenario: Highlight and Book node colors unified across renderers
- **WHEN** the 2D canvas renders Highlight and Book nodes
- **THEN** Highlight nodes render cyan `#06b6d4` and Book nodes render indigo `#6366f1`, matching the legend swatches and the 3D renderer
- **AND** no Highlight node renders the legacy violet fill and no Book node renders the legacy slate fill.

#### Scenario: Stable CoSE layout without collapsed horizontal stacking
- **WHEN** a user navigates to `/graph` with categories that contain zero book nodes and zero user highlights
- **THEN** the topics belonging to those categories remain anchored to their respective Pillar Hub via `TopicToPillar` edges
- **AND** the CoSE simulation settles without stacking unconnected nodes into a horizontal line at the viewport perimeter.

#### Scenario: Obsidian 2D canvas background and hairline border styling
- **WHEN** the 2D canvas renders in dark mode
- **THEN** the canvas container background renders with neutral obsidian `#09090b` (`dark:bg-canvas`)
- **AND** node borders and edges in dark mode utilize `#27272a` hairline styling rather than legacy opaque slate colors.

## ADDED Requirements

### Requirement: Knowledge Graph Synapse Edge Styling
The knowledge graph SHALL render relational edges with a distinct visual style per relation-type family, applied consistently across the 2D Cytoscape canvas (`GraphCanvas.vue`) and the 3D WebGL cosmos (`GraphCanvas3D.vue`), so users can distinguish structural, atomic, and associative links at a glance.

Every edge relation type SHALL resolve to exactly one of three style families:
1. **Solid Constellation** — structural anchor links `TopicToPillar` and `BookToPillar`:
   - Rendered as a smooth Bézier curve (curve-style bezier) with curvature `0.35`, line width `2.0px`, solid line style, and active opacity `0.85`.
2. **Dotted Synapse** — atomic card links `CardToHighlight`, `CardToPillar`, and `CardToTopic`:
   - Rendered as a dotted line with dash pattern `[3, 4]`, line width `1.2px`, and active opacity `0.60`.
3. **Shared-Tag Dashed** — associative links `SharedTag` (and, as the default family for any relation type not named above — `BookToTopic`, `HighlightToBook`, `HighlightToTopic`):
   - Rendered as a bidirectional dashed line with dash pattern `[4, 4]`, line width `1.5px`, and a directional particle-pulse rate of `0.007`.

In the 3D renderer, active edges SHALL animate directional particles at pulse rate `0.007`, and idle edges SHALL carry none. In the 2D Cytoscape renderer, families are distinguished by line style, width, dash pattern, and curvature; any particle-flow affordance SHALL run only while an edge is active, selected, or hovered and SHALL cease when the canvas is idle, preserving the 0% idle-CPU guarantee. When any filter, live-search, or legend-hover dimming is active, non-matching edges SHALL dim to 15% opacity (`0.15`).

#### Scenario: Solid constellation edges anchor structural links
- **WHEN** the canvas renders a `TopicToPillar` or `BookToPillar` edge
- **THEN** the edge is drawn as a Bézier curve with curvature `0.35`, line width `2.0px`, solid style, and opacity `0.85` when active.

#### Scenario: Dotted synapse edges connect atomic card links
- **WHEN** the canvas renders a `CardToHighlight`, `CardToPillar`, or `CardToTopic` edge
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
