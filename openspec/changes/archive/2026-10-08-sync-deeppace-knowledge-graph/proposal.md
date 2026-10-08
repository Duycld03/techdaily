# Proposal

## Why

While the platform pivot to DeepPace successfully expanded the domain taxonomy across the application to include **Mental Models & Decisions** (`Category.MentalModels = 5`) and **Habits & Deep Work** (`Category.HabitsProductivity = 6`), the Knowledge Graph backend (`GetKnowledgeGraphQueryHandler.cs`) and frontend renderers (`graphVisualTokens.ts`, `GraphCanvas.vue`, `GraphCanvas3D.vue`) remain trapped in the legacy 5 software engineering pillars.

Consequently, when learners ingest literature or review flashcards for mental models or deep work habits:
1. The backend edge derivation generates edges targeting `pillar-MentalModels` and `pillar-HabitsProductivity`, but because these pillars are omitted from `CanonicalPillars`, the query returns dangling edges pointing to non-existent nodes, violating domain invariants.
2. The frontend category normalizer (`normalizeCategory`) fails to recognize categories 5 and 6, falling back to `BackendDotNet`. On the 2D Cytoscape and 3D WebGL cosmos canvas, mindset and habit cards are miscolored and visually lumped into the .NET backend constellation.

## What Changes

- **Extend Backend Canonical Pillars to 7 DeepPace Domains (`GetKnowledgeGraphQueryHandler.cs`)**:
  - Add `pillar-MentalModels`: "Mô Hình Tư Duy & Quyết Định" ("Mental Models & Decisions") with first principles, cognitive biases, and structured decision-making descriptions.
  - Add `pillar-HabitsProductivity`: "Thói Quen & Tập Trung Sâu" ("Habits & Deep Work") with deliberate practice, focus protocols, and sustainable pace descriptions.
  - Eliminate dangling edge generation: books, chunks, and orphan review cards in categories 5 and 6 now link to valid, first-class pillar hub nodes (supporting 0..7 emitted hubs).
- **Expand Frontend Graph Visual Tokens & Palette (`graphVisualTokens.ts`)**:
  - Extend `PillarCategory` type union to include `'MentalModels'` and `'HabitsProductivity'`.
  - Add distinct studio color tokens to `CATEGORY_PALETTE`:
    - `MentalModels`: Deep Iris / Indigo (`#6366f1`, border `#818cf8`) representing cognitive architecture and deep thinking.
    - `HabitsProductivity`: Vibrant Emerald / Teal (`#10b981`, border `#34d399`) representing habit loops, growth, and daily streak vitality.
  - Update `normalizeCategory` to recognize mental models and habits keywords/aliases instead of falling back to `BackendDotNet`.
- **Update 2D Cytoscape & 3D WebGL Canvas Renderers (`GraphCanvas.vue`, `GraphCanvas3D.vue`)**:
  - Add Cytoscape selectors in `GraphCanvas.vue` for `MentalModels` and `HabitsProductivity` pillar hubs and chunk/topic nodes.
  - Update 3D canvas and minimap renderers to display the expanded palette.

## Capabilities

### Modified Capabilities
- `knowledge-graph`: Expand canonical pillar hubs to 7 DeepPace domains, eliminate dangling edges for categories 5 and 6, and assign dedicated visual palette tokens.

## Impact

- **Backend**: `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQueryHandler.cs`, `backend/tests/TechDaily.Tests/Application/GetKnowledgeGraphQueryHandlerTests.cs`.
- **Frontend**: `frontend/utils/graphVisualTokens.ts`, `frontend/components/graph/GraphCanvas.vue`, `frontend/components/graph/GraphCanvas3D.vue`, `frontend/tests/utils/graphVisualTokens.spec.ts`.
- **Database / Schema**: Zero database migrations or breaking changes. Uses existing integer enum mappings `5` and `6` in PostgreSQL.
