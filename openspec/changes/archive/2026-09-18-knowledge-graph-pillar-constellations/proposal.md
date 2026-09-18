# Proposal: Knowledge Graph Pillar Constellations & Orphan Node Resolution

## Why

### Problem Statement
TechDaily's interactive architecture knowledge graph (`/graph`) gives engineers a 2D associative map connecting curriculum topics, published library books, spaced repetition flashcards, and reading highlights. However, production exploration and visual graph inspections uncovered two fundamental relational and simulation defects that compromise graph clarity and structural aesthetics:

1. **Cartesian Category Blowout & Default Crawler Category Collision:**
   - In `GetKnowledgeGraphQueryHandler.cs`, book-to-topic relational edge generation previously used a naive Cartesian product (`book.Category == topic.Category`):
     ```csharp
     foreach (var book in books) {
         foreach (var topic in topics) {
             if (book.Category == topic.Category) {
                 edges.Add(new GraphEdgeDto(..., RelationType: GraphRelationType.BookToTopic));
             }
         }
     }
     ```
   - When users crawled or imported documentation using the web crawler (e.g. `aspnet-core-aspnetcore-10.0`), the frontend import modal defaulted `importCategory` to `0` (`Category.FrontendWeb`).
   - Consequently, the .NET backend book was labeled as `FrontendWeb` and created artificial associative edges to **every single Vue 3 and browser internals topic** in the 30-day curriculum (Days 1–7). This cluttered the canvas with dozens of spurious, nonsensical cross-domain edges.

2. **Degree-0 Orphan Node Repulsion Collapse:**
   - In architectural pillars with currently fewer published books or newly seeded topics (such as `DatabaseStorage` and `SystemDesign`), curriculum topics often have no associated books, and newly registered users have not yet created flashcards or reading highlights targeting those topics.
   - As a result, all topics in those categories had an edge degree of zero ($degree = 0$, zero incoming and outgoing edges).
   - In Cytoscape.js, the CoSE (Compound Spring Embedder) force-directed physics layout relies on spring forces along edges to bind nodes together into clusters while repulsive forces push unconnected nodes apart.
   - Because degree-0 nodes experience zero spring forces and only mutual repulsion, the simulation pushes them to the outer perimeter, flattening and stacking them into an overlapping horizontal line along the bottom border of the canvas viewport.

### Proposed Solution: Pillar Hub Constellations & Relational Refinements
We propose introducing **Pillar Hub Nodes** as permanent gravitational centers in the knowledge graph, eliminating all degree-0 topics while refining book-to-topic relational linking and crawler category detection:

1. **Pillar Hub Nodes (`GraphNodeType.Pillar`):**
   - Inject 5 canonical architectural Pillar Hub nodes representing the core curriculum domains:
     - `pillar-FrontendWeb`: **Frontend & Web** (`Category.FrontendWeb`)
     - `pillar-BackendDotNet`: **Backend (.NET)** (`Category.BackendDotNet`)
     - `pillar-DatabaseStorage`: **Database & Storage** (`Category.DatabaseStorage`)
     - `pillar-SystemDesign`: **Distributed Systems** (`Category.SystemDesign`)
     - `pillar-EngineeringCraft`: **Engineering Craft** (`Category.EngineeringCraft`)
   - Pillar hubs act as primary gravitational anchors for their respective domains.

2. **Guaranteed Constellation Connectivity (`TopicToPillar` & `BookToPillar`):**
   - **Zero Orphan Topics:** Every curriculum topic automatically links to its parent Pillar Hub via a `TopicToPillar` edge ($Source = \text{topic.Id}, Target = \text{pillar.Id}$). Consequently, every topic has degree $\ge 1$, belonging to a coherent visual cluster.
   - **Book-to-Pillar Links:** Every book links to its respective Pillar Hub via a `BookToPillar` edge. Multi-disciplinary books (such as the legacy 30-Day Master Curriculum) link to all 4 foundational technical pillars.
   - **Refined Book-to-Topic Linking:** Eliminate blind Cartesian category matching (`book.Category == topic.Category`). Books only establish direct `BookToTopic` edges if explicitly covered by book chunks (`DocumentChunk` metadata) or via direct title/slug keyword matching.

3. **Intelligent Book Category Inference:**
   - In URL crawling and document ingestion, automatically infer `BackendDotNet` when the book title, source URL, or crawled metadata contains `.net`, `dotnet`, `csharp`, `c#`, `aspnet`, or `aspnetcore` instead of defaulting blindly to `0` (`FrontendWeb`).

4. **Cytoscape CoSE Physics & Visual Layout Tuning:**
   - Configure CoSE layout parameters with `randomize: true`, `componentSpacing: 120`, `nodeRepulsion: () => 500000`, and `idealEdgeLength: () => 120`.
   - Add distinctive CSS styling in `GraphCanvas.vue` for `pillar` nodes: prominent size ($56\times 56\text{px}$), glowing accent borders, bold typography, and domain-tinted badge colors.
   - Add specialized display in `GraphDetailDrawer.vue` for pillar nodes detailing domain purpose, total connected topics, and quick filter triggers.

### Value & Invariants
- **Zero Orphan Nodes:** $100\%$ of topics and books belong to visual constellations anchored by their parent pillar hub.
- **Zero VPS Overhead Invariant:** Graph derivation remains an instantaneous, indexed PostgreSQL relational projection executed in sub-5ms with zero auxiliary graph databases or server-side physics engines.
- **Client-Side Canvas 2D:** Physics continues to run exclusively in the browser, settling and halting within 1.5 seconds to achieve 0% idle CPU/GPU usage.

### Scope & Non-Goals
- **In Scope:**
  - Backend `GraphNodeType.Pillar` and `GraphRelationType.BookToPillar` DTO definitions.
  - Backend query handler updates in `GetKnowledgeGraphQueryHandler.cs` generating pillar nodes, `TopicToPillar` edges, `BookToPillar` edges, and refined book-to-topic connections.
  - Crawler/import category auto-inference in frontend library ingestion and backend crawler endpoints.
  - Frontend canvas styling (`GraphCanvas.vue`), drawer view (`GraphDetailDrawer.vue`), and store updates (`useKnowledgeGraphStore.ts`).
  - Unit tests verifying pillar generation, zero degree-0 topics, and category inference.
- **Non-Goals:**
  - Introducing server-side layout calculation or Neo4j graph databases.
  - Modifying the underlying database schema (`Topic` or `DocumentBook` tables require no migrations; pillar hubs are virtual architectural projections).
  - Multi-user collaborative graph manipulation or drag-and-drop relationship persistence.

---

## What Changes

### 1. Backend DTO & Relational Graph Derivation
- Extend `GraphNodeType` with `public const string Pillar = "pillar";`.
- Extend `GraphRelationType` with `public const string BookToPillar = "BookToPillar";`.
- Update `GetKnowledgeGraphQueryHandler`:
  - Generate 5 canonical Pillar Hub nodes (`pillar-{category}`).
  - Create `TopicToPillar` edges from every topic to its parent pillar hub.
  - Create `BookToPillar` edges from every published book to its parent pillar hub (and to all technical pillars for universal multi-topic books).
  - Replace blind category Cartesian product for `BookToTopic` with specific chunk/title matching.
  - Include `pillar` in `nodeTypeCounts` and total node statistics.

### 2. Intelligent Book Category Inference
- Enhance crawler and import ingestion flows to analyze title and URL patterns (`aspnet`, `dotnet`, `csharp`, `c#`) and automatically assign `Category.BackendDotNet` instead of defaulting to `0` (`FrontendWeb`).

### 3. Frontend Canvas Styling & Physics Tuning
- In `GraphCanvas.vue`:
  - Add cytoscape style rules for `node[type = "pillar"]`: circular shape, size $56\times 56\text{px}$, bold font, prominent borders, and pillar-specific glowing accents.
  - Tune `cose` layout parameters: `randomize: true`, `componentSpacing: 120`, `nodeRepulsion: () => 500000`, `idealEdgeLength: () => 120`, ensuring high aesthetic separation between constellations.
- In `GraphDetailDrawer.vue`:
  - Handle `nodeType === 'pillar'`, displaying the architectural pillar description, total connected topics, and quick-filter actions.
