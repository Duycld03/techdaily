# Tasks

## 1. Frontend: Reader Navigation & History Stack Preservation

- [x] 1.1 Update `frontend/pages/read/[bookId].vue` page middleware to eliminate destructive `window.location.replace()`, replacing it with non-destructive navigation so referring history entries are preserved.
- [x] 1.2 Update reader navigation links across `HomeBentoDashboard.vue`, `DocReaderPane.vue`, and `frontend/pages/library.vue` to append an explicit `from` query parameter (e.g. `from=/` or `from=/today`).
- [x] 1.3 Enhance `handleBackNavigation` in `frontend/components/reader/ReaderHeaderBar.vue` to inspect `route.query.from` and navigate directly to the referring path when present, with resilient fallback to `router.back()` and root `/`.
- [x] 1.4 Add unit tests in `frontend/tests/components/reader/ReaderComponents.spec.ts` or a dedicated test suite verifying that `handleBackNavigation` respects `from` parameter and handles missing history safely.

## 2. Frontend: SSR & Client Route Guard Parity

- [x] 2.1 Update `frontend/middleware/auth.global.ts` to implement safe JWT decoding and expiration validation during server-side rendering (`import.meta.server`).
- [x] 2.2 In `frontend/middleware/auth.global.ts`, when `techdaily_token` is expired and no valid `refreshToken` cookie exists, immediately issue an HTTP 302 redirect to `/login?redirect={targetUrl}` during SSR, preventing server rendering of protected shells.
- [x] 2.3 Refine client-side token refresh failure handling in `frontend/stores/useAuthStore.ts` and `frontend/composables/useApiClient.ts` to avoid wiping local credentials on network drops or cross-port refresh failures.
- [x] 2.4 Update unit tests in `frontend/tests/middleware/auth.spec.ts` covering SSR expiration validation, 302 redirect issuance, and zero DOM hydration mismatch on `/login`.

## 3. Frontend: Today Studio Loading State Fallback

- [x] 3.1 Update `frontend/pages/today.vue` to expand the loading state condition to `focusStore.isLoading || (!focusStore.data && !focusStore.error)`, displaying the centered loading spinner and subtitle whenever data has not yet resolved.
- [x] 3.2 Verify `pages/today.vue` never renders an unconditioned blank container during initial SSR or before client queries finish.
- [x] 3.3 Add unit tests in `frontend/tests/pages/today.spec.ts` asserting that the loading indicator renders during the initial pre-fetch unresolved state.

## 4. Verification: Vitest Test Suite & Headless Browser Inspection

- [x] 4.1 Run full frontend unit test suite (`npm test`) to verify 100% pass across auth middleware, reader components, and today studio pages.
- [x] 4.2 Execute automated headless Chromium dual-gate verification via `browser` in `eval` (Desktop and Mobile viewports) exercising:
  - Navigating from `/` to `/read/[bookId]?slice=2` and clicking `< Quay Lại`, confirming seamless return to `/`.
  - Navigating to `/today` and observing clean loading transition without black screen void.
  - Accessing protected routes with an expired token, verifying clean redirection to `/login` without hydration mismatch console warnings.
