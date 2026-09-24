# Tasks: Frontend UI Phase 4 - Exploratory & Visual Canvases

## 1. Frontend - Knowledge Graph Explorer Canvas Bounding

- [x] 1.1 Upgrade root container in `frontend/pages/graph.vue` to use responsive dynamic viewport height units (`h-[calc(100vh-4rem)] h-[calc(100dvh-4rem)]`), preventing address bar drift on mobile devices
- [x] 1.2 Verify HUD docking layers in `frontend/pages/graph.vue` ensuring `GraphControlBar.vue`, `GraphLegend.vue`, `GraphMinimap.vue`, and `GraphDetailDrawer.vue` adhere to standard z-index clearance (`z-20` controls, `z-40` drawer)

## 2. Frontend - Roadmap Mindmap Canvas Standardization

- [x] 2.1 Upgrade `frontend/components/roadmap/RoadmapMindmapCanvas.vue` container height to responsive viewport bounding (`h-[520px] sm:h-[640px] lg:h-[calc(100dvh-18rem)] lg:min-h-[640px] lg:max-h-[860px]`)
- [x] 2.2 Standardize floating search pod and zoom toolbar HUD docking with pointer event isolation and touch gesture safety in `frontend/components/roadmap/RoadmapMindmapCanvas.vue`
- [x] 2.3 Verify track switcher dropdown z-index in `frontend/pages/roadmap.vue` cleanly hovers above mindmap HUD controls (`z-40` / `z-50`)

## 3. Testing & Verification

- [x] 3.1 Verify graph and roadmap page tests (`frontend/tests/pages/graph.spec.ts`, `frontend/tests/pages/roadmap.spec.ts`, `frontend/tests/utils/roadmapTreeLayout.spec.ts`)
- [x] 3.2 Execute full frontend test suite (`npm test`) and run production build (`npm run build`) to ensure zero visual canvas regressions
