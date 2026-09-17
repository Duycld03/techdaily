# Proposal: Interactive Architecture Knowledge Graph

## Why

### Problem Statement
TechDaily empowers software engineers to build enduring technical mastery through daily reading slices, spaced repetition flashcards, senior scenario drills, curated tech insights, and structured curriculum roadmaps. However, these features currently operate in isolated functional silos across distinct pages:
- `/today`: Daily 3–5 minute reading slice and scenario challenge.
- `/roadmap`: 30-day senior curriculum milestone progression.
- `/library`: Curated and imported engineering books and documents.
- `/review`: Spaced repetition flashcard deck and retention scheduler.
- `/notes`: User highlights and personal annotations.
- `/insights`: Infinite feed of senior engineering trade-offs and runtime mechanics.
- `/quiz`: Topic-based active recall tests.

In a senior engineer's mental model, technical concepts are never isolated. Runtime memory management (.NET CLR Generational Garbage Collection and Large Object Heap) directly influences database connection pooling and query memory limits (PostgreSQL `work_mem` and `shared_buffers`). These database mechanics in turn dictate distributed architectural patterns (Transactional Outbox, Event Sourcing, Idempotent Consumers), which ultimately impact frontend critical rendering paths and browser network waterfalls.

Today, engineers using TechDaily cannot visualize how their highlighted quotes, personal notes, flashcard mastery levels, and book chapters cross-pollinate with core architectural pillars. The lack of an associative mental map creates cognitive fragmentation, obscures knowledge gaps, and diminishes the perceived compounding value of daily study.

### Proposed Solution: Interactive Architecture Knowledge Graph (`/graph`)
We propose introducing a first-class **Interactive Architecture Knowledge Graph** accessible at `/graph`. The knowledge graph functions as an interactive "Second Brain" for software engineers:
1. **Pillar Constellations:** Topics are clustered around universal engineering pillars (Backend Runtime & Concurrency, Data Storage & Persistence, Distributed Systems & Architecture, Frontend & Browser Engineering, and Engineering Craft & Mindset).
2. **Relational Synthesis:** Visualizes live connections linking high-level curriculum topics to parent books, active flashcards, and tagged highlight quotes created during daily reading.
3. **Actionable Bridges:** Every node on the graph is actionable. Selecting a node reveals a contextual slide-over drawer with key takeaways and 1-click navigation bridges directly into review sessions, chapter reading, or scenario drills.
4. **Mastery Radar:** Nodes visually reflect the learner's spaced repetition retention state, distinguishing newly acquired concepts from well-retained, mastered principles.

### Business & Learning Value
- **Learning Retention:** Associative semantic graphs activate non-linear memory retrieval, accelerating concept synthesis and retention far beyond linear lists.
- **Engagement & Compounding Motivation:** Users visibly witness their personal graph expand and change color as they highlight books, practice quizzes, and master flashcards.
- **Session Duration & Feature Discovery:** Serves as a central navigation hub that connects reading, review, quiz, and notes, driving cross-feature user engagement.

### Architectural Invariants: VPS Zero-Overhead & Client-Side Canvas 2D
A primary design constraint for TechDaily is strict cost-efficiency and lean resource utilization on a budget VPS (4 vCPUs, 8 GB RAM shared across PostgreSQL, Nginx, .NET runtime, and background workers):
1. **Zero New Infrastructure (No Neo4j / Graph Databases):**
   - Dedicated graph engines (such as Neo4j or Memgraph) require 1.5–3 GB of baseline resident memory, persistent JVM/C++ daemons, and dedicated backup pipelines.
   - TechDaily strictly avoids introducing any auxiliary database engine. All graph nodes, edges, and associative connections are derived dynamically from existing PostgreSQL 17 relational tables (`Topics`, `DocumentBooks`, `SpacedRepetitionCards`, `UserHighlights`, and `DocumentChunks`).
2. **Zero Continuous Server-Side Math (No $O(N^2)$ Runtime Vector Calculations):**
   - High-overhead semantic similarity clustering on the server is rejected. The server executes an indexed relational query joining user data in sub-5ms, outputting a compact JSON graph payload (~30–60 KB).
3. **100% Client-Side Physics Simulation:**
   - Force-directed layout algorithms (such as Cytoscape.js CoSE or Web Worker force simulations) run exclusively in the client's browser using Canvas 2D.
   - The simulation automatically settles and freezes after 1.5 seconds. Once stabilized, the graph consumes zero ongoing CPU and GPU cycles on both the client device and the server.

---

## What Changes

### 1. Navigation & Placement
- Add a new first-class route `/graph` under the **"Knowledge"** navigation group (`nav.group_knowledge`) in `frontend/components/layout/AppSidebar.vue` (desktop) and `frontend/components/layout/AppHeader.vue` (mobile drawer).
- Integrate the `Network` icon from `lucide-vue-next`.
- Localize navigation labels: `"Knowledge Graph"` in `en.json` and `"Sơ Đồ Tri Thức"` in `vi.json`.

### 2. Backend Graph Extraction Endpoint (`GET /api/v1/graph`)
- Expose a new protected endpoint `GET /api/v1/graph` mapped in `KnowledgeGraphEndpoints.cs` requiring standard JWT Bearer authorization (`.RequireAuthorization()`).
- Implement `GetKnowledgeGraphQueryHandler` implementing `IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>` registered via Pure Dependency Injection in `TechDaily.Application`.
- Derive nodes and edges in a single efficient PostgreSQL query:
  - **Topic Nodes:** Master curriculum topics partitioned by pillar categories.
  - **Book Nodes:** Published library books and user-paced reading materials.
  - **Card Nodes:** Spaced repetition flashcards owned by the authenticated user, indicating SM-2 mastery status (`Learning`, `Reviewing`, `Mastered`).
  - **Highlight Nodes:** User highlights linked to their source document chunk and book, interconnected via overlapping tag arrays.
  - **Edges:** Explicit relational links (`TopicToPillar`, `CardToTopic`, `BookToTopic`, `HighlightToBook`, `HighlightToTopic`) and associative tag intersections (`SharedTag`).
- Sub-5ms database query execution with zero N+1 queries using EF Core `AsNoTracking()` projections.

### 3. Frontend Interactive Graph Canvas & Controls
- Implement `frontend/pages/graph.vue` featuring a full-viewport graph canvas container with glassmorphic floating control overlays.
- Create `frontend/components/graph/GraphCanvas.vue` using Cytoscape.js (Canvas 2D rendering) with dynamic theme support (automatic dark/light mode palette synchronization with `@nuxtjs/color-mode`).
- Implement `frontend/components/graph/GraphControlBar.vue` with multi-dimensional filtering:
  - **Pillar Filter:** Filter by universal engineering category (All, Backend Runtime, Data Storage, Distributed Systems, Frontend Engineering, Engineering Craft).
  - **Node Type Filter:** Toggle visibility of Topics, Books, Cards, or Highlights.
  - **Mastery Status Filter:** Filter flashcard nodes by learning state (`All`, `Learning`, `Reviewing`, `Mastered`).
  - **Live Search Input:** Real-time text search highlighting matching nodes and centering the viewport.
- Implement `frontend/components/graph/GraphMinimap.vue` providing visual canvas orientation and fast navigation across large graphs.

### 4. Slide-Over Detail Drawer & 1-Click Action Bridges
- Implement `frontend/components/graph/GraphDetailDrawer.vue` (slide-over drawer on desktop, bottom sheet on mobile):
  - Displays selected node metadata: title, type icon, category badge, and key takeaways or excerpt markdown.
  - **1-Click Action Bridges:**
    - Topic nodes: *"Practice Quiz"* (`/quiz?topic={slug}`) and *"View Roadmap"* (`/roadmap#{dayOrder}`).
    - Book nodes: *"Browse in Library"* (`/library`) and *"Read Slices"* (`/read/{bookId}`).
    - Card nodes: *"Review Flashcard"* (`/review?cardId={id}`) with SM-2 interval and ease factor stats.
    - Highlight nodes: *"Read Chapter"* (`/read/{bookId}#slice-{chunkOrder}`) and *"View in Notes"* (`/notes?highlightId={id}`).
  - Strict compliance with the Bilingual Responsive Layout Invariant (`whitespace-nowrap shrink-0` on action buttons and badges in both English and Vietnamese).

---

## Scope & Non-Goals

### In-Scope
- Backend Pure DI query handler, DTOs, and protected API endpoint `GET /api/v1/graph`.
- Frontend Pinia store `useKnowledgeGraphStore.ts` managing graph fetching, filtering, search, and selection state.
- Cytoscape.js Canvas 2D visualization with force layout settling within 1.5 seconds.
- Multi-dimensional filter control bar, search centering, and minimap.
- Slide-over detail drawer with 1-click navigation bridges.
- Desktop sidebar and mobile header drawer integration.
- Full bilingual localization in `en.json` and `vi.json`.
- Comprehensive backend and frontend automated tests.

### Non-Goals
- Introducing auxiliary database containers (Neo4j, ArangoDB, Memgraph).
- Server-side image rasterization or heavy server-side clustering algorithms.
- Free-form user graph editing (e.g. manually drawing custom arrows or adding arbitrary nodes); graph structure is strictly grounded in database relational entities.
- Real-time multi-user collaborative editing via WebSockets.
