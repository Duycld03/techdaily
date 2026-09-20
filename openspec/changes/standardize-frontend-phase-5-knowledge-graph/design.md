# Design

## Context

See `proposal.md` for motivation. The Knowledge Graph Studio (`pages/graph.vue`) renders high-density interactive 3D WebGL scenes (Three.js / `3d-force-graph`) and 2D node graphs (Cytoscape.js). Under `antfu/skills`, strict `shallowRef` patterns are required to eliminate Vue proxy traps on 3D meshes, OrbitControls listeners must be cleanly torn down on unmount, and the inspection drawer must adapt to a touch-swipeable bottom sheet on mobile devices.

## Goals / Non-Goals

**Goals:**
- Enforce `shallowRef` for `ForceGraph3D`, Three.js objects (`scene`, `camera`, `renderer`), and Cytoscape core.
- Safely clean up OrbitControls listeners in `GraphCanvas3D.vue` (lines 368-372) on component unmount.
- Transform `GraphDetailDrawer.vue` into a responsive bottom sheet on viewports $< 768\text{px}$ with `max-h-[85dvh]` and safe area clearance.
- Add dynamic safe area bottom clearance to `GraphControlBar.vue` and `GraphLegend.vue`.

**Non-Goals:**
- Modifying backend PostgreSQL graph projection queries or relational node generation.
- Replacing Three.js or Cytoscape with alternative rendering engines.

## Decisions

### 1. `shallowRef` for WebGL and 3D Instances
- *Rationale*: Three.js `Scene` objects contain recursive hierarchies, matrices, and geometry buffers. Deep Vue reactivity traverses the entire scene graph on every tick, degrading frame rates from 60fps to <15fps. `shallowRef` prevents proxying while preserving component-level reactivity.

### 2. Dual-Mode Presentation for Detail Drawer
- *Rationale*: A fixed 400px right-side drawer completely covers the graph canvas on mobile screens and causes viewport clipping. Rendering as a bottom sheet with a swipe handle provides a natural mobile experience.

### 3. Safe Area Inset Clearances
- *Rationale*: Floating canvas controls positioned with `bottom-4` collide directly with the iOS virtual home indicator bar. Using `pb-[max(0.75rem,env(safe-area-inset-bottom))]` guarantees touch safety.

## Risks / Trade-offs

- **Risk**: WebGL context loss when switching rapidly between 2D and 3D views.
  - **Mitigation**: Implement clean `dispose()` routines on renderers, geometries, and materials in `onBeforeUnmount`.
