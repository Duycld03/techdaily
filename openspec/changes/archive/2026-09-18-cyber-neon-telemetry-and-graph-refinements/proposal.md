# Proposal: Cyber Neon Telemetry & Knowledge Graph Refinements

## Why

The Knowledge Graph radar card on the Bento Dashboard and the `/graph` visualization studio are TechDaily's technical flagship surfaces. Currently, they display static numerical counters and standard gray borders without the cyber telemetry styling, animated radar sweep, and high-tech telemetry HUD envisioned in Reference Image #5. Modernizing these surfaces with cyber cyan (`#22d3ee`), electric violet (`#8b5cf6`), animated radar telemetry, and studio glassmorphism completes the Dev-Learning Studio aesthetic.

## What Changes

- **Animated Knowledge Radar Widget (`TodayBentoDashboard.vue`)**:
  - Replace static grid blocks in Card E with an interactive cyber radar canvas featuring concentric polar grid rings, rotating sweep needle animation, and reactive node blips.
  - Display real-time telemetry badges: `[ONLINE]`, `[SYNCED]`, and dynamic node/edge ratio indicators.
- **High-Tech Telemetry HUD Overlay (`frontend/pages/graph.vue`)**:
  - Add a floating telemetry HUD ribbon displaying live graph metrics: active node count, edge density, active architectural pillar constellation, and render engine mode (`2D Cytoscape` vs `3D Cosmos`).
- **Studio Glassmorphism & Cyber Accents (`GraphControlBar.vue`, `GraphLegend.vue`)**:
  - Refine graph control bar and legend with `.glass-panel`, hairline borders (`border-white/[0.08]`), and cyan/violet glow highlights (`glow-subtle`).
  - Preserve responsive mobile clearance ($84\text{px}$) and touch-friendly controls.

## Capabilities

### Modified Capabilities

- `knowledge-graph`: Add requirements for cyber neon telemetry HUD, animated radar sweep widget, and studio glassmorphic surface refinements.
