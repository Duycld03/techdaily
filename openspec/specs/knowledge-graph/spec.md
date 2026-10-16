# knowledge-graph Specification

## Purpose
Provides an interactive associative knowledge graph extracting relational learning artifacts (curriculum topics, user books, highlights, flashcards) for the authenticated user, rendered via 2D Canvas and 3D WebGL visualizers with multi-dimensional filtering and deep navigation bridges.

## Requirements

### Requirement: Knowledge Graph Relational Extraction API
The system SHALL expose protected `GET /api/v1/graph` returning a personal graph derived strictly from the authenticated user's relational learning artifacts, without auxiliary graph databases or continuous server-side vector calculations. It SHALL represent the user's imported books, referenced chunks, personal highlights, and spaced-repetition cards, never another user's content.

The projection SHALL support every nondeleted category referenced by surviving user artifacts, including inactive and administrator-created categories, without a fixed pillar count. Authentication SHALL return HTTP 401 with problem details when missing. All node queries SHALL honor soft deletion.

The response SHALL contain:
1. `nodes`:
   - Card nodes owned by the user: `id`, `label`, `documentChunkId`, retention `status`, `intervalDays`, `easeFactor`, and `repetitionCount`.
   - Highlight nodes owned by the user: `id`, truncated quote `label`, `documentChunkId`, `bookId`, `note`, `tags`, and `createdAt`.
   - Book nodes for the user's published, nondeleted imported books only: `id`, `label`, `categoryId`, authoritative category descriptor, `totalChunks`, and `authorOrSourceUrl`.
   - Chunk nodes belonging to surviving user books: `id`, chapter `label`, `bookId`, `chunkOrder`, and `summary`. A chunk SHALL appear only when referenced by the user's card or highlight, not merely because it exists.
   - Category pillar hubs: `id` equal to `pillar-{categoryId}`, current bilingual category metadata, and `type: "pillar"`. A hub SHALL appear only when anchoring a surviving user book or classified card. Unknown source provenance SHALL NOT fabricate an assignment or empty hub.
2. `edges`, whose endpoints SHALL both occur in `nodes`:
   - `BookToPillar`: book to its own `pillar-{categoryId}`.
   - `ChunkToBook`: referenced chunk to its parent book.
   - `CardToChunk`: card to its surviving source chunk.
   - `CardToHighlight`: highlight-sourced card to its source highlight.
   - `CardToPillar`: otherwise unlinked card to its authoritative source question's category hub when category provenance exists.
   - `HighlightToChunk` and `HighlightToBook`: highlight to its surviving chunk and book.
   - `SharedTag`: highlights sharing normalized tag keywords.
3. `stats`: `totalNodes`, `totalEdges`, `nodeTypeCounts` for pillar/book/chunk/card/highlight, and `pillarCounts` keyed by category UUID.

The query SHALL use a consolidated no-tracking read with foreign-key indexes to keep PostgreSQL query time low. The existing personal-graph payload budget of well under 100 KB SHALL remain unchanged. Global category metadata SHALL NOT expose other users' activity or produce unused hubs.

#### Scenario: Unauthenticated request to knowledge graph endpoint
- **WHEN** an unauthenticated client requests the graph
- **THEN** HTTP 401 problem details are returned.

#### Scenario: Authenticated user requests knowledge graph
- **WHEN** an authenticated user with books, highlights, and cards requests the graph
- **THEN** HTTP 200 contains their own books, referenced chunks, cards and highlights, with source and shared-tag edges
- **AND** other users' artifacts are excluded.

#### Scenario: New user with zero flashcards or highlights requests graph
- **WHEN** a user with no imported books, cards or highlights requests the graph
- **THEN** all node/edge collections are empty without null errors
- **AND** the client renders an empty state, not seeded curriculum hubs.

#### Scenario: Flashcard created from highlight links to highlight node
- **WHEN** a user has a highlight-sourced card with a surviving source highlight
- **THEN** `CardToHighlight` connects them and places the card in its source highlight cluster.

#### Scenario: Flashcard sourced from a document chunk links to chunk node
- **WHEN** a user's card references a surviving chunk of their imported book
- **THEN** `CardToChunk` connects card to chunk and `ChunkToBook` connects chunk to parent book.

#### Scenario: Flashcard with no linked topic or highlight links to pillar hub
- **WHEN** a user's card has no linked chunk/highlight but has an authoritative source question category UUID
- **THEN** `CardToPillar` targets that category's emitted hub rather than a default category.

#### Scenario: Shared tag associative edge generation
- **WHEN** two user highlights share normalized tag `mvcc`
- **THEN** a directed or bidirectional `SharedTag` edge connects them with that label.

#### Scenario: Authenticated user receives pillar hub nodes and guaranteed connected topics
- **WHEN** a user's artifacts cover only a subset of categories
- **THEN** HTTP 200 contains hubs only for categories represented by surviving user books or classified cards
- **AND** every book has its `BookToPillar` edge and emitted book/chunk nodes are connected.

#### Scenario: Universal multi-disciplinary book connections
- **WHEN** a book covers multiple subjects
- **THEN** it connects only to its assigned category hub and its referenced chunks
- **AND** no automatic fan-out to other category hubs occurs.

#### Scenario: Specific book-to-topic linking without Cartesian blowout
- **WHEN** a Backend book covers GC and allocation in several chunks
- **THEN** only its own referenced chunks receive `ChunkToBook` edges
- **AND** sharing a category UUID does not create Cartesian edges to unrelated books or chunks.

#### Scenario: Only user-imported books appear as book nodes
- **WHEN** a user has one imported book and another account has a separate published book
- **THEN** only the user's book appears.

#### Scenario: Only touched curriculum topics appear
- **WHEN** the user references Chunk A but has no card/highlight referencing Chunk B
- **THEN** A appears with its parent-book edge and B is absent.

#### Scenario: Deleted highlight or flashcard is excluded from the graph
- **WHEN** the user soft-deletes a highlight or card and reloads
- **THEN** the deleted node, its edges, and hubs/chunks with no surviving anchors are omitted.

#### Scenario: Authenticated user receives DeepPace pillar hub nodes and guaranteed connected topics
- **WHEN** the user's artifacts include Mental Models, Habits, or an administrator-created category
- **THEN** each represented category has a hub identified by its UUID and labeled from current metadata
- **AND** all book/card-to-hub edges have existing endpoints without a seven-category ceiling.

#### Scenario: No dangling edges for non-technical learning artifacts
- **WHEN** the user's books/cards cover non-technical subjects or inactive categories
- **THEN** every edge endpoint exists and no edge references an un-emitted pillar.

#### Scenario: Rename and deactivation preserve graph identity
- **WHEN** an administrator renames or deactivates a referenced category
- **THEN** refreshed labels/status change but `pillar-{categoryId}`, artifact IDs and edge endpoints remain stable.

### Requirement: Client-Side Canvas 2D Force Layout Visualization
The client SHALL provide `/graph` Canvas 2D visualization with client-side asynchronous force layout, not server-side physics. Existing random initialization, component spacing 120, node repulsion 500000, ideal edge length 120, cooling factor 0.95 and 300-iteration bounds SHALL preserve separation and settle within 1.5 seconds, then stop idle simulation.

Node geometry SHALL remain: prominent 54x54px pillar circles with 3.5px neon halo, 18px blur, bold 13px labels and highest stacking rank; 30x30px chunk circles; indigo 34x26px rounded book rectangles with 6px corners/bookmark/source emblems; 26x26px card diamonds; cyan 22x22px highlight diamonds. Card retention colors SHALL remain Learning amber, Reviewing blue, and Mastered brand violet.

Pillar and chunk accents SHALL come from one deterministic palette keyed by stable category UUID, shared by 2D, 3D, detail badges, and legend. No subject-name selector or Backend fallback SHALL be needed for new categories. Rename and reorder SHALL NOT change a category's palette assignment.

The viewport SHALL retain dark obsidian `#09090b`, dark hairline `#27272a` borders/edges, and live dark/light synchronization without refetching or resetting coordinates. Mouse/touch pan, 0.2x–3.0x zoom, box selection, dragging, and double-click/button fit SHALL remain available.

#### Scenario: Graph initialization and physics auto-settle
- **WHEN** a user opens `/graph`
- **THEN** the personal nodes/edges initialize, animate into clusters, and stop physics within 1.5 seconds.

#### Scenario: Level-of-Detail label decluttering at overview zoom
- **WHEN** zoom is below 1.1x
- **THEN** card/highlight labels are hidden while hub/book/chunk labels remain legible.

#### Scenario: Card label reveals on hover or selection
- **WHEN** a card is hovered or tapped
- **THEN** its hidden label and connecting edge are revealed
- **AND** unselecting restores overview decluttering.

#### Scenario: Viewport pan and zoom controls
- **WHEN** the user wheels or pinches
- **THEN** zoom stays within 0.2x–3.0x around the pointer
- **WHEN** Fit to Screen is clicked
- **THEN** all visible nodes fit with comfortable padding.

#### Scenario: Dark/light mode theme synchronization
- **WHEN** theme changes
- **THEN** labels, grid contrast, and edges update without refetching data or resetting coordinates.

#### Scenario: Pillar node rendering and visual prominence
- **WHEN** the canvas renders
- **THEN** hubs retain their larger geometry, bold label, halo, and surrounding book constellation.

#### Scenario: Highlight and Book node colors unified across renderers
- **WHEN** highlights and books render in 2D
- **THEN** highlights use cyan `#06b6d4` and books indigo `#6366f1`, matching 3D/legend rather than legacy violet/slate.

#### Scenario: Stable CoSE layout without collapsed horizontal stacking
- **WHEN** a book has referenced chunks but no user highlights
- **THEN** source edges anchor chunks to the book and book to hub without collapsed perimeter stacking.

#### Scenario: Obsidian 2D canvas background and hairline border styling
- **WHEN** dark mode renders
- **THEN** the viewport uses obsidian `#09090b` with `#27272a` hairline node borders and edges.

#### Scenario: DeepPace pillar and chunk node rendering on 2D canvas
- **WHEN** Mental Models, Habits, and a newly created subject render
- **THEN** all use the same UUID-keyed palette algorithm and normal hub geometry/halo
- **AND** chunks inherit their category accent with no .NET/Backend fallback or special-case dictionary.

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The graph SHALL provide a floating glassmorphic control bar with client-side filtering that does not fetch data on each interaction:
1. 2D/3D engine switching SHALL preserve filters and persist `deeppace_graph_view_mode`.
2. Category filtering SHALL expose All and every category represented in the personal graph, including inactive categories. Labels SHALL use stored bilingual metadata and selection SHALL use UUIDs. Responsive wrapping or an accessible bounded overflow control SHALL keep all categories reachable without a fixed eight-pill requirement.
3. Type toggles SHALL control Books, Chunks, Flashcards and Highlights.
4. Mastery filtering SHALL retain All/Learning/Reviewing/Mastered, with brand-violet Mastered styling.
5. Live search SHALL match titles, tags, and summaries, highlighting matches and fading others/edges to 15% in both engines.
6. Reset SHALL restore all filters, search, and opacity.
7. Fit/Reset actions SHALL retain the compact 32px height, text/icon scale, padding and baseline of the engine switcher.

Controls SHALL retain existing glass/obsidian surface and hairline design tokens rather than legacy slate surfaces. Category selection and inactive status SHALL be programmatically available. Existing DOM controls and the detail drawer SHALL expose represented categories and counts with keyboard filtering/detail access in either engine; canvas labels and category colors alone SHALL NOT be the only equivalent identification.

#### Scenario: Switching between 2D and 3D view modes
- **WHEN** the user selects 3D Cosmos
- **THEN** 2D is hidden/unmounted and 3D mounts with current category/type filters
- **AND** the engine choice persists for subsequent visits.

#### Scenario: Filtering by pillar category
- **WHEN** a category chip is selected
- **THEN** nodes from other categories are hidden and the viewport focuses on the selected UUID's cluster.

#### Scenario: Toggling node types via type filters
- **WHEN** a Books, Chunks, Flashcards or Highlights toggle is disabled
- **THEN** that type and connecting edges disappear and no obsolete Topics toggle appears.

#### Scenario: Filtering by flashcard mastery level
- **WHEN** Mastered is selected
- **THEN** Learning/Reviewing cards are hidden and cards with interval at least 21 days remain with their connected chunks.

#### Scenario: Live search node focus
- **WHEN** the user searches MVCC
- **THEN** matches remain highlighted and unrelated nodes/edges fade to 15%
- **AND** Enter or selecting a match focuses it at 1.5x zoom.

#### Scenario: Resetting filters
- **WHEN** Reset is clicked
- **THEN** all type/category/mastery/search controls return to default and nodes/edges return to full visibility.

#### Scenario: Bilingual responsive category pills wrapping and complete localization
- **WHEN** the category controls render in English or Vietnamese
- **THEN** current stored names render without raw enum names or clipped actions
- **AND** all represented categories remain reachable on narrow viewports, regardless of category count.

#### Scenario: Action buttons match 2D/3D toggle height and typography
- **WHEN** the control bar renders
- **THEN** Fit and Reset match the engine toggle's compact height, typography, icons and horizontal baseline.

#### Scenario: DeepPace category filtering in control bar
- **WHEN** the user chooses Mental Models, Habits, or an administrator-created category
- **THEN** other categories are filtered out and the selected constellation is focused without clipped or overflowing category controls.

#### Scenario: Category details are available without canvas interaction
- **WHEN** a keyboard or assistive-technology user selects a represented category through the DOM controls in either engine
- **THEN** the selected UUID, current name/slug, inactive status and available category counts are exposed through controls/detail content
- **AND** duplicate names or palette collisions remain distinguishable without hover-only disclosure.

#### Scenario: Long category controls reflow without losing choices
- **WHEN** 100-character or duplicate English/Vietnamese names appear on mobile or at effective 320 CSS-pixel width
- **THEN** every represented category remains reachable, its selection is announced, and immutable slugs disambiguate names
- **AND** full labels are obtainable with keyboard/click while compact badges/actions keep no-wrap behavior and do not collide.

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
The client SHALL provide lazy-loaded `/graph` WebGL 3D visualization with client-side calculations and no backend physics/memory workload. Dark backgrounds SHALL remain obsidian `#09090b`; billboard backgrounds SHALL use elevated `rgba(18,18,21,0.85)`, not legacy slate.

The cosmos SHALL preserve luminous category hubs, category-colored chunk planets, emblem-accented books, retention-colored compact cards, highlight satellites, and relational 3D edges. Category accents SHALL use the same UUID-keyed palette as 2D.

Orbit controls SHALL support full rotation, pan, pinch zoom and camera reset. Auto-Rotate SHALL orbit at the current altitude/distance, pause on dragging, and stop on toggle. Overview labels SHALL be limited to represented category hubs, not five fixed pillars; book/chunk/card/highlight labels SHALL reveal at close range below distance 250, on hover/tap, or with the HUD label toggle. The simulation SHALL stop within 120 warmup ticks and render on demand when idle. Selecting a node SHALL animate focus and open the detail drawer.

#### Scenario: 3D Galaxy visualization initialization
- **WHEN** 3D Cosmos is activated
- **THEN** the engine loads lazily without blocking route transition and initializes spherical nodes in 3D
- **AND** physics settles within 120 ticks without freezing the UI.

#### Scenario: 360-degree camera orbital navigation
- **WHEN** the user drags the canvas
- **THEN** the camera rotates smoothly around the focus at 60 FPS and wheel zoom scales distance along the line of sight.

#### Scenario: 360-degree camera auto-orbit rotation
- **WHEN** Auto Rotate is enabled
- **THEN** its indicator activates and the camera orbits the constellation center consistently
- **WHEN** it is disabled
- **THEN** motion stops at the current vantage point.

#### Scenario: Distance-based Level-of-Detail label culling
- **WHEN** the camera shows the galaxy overview
- **THEN** non-hub labels are culled and represented category hub labels are eligible without a fixed count
- **WHEN** the user zooms close or hovers a node
- **THEN** its high-contrast label faces the camera and becomes visible.

#### Scenario: Toggling all labels visibility via HUD control
- **WHEN** the HUD label toggle is clicked
- **THEN** it switches between Hubs Only and All Visible Nodes labels.

#### Scenario: 3D node selection and detail drawer sync
- **WHEN** a 3D node is selected
- **THEN** the camera focuses it and the same details/action bridges as 2D appear.

#### Scenario: Render-on-demand battery preservation
- **WHEN** layout has settled and no camera/animation activity remains
- **THEN** active frame rendering stops until interaction resumes, retaining negligible idle CPU/GPU work.

#### Scenario: Obsidian 3D galaxy canvas background and elevated billboard styling
- **WHEN** dark mode renders
- **THEN** the background remains obsidian and labels use elevated translucent backgrounds with hairline borders.

### Requirement: Interactive Visual Graph Legend & Entity Guide
The graph SHALL retain a floating, collapsible bottom-left legend describing node geometries/scales, dynamic category accents, and SM-2 retention in 2D/3D. It SHALL show:
1. Localized Visual Legend title, layers glyph and collapse control.
2. Unabridged single-column entity rows with live `stats.nodeTypeCounts`, omitting missing counts rather than showing undefined/NaN. Swatches SHALL match ringed/luminous category hubs, circular/planetary topics, rounded/indigo books, and diamond/cyan highlights. Category hues SHALL match the shared UUID palette, not five canonical pillars.
3. Complete retention labels/intervals: Learning below 6 days amber, Reviewing 6–20 days blue, Mastered at least 21 days brand violet `#7c3aed`, not legacy emerald.
4. Hover isolation: matching nodes stay opaque, others/edges fade to 15% in both engines; leaving restores normal opacity.
5. Localized hover-to-isolate hint and L shortcut chip.
6. A 224px expanded card with at least 80px clearance from the minimap at 768px with sidebar open. Below 1024px it SHALL default collapsed when no preference exists; `deeppace_graph_legend_collapsed` SHALL persist the choice.

#### Scenario: Legend displays entity hierarchy and SM-2 status color keys
- **WHEN** the bottom-left legend is viewed in either engine
- **THEN** geometry-matched entity and retention swatches/descriptions display with live node-type counts.

#### Scenario: Entity rows show node-type counts from the graph payload
- **WHEN** the graph reports pillar/topic/book/highlight counts of 5/48/19/184
- **THEN** those numbers display in the corresponding rows and unavailable counts are omitted.

#### Scenario: Mastered retention swatch matches canvas violet fill
- **WHEN** the expanded retention legend is inspected
- **THEN** Mastered uses the same brand violet as the canvas and no emerald mastered swatch appears.

#### Scenario: Complete unclipped legend text in English and Vietnamese
- **WHEN** the expanded legend is inspected in either locale
- **THEN** entity and retention labels display completely, including Curriculum Topic and Personal Note/Highlight translations, without ellipsis.

#### Scenario: Tablet clearance between Visual Legend and Minimap
- **WHEN** the legend is expanded at 768px with sidebar open
- **THEN** legend/minimap clearance is at least 80px and neither occludes the other.

#### Scenario: Interactive legend hover dims non-matching graph nodes
- **WHEN** Highlights or Mastered is hovered
- **THEN** nonmatching nodes/edges fade to 15% and matches keep their glowing accents
- **WHEN** hover ends
- **THEN** normal opacity is restored.

#### Scenario: Collapsing and expanding the legend panel with state persistence
- **WHEN** the legend is collapsed
- **THEN** it becomes a localized Visual Legend pill and saves the preference
- **WHEN** the page is revisited
- **THEN** the collapsed state is restored.

#### Scenario: Default collapsed state on tablet and mobile viewports
- **WHEN** a user below 1024px has no stored legend preference
- **THEN** the legend starts as a collapsed localized pill.

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
