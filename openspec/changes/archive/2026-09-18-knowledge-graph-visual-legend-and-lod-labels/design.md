## Context

The 3D WebGL knowledge graph view (`/graph`) visualizes architectural concepts as a celestial constellation. However, in initial deployment, all 30+ topic nodes render 3D text billboards simultaneously with the 5 pillar hubs at default overview distances. Because billboards face the camera, text labels overlap heavily and create a visually congested "text storm".

Furthermore, user feedback indicates confusion regarding what the spherical scales and colors represent (e.g. what is a large sphere vs medium sphere, what do amber, blue, and emerald mean on flashcard nodes).

## Goals / Non-Goals

**Goals:**
- Eliminate text clutter in 3D overview by restricting default billboard labels strictly to the 5 primary Pillar Hubs.
- Dynamically reveal topic, book, card, and highlight labels on hover, selection, close zoom ($d < 250$), or when the user toggles the HUD label switch.
- Add an `[Aa]` toggle button in the 3D HUD allowing users to switch between "Clean Cosmos" and "Full Inspection" modes.
- Build an interactive, collapsible visual legend panel (`GraphLegend.vue`) in the bottom-left corner (`bottom-5 left-5`) explaining all node shapes, sizes, category colors, and SM-2 retention metrics.
- Support interactive hover-dimming: hovering over an item in the legend highlights matching nodes on the active canvas (both 2D and 3D) and dims unrelated nodes to 20% opacity.
- Provide full bilingual support in English and Vietnamese (`en/vi`).

**Non-Goals:**
- Modifying backend graph query APIs or database tables.
- Disabling tooltips or node details (rich HTML tooltip on hover and slide-over drawer remain fully functional).

## Decisions

### 1. Strict Overview LOD in `GraphCanvas3D.vue`
- **Decision**: In `GraphCanvas3D.vue`, billboard text sprites for non-pillar nodes (Topics, Books, Cards, Highlights) are not rendered by default at overview camera distances unless `showAllLabels` is true, the node is hovered, or the node is selected.
- **Rationale**: 5 primary Pillar Hubs give clear macro-level orientation (Frontend, Backend, Database, Distributed Systems, Engineering Craft). Culling 30+ topic labels restores the clean aesthetic of the cosmic constellation.
- **Alternatives Considered**: 
  - *Small font size for topics*: Rejected. At overview zoom, small text becomes an unreadable blur and still covers the underlying spheres.

### 2. HUD Label Visibility Toggle (`showAllLabels`)
- **Decision**: Add a button with a `Type` / `[Aa]` icon to the bottom-right floating HUD in `GraphCanvas3D.vue`. Toggling it triggers a reactive graph update that forces billboard labels for all visible nodes.
- **Rationale**: Gives power users the ability to read all labels when scanning for a specific term without having to zoom into each cluster individually.

### 3. Dedicated Legend Component (`GraphLegend.vue`)
- **Decision**: Create `frontend/components/graph/GraphLegend.vue` placed at `bottom-5 left-5` in `pages/graph.vue`, visible in both 2D and 3D modes.
- **Structure**:
  - Glassmorphic container with collapse/expand button (`ChevronDown` / `HelpCircle`).
  - Hierarchy key: Pillar Hub (large), Topic (medium), Book (indigo), Highlight (cyan).
  - SM-2 status key: Learning (amber), Reviewing (blue), Mastered (emerald).
  - Persists collapsed state in `localStorage` under `techdaily_graph_legend_collapsed`.

### 4. Interactive Legend Hover Dimming via Store
- **Decision**: Add `hoveredLegendType = ref<string | null>(null)` to `useKnowledgeGraphStore.ts`. When a user hovers over an item in `GraphLegend.vue`, `setHoveredLegendType(type)` is called.
- **Rationale**: Both `GraphCanvas.vue` (2D) and `GraphCanvas3D.vue` (3D) reactively dim non-matching nodes to 20% opacity and boost matching nodes, allowing instant visual identification of any entity type.

## Risks / Trade-offs

- **[Risk] Legend panel overlapping canvas elements on mobile viewports**:
  → **Mitigation**: On mobile screens ($< 640\text{px}$), the legend defaults to a compact floating badge (`? Legend`) that expands into a modal or compact sheet on tap, avoiding screen obstruction.
- **[Risk] Frequent re-renders when toggling labels in 3D**:
  → **Mitigation**: Use `graphInstance.refresh()` or re-bind `nodeThreeObject` without rebuilding the physics graph data.

## Migration Plan

1. Extend `frontend/stores/useKnowledgeGraphStore.ts` with `hoveredLegendType` and `setHoveredLegendType(type)`.
2. Update `frontend/components/graph/GraphCanvas3D.vue`:
   - Restrict default LOD labels to Pillar Hubs only (`node.type === 'pillar'`).
   - Add `showAllLabels` ref and HUD toggle button.
   - React to `hoveredLegendType` by adjusting node/edge opacities.
3. Update `frontend/components/graph/GraphCanvas.vue` (2D):
   - React to `hoveredLegendType` by updating node class styles.
4. Create `frontend/components/graph/GraphLegend.vue` and mount in `frontend/pages/graph.vue`.
5. Add translation strings in `en.json` and `vi.json`.
6. Add unit tests for `GraphLegend.vue` and updated `GraphCanvas3D.vue`.
