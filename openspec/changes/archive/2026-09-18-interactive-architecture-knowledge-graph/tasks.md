# Tasks: Interactive Architecture Knowledge Graph

## 1. Backend Application Layer & DTOs

- [x] 1.1 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/DTOs/KnowledgeGraphDtos.cs`, define `GraphNodeDto`, `GraphEdgeDto`, `KnowledgeGraphResponse`, `GraphStatsDto`, `GraphNodeType`, `MasteryStatus`, and `GraphRelationType`.
- [x] 1.2 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQuery.cs`, define `GetKnowledgeGraphQuery(Guid UserId)`.
- [x] 1.3 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQueryHandler.cs`, implement `IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>`:
  - Query `Topics` ordered by `DayOrder` and project into `Topic` nodes and pillar cluster hubs.
  - Query published `DocumentBooks` and project into `Book` nodes and `BookToTopic` edges.
  - Query `SpacedRepetitionCards` for the current user, derive SM-2 `MasteryStatus`, and project into `Card` nodes and `CardToTopic` edges.
  - Query `UserHighlights` with `DocumentChunk` for the current user, project into `Highlight` nodes and `HighlightToBook` edges.
  - Match highlight tags with topic titles/slugs for `HighlightToTopic` edges.
  - Derive pairwise associative `SharedTag` edges between highlights sharing common normalized tags.
  - Assemble `GraphStatsDto` with node counts, pillar breakdowns, and mastered card totals.
- [x] 1.4 In `backend/src/TechDaily.Application/DependencyInjection.cs`, register `GetKnowledgeGraphQueryHandler` using Pure Dependency Injection (`services.AddScoped<IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>, GetKnowledgeGraphQueryHandler>()`).

## 2. Backend API Endpoint & Routing

- [x] 2.1 In `backend/src/TechDaily.Api/Endpoints/KnowledgeGraphEndpoints.cs`, create `MapKnowledgeGraphEndpoints` mapping `GET /` with `.RequireAuthorization()`, resolving `GetKnowledgeGraphQueryHandler`, and returning `200 OK` with `KnowledgeGraphResponse` or `401 Unauthorized`.
- [x] 2.2 In `backend/src/TechDaily.Api/Program.cs`, map the knowledge graph endpoint group under `app.MapGroup("/api/v1/graph").WithTags("Knowledge Graph").RequireAuthorization().MapKnowledgeGraphEndpoints()`.

## 3. Frontend Navigation & Routing Placement

- [x] 3.1 In `frontend/components/layout/AppSidebar.vue`, import `Network` icon from `lucide-vue-next` and add the `/graph` route under `nav.group_knowledge` with name key `nav.graph`.
- [x] 3.2 In `frontend/components/layout/AppSidebar.vue`, update `isLinkActive` to recognize `/graph`.
- [x] 3.3 In `frontend/components/layout/AppHeader.vue`, import `Network` icon from `lucide-vue-next` and add the `/graph` route under `nav.group_knowledge` in the mobile drawer menu.
- [x] 3.4 In `frontend/components/layout/AppHeader.vue`, ensure tapping `/graph` in the mobile drawer automatically closes the drawer and navigates to `/graph`.

## 4. Frontend Dependencies & Pinia Store

- [x] 4.1 In `frontend/package.json`, add `cytoscape` and `@types/cytoscape` dependencies.
- [x] 4.2 In `frontend/stores/useKnowledgeGraphStore.ts`, implement `useKnowledgeGraphStore`:
  - Define state: `rawData`, `filters` (`category`, `nodeTypes`, `mastery`, `searchQuery`), `selectedNodeId`, `isLoading`, and `error`.
  - Implement computed getters: `filteredNodes`, `filteredEdges`, `selectedNode`, `nodeStats`, and `activePillars`.
  - Implement actions: `fetchGraph()`, `selectNode(id)`, `resetFilters()`, and filter setters.

## 5. Cytoscape.js Canvas 2D Component

- [x] 5.1 In `frontend/components/graph/GraphCanvas.vue`, initialize Cytoscape.js with Canvas 2D rendering inside a full-height container.
- [x] 5.2 Configure CoSE force layout with rapid cooling factor that settles and freezes physics simulation within 1.5 seconds, dropping to 0% CPU consumption when idle.
- [x] 5.3 Configure dynamic theme stylesheet reacting to `@nuxtjs/color-mode`, updating node fills, node borders, labels, background grid, and edge colors for dark and light palettes.
- [x] 5.4 Implement viewport interactions: smooth mouse wheel and pinch zoom ($0.2\times$ to $3.0\times$), pan, fit-to-screen button, and node selection tap events dispatching to `useKnowledgeGraphStore`.

## 6. Graph Control Bar, Minimap & Multi-Dimensional Filtering

- [x] 6.1 In `frontend/components/graph/GraphControlBar.vue`, build a floating glassmorphic control bar featuring:
  - Pillar category filter chips (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`).
  - Node type toggles (`Topics`, `Books`, `Flashcards`, `Highlights`).
  - Flashcard mastery status selector (`All`, `Learning`, `Reviewing`, `Mastered`).
  - Live search input with clear button.
  - Reset filters button.
- [x] 6.2 Implement real-time live search dimming in `GraphControlBar.vue` and `GraphCanvas.vue` that highlights matching nodes, fades non-matching nodes to 15% opacity, and animates viewport centering on selection.
- [x] 6.3 In `frontend/components/graph/GraphMinimap.vue`, implement a compact canvas minimap displaying node clusters and current viewport bounds.

## 7. Slide-Over Detail Drawer & 1-Click Action Bridges

- [x] 7.1 In `frontend/components/graph/GraphDetailDrawer.vue`, implement slide-over drawer (desktop: right slide-in) and bottom sheet (mobile: bottom swipeable sheet) displaying selected node details.
- [x] 7.2 Render contextual node metadata:
  - Topic nodes: key takeaways, curriculum day, and difficulty badge.
  - Book nodes: chapter count, source author/URL, and cover emblem.
  - Card nodes: SM-2 interval days, ease factor, repetition count, and next review date.
  - Highlight nodes: blockquote quote text, source chapter reference, and user notes.
- [x] 7.3 Implement 1-click action bridge buttons:
  - Topic: "Practice Quiz" (`/quiz?topic={slug}`) and "View Roadmap" (`/roadmap#{dayOrder}`).
  - Book: "Browse in Library" (`/library`) and "Read Slices" (`/read/{bookId}`).
  - Card: "Review Flashcard" (`/review?cardId={id}`).
  - Highlight: "Read Chapter" (`/read/{bookId}#slice-{chunkOrder}`) and "View in Notes" (`/notes?highlightId={id}`).
- [x] 7.4 Enforce `whitespace-nowrap shrink-0` and responsive flex gaps on all action buttons and status badges in `GraphDetailDrawer.vue`.

## 8. Main Graph Page Assembly & Layout

- [x] 8.1 In `frontend/pages/graph.vue`, construct the main page layout with full viewport height (`h-[calc(100vh-4rem)]`), integrating `GraphCanvas.vue`, `GraphControlBar.vue`, `GraphDetailDrawer.vue`, and `GraphMinimap.vue`.
- [x] 8.2 In `frontend/pages/graph.vue`, implement skeleton loading overlay during initial fetch, error state with localized retry button, and empty state guidance for new users.
## 9. Localization (English & Vietnamese)

- [x] 9.1 In `frontend/i18n/locales/en.json`, add `"nav.graph": "Knowledge Graph"` and full `graph.*` dictionary:
  - Control bar labels, filter chip titles, mastery status labels, search placeholder.
  - Node drawer titles, takeaway headers, SM-2 metrics labels, and 1-click action button copy.
  - Empty state messages and error retry text.
- [x] 9.2 In `frontend/i18n/locales/vi.json`, add `"nav.graph": "Sơ Đồ Tri Thức"` and exact localized Vietnamese translations for all `graph.*` keys.

## 10. Verification & Quality Assurance

- [x] 10.1 In `backend/tests/TechDaily.Tests/Application/GetKnowledgeGraphQueryHandlerTests.cs`, add unit tests:
  - Verify topics, published books, user cards, and user highlights are projected correctly.
  - Verify card and highlight user data isolation (user A cannot see user B's cards or highlights).
  - Verify SM-2 mastery status categorization (`Learning`, `Reviewing`, `Mastered`).
  - Verify associative `SharedTag` edge derivation between highlights sharing tags.
  - Verify empty card/highlight collections for new users return valid response without errors.
- [x] 10.2 In `frontend/tests/stores/useKnowledgeGraphStore.spec.ts`, add unit tests verifying:
  - Multi-dimensional filtering across pillars, node types, and card mastery.
  - Search query filtering and node matching.
  - Filter reset restoring complete visibility.
- [x] 10.3 In `frontend/tests/components/graph/GraphDetailDrawer.spec.ts`, add component tests verifying action bridge URLs and `whitespace-nowrap shrink-0` button rendering.
- [x] 10.4 Run `openspec validate --strict interactive-architecture-knowledge-graph` to ensure strict OpenSpec compliance.
