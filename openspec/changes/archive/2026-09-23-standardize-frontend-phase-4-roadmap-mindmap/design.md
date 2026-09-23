# Design

## Context

See `proposal.md` for motivation. The Roadmap & Mindmap Studio (`pages/roadmap.vue`, `RoadmapMindmapCanvas.vue`, `RoadmapViewSwitcher.vue`) renders curriculum progressions and hierarchical tree diagrams. Under `antfu/skills`, the D3 hierarchy tree must use `shallowRef` to avoid Vue proxy overhead, touch gestures must be standardized for mobile devices, and the dual-view switcher must enforce zero-shift border geometry.

## Goals / Non-Goals

**Goals:**
- Replace deep `ref` with `shallowRef` for large D3 hierarchy nodes in `RoadmapMindmapCanvas.vue`.
- Implement smooth touch pan and pinch-zoom handling using pointer events and VueUse `useEventListener`.
- Enforce zero-shift border styling on `RoadmapViewSwitcher.vue`.
- Ensure curriculum chapter cards in timeline view (`pages/roadmap.vue`) collapse cleanly into `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`.

**Non-Goals:**
- Altering the curriculum seed data or backend progress tracking endpoints.
- Replacing D3 hierarchy layout mathematics.

## Decisions

### 1. `shallowRef` for D3 Hierarchies
- *Rationale*: D3 layout trees contain circular parent/children references and deep node properties. Vue's deep reactive proxy converts thousands of internal objects, causing noticeable GC stalls during zoom and pan interactions. `shallowRef` triggers reactivity only when the whole tree reference changes.

### 2. Pointer Events for Touch Pan/Zoom
- *Rationale*: Mobile browsers handle touch events with default scroll behaviors unless `touch-action: none` is set on the SVG container. Pointer events unify mouse and touch interaction cleanly.

### 3. Constant Border Box in View Switcher
- *Rationale*: Adding a border only on the active tab causes a 1px layout shift. Using `border border-transparent` on inactive tabs ensures constant layout geometry.

## Risks / Trade-offs

- **Risk**: Gesture collision with browser back-swipe on mobile edges.
  - **Mitigation**: Add horizontal margins to the canvas touch interception container.
