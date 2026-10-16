# Spec Delta

## MODIFIED Requirements

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
