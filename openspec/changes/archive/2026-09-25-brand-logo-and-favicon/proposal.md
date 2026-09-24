# Proposal: Brand Logo & Favicon Redesign with Stitch Developer Emblem

## Why
The current TechDaily branding uses a generic Lucide `BookOpen` icon inside a basic gradient square for both the topbar navigation emblem and the browser tab favicon (`favicon.svg`). This generic book iconography fails to communicate TechDaily's core mission: a high-leverage developer learning cockpit combining distributed systems documentation, Ebbinghaus SM-2 spaced repetition, and interactive code comprehension.

Integrating the custom **Stitch Developer Emblem** (vector squircle canvas, violet/purple gradient hexagon outer ring, inner architecture hairline hexagon, symmetrical `< >` code brackets, and central glowing memory core node) establishes a distinctive, high-end engineering brand identity across browser tabs, desktop topbars, mobile navigation drawers, and the authentication cockpit.

## What Changes
- **Universal Scalable Favicon (`frontend/public/favicon.svg`)**:
  - Replace the legacy green book SVG with the optimized Stitch developer emblem vector.
  - Ensure high legibility at 16x16, 32x32, and 48x48 pixel rendering dimensions in dark and light browser chrome.
- **Reusable Brand Logo Component (`frontend/components/common/AppLogo.vue`)**:
  - Create an accessible, reusable SVG logo component supporting flexible dimensions (`sm`, `md`, `lg`, or custom pixel sizes) and responsive layouts.
  - Encapsulate the complete SVG definitions (gradients, squircle boundary, symmetrical hexagon ring, code brackets, central glowing core node).
- **Application Shell Header Integration (`frontend/components/layout/AppHeader.vue`)**:
  - Replace the generic `BookOpen` gradient box in the desktop topbar with `AppLogo.vue`.
  - Replace the legacy emblem in the mobile drawer brand header with `AppLogo.vue`.
  - Maintain the existing routing invariant (clicking the logo navigates to root `/`).
- **Studio Auth Cockpit Integration (`frontend/pages/login.vue`)**:
  - Update the brand emblem in the `/login` header navigation bar to utilize the new Stitch logo mark, harmonizing with the dark ambient canvas and violet glow effects.
- **Visual & Cross-Mode Polish**:
  - Verify crisp anti-aliasing and color fidelity across both dark mode (`#070709` / canvas) and light mode (`slate-50`).

## Capabilities

### Modified Capabilities
- `core-platform`: Update topbar branding and shell navigation requirements to specify the Stitch Developer Emblem (squircle, hexagon, code brackets, glowing core node) and universal SVG favicon.
