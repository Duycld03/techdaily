# Spec Delta: knowledge-graph

## ADDED Requirements

### Requirement: Cyber Neon Telemetry HUD & Animated Knowledge Radar
The knowledge graph visualization surfaces SHALL provide cyber neon telemetry displays across both the dashboard overview (`/today`) and the dedicated graph studio (`/graph`).

In the dashboard Bento Grid, the Knowledge Graph Radar card SHALL render an animated cyber radar display featuring concentric polar coordinate rings, a rotating telemetry sweep indicator, reactive cyber cyan (`#22d3ee`) node blips, and status badges (`ONLINE`, `SYNCED`, node and edge density counts).

In the dedicated graph studio (`/graph`), the interface SHALL render a floating telemetry HUD ribbon displaying live graph metrics, including connected node count, edge density, selected pillar constellation, and active render mode (`2D Cytoscape` vs `3D Cosmos`).

#### Scenario: Animated knowledge radar display on dashboard
- **WHEN** user views the Bento Dashboard on `/today`
- **THEN** the Knowledge Radar card displays an animated SVG polar radar with rotating sweep needle, concentric coordinate rings, and cyber cyan node blips representing graph density.

#### Scenario: Telemetry HUD overlay in graph studio
- **WHEN** user navigates to `/graph`
- **THEN** a floating telemetry HUD ribbon displays real-time metrics including total nodes, relations count, active constellation category, and engine render mode.

#### Scenario: Studio glassmorphic surface polish
- **WHEN** control bars and legends are rendered in the graph studio
- **THEN** they utilize `.glass-panel` styling, hairline borders (`border-white/[0.08]`), and cyber glow highlights (`glow-subtle`) adhering to the dev studio design tokens.
