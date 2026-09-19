# Proposal: Clean Up /graph UI and Remove Redundant HUD Telemetry Ribbon

## Why

The `/graph` Knowledge Graph studio currently displays a floating "HUD LIVE" telemetry ribbon in the top-right viewport corner displaying node/edge counters (`N: 39 E: 67`) and engine mode (`ENGINE: 3D COSMOS`). This overlay is redundant because the active engine mode (2D vs 3D) is already prominently displayed and interactively controllable via the main `GraphControlBar` switcher, while raw node/edge counts represent low-value debug telemetry that obscures graph nodes and creates visual noise. Removing this floating HUD ribbon declutters the canvas, maximizes visible graph area, and produces a clean, distraction-free architectural exploration experience.

## What Changes

- **Remove Floating HUD Ribbon on `/graph`**: Delete the floating telemetry ribbon container (`<!-- Cyber Neon Telemetry HUD Ribbon (Desktop Top-Right) -->`) in `frontend/pages/graph.vue` containing the pulsing "HUD Live" badge, node/edge counters, and engine mode indicator.
- **Retain Essential Controls**: Ensure the primary `GraphControlBar` (2D/3D switcher, live search, filter chips, fit screen), `GraphLegend`, `GraphMinimap`, and `GraphDetailDrawer` remain fully functional and uncluttered.
- **Preserve Dashboard Radar**: Retain the animated Cyber Radar display on the `/today` dashboard Bento Grid, removing only the redundant overlay from the dedicated `/graph` studio canvas.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `knowledge-graph`: Update `Cyber Neon Telemetry HUD & Animated Knowledge Radar` requirement to eliminate the redundant floating telemetry HUD ribbon from the dedicated `/graph` studio view while retaining the dashboard animated knowledge radar on `/today`.

## Impact

- **Frontend UI**: `frontend/pages/graph.vue` simplified by removing lines 49-76 (the floating HUD markup and its associated telemetry elements).
- **Zero Breaking Changes**: No backend APIs, store actions, or database schemas are altered.
