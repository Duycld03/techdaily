# Tasks

## 1. WebGL 3D & 2D Cytoscape Performance Optimization

- [x] 1.1 Refactor `GraphCanvas3D.vue` and `GraphCanvas.vue` to store all 3D/canvas instances (`scene`, `camera`, `renderer`, `controls`, `cy`) in `shallowRef`
- [x] 1.2 Implement explicit unmount cleanup for OrbitControls listeners (`start`, `end`) and cancel active requestAnimationFrame loops
- [x] 1.3 Debounce window resize events using VueUse `useDebounceFn` and `useEventListener(window, 'resize')`

## 2. Graph Detail Drawer Mobile Sheet & Responsive Layout

- [x] 2.1 Refactor `GraphDetailDrawer.vue` into dual-mode presentation: side drawer on desktop ($\ge 768\text{px}$) and bottom sheet on mobile ($< 768\text{px}$)
- [x] 2.2 Add touch drag handle, swipe-to-dismiss behavior, and `max-h-[85dvh]` scroll containment
- [x] 2.3 Ensure connected topics grid wraps cleanly into 2 columns on 320px screens

## 3. Safe Area Insets & Responsive Controls

- [x] 3.1 Update `GraphControlBar.vue` and `GraphLegend.vue` to respect bottom safe areas (`env(safe-area-inset-bottom)`)
- [x] 3.2 Verify category pill filters scroll horizontally on narrow mobile screens
- [x] 3.3 Verify unit test suite passes for graph components via `npm test`
