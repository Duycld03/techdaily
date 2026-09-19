# Tasks

## 1. Graph Performance Optimization

- [x] 1.1 In `frontend/pages/graph.vue`, replace `ref<Core | null>(null)` with `shallowRef<Core | null>(null)` for `cyInstance` to prevent deep reactive proxy overhead on Cytoscape.

## 2. Review Studio Event Hygiene

- [x] 2.1 In `frontend/components/review/FlashcardDeck.vue`, refactor manual window `keydown` listener to VueUse `useEventListener`, eliminating manual `onMounted` and `onUnmounted` listener boilerplate.

## 3. Roadmap Mindmap Gesture Hygiene

- [x] 3.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, refactor pan/drag event listeners (`mousemove`, `mouseup`, `touchmove`, `touchend`, `touchcancel`) to VueUse `useEventListener` with safe lifecycle cleanup.

## 4. Book Reader DOM & Navigation Hygiene

- [x] 4.1 In `frontend/pages/read/[bookId].vue`, refactor typography dropdown click-outside and keyboard navigation shortcuts to VueUse `onClickOutside` and `useEventListener`.

## 5. Automated Verification & Testing

- [x] 5.1 Run frontend test suite (`npm --prefix frontend test`) ensuring 100% pass across all 55 test files.
- [x] 5.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
