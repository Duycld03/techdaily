## Why

Today (2026-09-18), significant interactive capabilities and visual layout enhancements were shipped across the Knowledge Graph (`/graph`) and Roadmap (`/roadmap`):
1. 3D WebGL celestial galaxy cosmos with dynamic Level-of-Detail (LOD) label decluttering.
2. 360-degree orbital camera auto-rotation with interactive drag pause and clean lifecycle termination.
3. Interactive visual graph legend (`GraphLegend.vue`) with 15% hover-dimming across both 2D and 3D canvases.
4. Responsive `flex-wrap` category pill containers eliminating Vietnamese text truncation (`Engineeri...`).
5. Roadmap track switcher popover with elevated z-index preventing mindmap canvas clipping.

Manually testing these interactive workflows across Desktop (`1920×1080`), Tablet (`768×1024`), and Mobile (`375×812`) viewports in both English and Vietnamese is time-consuming and prone to human oversight. Providing an automated Playwright E2E smoke runner that navigates these critical paths and optionally captures full-page visual screenshots delivers repeatable confidence before and after production deployments.

## What Changes

- **Automated E2E Smoke Script**: Add `frontend/e2e/test-graph-roadmap-responsive.mjs` using Playwright Chromium:
  - Tests 3 canonical viewports: Desktop (`1920×1080`), Tablet (`768×1024`), and Mobile (`375×812`).
  - Tests both English (`en`) and Vietnamese (`vi`) locales.
  - Verifies `/graph`: 2D Cytoscape rendering, 3D WebGL engine initialization, Auto-Rotate HUD toggle, Show-All-Labels LOD toggle, `GraphLegend` presence, and that all 6 category filter pills are visible and wrapped without truncation.
  - Verifies `/roadmap`: Track switcher popover visibility above the SVG mindmap canvas.
  - Supports configurable target environment via `TARGET_URL` (e.g. `TARGET_URL=https://techdaily.app`).
  - Supports optional visual snapshot export via `--screenshot` saving crisp PNG captures into `frontend/e2e/screenshots/` for quick human review.
- **NPM Script Integration**: Add `"test:e2e:graph": "node e2e/test-graph-roadmap-responsive.mjs"` in `frontend/package.json`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Add requirement scenario for automated responsive multi-viewport and bilingual smoke verification.

## Impact

- **Frontend Tooling**: `frontend/e2e/test-graph-roadmap-responsive.mjs` and `frontend/package.json`.
- **Backend / Database**: Zero impact. Pure testing infrastructure.
