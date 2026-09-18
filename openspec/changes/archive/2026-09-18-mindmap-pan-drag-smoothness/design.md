# Design: Zero-Stutter Mindmap Pan & Cursor Release Guarantee

## Context
TechDaily's `/roadmap` Mindmap canvas allows users to drag-to-pan across large technical knowledge trees. In the current implementation:
```html
<div
  @mousedown="startPan"
  @mousemove="onMouseMove"
  @mouseup="endPan"
  @mouseleave="endPan"
  :class="isPanning ? 'cursor-grabbing' : 'cursor-grab'"
>
```
This local-element binding exhibits three distinct failure modes:
1. **Sticky Fist Cursor:** If the mouse leaves the container boundary during rapid movement, or releases over certain child elements, the container element does not receive `mouseup`. The canvas retains `isPanning = true`, locking the cursor in `cursor-grabbing` until an intentional second click inside the container clears it.
2. **First-Frame Hitch (100–200ms lag):** Without `e.preventDefault()` on `mousedown`, modern browsers run native drag-and-drop / text-selection anticipation heuristics before delegating mousemove to JavaScript.
3. **Transition Interpolation Lag:** Connecting SVG bezier paths have `transition-all duration-300` active during drag. Rapid 60fps pan updates conflict with CSS transitions, producing perceived drag friction.

---

## Goals & Non-Goals

### Goals
- **100% Guaranteed Cursor Release:** Moving or releasing the pointer anywhere on the screen (including off-canvas or outside the browser window) immediately terminates panning and resets cursor to `cursor-grab`.
- **Instantaneous Drag Start:** Zero frame hitch when clicking down to drag.
- **Zero-Latency Dragging:** 1:1 responsive tracking with zero CSS interpolation lag during dragging.
- **Leak-Free Lifecycle:** Safe listener registration on `window` with guaranteed cleanup in `endPan` and `onBeforeUnmount`.
- **Touch Gesture Parity:** Smooth touch-panning behavior with identical window-level cleanup.

### Non-Goals
- Altering the mathematical tree layout or bezier path algorithms in `roadmapTreeLayout.ts`.
- Replacing SVG rendering with Canvas 2D or WebGL.

---

## Decisions & Implementation Architecture

### 1. Window-Level Pointer Capture Pattern
Instead of binding `@mousemove` and `@mouseup` statically to the container `<div>`, we attach them dynamically to `window` only while actively dragging:

```
[User clicks canvas]
        │
        ▼
   startPan(e)
        ├── e.preventDefault()
        ├── isPanning = true
        ├── dragStart = (e.clientX - pan.x, e.clientY - pan.y)
        ├── window.addEventListener('mousemove', onMouseMove)
        └── window.addEventListener('mouseup', endPan)
        │
   [User drags across screen / off-window]
        │
        ├── onMouseMove(e) updates pan.value (zero frame hitch)
        │
   [User releases mouse ANYWHERE]
        │
        ▼
     endPan()
        ├── isPanning = false
        ├── window.removeEventListener('mousemove', onMouseMove)
        └── window.removeEventListener('mouseup', endPan)
```

### 2. Lifecycle Teardown Invariant
To prevent orphan listeners on `window` if the component unmounts mid-drag (e.g. user triggers a hotkey or navigation while dragging):
```ts
onBeforeUnmount(() => {
  if (typeof window !== 'undefined') {
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', endPan)
    window.removeEventListener('touchmove', onTouchMove)
    window.removeEventListener('touchend', onTouchEnd)
  }
})
```

### 3. Transition Suppression During Active Drag
When `isPanning === true`, transition classes on bezier edges and SVG transform groups are conditionally swapped:
```vue
:class="[
  isPanning ? 'transition-none' : 'transition-all duration-300',
  ...
]"
```
This guarantees that while the user is physically dragging, updates occur with zero latency, and smooth transitions re-engage immediately upon release for zoom and focus actions.

### 4. Test Harness Alignment (Advisor Guidance)
Because active panning event listeners migrate from local container element bindings to `window`, unit tests in `RoadmapMindmapCanvas.spec.ts` must dispatch simulated movement and release events on `window`:
```ts
// Start pan on canvas container
await container.trigger('mousedown', { clientX: 100, clientY: 100 })

// Move and release dispatched on window
window.dispatchEvent(new MouseEvent('mousemove', { clientX: 150, clientY: 150 }))
window.dispatchEvent(new MouseEvent('mouseup'))
```
To ensure robust test compatibility and guard against edge cases, container-level `@mousemove` and `@mouseup` may also remain connected as delegated proxies during the transition, ensuring all test suites continue passing seamlessly.

---

## Risks & Trade-offs

| Risk | Impact | Mitigation |
|---|---|---|
| **Interfering with Interactive Children:** `e.preventDefault()` on `mousedown` could block clicks on buttons or nodes. | High | Guard check at the start of `startPan`: if `target?.closest('.interactive-node') || target?.closest('button') || target?.closest('input')`, immediately return without calling `preventDefault()`. |
| **Window Listener Leaks:** Unconsumed listeners if unmounted during pan. | Low | Symmetric removal in both `endPan` and Vue's `onBeforeUnmount` lifecycle hook. |
