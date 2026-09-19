# Proposal: Standardize UI Lifecycle and Event Hygiene (Batch 3)

## Why

Completing TechDaily's UI event listener modernization under `antfu/skills` and `@vueuse/nuxt` conventions (following Batch 1 performance/canvas hygiene and Batch 2 dropdown/dialog hygiene), five remaining components and pages across the application still rely on manual `addEventListener` / `removeEventListener` lifecycle management:
1. **Dropdown Floating Positioning (`components/common/AppSelect.vue`)**: Attaches manual `window.addEventListener('scroll', ...)` and `window.addEventListener('resize', ...)` with manual removals in `closeDropdown` and `onUnmounted`.
2. **2D Graph Canvas Responsiveness (`components/graph/GraphCanvas.vue`)**: Manages window `resize` using manual `addEventListener` in `onMounted` and `removeEventListener` in `onBeforeUnmount`.
3. **Reader Pane Selection & Typography (`components/today/DocReaderPane.vue`)**: Employs manual `document.addEventListener('click')` and `document.addEventListener('keydown')` for typography popover dismissal and selection menu unbinding.
4. **Insights Feed Keyboard Navigation (`pages/insights.vue`)**: Binds manual window `keydown` listeners for card carousel navigation (`Space`, `ArrowLeft`, `ArrowRight`).
5. **Flashcard Review Tab Navigation (`pages/review.vue`)**: Binds manual window `keydown` listeners for review session keyboard shortcuts.

Standardizing these five components with declarative VueUse composables (`useEventListener`, `onClickOutside`) eliminates listener leaks during interrupted unmounts, guarantees SSR safety, and unifies event management across the entire frontend.

## What Changes

- **Custom Select Dropdown Hygiene (`components/common/AppSelect.vue`)**:
  - Replace manual window `scroll` and `resize` listeners with declarative VueUse `useEventListener` (with passive and capture options).
  - Eliminate manual window listener cleanup boilerplate in `openDropdown`, `closeDropdown`, and `onUnmounted`.
- **2D Knowledge Graph Canvas Hygiene (`components/graph/GraphCanvas.vue`)**:
  - Replace manual window `resize` listener with VueUse `useEventListener`.
  - Eliminate manual `removeEventListener` in `onBeforeUnmount`.
- **Today Reader Pane Hygiene (`components/today/DocReaderPane.vue`)**:
  - Refactor typography popover outside-click detection to VueUse `onClickOutside`.
  - Refactor document click and Escape `keydown` listeners to VueUse `useEventListener`.
  - Eliminate manual `document.removeEventListener` in `onUnmounted`.
- **Insights Feed Hygiene (`pages/insights.vue`)**:
  - Refactor feed navigation `keydown` listener to VueUse `useEventListener`.
  - Eliminate manual `window.removeEventListener` in `onUnmounted`.
- **Review Page Hygiene (`pages/review.vue`)**:
  - Refactor tab navigation `keydown` listener to VueUse `useEventListener`.
  - Eliminate manual `window.removeEventListener` in `onUnmounted`.

## Capabilities

### New Capabilities
*(None - this change standardizes event handling and component lifecycles within existing capabilities)*

### Modified Capabilities
- `core-platform`: Expands declarative DOM event and lifecycle hygiene requirements to cover select dropdowns, 2D graph canvas, reader pane popovers, insights feeds, and review tab navigation.

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Design**: Zero regression or styling shifts.
- **Reliability**: 100% declarative event listener management across all interactive views, eliminating dangling document and window listeners.
