# Proposal: Frontend UI Phase 4 - Exploratory & Visual Canvases

## Why

Interactive visual canvas surfaces (`frontend/pages/roadmap.vue` and `frontend/pages/graph.vue`) exhibit layout collisions between the visual rendering viewport (Milestone timeline track, Mindmap hierarchical canvas, and Cytoscape 2D/3D knowledge graph) and floating HUD control decks or inspection drawers. On 1080p and 2K screens, canvas nodes and floating overlays suffer from viewport clipping, inconsistent height bounding, and overlapping control clusters. Standardizing these exploratory surfaces ensures seamless full-bleed interaction, clean HUD docking, and fluid inspection across desktop, tablet, and mobile devices.

## What Changes

- **Roadmap Learning Paths (`frontend/pages/roadmap.vue` & `RoadmapMindmapCanvas.vue`)**:
  - Harmonize dual-view toggle between structured Milestones track view and interactive Mindmap canvas with unified visual density.
  - Provide dynamic canvas bounding with viewport-relative sizing (`calc(100dvh - headerHeight)`), floating control decks, and responsive node inspection drawer/popover clearance.
  - Standardize touch interaction hygiene, wheel zoom bounds, and keyboard accessibility on the mindmap canvas.
- **Knowledge Graph Explorer (`frontend/pages/graph.vue`)**:
  - Anchor the 2D/3D Cytoscape knowledge graph canvas with HUD docking controls, responsive legend stack clearance, and dynamic viewport sizing (`h-[calc(100dvh-4rem)]`).
  - Standardize slide-over detail drawer positioning and mobile bottom-sheet transitions without obscuring HUD controls or locator minimap.
- **Canvas Layout Archetype Standard (`system-layout-archetypes`)**:
  - Formalize the `CanvasLayout` archetype establishing full-bleed viewport bounds, floating HUD deck z-index layers, touch event isolation, and slide-over telemetry drawers.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`: Formalize the full-bleed canvas layout archetype with floating HUD control decks, z-index hierarchy, and responsive drawer inspection.
- `knowledge-graph`: Establish Cytoscape 2D/3D viewport sizing invariants, mobile bottom-sheet docking, and legend clearance standards.
- `roadmap`: Formalize mindmap canvas viewport bounding, HUD control placement, and dual-view switcher visual density.

## Impact

- **Affected Surfaces**: `frontend/pages/roadmap.vue`, `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, `frontend/pages/graph.vue`, `frontend/components/graph/GraphControlBar.vue`, `frontend/components/graph/GraphDetailDrawer.vue`, `frontend/components/graph/GraphMinimap.vue`.
- **Dependencies**: Cytoscape.js, Three.js, VueUse (`useEventListener`, `onClickOutside`).
- **Breaking Changes**: Zero breaking changes to API contracts or data models. Pure frontend UI/UX and layout standardization.
