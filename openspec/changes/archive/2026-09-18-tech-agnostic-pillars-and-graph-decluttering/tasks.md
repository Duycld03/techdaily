# Tasks: Technology-Agnostic Architectural Pillars & Knowledge Graph Decluttering

## 1. Domain Layer & Database Dual-Conversion

- [x] 1.1 In `backend/src/TechDaily.Domain/Enums/DomainEnums.cs`, rename `Category.BackendDotNet` to `Category.BackendRuntime` while keeping integer value 1 (`BackendRuntime = 1`). Verify project builds with updated enum definition.
- [x] 1.2 In `backend/src/TechDaily.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`, implement a dual-reading `ValueConverter<Category, string>` for `Topic` and `DocumentBook` that maps legacy `"BackendDotNet"` and `"DotNet"` to `Category.BackendRuntime` when reading from the database, while persisting `"BackendRuntime"` when writing. Verify with unit tests.
- [x] 1.3 In `backend/src/TechDaily.Infrastructure/Data/curriculum-30-days.json`, update `"category": "BackendDotNet"` occurrences to `"category": "BackendRuntime"`. Verify JSON parses cleanly.
- [x] 1.4 In `backend/src/TechDaily.Infrastructure/Persistence/`, add an EF Core migration or SQL data update script executing `UPDATE "Topics" SET "Category" = 'BackendRuntime' WHERE "Category" IN ('BackendDotNet', 'DotNet')` and `UPDATE "DocumentBooks" SET "Category" = 'BackendRuntime' WHERE "Category" IN ('BackendDotNet', 'DotNet')`.

## 2. Backend Application & Service Decoupling

- [x] 2.1 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQueryHandler.cs`, update `CanonicalPillars` to define `("pillar-BackendRuntime", "Backend & Runtime", Category.BackendRuntime, ...)`. Verify graph response outputs `pillar-BackendRuntime`.
- [x] 2.2 In `backend/src/TechDaily.Application/Features/KnowledgeGraph/DTOs/KnowledgeGraphDtos.cs`, add `public const string CardToHighlight = "CardToHighlight";` and `public const string CardToPillar = "CardToPillar";` to `GraphRelationType`.
- [x] 2.3 In `GetKnowledgeGraphQueryHandler.cs`, implement edge derivation for cards: connect cards with `SourceHighlightId` to their highlight via `CardToHighlight`, and connect unlinked or quiz mistake cards to their category pillar hub via `CardToPillar`, asserting that 100% of card nodes have an edge degree $\ge 1$. Verify with unit tests.
- [x] 2.4 In `backend/src/TechDaily.Application/Features/Curriculum/GetCurriculumRoadmap/GetCurriculumRoadmapHandler.cs`, update module definition to `(Category.BackendRuntime, "Backend & Runtime Systems", ...)`. Verify roadmap API returns `BackendRuntime`.
- [x] 2.5 In `backend/src/TechDaily.Application/Features/Insights/GetInsightsMeta/GetInsightsMetaHandler.cs`, update category key and labels to `BackendRuntime` / `"Backend & Runtime Systems"`. Verify meta endpoint response.
- [x] 2.6 In `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`, `GeminiAiService.cs`, and `LibraryEndpoints.cs`, replace hardcoded fallbacks to `BackendDotNet` with balanced multi-language heuristics and fallback to `Category.EngineeringCraft` or `Category.BackendRuntime`. Verify crawler category inference tests.

## 3. Frontend Knowledge Graph Decluttering & Level-of-Detail

- [x] 3.1 In `frontend/components/graph/GraphCanvas.vue`, update Cytoscape stylesheet to hide text labels for `card` and `highlight` nodes by default at overview zoom, and add `.label-revealed` / `.lod-detailed` selector styles to reveal labels on hover, tap, or zoom-in ($zoom \ge 1.1\times$). Verify in browser/component tests.
- [x] 3.2 In `frontend/components/graph/GraphCanvas.vue`, register `mouseover`, `mouseout`, and `zoom` event listeners on the Cytoscape instance to toggle `.label-revealed` classes dynamically without re-running physics simulation.
- [x] 3.3 In `frontend/components/graph/GraphCanvas.vue`, increase layout padding to at least 90px and enforce initial viewport centering with top clearance so the floating control bar (`GraphControlBar.vue`) does not obscure top-level nodes.
- [x] 3.4 In `frontend/components/graph/GraphCanvas.vue`, `GraphControlBar.vue`, and `GraphDetailDrawer.vue`, update category selectors and badge styling to support `BackendRuntime` (with fallback for `BackendDotNet`). Verify pillar filtering works for `BackendRuntime`.
- [x] 3.5 In `frontend/i18n/locales/en.json` and `vi.json`, update category translations from "Backend (.NET)" to "Backend & Runtime" ("Hệ Thống Backend & Runtime"). Verify localized badge rendering.

## 4. Documentation & Product Framing Decoupling

- [x] 4.1 Delete `docs/curriculum-30-days.md` to prevent confusing the demo seed data with platform architectural specifications.
- [x] 4.2 In `AGENTS.md`, remove the reference to `docs/curriculum-30-days.md` and clarify that the 30-day curriculum is seed starter-pack data in `TechDaily.Infrastructure/Data/curriculum-30-days.json`.
- [x] 4.3 In `README.md` and `docs/features.md`, update documentation tables to emphasize TechDaily's technology-agnostic ingestion engine and custom user tracks.
- [x] 4.4 In `frontend/pages/roadmap.vue` and `frontend/pages/today.vue`, update copy to explicitly designate the 30-day curriculum as the "Starter Pack (Demo Track)" and add an actionable callout inviting users to upload their own books in the Library.

## 5. Automated Tests & Quality Assurance

- [x] 5.1 In `backend/tests/TechDaily.Tests/`, update unit tests in `GetKnowledgeGraphQueryHandlerTests.cs`, `CurriculumRoadmapTests.cs`, `GetInsightsMetaHandlerTests.cs`, and `WebArticleCrawlerTests.cs` to assert `BackendRuntime` and verify `CardToHighlight` / `CardToPillar` edge derivation.
- [x] 5.2 In `frontend/tests/`, update unit tests in `GraphCanvas.spec.ts`, `GraphControlBar.spec.ts`, `GraphDetailDrawer.spec.ts`, and `useKnowledgeGraphStore.spec.ts` to verify `BackendRuntime` filtering and LOD label classes.
- [x] 5.3 Run `dotnet test backend/tests/TechDaily.Tests` to verify 100% backend test pass rate.
- [x] 5.4 Run `npm --prefix frontend test` to verify 100% frontend test pass rate.
