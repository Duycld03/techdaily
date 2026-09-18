# Proposal: Technology-Agnostic Architectural Pillars & Knowledge Graph Decluttering

## Why

TechDaily was created as an AI-powered active recall and second-brain platform where software engineers can ingest arbitrary technical materials (PDFs, documentation links, Markdown notes) to master their chosen domain via SM-2 flashcards, spaced scenarios, and interactive graph exploration.

However, historical dogfooding artifacts have produced two significant issues:
1. **Misleading Stack Coupling & Curriculum Misconception:** The system hardcodes `Category.BackendDotNet` across enums, crawler defaults, AI prompts, and documentation (`docs/curriculum-30-days.md`). This creates the false impression that TechDaily is merely a fixed "30-Day .NET Course" rather than an open, technology-agnostic learning engine.
2. **Severe Knowledge Graph Visual Overlap & Orphan Nodes:** On `/graph`, flashcards created from highlights or quiz mistakes lack `TopicId` and have zero edges (degree = 0). The CoSE force-directed layout repels these disconnected diamond nodes to the bottom and side canvas borders, where un-culled 100px text labels severely collide and overlap into an illegible black mass. Additionally, the floating filter bar obstructs top-tier nodes due to inadequate canvas padding.

We must decouple the platform from tech-specific naming, retire misleading curriculum specification docs, and repair the graph visualization pipeline to deliver a clean, flexible learning experience.

## What Changes

- **Pillar Taxonomy Refactoring (BREAKING backend enum & DB mapping):**
  - Refactor `Category.BackendDotNet` (value 1) to `Category.BackendRuntime` in `DomainEnums.cs`.
  - Maintain backward compatibility in database conversions and JSON serialization so existing PostgreSQL records with `"BackendDotNet"` (or `"DotNet"`) string columns seamlessly map to `Category.BackendRuntime`.
  - Update all pillar labels and UI translations to "Backend & Runtime" across English (`en-US`) and Vietnamese (`vi-VN`).
  - Remove biased fallback defaults in `WebArticleCrawler`, `GeminiAiService`, `LibraryEndpoints`, and frontend ingestion that coerced unknown documents into .NET.
- **Documentation & Product Decoupling:**
  - Remove `docs/curriculum-30-days.md` and reframe references in `AGENTS.md`, `README.md`, and `docs/features.md` to clearly define the 30-day curriculum as a demo seed ("Starter Pack"), not the core identity or boundary of the platform.
  - Update roadmap and today view copy to explicitly invite users to upload their own books and set custom active tracks.
- **Knowledge Graph Relational Repairs & Edge Derivation:**
  - Connect all flashcard nodes to the graph network: cards generated from highlights connect to their source highlight (`CardToHighlight`) or source book, and cards generated from quizzes or general topics connect to their category pillar hub (`CardToPillar`), ensuring 100% of nodes have degree $\ge 1$.
  - Refactor `GetKnowledgeGraphQueryHandler` to generate `pillar-BackendRuntime` instead of `pillar-BackendDotNet`.
- **Knowledge Graph Canvas Decluttering & Level-of-Detail (LOD):**
  - Implement dynamic label Level-of-Detail (LOD) in `GraphCanvas.vue`: leaf nodes (cards, highlights) hide text labels by default at macroscopic zoom levels, revealing them on hover, node selection, or close zoom.
  - Increase layout padding-top (minimum 100px) and optimize CoSE repulsion and edge length to guarantee clear separation from the floating filter bar and prevent central cluster crowding.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Update `pillar-BackendDotNet` to `pillar-BackendRuntime`, introduce `CardToHighlight` and `CardToPillar` edge relations to eliminate degree-0 orphan cards, and mandate canvas Level-of-Detail label decluttering and floating bar clearance.
- `roadmap`: Update technical module grouping from `BackendDotNet` to `BackendRuntime`, and explicitly specify the 30-day curriculum as an optional demo Starter Pack fallback rather than the mandatory system scope.

## Impact

- **Database:** PostgreSQL tables `Topics` and `DocumentBooks` store `Category` as string (`HasConversion<string>()`). Database data migration and EF Core enum conversion must tolerate legacy `"BackendDotNet"` and new `"BackendRuntime"`. Tables `TechInsights` and `InterviewQuestions` store `Category` as `int` (value `1` remains unchanged).
- **Backend APIs:** `GET /api/v1/graph`, `GET /api/v1/curriculum/roadmap`, `GET /api/v1/insights/meta`, `POST /api/v1/library/import`.
- **Seed Data:** `curriculum-30-days.json` category string updated to `"BackendRuntime"`.
- **Frontend Components:** `GraphCanvas.vue`, `GraphControlBar.vue`, `GraphDetailDrawer.vue`, `useKnowledgeGraphStore.ts`, `pages/library.vue`, `pages/roadmap.vue`, and i18n locales (`en.json`, `vi.json`).
- **Documentation:** Delete `docs/curriculum-30-days.md`, update `AGENTS.md`, `README.md`, `docs/features.md`.
