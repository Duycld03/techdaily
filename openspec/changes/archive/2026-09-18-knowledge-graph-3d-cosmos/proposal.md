## Why

In the current 2D planar graph visualization (`/graph`), as the user accumulates reading highlights, flashcards, and topics, graph density increases. In a 2D Euclidean plane, cross-cutting relational edges (such as flashcards connecting to highlights across different pillars) mathematically force edge collisions and line overlap ("hairball" clutter), making dense clusters difficult to dissect.

Introducing a 3D WebGL space $(x, y, z)$ eliminates planar edge collisions by dispersing nodes across spherical depth shells. Transforming the graph into a 3D "Cosmic Constellation" allows engineers to orbit 360° around architectural concepts, isolating clusters intuitively. Furthermore, by running 100% client-side via WebGL hardware acceleration, this feature incurs **0 server cost** on Google Cloud Free Tier VPS while achieving 60–120 FPS through distance-based Level-of-Detail (LOD) culling.

## What Changes

- **Dual-Engine Mode Switcher**: Add a 2D Planar / 3D Cosmos toggle in `GraphControlBar.vue`, persisting user preference in `localStorage`.
- **Client-Side 3D WebGL Galaxy Engine**: Implement `GraphCanvas3D.vue` using `3d-force-graph` / Three.js, rendering pillar hubs as luminous galactic centers, topics as planetary nodes, and cards/highlights as orbiting stellar bodies.
- **360° Orbit & Camera Controls**: Support intuitive mouse and touch gestures (orbit rotate, pan, pinch-zoom, and smooth auto-rotation toggle).
- **Zero-Freeze Physics & Battery-Safe Rendering**:
  - Limit 3D force simulation ticks to prevent main-thread UI freeze during initial mounting.
  - Implement render-on-demand, halting the render loop when the camera and nodes are static to eliminate mobile battery drain.
- **Level-of-Detail (LOD) Sprite Text Culling**: Keep overview zoom clean by hiding card/highlight labels until the user zooms in close or hovers over a sphere.
- **Full Interaction Parity with 2D**:
  - Clicking any 3D node smoothly focuses the camera and opens `GraphDetailDrawer.vue` with 1-click action bridges.
  - Category filters and live search dim non-matching 3D nodes and edges with translucent opacity.
  - Dark and light theme synchronization via `@nuxtjs/color-mode`.
- **Lazy Bundle Loading**: Dynamically import the 3D WebGL bundle so visitors only downloading reading slices or quizzes do not load Three.js dependencies.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Add `Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization` and update `Requirement: Multi-Dimensional Graph Filtering & Live Search` with 2D/3D dual-engine toggle specification.

## Impact

- **Frontend Dependencies**: Addition of `3d-force-graph` and `three` to `frontend/package.json` (lazy-loaded asynchronously, ~180KB compressed impact only on `/graph` 3D view).
- **Backend / Database / VPS**: Zero impact. Reuses the existing `GET /api/v1/graph` endpoint without extra queries or server memory allocation.
- **User Experience**: Drastic reduction in visual clutter for complex graphs and interactive 3D spatial exploration.
