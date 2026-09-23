# Tasks

## 1. Frontend Governance & Playground Infrastructure

- [x] 1.1 Codify Rules 19, 20, and 21 in `AGENTS.md` (Mandatory Layouts, Prohibition of Native Select, Playground Sandbox & Screenshot Protocol).
- [x] 1.2 Scaffold `frontend/pages/playground/` directory with a guide `README.md` and initial interactive prototyping route.

## 2. Bento Dashboard Layout Primitive

- [x] 2.1 Implement `BentoDashboardLayout.vue` (`frontend/components/layout/BentoDashboardLayout.vue`) with `#header`, `#action-stage`, `#telemetry-dock`, and `#footer` slots.
- [x] 2.2 Prototype modernized dashboard in `frontend/pages/playground/dashboard.vue`, verify 1080p density, and capture screenshot proof.
- [x] 2.3 Refactor `frontend/components/dashboard/HomeBentoDashboard.vue` to adopt `BentoDashboardLayout`.

## 3. Settings & Notes Layout Adoption

- [x] 3.1 Refactor `frontend/pages/settings.vue` to adopt `MasterDetailLayout.vue` with 256px rail and 2-column form grid.
- [x] 3.2 Refactor `frontend/pages/notes.vue` to adopt `BoardLayout.vue` with sticky filter toolbar and 2-to-3 column auto-flowing card grid.
- [x] 3.3 Equalize Tier 3 column heights in `frontend/pages/profile.vue`.

## 4. Verification & Testing

- [x] 4.1 Write component unit tests for `BentoDashboardLayout.vue` in `frontend/tests/components/BentoDashboardLayout.spec.ts`.
- [x] 4.2 Verify full application test suite passes (`npm test`) and capture headless visual verification screenshots of `/`, `/settings`, and `/notes`.
