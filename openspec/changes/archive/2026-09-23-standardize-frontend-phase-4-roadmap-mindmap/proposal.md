# Proposal: Standardize Frontend Phase 4 — Roadmap & Mindmap Studio

## Why

The Roadmap & Mindmap Studio (`pages/roadmap.vue`, `RoadmapMindmapCanvas.vue`, `RoadmapViewSwitcher.vue`) allows engineers to explore curriculum milestones either through a structured timeline or an interactive SVG mindmap tree. Under `antfu/skills` guidelines, this visualization-heavy surface requires standardization to:
1. Optimize reactivity and memory allocation using `shallowRef` for large D3 hierarchy data trees and node layouts.
2. Standardize mobile touch gestures (pinch-to-zoom, single-finger pan) and viewport containment on devices $< 768\text{px}$.
3. Ensure the Dual-View mode switcher (`RoadmapViewSwitcher.vue`) maintains constant border geometry with zero layout shifting.
4. Prevent the curriculum track selection dropdown from clipping or obscuring mindmap controls on narrow displays.

## What Changes

- **Roadmap Page & Timeline View (`pages/roadmap.vue`)**:
  - Audit chapter collapsible accordions and slice grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4`) for mobile responsiveness.
  - Standardize milestone status badges (completed, in-progress, locked) with `whitespace-nowrap shrink-0` and responsive gap.
- **Interactive Mindmap Canvas (`RoadmapMindmapCanvas.vue`)**:
  - Refactor tree hierarchy data to use `shallowRef` instead of deep `ref`, avoiding proxy overhead for hundreds of SVG node objects.
  - Audit touch gesture event listeners (`touchstart`, `touchmove`, `touchend`) with VueUse `useEventListener` and pointer capture.
  - Ensure SVG canvas scales to dynamic container height (`h-[calc(100dvh-12rem)]` on mobile) with smooth zooming bounds.
- **Dual-View Switcher (`RoadmapViewSwitcher.vue`)**:
  - Enforce zero-shift border standard (`border border-transparent` inactive vs `border border-white/[0.12]` active).
  - Use clean icons (`Map` for Timeline, `GitFork` / `Network` for Mindmap) with `:stroke-width="1.5"`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `roadmap`: Standardize mindmap SVG touch gesture handling, `shallowRef` reactivity performance, and mobile dual-view switcher invariants.

## Impact

- **Affected Files**: `frontend/pages/roadmap.vue`, `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, `frontend/components/roadmap/RoadmapViewSwitcher.vue`.
- **Testing**: Unit tests in `frontend/tests/pages/roadmap.spec.ts`, `frontend/tests/components/roadmap/`, and Playwright touch emulation tests.
