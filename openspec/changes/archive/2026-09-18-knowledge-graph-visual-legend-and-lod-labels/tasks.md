## 1. Store Extension for Legend Interactions

- [x] 1.1 In `frontend/stores/useKnowledgeGraphStore.ts`, add `hoveredLegendType` ref and `setHoveredLegendType(type: string | null)` to manage interactive legend hover highlighting across canvases.

## 2. 3D Canvas LOD Decluttering & Label Toggle

- [x] 2.1 In `frontend/components/graph/GraphCanvas3D.vue`, restrict default 3D billboard labels to Pillar Hubs only (`node.type === 'pillar'`), culling Topic, Book, Card, and Highlight labels at overview zoom.
- [x] 2.2 In `frontend/components/graph/GraphCanvas3D.vue`, add `showAllLabels` reactive state and a toggle button in the floating HUD (`bottom-5 right-5`) allowing users to switch between Clean Cosmos and Full Inspection modes.
- [x] 2.3 In `frontend/components/graph/GraphCanvas3D.vue`, react to `store.hoveredLegendType` by highlighting matching node spheres and dimming non-matching nodes.

## 3. Interactive Visual Legend Component

- [x] 3.1 Create `frontend/components/graph/GraphLegend.vue` displaying color-coded entity keys (Pillars, Topics, Books, Highlights, SM-2 status colors) with a collapsible toggle and `localStorage` persistence under `techdaily_graph_legend_collapsed`.
- [x] 3.2 In `frontend/components/graph/GraphLegend.vue`, wire hover events to `store.setHoveredLegendType` for interactive graph dimming.
- [x] 3.3 In `frontend/pages/graph.vue`, mount `GraphLegend.vue` positioned in the bottom-left corner (`bottom-5 left-5`).
- [x] 3.4 Add localized translation keys for the legend in `frontend/i18n/locales/en.json` and `vi.json`.

## 4. Automated Tests & Quality Assurance

- [x] 4.1 Create unit test in `frontend/tests/components/graph/GraphLegend.spec.ts` verifying legend item rendering, collapse/expand toggle, and hover interaction.
- [x] 4.2 Update unit test in `frontend/tests/components/graph/GraphCanvas3D.spec.ts` verifying HUD label toggle and LOD label culling.
- [x] 4.3 Run `npm --prefix frontend test` and verify all tests pass with zero regressions.
- [x] 4.4 Validate OpenSpec change integrity by running `openspec validate knowledge-graph-visual-legend-and-lod-labels --type change` and verifying 0 errors.
