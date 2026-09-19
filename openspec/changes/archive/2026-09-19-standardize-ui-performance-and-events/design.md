# Design: Standardize UI Performance and Event Hygiene (Batch 1)

## Context

TechDaily's frontend uses Vue 3.5 and Nuxt 3 with `@vueuse/nuxt` installed. Four high-interaction components and pages contain legacy patterns:
1. `pages/graph.vue` stores Cytoscape `Core` in a deep `ref()`.
2. `components/review/FlashcardDeck.vue` attaches `keydown` listeners on window manually.
3. `components/roadmap/RoadmapMindmapCanvas.vue` attaches 5 mouse/touch listeners during pan gestures.
4. `pages/read/[bookId].vue` attaches manual click and keydown listeners with manual removal in `onUnmounted`.

## Goals / Non-Goals

**Goals:**
- Eliminate Vue deep proxy overhead for Cytoscape `Core` in `pages/graph.vue` via `shallowRef`.
- Standardize window keyboard shortcuts in `FlashcardDeck.vue` using VueUse `useEventListener`.
- Cleanly manage mouse/touch pan listeners in `RoadmapMindmapCanvas.vue` with VueUse `useEventListener`.
- Refactor reader typography outside-click to `onClickOutside` and shortcuts to `useEventListener` in `pages/read/[bookId].vue`.
- Ensure all 55 frontend test files (378 tests) continue passing with zero regressions.

**Non-Goals:**
- Rewriting Cytoscape graph layout logic, 3D physics, or node style rules.
- Altering the SM-2 algorithm or flashcard grading semantics.
- Altering reading bookmarking, slice pagination, or AI formatting logic.

## Decisions

### 1. `shallowRef` for Cytoscape in `pages/graph.vue`
- **Rationale**: Vue's `ref()` traverses all properties and getters recursively to wrap them in reactive proxies. Cytoscape `Core` contains thousands of graph elements, private caches, and event listeners. Deep reactivity on `cyInstance` causes unnecessary CPU cycles on every tick and can trigger circular traps. Using `shallowRef<Core | null>(null)` tracks only the top-level pointer assignment (`cyInstance.value = cy`), maintaining 100% functionality with minimal overhead.

### 2. VueUse `useEventListener` for `FlashcardDeck.vue`
- **Rationale**: Currently, `FlashcardDeck.vue` binds `window.addEventListener('keydown', handleKeyDown)` inside `onMounted` and calls `removeEventListener` in `onUnmounted`.
- **Implementation**:
  ```typescript
  useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)
  ```
  VueUse automatically handles listener detachment on component scope disposal, making the component resilient against fast route transitions.

### 3. Declarative Gesture Listeners in `RoadmapMindmapCanvas.vue`
- **Rationale**: `RoadmapMindmapCanvas.vue` attaches `mousemove` and `mouseup` (or `touchmove`, `touchend`, `touchcancel`) on window upon `mousedown` or `touchstart`, then removes them on `endPan`.
- **Implementation**: Retain window-scoped event tracking during active drag, using `useEventListener` instances or conditional early returns (`if (!isPanning.value) return`), ensuring all window listeners are safely unmounted even if the user navigates away mid-drag.

### 4. `onClickOutside` & `useEventListener` in `pages/read/[bookId].vue`
- **Rationale**: Replaces manual `handleTypographyClickOutside` checking and window `keydown` removal in `onUnmounted`.
- **Implementation**:
  - `onClickOutside(typographyDropdownRef, () => { isTypographyOpen.value = false }, { ignore: [typographyTriggerRef] })`
  - `useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)`

## Risks / Trade-offs

- **Vitest Environment Compatibility**:
  - Vitest runs under `happy-dom`. Tests dispatching synthetic `MouseEvent` and `KeyboardEvent` must cleanly bubble to the listeners.
  - *Mitigation*: The `useEventListener` implementation using `typeof window !== 'undefined' ? window : null` ensures seamless execution in both SSR (Node.js) and browser/test (DOM) contexts.
