# Design: Standardize UI Async Timers and Search Debouncing (Batch 4)

## Context

Following event listener and outside-click modernization across application views, multiple components and pages still implement manual asynchronous timers and debouncing logic using raw `setInterval` and `setTimeout`. These patterns introduce mutable timer references, require manual `clearTimeout`/`clearInterval` in `onUnmounted`, and risk background timer execution if components are unmounted during in-flight operations.

Standardizing against `@vueuse/nuxt` composables (`useIntervalFn`, `useDebounceFn`, `useClipboard`) provides automatic lifecycle management and clean reactive state.

## Goals / Non-Goals

**Goals:**
- Replace raw `setInterval` in `AISynthesisCard.vue` with VueUse `useIntervalFn` to manage elapsed generation time and timeout.
- Replace manual `setTimeout` debouncing in `pages/review.vue`, `pages/notes.vue`, and `components/roadmap/RoadmapMindmapCanvas.vue` with VueUse `useDebounceFn`.
- Standardize clipboard copy feedback in `components/common/ShikiCodeBlock.vue` and `components/today/TermExplainerModal.vue` with VueUse `useClipboard`.
- Maintain 100% test pass rate across the Vitest suite (55 test files / 378 tests).

**Non-Goals:**
- Alter search ranking, query APIs, or debounce latency thresholds (retain exact 300ms for deck/notes, 150ms for mindmap, 2000ms for copy feedback).
- Modify visual styling, layouts, or component animations.

## Decisions

### 1. Elapsed Time Interval in `AISynthesisCard.vue`
- **Decision**: Use `useIntervalFn`:
  ```typescript
  const elapsedSeconds = ref(0)
  const isTimedOut = ref(false)

  const { pause, resume } = useIntervalFn(() => {
    elapsedSeconds.value++
    if (elapsedSeconds.value >= 6) {
      isTimedOut.value = true
    }
  }, 1000)

  function handleRetry() {
    elapsedSeconds.value = 0
    isTimedOut.value = false
    resume()
    emit('retry')
  }
  ```
- **Rationale**: VueUse automatically stops the interval on component unmount, eliminating manual `timer` refs and `clearInterval` in `onUnmounted`.

### 2. Debounced Search in `pages/review.vue` and `pages/notes.vue`
- **Decision**: In `review.vue`:
  ```typescript
  const debouncedFetchDeck = useDebounceFn(() => {
    fetchDeck(1)
  }, 300)

  watch(searchQuery, () => {
    debouncedFetchDeck()
  })
  ```
  In `notes.vue`:
  ```typescript
  const debouncedSyncQuery = useDebounceFn((val: string) => {
    router.replace({
      query: {
        ...route.query,
        q: val ? val : undefined,
        page: undefined
      }
    })
  }, 300)

  watch(highlightSearchQuery, (newVal) => {
    debouncedSyncQuery(newVal)
  })
  ```
- **Rationale**: Eliminates mutable `searchTimer` / `searchDebounceTimer` variables and manual `clearTimeout` boilerplate.

### 3. Mindmap Search Debounce in `RoadmapMindmapCanvas.vue`
- **Decision**:
  ```typescript
  const debouncedSearch = ref('')
  const applyDebouncedSearch = useDebounceFn((val: string) => {
    debouncedSearch.value = val.trim().toLowerCase()
  }, 150)

  watch(searchQuery, (newVal) => {
    applyDebouncedSearch(newVal)
  })
  ```
- **Rationale**: Replaces manual `searchTimeout` with clean declarative debouncing.

### 4. Clipboard Copy Feedback in `ShikiCodeBlock.vue` & `TermExplainerModal.vue`
- **Decision**: Use `const { copy, copied } = useClipboard({ copiedDuring: 2000 })` to handle clipboard write and temporary copied feedback automatically.
- **Rationale**: Avoids manual `setTimeout(() => copied.value = false, 2000)` and ensures copied status resets cleanly even if component unmounts.

## Risks / Trade-offs

- **Timer Behavior in Vitest (`happy-dom`)**:
  - *Risk*: Vitest tests that use `vi.advanceTimersByTime` or `vi.useFakeTimers()`.
  - *Mitigation*: VueUse's `useIntervalFn` and `useDebounceFn` rely on standard `window.setInterval` and `window.setTimeout` internally, maintaining full compatibility with Vitest fake timer mocks.
