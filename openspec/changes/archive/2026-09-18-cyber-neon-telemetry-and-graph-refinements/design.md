# Design: Cyber Neon Telemetry & Knowledge Graph Refinements

## Context

TechDaily's Knowledge Graph surfaces span two primary touchpoints:
1. **Dashboard Overview (`TodayBentoDashboard.vue` - Card E)**: Displays static node and edge count boxes without animated telemetry.
2. **Dedicated Studio (`/graph`)**: A full-screen 2D Cytoscape / 3D Three.js canvas with floating controls and legend, but lacking real-time telemetry telemetry HUD readouts.

## Goals / Non-Goals

**Goals:**
- Create `CyberRadarWidget.vue`: A high-performance pure SVG radar display with concentric polar coordinates, rotating radar sweep line, reactive cyber cyan node blips, and telemetry badges (`ONLINE`, `SYNCED`).
- Integrate `CyberRadarWidget.vue` into Card E of `TodayBentoDashboard.vue`.
- Implement a floating Telemetry HUD overlay on `/graph` displaying live graph vitals (node count, edge density, active architectural pillar constellation, and engine mode `2D Cytoscape` / `3D Cosmos`).
- Polish `GraphControlBar.vue` and `GraphLegend.vue` with `.glass-panel`, hairline borders (`border-white/[0.08]`), and cyan/violet cyber glow accents (`glow-subtle`).
- Maintain $100\%$ zero regression on all existing graph interactions (filtering, search, pillar selection, 2D/3D toggle, minimap).

**Non-Goals:**
- Zero backend API or database schema changes.
- No modifications to the underlying Cytoscape physics simulation or Three.js particle shader pipelines.

## Decisions

### Decision 1: Pure SVG & CSS Keyframes for Radar Telemetry
Implement `CyberRadarWidget.vue` using pure SVG elements:
- Polar grid: Concentric circles ($R = 18, 36, 54, 70\text{px}$) with crosshairs and tick marks in `stroke-cyber-500/20` and `stroke-brand-500/30`.
- Sweep needle: A gradient radial wedge rotating via CSS `@keyframes radar-sweep` (`transform: rotate(360deg)` with `transform-origin: center`).
- Reactive blips: SVG circles placed at polar offsets representing active node clusters, pulsing with `opacity` and `scale`.
- *Rationale*: Guarantees crisp resolution on Retina/HiDPI screens, minimal CPU consumption ($<0.5\%$), and seamless reactivity with Pinia state without Canvas context memory leaks.

### Decision 2: Floating Telemetry HUD on `/graph`
Position a high-tech HUD status pill in `frontend/pages/graph.vue`:
- Renders:
  - `[● ONLINE]` live pulsing status.
  - `NODES: {count}` and `EDGES: {count}` with cyber cyan typography.
  - `MODE: 2D GRAPH` or `MODE: 3D COSMOS`.
  - `PILLAR: {selectedPillar || 'ALL CONSTELLATIONS'}`.
- Style: `.glass-panel` with hairline border `border-white/[0.08]`, backdrop blur, and mono telemetry font.
- Collapsible on mobile viewports to prevent obstruction.

### Decision 3: Studio Surface Refinements
- `GraphControlBar.vue`: Upgrade container and search input to `.glass-panel`, replacing harsh slate borders with `border-white/[0.08]`, cyber cyan active search focus rings, and violet filter chips.
- `GraphLegend.vue`: Refine legend header and node item badges with glowing indicators.

## Risks / Trade-offs

- **Risk**: Performance degradation from continuous CSS radar rotation on low-end devices.
  - **Mitigation**: The sweep is GPU-composited via `transform: rotate()`, avoiding layout recalculations or paint cycles.
