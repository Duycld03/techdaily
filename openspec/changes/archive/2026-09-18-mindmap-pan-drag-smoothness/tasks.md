# Tasks: Zero-Stutter Mindmap Pan & Cursor Release Guarantee

## 1. Window-Level Pointer Capture & Drag Ergonomics

- [x] 1.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, update `startPan` to call `e.preventDefault()` for non-interactive elements, set `isPanning = true`, and dynamically register `mousemove` and `mouseup` event listeners on `window`.
- [x] 1.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, update `endPan` to remove `window` mouse event listeners and reset `isPanning = false`.
- [x] 1.3 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, add `onBeforeUnmount` lifecycle hook to ensure all window-level mouse and touch listeners are cleanly removed if the component unmounts mid-drag.
- [x] 1.4 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, apply corresponding window-level listener registration and cleanup to touch gestures in `onTouchStart`, `onTouchMove`, and `onTouchEnd`.

## 2. Transition Suppression During Active Drag

- [x] 2.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, conditionally apply `transition-none` instead of `transition-all duration-300` on SVG bezier curve paths when `isPanning` is `true`, eliminating visual friction and latency during active pointer drag.

## 3. Automated Tests & Verification

- [x] 3.1 In `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`, align pan test simulations to dispatch `mousemove` and `mouseup` events on `window` (per advisor guidance) to accurately verify window-level pointer capture, ensure `isPanning` resets even when released outside the container, and keep all existing and new test suites 100% green.
- [x] 3.2 Run `npm --prefix frontend test -- tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` to verify all component tests pass.
- [x] 3.3 Run full test suites `npm --prefix frontend test` and `dotnet test backend/tests/TechDaily.Tests` to verify zero application regressions.
- [x] 3.4 Run `openspec validate mindmap-pan-drag-smoothness --strict` to confirm 100% specification compliance.
