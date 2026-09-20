# Proposal: Standardize Frontend Phase 5 — Knowledge Graph 3D & 2D Cosmos

## Why

The Knowledge Graph Studio (`pages/graph.vue`) provides an interactive 3D cosmos (via Three.js / `3d-force-graph`) and 2D architecture constellation (via Cytoscape.js) representing engineering concepts. Under `antfu/skills` standards, this WebGL/canvas-intensive system requires standardization to:
1. Guarantee peak runtime performance and zero memory leaks by strictly enforcing `shallowRef` for all Three.js scenes, cameras, ForceGraph3D, and Cytoscape core instances.
2. Standardize OrbitControls and auto-rotation listeners in `GraphCanvas3D.vue` using VueUse lifecycle hygiene and automatic teardown.
3. Optimize the node inspection drawer (`GraphDetailDrawer.vue`) for mobile viewports ($< 768\text{px}$), transforming it into a responsive touch-swipeable bottom sheet with `85dvh` max height and safe area insets.
4. Ensure floating canvas controls (`GraphControlBar.vue`) and category legends (`GraphLegend.vue`) respect mobile bottom safe areas (`env(safe-area-inset-bottom)`) without obscuring graph interactions.

## What Changes

- **WebGL & 2D Canvas Engines (`GraphCanvas.vue`, `GraphCanvas3D.vue`)**:
  - Enforce `shallowRef` on `graphInstance`, `renderer`, `scene`, `camera`, and `controls` to prevent Vue deep proxy traps on 3D meshes and graph node collections.
  - Standardize OrbitControls start/end auto-rotation listeners with safe unmount cleanup.
  - Audit WebGL context loss handling and window resize debounce via VueUse `useDebounceFn` and `useEventListener(window, 'resize')`.
- **Node Detail Inspection Drawer (`GraphDetailDrawer.vue`)**:
  - Implement dual-mode presentation: right-side drawer on desktop ($\ge 768\text{px}$), bottom-sheet modal on mobile ($< 768\text{px}$) with touch drag handle.
  - Ensure 2-column connected topics grid (`grid-cols-2 gap-2`) adapts cleanly on 320px screens.
- **Canvas Controls & Legend (`GraphControlBar.vue`, `GraphLegend.vue`, `GraphMinimap.vue`)**:
  - Pin the floating control bar with dynamic safe area clearance (`pb-[max(0.75rem,env(safe-area-inset-bottom))]`).
  - Enable horizontal scroll snapping for category pill filters on mobile screens.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Standardize WebGL/canvas shallow reactivity, mobile bottom-sheet detail drawer, and responsive control bar clearance specifications.

## Impact

- **Affected Files**: `frontend/pages/graph.vue`, `frontend/components/graph/*.vue`.
- **Testing**: Unit tests in `frontend/tests/pages/graph.spec.ts`, `frontend/tests/components/graph/`, and WebGL mock test suites.
