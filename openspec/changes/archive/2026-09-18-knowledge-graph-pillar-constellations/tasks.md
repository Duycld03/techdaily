# Tasks: Knowledge Graph Pillar Constellations & Orphan Node Resolution

## 1. Backend DTOs & Handler Implementation

- [x] 1.1 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/DTOs/KnowledgeGraphDtos.cs`, add `public const string Pillar = "pillar";` to `GraphNodeType` and add `public const string BookToPillar = "BookToPillar";` to `GraphRelationType`.
- [x] 1.2 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQueryHandler.cs`, define the 5 canonical architectural pillars (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`) and project them as `GraphNodeType.Pillar` nodes into the graph response.
- [x] 1.3 In `GetKnowledgeGraphQueryHandler.cs`, generate `TopicToPillar` edges for every curriculum topic connecting the topic to its respective pillar hub (`Topic.Id -> pillar-{Category}`), ensuring every curriculum topic has an edge degree $\ge 1$.
- [x] 1.4 In `GetKnowledgeGraphQueryHandler.cs`, generate `BookToPillar` edges connecting published books to their parent pillar hub, and linking universal multi-pillar curriculum series (e.g. 30-Day Master Curriculum) to all four foundational technical pillars.
- [x] 1.5 In `GetKnowledgeGraphQueryHandler.cs`, replace the blind Cartesian product (`book.Category == topic.Category`) for `BookToTopic` with refined content/slug/title keyword matching to prevent spurious cross-topic edges.
- [x] 1.6 In `GetKnowledgeGraphQueryHandler.cs`, update `nodeTypeCounts` in `GraphStatsDto` to include the count of `pillar` nodes.

## 2. Book Category Inference & Crawler Auto-Detection

- [x] 2.1 In `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs` (or a shared categorization helper), implement `InferCategoryFromContext(string title, string url, string? content)` detecting .NET/C# keywords (`aspnet`, `aspnetcore`, `dotnet`, `.net`, `csharp`, `c#`) and mapping them to `Category.BackendDotNet`.
- [x] 2.2 In `frontend/pages/library.vue`, update `handleCrawlUrl` to automatically set `importCategory` based on keyword detection from `crawlUrlInput` and crawled article metadata, avoiding accidental default to `0` (`FrontendWeb`).

## 3. Frontend Canvas Styling & Physics Tuning

- [x] 3.1 In `frontend/components/graph/GraphCanvas.vue`, add Cytoscape CSS selectors for `node[type = "pillar"]` styling them with prominent size ($56\times 56\text{px}$), $3\text{px}$ glowing borders, bold typography, and domain color backgrounds.
- [x] 3.2 In `frontend/components/graph/GraphCanvas.vue`, tune the CoSE physics layout options: set `randomize: true`, `componentSpacing: 120`, `nodeRepulsion: () => 500000`, and `idealEdgeLength: () => 120` to prevent dense clustering and eliminate perimeter node collapse.
- [x] 3.3 In `frontend/components/graph/GraphDetailDrawer.vue`, add handling for `nodeType === 'pillar'`, rendering pillar domain takeaways, connected topic/book counts, and a quick-action button to filter the canvas to the selected pillar.
- [x] 3.4 In `frontend/stores/useKnowledgeGraphStore.ts`, ensure `filteredNodes` and `filteredEdges` gracefully support `type: "pillar"` when filtering by category or search query.

## 4. Automated Tests & Verification

- [x] 4.1 In `backend/tests/TechDaily.Tests/Application/GetKnowledgeGraphQueryHandlerTests.cs`, add unit tests asserting that exactly 5 Pillar nodes are returned with `type: "pillar"`.
- [x] 4.2 In `GetKnowledgeGraphQueryHandlerTests.cs`, add unit tests asserting that every topic in the database has a corresponding `TopicToPillar` edge and zero degree-0 topics exist.
- [x] 4.3 In `GetKnowledgeGraphQueryHandlerTests.cs`, add unit tests asserting that multi-pillar books generate `BookToPillar` edges across all technical pillars, and that unrelated books do not establish Cartesian `BookToTopic` edges.
- [x] 4.4 In `backend/tests/TechDaily.Tests/Infrastructure/WebArticleCrawlerTests.cs` (or application unit tests), add unit tests validating category auto-inference for .NET and C# keywords.
- [x] 4.5 Run `dotnet test backend/tests/TechDaily.Tests` to ensure all backend tests pass.
- [x] 4.6 Run `npm --prefix frontend test` to verify frontend test suite stability.
- [x] 4.7 Run `openspec validate --strict knowledge-graph-pillar-constellations` to confirm OpenSpec schema and specification compliance.
