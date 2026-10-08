# Tasks

## 1. Backend Application Layer & Knowledge Graph Projection

- [x] 1.1 Update `CanonicalPillars` in `GetKnowledgeGraphQueryHandler.cs` to include `pillar-MentalModels` and `pillar-HabitsProductivity` with full descriptive metadata.
- [x] 1.2 Verify edge derivation and hub filtering in `GetKnowledgeGraphQueryHandler.cs` to ensure up to 7 pillar hubs are emitted with zero dangling edges.
- [x] 1.3 Add backend unit tests in `GetKnowledgeGraphQueryHandlerTests.cs` verifying `MentalModels` and `HabitsProductivity` book and card nodes emit their corresponding pillar hubs and connect without dangling edges.

## 2. Frontend Visual Design System & Cytoscape Styling

- [x] 2.1 Update `frontend/app/utils/graphVisualTokens.ts`: extend `PillarCategory` with `'MentalModels' | 'HabitsProductivity'`, add Indigo and Emerald color tokens to `CATEGORY_PALETTE`, and update `normalizeCategory` alias matching.
- [x] 2.2 Update `frontend/app/components/graph/GraphCanvas.vue` Cytoscape stylesheets to include node styling for `MentalModels` and `HabitsProductivity` pillar hubs and chunk nodes.
- [x] 2.3 Update `frontend/app/components/graph/GraphControlBar.vue` category filter pills to include `MentalModels` and `HabitsProductivity`.
- [x] 2.4 Update `frontend/app/i18n/locales/en.json` and `frontend/app/i18n/locales/vi.json` with category filter localization keys and update "Engineering Pillars" to "Knowledge Pillars".
- [x] 2.5 Update frontend unit tests in `frontend/tests/utils/graphVisualTokens.spec.ts` to assert canonical color tokens and alias normalization for the new DeepPace categories.

## 3. Verification & Dual-Gate Validation

- [x] 3.1 Run backend unit tests via `dotnet test` to verify 100% passing test suites across all graph query handler invariants.
- [x] 3.2 Run frontend unit tests via `npm test` to verify data contracts and visual token normalization across test suites.
- [x] 3.3 Execute headless browser automated inspection on `/graph` for Desktop (1440x900) and Mobile (390x844) viewports, verifying all 7 pillar constellations, filter chips, and visual legends render cleanly without clipping.
