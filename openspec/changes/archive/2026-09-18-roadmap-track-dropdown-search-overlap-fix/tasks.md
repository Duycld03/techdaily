## 1. Header Banner & Stacking Context Elevation

- [x] 1.1 In `frontend/pages/roadmap.vue`, remove `z-10` from the inner title and controls flex row to prevent clamping the dropdown popover's stacking context.
- [x] 1.2 In `frontend/pages/roadmap.vue`, dynamically elevate the header banner container to `z-40` when `isTrackMenuOpen` is active (defaulting to `z-20` when closed).

## 2. Mindmap Canvas Floating Controls Calibration

- [x] 2.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, adjust the in-canvas search bar container from `z-20` to `z-10`.
- [x] 2.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, adjust the in-canvas floating toolbar from `z-20` to `z-10`.

## 3. Automated Tests & Quality Assurance

- [x] 3.1 Update unit tests in `frontend/tests/pages/roadmap.spec.ts` to verify that the header banner dynamically elevates its z-index when `isTrackMenuOpen` is true.
- [x] 3.2 Update unit tests in `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` to verify the search bar container renders with `z-10`.
- [x] 3.3 Run `npm --prefix frontend test` and verify all tests pass with zero regressions.
- [x] 3.4 Validate OpenSpec change integrity by running `openspec validate roadmap-track-dropdown-search-overlap-fix --type change` and verifying 0 errors.
