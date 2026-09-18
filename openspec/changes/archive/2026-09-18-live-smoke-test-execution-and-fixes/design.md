## Context

Following the implementation of the 3D WebGL cosmos, spherical auto-rotation, visual graph legend, and responsive filter bar, an automated E2E smoke runner (`frontend/e2e/test-graph-roadmap-responsive.mjs`) was introduced to test the application across Desktop ($1920\times 1080$), Tablet ($768\times 1024$), and Mobile ($375\times 812$) viewports.

Executing this suite live against the active frontend environment will surface real-world rendering artifacts, timing issues, or viewport-specific glitches that unit tests in happy-dom cannot detect.

## Goals / Non-Goals

**Goals:**
- Execute `node frontend/e2e/test-graph-roadmap-responsive.mjs --screenshot` against the active application environment.
- Capture and inspect high-resolution visual screenshots for all 3 viewports.
- Detect and systematically resolve any visual, responsive, or interaction defects found during execution.
- Ensure all 316 unit tests continue to pass with zero regressions.

**Non-Goals:**
- Modifying backend APIs or database models.
- Rewriting core Three.js force simulation algorithms.

## Decisions

### 1. Headless Browser Execution with WebGL Simulation
- **Decision**: Execute the Playwright test suite using Chromium with `--enable-webgl --use-gl=angle --no-sandbox` flags.
- **Rationale**: Allows 3D canvas and Cytoscape 2D layouts to initialize and render genuine pixel buffers in headless Linux environments without requiring an X11 display server.

### 2. Defect Remediation Loop
- **Decision**: Follow a strict 4-step inspection cycle:
  1. **Run**: Execute smoke runner with `--screenshot`.
  2. **Inspect**: Review console output, assertion verdicts, and generated PNG files in `frontend/e2e/screenshots/`.
  3. **Fix**: If an assertion fails or visual clipping/misalignment is spotted, apply surgical fixes to the responsible component.
  4. **Verify**: Re-run the smoke runner and the 316 Vitest unit tests until all pass with 100% success.

## Risks / Trade-offs

- **[Risk] Dev server not running during live test**:
  → **Mitigation**: Launch local dev server or verify against production `TARGET_URL` before executing the test script.

## Migration Plan

1. Ensure the target environment is reachable.
2. Run `npm --prefix frontend run test:e2e:graph` (or `node frontend/e2e/test-graph-roadmap-responsive.mjs --screenshot`).
3. Analyze test results and fix any detected defects.
4. Verify with full test suite.
