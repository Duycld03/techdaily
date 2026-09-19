# Tasks

## 1. Graph Studio Template Cleanup

- [x] 1.1 In `frontend/pages/graph.vue`, remove the floating telemetry HUD ribbon markup (`<!-- Cyber Neon Telemetry HUD Ribbon (Desktop Top-Right) -->`, lines 49–76) including the pulsing "HUD Live" status badge, node/edge counters (`N: ... E: ...`), and engine mode indicator.

## 2. Verification and Quality Assurance

- [x] 2.1 Validate OpenSpec changes and main specifications using `openspec validate --changes` and `openspec validate --specs`.
- [x] 2.2 Run frontend unit tests (`npm test` in `frontend/`) and verify Nuxt production build (`npm run build` in `frontend/`).
