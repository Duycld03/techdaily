## Context

On `/roadmap`, the page displays a top header banner with an interactive Track Switcher dropdown button (`trackMenuRef`), followed by a dual-view switcher and either the linear timeline or the hierarchical mindmap canvas (`RoadmapMindmapCanvas.vue`).

When the user opens the Track Switcher dropdown in mindmap view, the dropdown popover extends downwards. However, the dropdown is visually cut and overlapped by the in-canvas mindmap search bar ("Tìm kiếm chủ đề trong sơ đồ...").

### Stacking Context Root Cause
1. In `frontend/pages/roadmap.vue`, the header banner has `relative z-20 overflow-visible`. Inside this banner, the inner row containing the title and track switcher button is styled with `relative z-10`. This creates an intermediary stacking context that clamps the dropdown popover's `z-50` within level 10 of the header.
2. Sibling containers below the header (the view switcher and mindmap container) appear later in the DOM tree. Inside `RoadmapMindmapCanvas.vue`, the floating search bar is styled with `absolute z-20`.
3. Because the mindmap canvas appears later in DOM order with a matching or higher effective stacking level (`z-20`), the browser renders the search bar over the dropdown popover.

## Goals / Non-Goals

**Goals:**
- Guarantee that the Track Switcher dropdown menu cleanly and unconditionally floats above all subsequent page content, including the mindmap canvas, in-canvas search bar, and floating toolbar.
- Maintain proper layering inside the mindmap canvas so the search bar and toolbar remain above internal SVG nodes and connecting edges.
- Prevent any regression to timeline view, track switching logic, or responsiveness.

**Non-Goals:**
- Modifying backend track pacer APIs or database models.
- Changing the layout coordinates, panning, or zoom behavior of the mindmap tree.

## Decisions

### 1. Remove Inner Row Stacking Clamp in `roadmap.vue`
- **Decision**: Remove `z-10` from `<div class="relative flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">`.
- **Rationale**: The inner row only needs `relative` positioning for layout; assigning `z-10` created an artificial sub-stacking context that trapped the popover (`z-50`). Removing it allows `trackMenuRef` (`z-30`) and popover (`z-50`) to project directly into the header's outer stacking context.

### 2. Elevate Header Banner Stacking Context
- **Decision**: Update the header banner container in `frontend/pages/roadmap.vue` to dynamically apply `z-40` when `isTrackMenuOpen` is true (`:class="[ ..., isTrackMenuOpen ? 'z-40' : 'z-20' ]"`).
- **Rationale**: When the dropdown is open, elevating the entire header to `z-40` guarantees that all dropdown content dominates any subsequent DOM sibling on the page.

### 3. Calibrate Mindmap In-Canvas Floating Controls to `z-10`
- **Decision**: In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, set the in-canvas search bar (`data-testid="mindmap-search-input"` container) and floating toolbar to `z-10` instead of `z-20`.
- **Rationale**: The SVG canvas elements (bezier paths, chapter cards, slice pills) render at default z-index (0). Setting floating canvas controls to `z-10` is more than sufficient to float over SVG nodes while staying below page-level overlays like header dropdowns.

## Risks / Trade-offs

- **[Risk] Mindmap nodes rendering over the search bar**:
  → **Mitigation**: Verified SVG nodes and paths do not have a positive z-index. `z-10` keeps floating controls comfortably above the canvas layer.
- **[Risk] Dropdown clipping on small mobile viewports**:
  → **Mitigation**: Retain `overflow-visible` on the header banner and max-height scrolling (`max-h-[calc(100vh-14rem)] overflow-y-auto`) on the popover.

## Migration Plan

1. Update `frontend/pages/roadmap.vue`:
   - Bind dynamic `z-40` when `isTrackMenuOpen` is true on the header banner.
   - Remove `z-10` on the inner flex row.
2. Update `frontend/components/roadmap/RoadmapMindmapCanvas.vue`:
   - Change search bar and floating toolbar z-index classes from `z-20` to `z-10`.
3. Update automated tests in `frontend/tests/pages/roadmap.spec.ts` and `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`.
