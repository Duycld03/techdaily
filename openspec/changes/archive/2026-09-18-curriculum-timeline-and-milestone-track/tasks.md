# Tasks

## 1. Frontend - Continuous Timeline Spine & Milestone Telemetry

- [x] 1.1 Implement continuous vertical milestone connector spine in `frontend/pages/roadmap.vue` for the active document book track.
- [x] 1.2 Implement continuous vertical milestone connector spine in `frontend/pages/roadmap.vue` for the 30-day curriculum track.
- [x] 1.3 Add illuminated active telemetry styling to today's active slice/day node (glowing pulse ring, ember flame badge, fast-action button).

## 2. Frontend - Studio Tokens & Card Refinements

- [x] 2.1 Modernize chapter accordion milestone headers and search toolbar with dark canvas tokens (`bg-canvas-subtle`, `bg-canvas-elevated`, hairline borders `border-white/[0.08]`).
- [x] 2.2 Modernize slice and daily challenge cards with `.glass-card` elevation, clean category pills, and responsive difficulty badges.

## 3. Verification & Automated Tests

- [x] 3.1 Update unit tests in `frontend/tests/pages/roadmap.spec.ts` covering milestone spine rendering and active telemetry highlights.
- [x] 3.2 Run frontend tests (`npm --prefix frontend test`) and backend tests (`dotnet test backend/TechDaily.sln`) to verify zero regressions.
- [x] 3.3 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).
