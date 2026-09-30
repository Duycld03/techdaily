# Proposal

## Why

Navigating back from the technical reader view (`/read/[bookId]`) currently triggers a cascading failure: `window.location.replace` in the reader page middleware wipes the preceding navigation history entry (e.g. `/`), causing `router.back()` to jump into an unintended route like `/today`. On `/today`, an asymmetric authentication guard causes server-side rendering to emit protected DOM while client hydration re-evaluates an expired token or failed cross-origin refresh, abruptly redirecting to `/login` and generating severe Vue hydration node mismatches over a pitch-black canvas void.

Fixing this now restores seamless back navigation for readers, eliminates jarring authentication ejections and hydration mismatch console errors, and guarantees robust visual states during daily study loading transitions.

## What Changes

- **Preserve Reader Navigation History**: Remove destructive `window.location.replace()` in `frontend/pages/read/[bookId].vue` middleware, replacing it with history-preserving navigation or origin isolation checks that do not erase the referring route from browser history.
- **Context-Aware Reader Back Navigation**: Enhance `handleBackNavigation` in `ReaderHeaderBar.vue` to accept an explicit source route (e.g. `from` route query parameter) while safely falling back to home `/` or library when history traversal is ambiguous.
- **SSR & Client Auth Guard Parity**: Update `frontend/middleware/auth.global.ts` to decode and validate JWT expiration during server-side rendering (`import.meta.server`), redirecting unauthenticated or expired requests with a clean HTTP 302 to `/login` before rendering protected DOM, completely eliminating client hydration node mismatches.
- **Resilient Local & Cross-Origin Token Refresh**: Protect `tryRefreshToken()` in `useAuthStore.ts` and `useApiClient.ts` from prematurely wiping valid session tokens in `localStorage` when transient network drops, CORS boundary issues, or cross-port refresh failures occur.
- **Deterministic Studio Loading State on `/today`**: Fix `frontend/pages/today.vue` to render a centered skeleton or loading indicator when `focusStore.data` is null and `focusStore.error` is null, preventing the pitch-black viewport void during initial mount and revalidation.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `reader`: Update reader navigation requirements to preserve browser history stack and guarantee predictable return navigation to the originating route without hard document history erasure.
- `auth`: Update platform authentication and route guard requirements to enforce SSR JWT expiration verification parity, eliminating hydration mismatches between SSR output and client route guards.
- `today`: Update `/today` studio requirements to guarantee an explicit loading state whenever focus data has not yet resolved, eliminating unstyled blank canvas voids.

## Impact

- **Affected Code**:
  - `frontend/pages/read/[bookId].vue` (middleware and crossOriginIsolated reload handling)
  - `frontend/components/reader/ReaderHeaderBar.vue` (back navigation logic)
  - `frontend/middleware/auth.global.ts` (SSR token expiration check & redirect logic)
  - `frontend/stores/useAuthStore.ts` and `frontend/composables/useApiClient.ts` (session persistence & refresh error recovery)
  - `frontend/pages/today.vue` (template loading state fallback)
- **Breaking Changes**: None. APIs and existing data contracts remain backwards-compatible.
- **Testing**: Vitest unit tests in `frontend/tests/middleware/auth.spec.ts`, `frontend/tests/pages/today.spec.ts`, and reader component tests. Visual verification via headless Chromium.
