## 1. Dependencies & Store Extension

- [x] 1.1 Install `3d-force-graph` and `three` in `frontend/package.json` and verify successful package resolution.
- [x] 1.2 Extend `frontend/stores/useKnowledgeGraphStore.ts` with `viewMode: '2d' | '3d'`, `setViewMode(mode)`, and `localStorage` persistence under `techdaily_graph_view_mode`.

## 2. 3D WebGL Canvas Component Implementation

- [x] 2.1 Create `frontend/components/graph/GraphCanvas3D.vue` using `3d-force-graph`, rendering nodes as 3D glowing spheres color-coded by pillar category and SM-2 status.
- [x] 2.2 Implement 360-degree OrbitControls with smooth camera fly-to on node selection, camera reset, and optional subtle galaxy rotation.
- [x] 2.3 Implement distance-based Level-of-Detail (LOD) label culling and bounded physics warmup (max 120 ticks) with idle render-on-demand.
- [x] 2.4 Wire 3D node click and hover events to `useKnowledgeGraphStore.ts` to trigger `GraphDetailDrawer.vue` with 1-click action bridges.

## 3. UI Control Bar & Dual-Engine Integration

- [x] 3.1 In `frontend/components/graph/GraphControlBar.vue`, add a 2D Planar / 3D Cosmos segmented switch button with active indicator and responsive layout.
- [x] 3.2 In `frontend/pages/graph.vue`, integrate dynamic lazy loading for `GraphCanvas3D.vue` wrapped in `<ClientOnly>` with loading fallback.
- [x] 3.3 Add localized translation strings for 2D/3D mode toggles in `frontend/i18n/locales/en.json` and `vi.json`.

## 4. Automated Tests & Quality Assurance

- [x] 4.1 Create unit test in `frontend/tests/components/graph/GraphCanvas3D.spec.ts` verifying viewMode reactivity and component mounting.
- [x] 4.2 Run `npm --prefix frontend test` and verify all tests pass with zero regressions.
- [x] 4.3 Validate OpenSpec change integrity by running `openspec validate knowledge-graph-3d-cosmos --type change` and verifying 0 errors.
