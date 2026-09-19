# Proposal: Standardize UI Async Timers and Search Debouncing (Batch 4)

## Why

Following the event listener standardization across application views (Batches 1–3), several key components and pages still manage asynchronous intervals, timeouts, and debouncing via manual `setTimeout` / `setInterval` references:
1. **AI Synthesis Elapsed Timer (`components/today/AISynthesisCard.vue`)**: Employs manual `setInterval` in `onMounted` and `clearInterval` in `onUnmounted` to track generation timeout. If unmounting is interrupted, the interval can linger.
2. **Review Deck Search Debounce (`pages/review.vue`)**: Manages a mutable `searchTimer` reference with `clearTimeout` / `setTimeout` on `watch(searchQuery)`.
3. **Notes Search Debounce (`pages/notes.vue`)**: Manages a mutable `searchDebounceTimer` with `clearTimeout` / `setTimeout` on `watch(highlightSearchQuery)`.
4. **Roadmap Mindmap Search Debounce (`components/roadmap/RoadmapMindmapCanvas.vue`)**: Manages a mutable `searchTimeout` with `clearTimeout` / `setTimeout` on `watch(searchQuery)`.
5. **Clipboard Copy Feedback Timers (`components/common/ShikiCodeBlock.vue`, `components/today/TermExplainerModal.vue`)**: Manages manual `copied = true; setTimeout(() => copied = false, 2000)` timeouts.

Standardizing these components against VueUse declarative composables (`useIntervalFn`, `useDebounceFn`, `useClipboard`) guarantees automatic timer teardown upon unmount, eliminates manual timer variable state, and aligns debounced search behavior across all views.

## What Changes

- **AI Synthesis Card Interval Hygiene (`components/today/AISynthesisCard.vue`)**:
  - Refactor manual `setInterval`/`clearInterval` to VueUse `useIntervalFn`.
  - Eliminate manual `onMounted` and `onUnmounted` timer boilerplate.
- **Review Page Search Debounce Hygiene (`pages/review.vue`)**:
  - Refactor manual `searchTimer` (`setTimeout`/`clearTimeout`) to VueUse `useDebounceFn` (300ms).
  - Eliminate manual timer variables and cancellation branches.
- **Notes Page Search Debounce Hygiene (`pages/notes.vue`)**:
  - Refactor manual `searchDebounceTimer` to VueUse `useDebounceFn` (300ms).
  - Eliminate manual timer variables.
- **Roadmap Mindmap Search Debounce Hygiene (`components/roadmap/RoadmapMindmapCanvas.vue`)**:
  - Refactor manual `searchTimeout` to VueUse `useDebounceFn` (150ms).
  - Eliminate manual timeout variables.
- **Clipboard Feedback Standardization (`components/common/ShikiCodeBlock.vue`, `components/today/TermExplainerModal.vue`)**:
  - Refactor manual `setTimeout` copied state to VueUse `useClipboard` with `copiedDuring: 2000`.

## Capabilities

### New Capabilities
*(None - this change standardizes timer and debounce management within existing capabilities)*

### Modified Capabilities
- `core-platform`: Extends frontend composable standards to cover asynchronous intervals (`useIntervalFn`), reactive debouncing (`useDebounceFn`), and clipboard feedback (`useClipboard`).

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Design**: Zero regression or styling shifts.
- **Reliability & Memory**: Guaranteed timer cancellation on route transitions and unmounts, preventing background interval execution.
