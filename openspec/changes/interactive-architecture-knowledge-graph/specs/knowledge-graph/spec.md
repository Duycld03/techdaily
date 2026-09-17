## ADDED Requirements

### Requirement: Knowledge Graph Relational Extraction API
The system SHALL expose a protected HTTP GET endpoint `GET /api/v1/graph` that extracts and returns an associative architecture knowledge graph for the authenticated user, derived strictly from relational entities in PostgreSQL 17 without introducing auxiliary graph databases or continuous server-side vector calculations.

The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and SHALL return `HTTP 401 Unauthorized` with RFC 7807 problem details when invoked without a valid token.

The returned response payload (`KnowledgeGraphResponse`) SHALL consist of:
1. `nodes`: An array of `GraphNodeDto` items representing:
   - **Topic Nodes:** Master curriculum topics from `Topics`, containing `id`, `label`, `category` (Pillar category), `dayOrder`, `summary`, and `difficulty`.
   - **Book Nodes:** Published engineering books from `DocumentBooks`, containing `id`, `label`, `category`, `totalChunks`, and `authorOrSourceUrl`.
   - **Card Nodes:** Spaced repetition flashcards from `SpacedRepetitionCards` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label`, `topicId`, `status` (`Learning`, `Reviewing`, `Mastered`), `intervalDays`, `easeFactor`, and `repetitionCount`.
   - **Highlight Nodes:** Personal reading highlights from `UserHighlights` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label` (truncated quote), `documentChunkId`, `bookId`, `note`, `tags`, and `createdAt`.
2. `edges`: An array of `GraphEdgeDto` items representing relational and associative connections:
   - `TopicToPillar`: Connecting topics to their parent architectural category pillar.
   - `CardToTopic`: Connecting active user flashcards to their target curriculum topic (`Card.TopicId -> Topic.Id`).
   - `BookToTopic`: Connecting document books to curriculum topics sharing matching category and domain keywords.
   - `HighlightToBook`: Connecting user highlights to their source document book via `DocumentChunk.DocumentBookId`.
   - `HighlightToTopic`: Connecting user highlights to curriculum topics when highlight tags match topic slugs or titles.
   - `SharedTag`: Connecting user highlights to other highlights that share one or more normalized tag keywords.
3. `stats`: Metadata containing `totalNodes`, `totalEdges`, `nodeTypeCounts`, and `pillarCounts`.

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

#### Scenario: Shared tag associative edge generation
- **WHEN** an authenticated user has two highlights that both contain the tag `"mvcc"`
- **THEN** the backend graph projection derives a bidirectional or directed edge between the two highlight nodes with `relationType: "SharedTag"` and `label: "mvcc"`.

---

### Requirement: Client-Side Canvas 2D Force Layout Visualization
The client application SHALL provide an interactive graph visualization at `/graph` using Canvas 2D rendering powered by Cytoscape.js, executing physics layout calculations entirely inside the user's browser without placing computational load on the VPS.

The visualization SHALL execute an asynchronous force-directed layout (such as CoSE or Web Worker-driven simulation) upon mounting, with a physics cooling parameter that settles and halts all movement within 1.5 seconds. Once settled, the canvas SHALL transition to static interactive mode, consuming 0% ongoing CPU or GPU cycles when idle.

The canvas SHALL visually differentiate node types and retention status:
- **Topic Nodes:** Elliptical nodes color-coded by their engineering pillar:
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

#### Scenario: Viewport pan and zoom controls
- **WHEN** a user scrolls the mouse wheel or pinches the touch screen on the canvas
- **THEN** the canvas smoothly zooms within bounds ($0.2\times$ to $3.0\times$) centered on the cursor position
- **WHEN** the user clicks the "Fit to Screen" button
- **THEN** the canvas animates viewport bounds to display all visible nodes with comfortable padding.

#### Scenario: Dark/light mode theme synchronization
- **WHEN** the user switches application theme from dark to light mode via `ThemeToggle.vue`
- **THEN** the graph canvas immediately updates node labels, background grid contrast, and edge line colors to match the light mode palette without re-fetching graph data or resetting node coordinates.

---

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions without triggering backend network requests:
1. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`).
2. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Topics`, `Books`, `Flashcards`, `Highlights`).
3. **Mastery Status Filter:** Dropdown or pill selector to filter flashcard nodes by SM-2 status (`All`, `Learning`, `Reviewing`, `Mastered`).
4. **Live Search Input:** Text input that dynamically matches node titles, tags, and summary keywords. Matching nodes SHALL remain fully opaque and highlighted, while non-matching nodes SHALL fade to 15% opacity with edges dimmed.
5. **Reset Filters CTA:** A button to immediately reset all filters, search inputs, and node opacities back to the default global view.

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

---

### Requirement: Node Detail Slide-Over Drawer & 1-Click Action Bridges
Selecting any node on the graph canvas SHALL open a responsive slide-over drawer (`GraphDetailDrawer.vue`) on desktop ($\ge 768\text{px}$) or bottom sheet on mobile ($< 768\text{px}$) displaying contextual details and 1-click action bridges into the corresponding platform feature.

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

---

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
