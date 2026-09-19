# Proposal: Standardize UI Performance and Event Hygiene (Batch 1)

## Why

Following the adoption of `antfu/skills` and `@vueuse/nuxt`, TechDaily's interactive frontend views have several critical performance and lifecycle hygiene opportunities:
1. **Graph Reactivity Overhead**: `pages/graph.vue` stores the Cytoscape `Core` instance in a deep Vue `ref()`. Because Cytoscape instances hold massive internal object graphs, caches, and circular event references, Vue's recursive proxying introduces significant memory overhead and hampers rendering FPS during 2D/3D graph interactions.
2. **Keyboard & Touch Listener Boilerplate**: `components/review/FlashcardDeck.vue`, `components/roadmap/RoadmapMindmapCanvas.vue`, and `pages/read/[bookId].vue` manually bind and unbind `window.addEventListener` / `document.addEventListener` for keyboard shortcuts (`Space`, `1-4`, `Shift+Arrows`, `Escape`), touch gestures, and outside-click events. These manual bindings risk memory leaks when component destruction cycles are interrupted by route navigations or SSR transitions.

Standardizing these four hot spots against `antfu/skills` conventions ensures seamless event lifecycle cleanup and immediate frame-rate improvements.

## What Changes

- **Graph Canvas Performance (`pages/graph.vue`)**:
  - Replace `ref<Core | null>(null)` with `shallowRef<Core | null>(null)` for the Cytoscape instance, eliminating recursive proxy creation.
- **Flashcard Deck Event Hygiene (`components/review/FlashcardDeck.vue`)**:
  - Refactor manual window `keydown` listener to VueUse `useEventListener` / `onKeyStroke` for card flipping (`Space`), grading (`1-5`), and canceling (`Escape`).
  - Eliminate manual `onMounted` / `onUnmounted` listener boilerplate.
- **Roadmap Mindmap Gesture Hygiene (`components/roadmap/RoadmapMindmapCanvas.vue`)**:
  - Refactor window mouse and touch pan-drag listeners (`mousemove`, `mouseup`, `touchmove`, `touchend`, `touchcancel`) to use VueUse `useEventListener` with automatic cleanup upon drag completion and component unmount.
- **Reader Navigation Hygiene (`pages/read/[bookId].vue`)**:
  - Refactor typography popover outside-click detection to `onClickOutside`.
  - Refactor global shortcut navigation (`Shift + ArrowLeft/Right`, `Escape`) to VueUse `useEventListener`.
  - Eliminate manual `window.removeEventListener` in `onUnmounted`.

## Capabilities

### New Capabilities
*(None - this change optimizes performance and lifecycle hygiene within existing capabilities)*

### Modified Capabilities
- `core-platform`: Strengthens requirements for interactive canvas memory management (`shallowRef`) and declarative VueUse event handling.

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Design**: Zero regression or styling shifts.
- **Performance & Reliability**: Zero listener leaks across route transitions and smoother graph rendering.
