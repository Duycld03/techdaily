# Proposal: Zero-Stutter Mindmap Pan & Cursor Release Guarantee

## Why
When panning the `/roadmap` Mindmap canvas, users experience two ergonomic defects:
1. **Sticky Fist Cursor (`cursor-grabbing`):** The mouse release event (`mouseup`) is attached only to the container `<div>`. When users pan quickly, the cursor moves outside the canvas boundary or over child elements before releasing the mouse button. The release event is dropped, leaving `isPanning` stuck at `true` until an extra click occurs inside the canvas.
2. **Initial Drag Stutter & Lag:** Mouse drag starts without `e.preventDefault()`, triggering the browser's native text/image drag-selection heuristic (100–200ms hitch). Furthermore, active SVG edges and chapter nodes have CSS `transition-all duration-300` active during dragging, creating artificial visual latency against continuous 60fps mouse movement.

This change upgrades the canvas to standard window-level pointer capture with transition suppression during active drag.

## What Changes
1. **Window-Level Pointer Capture:** Dynamically register `mousemove` and `mouseup` (and corresponding touch listeners) on `window` upon `startPan`, ensuring 100% reliable release capture regardless of where the mouse is released, with clean removal in `endPan` and `onBeforeUnmount`.
2. **Native Drag Selection Suppression:** Add `e.preventDefault()` on non-interactive `mousedown` and `touchstart` to eliminate the first-frame selection hitch.
3. **Transition-Free Dragging:** Suppress CSS transitions (`transition-none`) on SVG transform layers, bezier edges, and foreignObject nodes while `isPanning === true`, restoring transitions when panning ends.
4. **Touch Parity & Boundary Safety:** Ensure single-touch pan gestures register on `window` and clean up gracefully when touch ends or cancels.
5. **Test Suite Alignment (Advisor Guidance):** Align `RoadmapMindmapCanvas.spec.ts` pan test simulations to dispatch `mousemove` and `mouseup` events directly on `window` while maintaining dual-target compatibility, ensuring all test suites accurately verify global capture and remain 100% green.

## Capabilities

### Modified Capabilities
- `roadmap`: Extends `Hierarchical Mindmap Interactive View` with requirements for window-level pointer release capture, zero-stutter drag initiation, and zero-latency transition suppression during active panning.

## Impact
- **Frontend Codebase:** `frontend/components/roadmap/RoadmapMindmapCanvas.vue` and `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`.
- **Zero VPS Overhead Invariant:** 100% client-side DOM interaction update; 0 backend or database changes.
