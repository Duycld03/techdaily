# Tasks

## 1. Frontend Implementation

- [x] 1.1 In `frontend/composables/useApiClient.ts`, add a single-flight redirect guard (`isRedirectingToLogin`) to debounce concurrent HTTP 401 responses and prevent duplicate/cancelled `navigateTo('/login')` calls.
- [x] 1.2 In `frontend/nuxt.config.ts`, configure `pageTransition: false` and `layoutTransition: false` under `app` to eliminate Vue `<Transition>` DOM retention race conditions on route changes.
- [x] 1.3 In `frontend/app.vue`, bind `:page-key="route => route.fullPath"` on `<NuxtPage />` to guarantee atomic route component replacement.
- [x] 1.4 In `frontend/app.vue`, expand `isAuthPage` to match all guest auth paths (`/login`, `/register`, `/forgot-password`, `/reset-password`) with trailing-slash tolerance, suppressing `AppHeader` and `AppSidebar`.
- [x] 1.5 In `frontend/pages/login.vue`, verify root element styling and ensure no vertical scroll bleeding or duplicate layout frames.

## 2. Frontend Tests & Verification

- [x] 2.1 Add unit tests for `useApiClient` in `frontend/tests/` asserting that concurrent 401 responses trigger only a single `navigateTo` call.
- [x] 2.2 Add unit tests for `app.vue` in `frontend/tests/` verifying that `isAuthPage` correctly identifies all auth routes and hides navigation chrome.
- [x] 2.3 Run full test suites (`npm test` and `dotnet test`) to verify 100% test pass rate for Gate 1.
- [x] 2.4 Programmatically drive headless Chromium via `browser` in `eval` across Desktop (1440x900) and Mobile (390x844) viewports simulating session expiration on `/` to visually verify that the Dashboard is cleanly replaced by the Login screen without stacking (Gate 2).
