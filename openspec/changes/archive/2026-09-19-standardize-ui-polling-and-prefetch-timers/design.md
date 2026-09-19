# Design: Standardize UI Background Polling and Prefetch Timers (Batch 5)

## Context

TechDaily's UI performance and event modernization (Batches 1–4) replaced raw DOM listeners, intervals, and debounce timers with VueUse composables. The final group of unmanaged timers consists of background polling routines and delayed lookahead prefetch schedulers across four pages:
- `pages/library.vue`: Background book processing and PDF upload status polling.
- `pages/today.vue`: Delayed next-day reading slice curation prefetch.
- `pages/read/[bookId].vue`: Lookahead adjacent slice prefetching.
- `pages/login.vue`: Google Sign-In script readiness polling.

Refactoring these to `@vueuse/nuxt` declarative utilities (`useIntervalFn`, `useTimeoutFn`) completes the platform-wide timer hygiene initiative.

## Goals / Non-Goals

**Goals:**
- Replace manual `setInterval` and `clearInterval` in `pages/library.vue` and `pages/login.vue` with VueUse `useIntervalFn`.
- Replace manual `setTimeout` and `clearTimeout` in `pages/today.vue` and `pages/read/[bookId].vue` with VueUse `useTimeoutFn`.
- Eliminate manual mutable timer variables (`backgroundPollTimer`, `pollInterval`, `nextDayPrefetchTimer`, `prefetchTimeoutId`) and unmount boilerplate.
- Maintain a 100% pass rate across the Vitest suite (55 test files / 378 tests).

**Non-Goals:**
- Alter polling intervals (maintain 2500ms for background books, 1500ms for PDF progress, 2500ms for slice prefetch).
- Change authentication flows or API endpoints.

## Decisions

### 1. Declarative Background Polling in `pages/library.vue`
- **Decision**: Define dedicated polling handlers and wrap them with `useIntervalFn(..., { immediate: false })`:
  ```typescript
  const { resume: startBackgroundPolling, pause: stopBackgroundPolling } = useIntervalFn(
    async () => {
      await libraryStore.fetchBooks({
        category: selectedCategory.value,
        search: searchQuery.value,
        page: libraryStore.currentPage,
        pageSize: libraryStore.pageSize
      })
      const stillActive = libraryStore.books.some(
        b => b.status === 'Processing' || (b.status as any) === 1
      )
      if (!stillActive) {
        stopBackgroundPolling()
      }
    },
    2500,
    { immediate: false }
  )
  ```
- **Rationale**: VueUse automatically disposes both polling timers on route navigation or unmount, eliminating orphaned polling intervals.

### 2. Prefetch Timeout Management in `today.vue` and `read/[bookId].vue`
- **Decision**:
  In `today.vue`:
  ```typescript
  const { start: scheduleNextDayPrefetch, stop: cancelPendingNextDayPrefetch } = useTimeoutFn(
    triggerNextDayPrefetch,
    2500,
    { immediate: false }
  )
  ```
  In `read/[bookId].vue`:
  ```typescript
  const { start: scheduleLookaheadPrefetch, stop: cancelPendingPrefetch } = useTimeoutFn(
    triggerLookaheadPrefetch,
    2500,
    { immediate: false }
  )
  ```
- **Rationale**: Invoking `start()` automatically resets any active timer countdown without manual `clearTimeout` branching.

### 3. Script Readiness Polling in `pages/login.vue`
- **Decision**:
  ```typescript
  let initAttempts = 0
  const { pause: stopGoogleCheck, resume: startGoogleCheck } = useIntervalFn(() => {
    initAttempts++
    if ((window as any).google?.accounts?.id) {
      stopGoogleCheck()
      setupGoogleButton()
    } else if (initAttempts >= 50) {
      stopGoogleCheck()
    }
  }, 200, { immediate: false })
  ```
- **Rationale**: Replaces raw `setInterval` and `setTimeout` with a single bounded interval function that automatically terminates.

## Risks / Trade-offs

- **Vitest Mocking with `useTimeoutFn` / `useIntervalFn`**:
  - *Risk*: Tests expecting immediate or fake-timer advances for prefetching or polling.
  - *Mitigation*: Both composables wrap standard browser timer APIs (`window.setTimeout`, `window.setInterval`), preserving full compatibility with Vitest timers.
