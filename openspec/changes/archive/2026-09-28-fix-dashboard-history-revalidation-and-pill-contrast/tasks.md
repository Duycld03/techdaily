# Tasks

## 1. Frontend Component & Navigation Enhancements

- [x] 1.1 Update `frontend/components/dashboard/HomeBentoDashboard.vue` to replace fragile semi-transparent classes on Card B itinerary strip pills with high-contrast, theme-resilient styling (`dark:bg-white/[0.04] dark:border-white/[0.06] dark:text-slate-200` in dark mode, and `bg-slate-100 text-slate-700 border-slate-200/80` in light mode).
- [x] 1.2 Update `frontend/pages/index.vue` to implement dual-mode data loading: initial await when `!focusStore.data`, and optimistic background revalidation (`focusStore.fetchTodayFocus`) when cached data is already present in memory.
- [x] 1.3 Add browser history and bfcache event listeners (`pageshow` and `popstate`) in `frontend/pages/index.vue` using `@vueuse/core` to trigger background focus revalidation when users return via forward/back navigation or mouse macro buttons.

## 2. Frontend Testing & Verification

- [x] 2.1 Update and add unit tests for `HomeBentoDashboard` in `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` asserting theme-resilient pill contrast classes.
- [x] 2.2 Update and add unit tests for `frontend/tests/pages/index.spec.ts` verifying background revalidation behavior and `pageshow`/`popstate` lifecycle handling without full-page spinner flickering.
- [x] 2.3 Run full test suites (`npm test` and `dotnet test`) to verify 100% test pass rate for Gate 1.
- [x] 2.4 Programmatically drive headless Chromium via `browser` in `eval` across Desktop (1440x900) and Mobile (390x844) viewports, including simulated back/forward history navigation, to visually verify high-contrast itinerary pills and synchronized drill completion status (Gate 2).
