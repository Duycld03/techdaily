# Tasks

## 1. Frontend - Design Tokens & Theme Configuration

- [x] 1.1 Update `frontend/tailwind.config.js` with neutral obsidian canvas (`#09090b`, `#121215`, `#18181b`) and refined Iris Violet brand palette.
- [x] 1.2 Update `frontend/assets/css/main.css` to refine glassmorphic utility classes (`.glass-card`, `.glass-panel`, `.glow-subtle`) for clean, neutral translucency and hairline borders.

## 2. Frontend - Dashboard & Navigation Decluttering

- [x] 2.1 Update `frontend/components/layout/AppSidebar.vue` to replace heavy purple active background with neutral glass surface and 2px left accent bar.
- [x] 2.2 Update `frontend/components/today/TodayBentoDashboard.vue` to remove ambient purple radial glows, de-purple secondary labels/icons, and elevate the primary CTA focal hierarchy.
- [x] 2.3 Refine `frontend/components/today/ConcentricMetricCard.vue` and `frontend/components/today/CyberRadarWidget.vue` for balanced, muted secondary telemetry contrast.

## 3. Verification & Automated Tests

- [x] 3.1 Run frontend unit test suite (`npm --prefix frontend test`) to verify zero regressions across all 52 test files.
- [x] 3.2 Run backend test suite (`dotnet test backend/TechDaily.sln`) to ensure total platform stability.
- [x] 3.3 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).
