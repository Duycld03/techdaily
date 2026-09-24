# Proposal: Frontend UI Phase 4 - Exploratory & Visual Canvases

## Why

Interactive canvas surfaces (`roadmap.vue` and `graph.vue`) exhibit layout collisions between the visual rendering viewport (Milestone track, Mindmap, Cytoscape 2D/3D knowledge graph) and floating HUD control decks or inspection drawers. On 1080p and 2K screens, canvas nodes and floating overlays suffer from viewport clipping and overlapping control clusters.

## What Changes

- **Roadmap Learning Paths (`frontend/pages/roadmap.vue`)**:
  - Harmonize dual-view toggle between structured Milestones track view and interactive Mindmap canvas.
  - Provide dynamic canvas bounding with viewport-relative sizing (`calc(100vh - headerHeight)`), floating control decks, and node inspection drawers.
- **Knowledge Graph Explorer (`frontend/pages/graph.vue`)**:
  - Anchor the 2D/3D Cytoscape knowledge graph canvas with HUD docking controls, responsive legend stack clearance, and an integrated inspect drawer for selected node telemetry.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Establish canvas layout archetype standards with HUD control clearing and responsive inspect panel docking.
- `knowledge-graph`:
  - Formalize Cytoscape viewport sizing invariants.

## Impact

- **Affected Surfaces**: `frontend/pages/roadmap.vue`, `frontend/pages/graph.vue`.
- **Dependencies**: None. Leverages Cytoscape.js and VueUse.
- **Breaking Changes**: None.
