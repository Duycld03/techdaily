# Tasks

## 1. Frontend Implementation

- [x] 1.1 Refactor `#dock` in `frontend/app/pages/admin/login.vue` from `justify-center` to top-aligned baseline layout (`justify-start pt-4 sm:pt-6 lg:pt-8`), synchronizing horizontal alignment with `#main`.
- [x] 1.2 Encapsulate the 3 context overview items in `#dock` into elevated hairline cards (`rounded-xl border border-slate-200 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-3.5 shadow-sm space-y-1.5`) featuring icon container badges and distinct titles.
- [x] 1.3 Synchronize `#main` container top padding (`pt-4 sm:pt-6 lg:pt-8`) in `frontend/app/pages/admin/login.vue` to align the "Administrator sign in" heading and the "Your system overview" heading on an identical baseline plane.

## 2. Verification & Testing

- [x] 2.1 Run Vitest unit tests (`npm test -- frontend/tests/pages/admin-login.spec.ts`) to confirm 100% test suite passage and zero data contract or accessibility regressions.
- [x] 2.2 Conduct automated headless browser visual verification on Desktop (1440x900) and Mobile (390x844) viewports across light/dark themes and English/Vietnamese locales, inspecting and presenting screenshots to verify baseline alignment and visual neatness.
