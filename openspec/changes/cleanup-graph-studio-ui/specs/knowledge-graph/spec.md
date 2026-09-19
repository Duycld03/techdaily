# Spec Delta

## MODIFIED Requirements

### Requirement: Cyber Neon Telemetry HUD & Animated Knowledge Radar
The knowledge graph visualization surfaces SHALL provide an animated knowledge radar telemetry display on the dashboard overview (`/today`). In the dedicated graph studio (`/graph`), the interface SHALL maintain a clean, uncluttered canvas where essential controls (mode switch, search, filters) reside exclusively in `GraphControlBar.vue`, omitting redundant floating telemetry ribbons or duplicate status counters from the viewport.

In the dashboard Bento Grid, the Knowledge Graph Radar card SHALL render an animated cyber radar display featuring concentric polar coordinate rings, a rotating telemetry sweep indicator, reactive cyber cyan (`#22d3ee`) node blips, and status badges (`ONLINE`, `SYNCED`, node and edge density counts).

#### Scenario: Animated knowledge radar display on dashboard
- **WHEN** user views the Bento Dashboard on `/today`
- **THEN** the Knowledge Radar card displays an animated SVG polar radar with rotating sweep needle, concentric coordinate rings, and cyber cyan node blips representing graph density.

#### Scenario: Telemetry HUD overlay in graph studio
- **WHEN** user navigates to `/graph`
- **THEN** the canvas viewport renders cleanly without floating telemetry HUD ribbons or redundant node/edge counters, keeping the upper screen area uncluttered.

#### Scenario: Studio glassmorphic surface polish
- **WHEN** control bars and legends are rendered in the graph studio
- **THEN** they utilize `.glass-panel` styling, hairline borders (`border-white/[0.08]`), and cyber glow highlights (`glow-subtle`) adhering to the dev studio design tokens.
