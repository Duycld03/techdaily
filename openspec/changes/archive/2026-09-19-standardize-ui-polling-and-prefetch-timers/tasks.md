# Tasks

## 1. Background Polling & Prefetch Modernization

- [x] 1.1 In `frontend/pages/library.vue`, refactor background book polling and PDF upload polling to VueUse `useIntervalFn` and remove manual `onUnmounted` timer boilerplate.
- [x] 1.2 In `frontend/pages/today.vue`, refactor next-day pacer slice prefetching to VueUse `useTimeoutFn`.
- [x] 1.3 In `frontend/pages/read/[bookId].vue`, refactor lookahead slice prefetching to VueUse `useTimeoutFn`.
- [x] 1.4 In `frontend/pages/login.vue`, refactor Google Sign-in readiness polling to VueUse `useIntervalFn`.

## 2. Automated Verification & Testing

- [x] 2.1 Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass across all 55 test files.
- [x] 2.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
