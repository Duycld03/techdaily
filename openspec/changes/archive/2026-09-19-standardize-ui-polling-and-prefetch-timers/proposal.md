# Proposal: Standardize UI Background Polling and Prefetch Timers (Batch 5)

## Why

As the final phase of TechDaily's VueUse and lifecycle hygiene modernization (following Batches 1–4), several long-running background processes and delayed prefetch triggers continue to use manual `setInterval` / `setTimeout` references:
1. **PDF Processing and In-Progress Book Polling (`pages/library.vue`)**: Manages `backgroundPollTimer` and `pollInterval` using raw `setInterval` loops with manual cleanup in `onUnmounted`. If unmounting is interrupted, polling could persist in the background.
2. **Next-Day Pacer Prefetch Timer (`pages/today.vue`)**: Uses mutable `nextDayPrefetchTimer` with manual `setTimeout` and `clearTimeout` to prefetch upcoming reading slices after a 2500ms delay.
3. **Immersive Reader Lookahead Prefetch Timer (`pages/read/[bookId].vue`)**: Uses mutable `prefetchTimeoutId` with manual `setTimeout` and `clearTimeout` to prefetch neighboring chunk slices.
4. **Third-Party Script Readiness Polling (`pages/login.vue`)**: Uses raw `setInterval` (200ms) with a 10-second `setTimeout` fallback to wait for Google Identity Services SDK initialization.

Standardizing these long-running timers against VueUse composables (`useIntervalFn`, `useTimeoutFn`) completes 100% frontend declarative timer management, guarantees leak-free unmounting, and simplifies prefetch cancellation.

## What Changes

- **Library Background Polling Hygiene (`pages/library.vue`)**:
  - Refactor in-progress book polling and PDF upload status polling to VueUse `useIntervalFn`.
  - Eliminate manual `backgroundPollTimer` and `pollInterval` references and `clearInterval` boilerplate in `onUnmounted`.
- **Today Next-Day Prefetch Hygiene (`pages/today.vue`)**:
  - Refactor `scheduleNextDayPrefetch` to VueUse `useTimeoutFn`.
  - Eliminate mutable `nextDayPrefetchTimer` reference and manual `clearTimeout` branches.
- **Reader Lookahead Prefetch Hygiene (`pages/read/[bookId].vue`)**:
  - Refactor `scheduleLookaheadPrefetch` to VueUse `useTimeoutFn`.
  - Eliminate mutable `prefetchTimeoutId` reference and manual `clearTimeout` branches.
- **Login OAuth Script Polling Hygiene (`pages/login.vue`)**:
  - Refactor Google Sign-in SDK readiness polling to VueUse `useIntervalFn` with timeout-based auto-pausing.

## Capabilities

### New Capabilities
*(None - this change standardizes asynchronous timers and polling within existing capabilities)*

### Modified Capabilities
- `core-platform`: Establishes declarative standards for background polling intervals (`useIntervalFn`) and delayed prefetch lookahead timers (`useTimeoutFn`).

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Design**: Zero regression or styling shifts.
- **Reliability**: 100% elimination of manual timer leaks across all frontend pages and background workers.
