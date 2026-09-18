## Context

TechDaily currently visualizes the user's architectural knowledge graph at `/graph` using Cytoscape.js on a 2D HTML5 Canvas (`GraphCanvas.vue`). The backend API `GET /api/v1/graph` serves a consolidated JSON payload of nodes (Pillars, Topics, Books, Cards, Highlights) and edges (relational links and shared tags).

While the 2D layout functions well for sparse graphs, expanding user notes and flashcards increases edge density. In a 2D plane, cross-cluster edges inevitably cross over each other, creating visual "hairball" clutter. Transitioning to 3D WebGL provides an extra spatial dimension $(z)$ that naturally eliminates edge crossing while turning the knowledge base into an engaging cosmic constellation.

## Goals / Non-Goals

**Goals:**
- Provide an interactive 3D WebGL Galaxy visualization running 100% in the browser with hardware acceleration.
- Implement a seamless 2D / 3D engine toggle in `GraphControlBar.vue` with `localStorage` persistence.
- Guarantee 0 server impact: no extra VPS CPU, memory, or bandwidth consumption on Google Cloud Free Tier.
- Ensure 60 FPS performance on both desktop and mobile through bounded force simulation, render-on-demand, and Level-of-Detail (LOD) label culling.
- Maintain full feature parity with 2D (filtering by pillar, node type toggles, live search, node selection, and detail drawer actions).
- Lazy-load the 3D WebGL bundle so non-graph routes and initial 2D loads do not incur Three.js payload weight.

**Non-Goals:**
- Replacing 2D completely (2D remains available as a fast, planar overview).
- Server-side 3D graph layout computation or spatial indexing.
- WebXR / VR headset integration.

## Decisions

### 1. Library Selection: `3d-force-graph` (Three.js based)
- **Decision**: Use `3d-force-graph` (powered by Three.js and `d3-force-3d`) for the 3D WebGL canvas.
- **Rationale**:
  - Mature, purpose-built library for 3D force-directed networks with native WebGL hardware acceleration.
  - Includes out-of-the-box `OrbitControls` (360° mouse/touch rotation, pan, zoom, auto-rotation).
  - Easily customizable node geometries (spheres with pillar/status colors), glowing emissive materials, link arrows, and sprite text billboards.
  - Provides camera fly-to animations (`cameraPosition()`) when a user selects a node.
- **Alternatives Considered**:
  - *Custom Three.js from scratch*: Maximum control, but requires writing custom force simulation loops, raycasting selection, and label billboards (~800 lines of boilerplate).
  - *Cytoscape 3D plugin*: Less mature than Three.js ecosystem, sluggish performance with custom shaders.

### 2. Lazy Bundle Loading via Nuxt Dynamic Component
- **Decision**: Wrap the 3D canvas in `defineAsyncComponent(() => import('~/components/graph/GraphCanvas3D.vue'))` inside `<ClientOnly>` in `pages/graph.vue`.
- **Rationale**: Keeps the core frontend bundle lean. Three.js and `3d-force-graph` (~180KB gzipped) are only downloaded when the user actively selects the 3D view mode.

### 3. Rendering Performance & Battery Protection
- **Decision**:
  - **Bounded Warmup**: Set `warmupTicks: 80` and `cooldownTicks: 120`. After 120 ticks, the force physics engine stops completely, preventing continuous background CPU usage.
  - **Level-of-Detail (LOD)**: Render card and highlight labels using `three-spritetext` with distance culling (`nodeLabel` or conditional visibility), keeping the cosmos clean from afar and legible up close.
  - **Render-on-Demand**: When the camera is static and cooldown has finished, pause the continuous render loop. Interaction events (rotate, zoom, pan, hover) re-awaken rendering.

### 4. Single Unified Store (`useKnowledgeGraphStore.ts`)
- **Decision**: Both `GraphCanvas.vue` (2D) and `GraphCanvas3D.vue` (3D) consume the same reactive state from `useKnowledgeGraphStore.ts` (`filteredNodes`, `filteredEdges`, `selectedNode`).
- **Rationale**: Ensures that switching between 2D and 3D retains active search queries, pillar filters, and selected nodes without duplicate state or re-fetching.

## Risks / Trade-offs

- **[Risk] Low-end mobile devices struggle with WebGL**:
  → **Mitigation**: 2D Canvas remains the default or immediate 1-click fallback. Bounded ticks and render-on-demand prevent phone overheating.
- **[Risk] SSR hydration mismatch (WebGL requires `window` and Canvas)**:
  → **Mitigation**: Render `GraphCanvas3D.vue` exclusively inside `<ClientOnly>` with a lightweight skeleton loader during initial script hydration.
- **[Risk] Text labels colliding in 3D space**:
  → **Mitigation**: Distance-based LOD culling hides lower-priority labels (cards/highlights) until the user hovers or zooms within close range.

## Migration Plan

1. Install frontend dependencies: `npm --prefix frontend install 3d-force-graph three @types/three`.
2. Update `useKnowledgeGraphStore.ts` to manage `viewMode: '2d' | '3d'` with `localStorage` persistence.
3. Build `frontend/components/graph/GraphCanvas3D.vue` with Three.js sphere nodes, glow materials, OrbitControls, and event bindings.
4. Add the 2D / 3D segmented toggle to `frontend/components/graph/GraphControlBar.vue`.
5. Update `frontend/pages/graph.vue` to render dynamic component `<component :is="viewMode === '3d' ? GraphCanvas3D : GraphCanvas" />`.
6. Add component unit tests for the 3D canvas mount and store viewMode transitions.
