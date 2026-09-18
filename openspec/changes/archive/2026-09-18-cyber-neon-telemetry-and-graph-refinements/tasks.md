# Tasks

## 1. Frontend - Animated Cyber Radar Widget

- [x] 1.1 Create `frontend/components/today/CyberRadarWidget.vue` with pure SVG polar grid coordinates, rotating sweep needle animation, reactive node blips, and telemetry badges.
- [x] 1.2 Update `frontend/components/today/TodayBentoDashboard.vue` to integrate `CyberRadarWidget.vue` into Card E with live node and edge telemetry.

## 2. Frontend - Telemetry HUD & Studio Refinements

- [x] 2.1 Implement floating Telemetry HUD overlay in `frontend/pages/graph.vue` displaying live node count, relation density, selected pillar, and engine render mode.
- [x] 2.2 Modernize `frontend/components/graph/GraphControlBar.vue` and `frontend/components/graph/GraphLegend.vue` with `.glass-panel` surfaces, hairline borders, and cyber glow highlights.

## 3. Verification & Automated Tests

- [x] 3.1 Create unit tests for `CyberRadarWidget.vue` in `frontend/tests/components/today/CyberRadarWidget.spec.ts` and update graph page tests.
- [x] 3.2 Run frontend unit tests (`npm --prefix frontend test`) and backend tests (`dotnet test backend/TechDaily.sln`) to verify zero regressions.
- [x] 3.3 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).
