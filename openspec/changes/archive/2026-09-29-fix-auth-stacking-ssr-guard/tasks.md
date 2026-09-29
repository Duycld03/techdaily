# Tasks

## 1. Frontend Implementation

- [x] 1.1 Harden SSR route guard in `frontend/middleware/auth.global.ts` to inspect `techdaily_token` and `refreshToken` cookies on `import.meta.server`, issuing an immediate HTTP 302 redirect to `/login?redirect=...` when both cookies are absent, and deferring to client-side hydration only when a session cookie is present.
- [x] 1.2 Restructure `frontend/app.vue` to isolate the authentication page outlet (`<template v-if="isAuthPage">`) from the application shell (`<template v-else>`), ensuring `<AppHeader>`, `<AppSidebar>`, and the persistent `<main class="flex-1 overflow-y-auto">` container are completely unmounted on guest authentication routes.

## 2. Frontend Testing and Verification

- [x] 2.1 Update `frontend/tests/middleware/auth.spec.ts` to test both SSR branches: immediate redirect to `/login` for unauthenticated visitors without cookies, and hydration deferral when a token cookie is present.
- [x] 2.2 Verify `frontend/tests/app.spec.ts` passes with the updated template structure, and run full Vitest test suite (`npm --prefix frontend test`) to ensure all contract and unit tests pass.
- [x] 2.3 Execute headless browser visual verification on Desktop (1440x900) and Mobile (390x844) viewports, verifying direct navigation to `/library` smoothly redirects to `/login?redirect=/library` without page stacking or DOM carryover.
