# Proposal

## Why
AI agents and developers frequently declare frontend UI tasks complete based solely on passing headless unit tests (`npm test` via Vitest + Happy-DOM), skipping the critical visual inspection gate. Because Happy-DOM does not compute CSS layout geometry, box models, text wrapping, or flex/grid collisions, severe visual regressions (such as the row height stretching and dead space discovered in `/notes`) slip through unnoticed. To prevent recurring visual regressions, `AGENTS.md` and the core platform specification must strictly define an automated browser screenshot protocol and enforce a Dual-Gate Verification rule that prohibits signing off on frontend changes without visual screenshot proof.

## What Changes
- **Enforce Automated Browser Screenshot Protocol in `AGENTS.md` (Pillar 3 & 5)**:
  - Codify the mandatory programmatic execution steps for verifying any frontend UI modification (`.vue`, styles, layouts):
    1. Launch headless Chromium via the `browser` device in `eval`.
    2. Inject authentic user session cookies (`techdaily_token`, `techdaily_user`) and local storage when navigating authenticated views.
    3. Programmatically capture screenshots at both **Desktop (1440x900 / 1920x1080)** and **Mobile (390x844)** viewports.
    4. Provide the captured screenshot images directly in the verification output.
- **Strict Prohibition of Premature Task Sign-Off (Dual-Gate Verification Invariant)**:
  - Define the two mandatory gates for any frontend UI change:
    - **Gate 1 (Behavioral Data Contract):** Vitest suite (`npm test`) passes data contracts, validation, store state, and lifecycle handling.
    - **Gate 2 (Visual Integrity Gate):** Direct browser screenshot capture verifying responsive layout, typography, text wrapping, and zero collisions.
  - Strictly prohibit marking tasks complete (`- [x]`) in `tasks.md` or yielding "Implementation Complete" to the user based solely on Gate 1.
- **Update Core Platform Specification (`specs/core-platform/spec.md`)**:
  - Elevate `Requirement: Frontend Testing Boundaries & Visual Inspection Standard` with scenarios requiring automated browser screenshot capture and prohibiting single-gate sign-off.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Codify the Dual-Gate Verification Invariant and the Automated Browser Screenshot Protocol for all frontend UI changes.

## Impact
- **Agent Governance & Guidelines**: `AGENTS.md` (Pillar 3: Frontend Testing Boundaries & Visual Inspection; Pillar 5: Verification, DevOps & Skills Protocol).
- **Core Platform Specification**: `openspec/specs/core-platform/spec.md`.
- **Engineering Workflow**: All future frontend changes will mandate automated browser execution and visual screenshot evidence before completion.
