# Design: Clean Up /graph UI and Remove Redundant HUD Telemetry Ribbon

## Context

The `/graph` route (`frontend/pages/graph.vue`) renders a floating HUD ribbon in the top-right corner of the canvas viewport:
```vue
<!-- Cyber Neon Telemetry HUD Ribbon (Desktop Top-Right) -->
<div class="absolute top-18 sm:top-20 right-3 sm:right-4 z-20 pointer-events-none hidden md:block">
  <div class="pointer-events-auto glass-panel px-3.5 py-2 flex items-center gap-3 text-xs font-mono select-none glow-subtle border border-slate-200/90 dark:border-white/[0.08]">
    ...
  </div>
</div>
```
This element shows an animated pulsing cyan status dot ("HUD Live"), node/edge counters (`N: ... E: ...`), and active engine mode (`ENGINE: 3D COSMOS` / `2D GRAPH`).

The engine mode is already selectable and highlighted in the primary `GraphControlBar.vue` centered at the top of the viewport. Raw node and edge counts are low-value debug telemetry that obscures celestial bodies and graph nodes in the upper-right canvas region.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- Remove the floating HUD telemetry ribbon markup from `frontend/pages/graph.vue`.
- Declutter the canvas viewport to maximize unobstructed graph visualization space.
- Preserve full functionality of `GraphControlBar.vue` (search, 2D/3D mode switch, filter pills, fit screen), `GraphLegend.vue`, `GraphMinimap.vue`, and `GraphDetailDrawer.vue`.
- Preserve the animated Cyber Radar card on the `/today` dashboard.

**Non-Goals:**
- Removing or altering the reactive state properties (`store.filteredNodes`, `store.filteredEdges`) in `useKnowledgeGraphStore.ts`, as they drive empty-state detection (`filteredNodes.length === 0`).
- Altering the 2D Cytoscape layout or 3D WebGL Three.js physics calculation pipelines.

## Decisions

### Decision 1: Direct Removal of Floating HUD Overlay

- **Action**: Completely remove lines 49–76 in `frontend/pages/graph.vue`.
- **Rationale**: The ribbon serves no interactive purpose (it is purely passive readout with `pointer-events-none` wrapper) and duplicates information already present in `GraphControlBar.vue`. Removing the template block leaves the canvas clean and unobstructed.

### Decision 2: Retention of Pinia Store Node/Edge State

- **Action**: Retain `store.filteredNodes` and `store.filteredEdges` computed getters in `useKnowledgeGraphStore.ts`.
- **Rationale**: These properties are essential for rendering the empty state overlay (`v-else-if="!store.isLoading && !store.error && store.filteredNodes.length === 0"`), so no store modifications are needed.

## Risks / Trade-offs

- **Zero Functional Risk**: The HUD ribbon has no click handlers, mutations, or downstream event bindings. Removing it has no side effects on graph rendering, physics simulation, or route lifecycle.
