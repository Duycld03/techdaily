# Spec Delta: Core Platform

## MODIFIED Requirements

### Requirement: Frontend Composable Utilities & DOM Lifecycle Hygiene
The web frontend SHALL integrate `@vueuse/nuxt` to standardize declarative DOM event listeners, outside-click detection, debounce utilities, asynchronous interval timers, delayed prefetch lookahead timers, and clipboard interactions across Vue components. Components requiring document-level event listeners, outside-click triggers, window resize tracking, global keyboard shortcuts, repeating interval timers, debounced search queries, or delayed lookahead prefetching MUST use VueUse composables (`onClickOutside`, `useEventListener`, `useDebounceFn`, `useIntervalFn`, `useTimeoutFn`, `useClipboard`) rather than manual `document.addEventListener`, `window.addEventListener`, `setInterval`, or `setTimeout` bindings to eliminate memory leaks, timer pollution, and ensure SSR hydration safety. Furthermore, heavy third-party canvas or rendering instances (such as Cytoscape `Core` or Three.js objects) SHALL be wrapped in `shallowRef` rather than deep `ref` to prevent recursive reactive proxying.

#### Scenario: User clicks outside a floating dropdown or modal
- **WHEN** a component utilizing `onClickOutside` (such as `AppSelect.vue` or `QuickHelpModal.vue`) is open and user clicks outside its target boundary
- **THEN** the component state closes smoothly without throwing SSR mismatch warnings or leaving unmanaged event listeners on unmount.

#### Scenario: Keyboard shortcuts and window event listeners detach cleanly
- **WHEN** a component registering window shortcuts via `useEventListener` is unmounted
- **THEN** all associated event listeners are automatically detached by VueUse without manual `onBeforeUnmount` boilerplate.

#### Scenario: Heavy graph canvas instance uses shallowRef
- **WHEN** `pages/graph.vue` initializes and stores the Cytoscape `Core` instance
- **THEN** the reference is stored using `shallowRef` to avoid recursive proxy overhead
- **AND** graph canvas interactions and layout calculations execute without Vue reactivity lag.

#### Scenario: Mindmap drag-to-pan listeners manage lifecycle safely
- **WHEN** user initiates dragging or touch panning in `RoadmapMindmapCanvas.vue`
- **THEN** mouse and touch movement listeners are managed via `useEventListener`
- **AND** all event listeners are cleanly detached upon pan release or component unmount.

#### Scenario: Page dropdown menus utilize onClickOutside for outside-click dismissal
- **WHEN** the slice selection menu in `pages/today.vue` or track menu in `pages/roadmap.vue` is open and user clicks outside
- **THEN** the menu closes cleanly via `onClickOutside` without manual document event listeners.

#### Scenario: Global command palette and graph drawers manage keyboard shortcuts via useEventListener
- **WHEN** `AppCommandPalette.vue` or `GraphDetailDrawer.vue` binds keyboard triggers (such as `Cmd+K` or `Escape`)
- **THEN** the shortcuts are attached using `useEventListener`
- **AND** all event listeners are automatically released upon unmount without manual removal boilerplate.

#### Scenario: Custom select dropdown manages window scroll and resize via useEventListener
- **WHEN** `AppSelect.vue` opens its floating options menu
- **THEN** window `scroll` and `resize` listeners tracking the floating trigger position are managed via declarative `useEventListener`
- **AND** all listeners are automatically released upon dropdown closure or component unmount without manual `removeEventListener` calls.

#### Scenario: 2D graph canvas manages resize responsiveness via useEventListener
- **WHEN** `GraphCanvas.vue` is mounted
- **THEN** window `resize` events triggering canvas relayout are managed via `useEventListener`
- **AND** unmounting the canvas cleanly releases the window resize listener alongside Cytoscape instance destruction.

#### Scenario: Reader pane typography popover and document clicks manage lifecycle cleanly
- **WHEN** `DocReaderPane.vue` opens its typography settings or selection menu
- **THEN** typography popover dismissal uses `onClickOutside` and keydown shortcuts use `useEventListener`
- **AND** all document click and keydown bindings detach automatically upon component unmount.

#### Scenario: Insights feed and review tabs handle navigation shortcuts via useEventListener
- **WHEN** user navigates cards in `pages/insights.vue` or switches tabs in `pages/review.vue` using keyboard navigation
- **THEN** window `keydown` listeners are registered via `useEventListener`
- **AND** all listeners detach automatically upon leaving the respective page routes.

#### Scenario: Library in-progress books and upload status poll via useIntervalFn with auto-teardown
- **WHEN** `pages/library.vue` initiates polling for PDF processing status or background processing books
- **THEN** repeating polling calls are managed via `useIntervalFn`
- **AND** all intervals automatically terminate upon component destruction without dangling background requests.

#### Scenario: Next-day pacer slice prefetching executes via useTimeoutFn
- **WHEN** `pages/today.vue` schedules prefetching for the next day's reading slice after initial focus load
- **THEN** the 2500ms delay is governed by `useTimeoutFn`
- **AND** pending prefetch timers cancel cleanly upon route changes or locale switching.

#### Scenario: Immersive reader lookahead slice prefetching executes via useTimeoutFn
- **WHEN** `pages/read/[bookId].vue` finishes rendering the active slice and schedules background lookahead prefetching
- **THEN** the 2500ms lookahead delay is governed by `useTimeoutFn`
- **AND** slice navigation immediately cancels pending prefetch calls to prevent redundant network fetches.

#### Scenario: External authentication SDK readiness polling executes via useIntervalFn
- **WHEN** `pages/login.vue` polls for third-party OAuth script readiness
- **THEN** the readiness polling is managed via `useIntervalFn` with bounded retry counts and automatic timer cessation.
