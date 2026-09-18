## MODIFIED Requirements

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

The canvas stylesheet SHALL dynamically synchronize with `@nuxtjs/color-mode`, updating node fills, borders, labels, and edge opacities when the user switches between dark and light themes without requiring a page reload.

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
