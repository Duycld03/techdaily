# Tasks

## 1. Frontend - Layout Architecture Simplification

- [x] 1.1 Revert `frontend/components/layout/BentoDashboardLayout.vue` to independent flex column containers (`flex flex-col justify-start`) on `<section>` and `<aside>`, removing `lg:grid-rows-2` and `lg:grid-rows-subgrid`.

## 2. Frontend - Telemetry Cards Refactor & Expansion

- [x] 2.1 Refactor Telemetry Card 1 (Practice Streak) in `frontend/components/dashboard/HomeBentoDashboard.vue` to compact natural vertical spacing, removing `justify-between` and excessive internal margins.
- [x] 2.2 Expand Telemetry Card 2 (Knowledge Radar) in `frontend/components/dashboard/HomeBentoDashboard.vue` with a 2x2 grid displaying 4 key metrics: Total Concepts (`totalNodes`), Active Relations (`totalEdges`), Due Cards (`reviewStats.due`), and Mastered Cards (`reviewStats.mastered`).
- [x] 2.3 Verify and ensure i18n keys for metric labels (`dashboard.active_relations`, `dashboard.stat_mastered_cards`) are properly localized in `frontend/i18n/locales/en.json` and `vi.json`.

## 3. Frontend - Verification & Visual Gate

- [x] 3.1 Run frontend unit tests (`npm test` in `frontend/`) ensuring all component and layout test suites pass.
- [x] 3.2 Execute dual-gate automated visual verification via headless Chromium on Desktop (`1440x900`) and Mobile (`390x844`), verifying the compact Streak card and flush bottom alignment of the Knowledge Radar card.
