## Why

On `/roadmap`, when the user clicks the Track Switcher dropdown button in the header banner while the Mindmap view is active, the dropdown popover menu ("TÀI LIỆU ĐANG ĐỌC") extends downwards over the mindmap canvas. However, the mindmap's in-canvas search bar ("Tìm kiếm chủ đề trong sơ đồ...") is rendered with `absolute z-20` and appears later in the DOM tree. Furthermore, the dropdown popover is nested inside an inner header flex row configured with `relative z-10`, which clamps the popover's `z-50` within a lower stacking context.

As a result, the mindmap search bar visually slices through and sits on top of the dropdown menu, obscuring track selection items and library links. Fixing this stacking context hierarchy guarantees that the header dropdown cleanly floats above all canvas controls.

## What Changes

- **Eliminate Header Inner Row Stacking Clamp**: Remove the restrictive `z-10` on the title and controls row (`relative flex flex-col md:flex-row...`) in `frontend/pages/roadmap.vue`, allowing the Track Switcher popover (`z-50`) to surface directly to the header banner's stacking level.
- **Elevate Header Banner Stacking Level**: Update the Header banner in `frontend/pages/roadmap.vue` to `z-30` (dynamically elevating to `z-40` when `isTrackMenuOpen` is true), ensuring the header layer unconditionally dominates subsequent DOM containers.
- **Calibrate Mindmap In-Canvas Floating Controls**: In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, reduce the z-index of the in-canvas search bar and floating toolbar from `z-20` to `z-10`. Since the internal SVG nodes and edges have default z-index (0), `z-10` is optimal to float above SVG elements without colliding with page-level header dropdowns.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `roadmap`: Update `Requirement: Active Track Synchronization & Switcher` to mandate that the track switcher dropdown popover maintains a higher stacking context than all subsequent view containers and in-canvas search/toolbar elements.

## Impact

- **Frontend Components**:
  - `frontend/pages/roadmap.vue`: Header banner and inner row z-index adjustments.
  - `frontend/components/roadmap/RoadmapMindmapCanvas.vue`: In-canvas search bar and floating toolbar z-index adjustments from `z-20` to `z-10`.
- **Backend / Database**: Zero impact. Purely frontend presentation and CSS stacking context correction.
