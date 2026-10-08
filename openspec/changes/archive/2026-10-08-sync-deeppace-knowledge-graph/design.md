# Design

## Context

See `proposal.md` for motivation. DeepPace expanded the domain taxonomy across backend entities, AI prompt personas, and document ingestion to support 7 core categories:
1. `FrontendWeb = 0`
2. `BackendRuntime = 1`
3. `DatabaseStorage = 2`
4. `SystemDesign = 3`
5. `EngineeringCraft = 4`
6. `MentalModels = 5`
7. `HabitsProductivity = 6`

However, the knowledge graph projection pipeline and visualization engine remained hardcoded to the 5 engineering pillars. When artifacts belonging to categories 5 and 6 are processed:
1. In `GetKnowledgeGraphQueryHandler.cs`, edge derivation constructs `edge-book-{book.Id}-pillar-{book.Category}` pointing to target `pillar-MentalModels` or `pillar-HabitsProductivity`. But `CanonicalPillars` only contains 5 items. As a result, the target node is never emitted into `nodes`, creating dangling edges that violate graph integrity.
2. In `frontend/utils/graphVisualTokens.ts`, `normalizeCategory(category)` contains no matches for `mental` or `habit`, defaulting to `'BackendDotNet'`. The visual tokens palette lacks distinct keys, causing both categories to inherit .NET sky blue tones.
3. In `frontend/components/graph/GraphCanvas.vue`, the Cytoscape selector stylesheet lacks rules for `MentalModels` and `HabitsProductivity`, while `GraphControlBar.vue` lacks category filter pills for these domains.

## Goals / Non-Goals

**Goals:**
- Expand backend `CanonicalPillars` in `GetKnowledgeGraphQueryHandler.cs` from 5 to 7 pillars to include `pillar-MentalModels` and `pillar-HabitsProductivity`.
- Ensure zero dangling edges in the graph projection: any edge linking to a pillar hub is guaranteed to resolve to an emitted pillar node.
- Support emission of between 0 and 7 pillar hub nodes based on user artifact activity.
- Extend frontend `PillarCategory` type union and `CATEGORY_PALETTE` with canonical color tokens for the 2 new domains:
  - `MentalModels`: Deep Iris / Indigo (`#6366f1`, border `#818cf8`)
  - `HabitsProductivity`: Vibrant Emerald / Teal (`#10b981`, border `#34d399`)
- Update `normalizeCategory` to match keywords (`mental`, `habit`, `productivity`).
- Add Cytoscape selectors in `GraphCanvas.vue` for pillar hubs and chunk/topic nodes of both categories.
- Add category filter pills in `GraphControlBar.vue` and corresponding i18n localization keys in `en.json` and `vi.json`.
- Update `en.json` and `vi.json` to rename "Engineering Pillars" / "Cột trụ kỹ thuật" to "Knowledge Pillars" / "Cột trụ tri thức".
- Provide full unit test coverage for backend query handler and frontend visual tokens.

**Non-Goals:**
- Modifying the underlying database schema or EF Core migrations (the `Category` enum already defines integer values 5 and 6).
- Changing the SM-2 algorithm or flashcard review mechanics.
- Altering the 3D physics engine or Cytoscape layout algorithms beyond color/selector tokens and filter options.

## Decisions

### 1. Canonical Pillars Extension in Backend
We define the two new canonical pillars directly in `CanonicalPillars` in `GetKnowledgeGraphQueryHandler.cs`:
```csharp
("pillar-MentalModels", "Mental Models & Decisions", Category.MentalModels, "First Principles, Cognitive Biases, Inversion", "Foundational cognitive frameworks, multi-disciplinary mental models, and structured decision-making mechanisms."),
("pillar-HabitsProductivity", "Habits & Deep Work", Category.HabitsProductivity, "Habit Loops, Focus Rituals, Attention Management", "Deliberate practice systems, environmental cue design, ultradian focus blocks, and sustainable daily pace.")
```
*Rationale*: Matches the existing `(string Id, string Label, Category Category, string Subtitle, string Summary)` pattern exactly. Preserves the `pillar-{Category}` ID convention and emits these hubs if and only if `referencedPillars.Contains(cat)`.

### 2. Frontend Visual Color Harmony
DeepPace unites deliberate technical craftsmanship with mental models and lifestyle habits.
The color assignments are:
- `FrontendWeb`: Amber (`#f59e0b`, border `#fbbf24`)
- `BackendDotNet`: Sky (`#0284c7`, border `#38bdf8`)
- `DatabaseStorage`: Cyan (`#0891b2`, border `#22d3ee`)
- `SystemDesign`: Purple (`#7c3aed`, border `#a78bfa`)
- `EngineeringCraft`: Rose/Pink (`#ec4899`, border `#fb7185`)
- `MentalModels`: Deep Iris / Indigo (`#6366f1`, border `#818cf8`)
- `HabitsProductivity`: Vibrant Emerald / Teal (`#10b981`, border `#34d399`)

*Rationale*:
- Indigo evokes deep focus, philosophical clarity, and cognitive reasoning.
- Emerald evokes growth, atomic habits, daily streak vitality, and deliberate practice energy.
- Both colors offer high contrast against the obsidian `#09090b` canvas in dark mode and light slate in light mode, avoiding collision with existing amber, sky, cyan, purple, and rose nodes.

### 3. Normalization and Filter Invariance
`normalizeCategory(category)` in `graphVisualTokens.ts` will check:
```typescript
if (c.includes('mental')) return 'MentalModels'
if (c.includes('habit') || c.includes('productivity')) return 'HabitsProductivity'
```
In `GraphControlBar.vue`:
Add `MentalModels` and `HabitsProductivity` to `categoryPills`. The pill container uses `flex-wrap`, so 8 chips will wrap gracefully on mobile and tablet without clipping, meeting the bilingual responsive layout invariant.

## Risks / Trade-offs

- **Risk: Cytoscape style specificity on topic chunks**:
  - *Mitigation*: Cytoscape selector rules will explicitly target `node[type = "chunk"][category = "MentalModels"]` and `node[type = "chunk"][category = "HabitsProductivity"]`, mirroring existing rules for `DatabaseStorage` and `SystemDesign`.
- **Risk: Existing tests asserting exact pillar counts**:
  - *Mitigation*: Existing unit tests in `GetKnowledgeGraphQueryHandlerTests.cs` (e.g. testing only Backend activity emits 1 pillar hub) will remain passing, while new dedicated tests will verify multi-category graphs including `MentalModels` and `HabitsProductivity`.
