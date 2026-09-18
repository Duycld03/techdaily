## Why

With the responsive E2E smoke runner (`frontend/e2e/test-graph-roadmap-responsive.mjs`) integrated, executing live tests against the running application provides immediate empirical proof of real-world rendering behavior across Desktop ($1920\times 1080$), Tablet ($768\times 1024$), and Mobile ($375\times 812$) viewports.

While unit tests validate isolated component state and calculations, running the live E2E suite exercises full browser layout rendering, Three.js WebGL canvas initialization, Cytoscape 2D DOM geometry, responsive font wrapping, and popover layering. Executing the live suite with visual screenshot exports and systematically resolving any uncovered defects guarantees rock-solid production quality.

## What Changes

- **Live Test Execution**: Launch the application runtime and execute `node frontend/e2e/test-graph-roadmap-responsive.mjs --screenshot` targeting the active environment.
- **Visual & Interaction Audit**:
  - Inspect generated screenshots and execution logs across all 3 viewports.
  - Check for edge cases such as canvas dimension shifts, text collision, scrollbar flashes, z-index layering, and mobile touch targets.
- **Defect Resolution**: Implement surgical fixes for any defects uncovered during the test run in:
  - `frontend/components/graph/GraphControlBar.vue`
  - `frontend/components/graph/GraphCanvas3D.vue`
  - `frontend/components/graph/GraphCanvas.vue`
  - `frontend/components/graph/GraphLegend.vue`
  - `frontend/components/roadmap/RoadmapMindmapCanvas.vue`
- **Re-Verification**: Re-run the smoke test suite and the 316-test unit suite to confirm complete resolution with zero regressions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Update `Requirement: Application Navigation & Mobile Responsive Placement` to mandate that live responsive smoke test suites pass with 100% assertion success and zero visual truncation across Desktop, Tablet, and Mobile viewports.

## Impact

- **Frontend Components**: Targeted components based on live test findings.
- **Testing**: `frontend/e2e/test-graph-roadmap-responsive.mjs` and related component spec files.
- **Backend / Database**: Zero impact.
