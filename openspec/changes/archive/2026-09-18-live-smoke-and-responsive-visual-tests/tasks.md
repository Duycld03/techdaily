## 1. Automated Responsive E2E Smoke Script

- [x] 1.1 Create `frontend/e2e/test-graph-roadmap-responsive.mjs` implementing multi-viewport testing (Desktop $1920\times 1080$, Tablet $768\times 1024$, Mobile $375\times 812$) and bilingual switching (`en`/`vi`).
- [x] 1.2 Implement verification checks for `/graph`: 2D/3D switcher, Auto-Rotate HUD toggle, Show-All-Labels LOD toggle, `GraphLegend` presence, and `flex-wrap` category filter pills without horizontal text truncation.
- [x] 1.3 Implement verification checks for `/roadmap`: Track switcher popover elevated z-index and SVG mindmap canvas interaction.
- [x] 1.4 Implement optional visual screenshot export to `frontend/e2e/screenshots/` when `--screenshot` CLI flag is provided.

## 2. Package Scripts & Git Configuration

- [x] 2.1 In `frontend/package.json`, register script `"test:e2e:graph": "node e2e/test-graph-roadmap-responsive.mjs"`.
- [x] 2.2 In `frontend/.gitignore`, ignore `e2e/screenshots/` to prevent committing generated PNG images.

## 3. Verification & Validation

- [x] 3.1 Validate script syntax and execution with `node --check frontend/e2e/test-graph-roadmap-responsive.mjs`.
- [x] 3.2 Run `npm --prefix frontend test` and verify all 316 unit tests continue to pass with zero regressions.
- [x] 3.3 Validate OpenSpec change integrity by running `openspec validate live-smoke-and-responsive-visual-tests --type change` and verifying 0 errors.
