# Tasks

## 1. D3 Hierarchy & Mindmap Performance

- [x] 1.1 Update `RoadmapMindmapCanvas.vue` to store D3 tree hierarchies in `shallowRef` to eliminate proxy overhead
- [x] 1.2 Implement touch pan and pinch-to-zoom gestures with `touch-action: none` and bounded scaling ($0.4\times$ to $2.5\times$)

## 2. Dual-View Switcher & Zero-Shift Borders

- [x] 2.1 Audit `RoadmapViewSwitcher.vue` for constant border dimensions (`border border-transparent` vs active hairline border)
- [x] 2.2 Verify switcher button labels use `whitespace-nowrap shrink-0` across English and Vietnamese locales

## 3. Timeline View & Track Switcher

- [x] 3.1 Audit `pages/roadmap.vue` timeline slice grid for responsive multi-column layout (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`)
- [x] 3.2 Ensure curriculum track switcher dropdown clamps cleanly on mobile displays
- [x] 3.3 Verify unit test suite passes for roadmap components via `npm test`
