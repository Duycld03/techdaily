# Proposal

## Why

When an unauthenticated visitor or a user whose session has expired navigates to a protected route (such as `/library` or `/today`), Nuxt Server-Side Rendering (SSR) bypasses server-side auth checking due to an unconditional SSR bypass introduced in commit `900f9f7`. As a result, the server renders the full HTML of the protected page (with empty states like *"No matching documents found"*). During client-side hydration, the route middleware discovers that the user is not authenticated, fails silent refresh, and triggers `navigateTo('/login?redirect=...')`.

Because both the Application Shell and the Login page share the persistent `<main class="flex-1 overflow-y-auto">` container in `frontend/app.vue`, the mid-hydration route transition fails to cleanly unmount the server-rendered DOM nodes of `/library`, appending `login.vue` directly below them. This produces visual page stacking where the Library view is frozen on top and the Login cockpit is stacked underneath.

This change hardens the SSR route middleware to immediately return an HTTP 302 redirect for guests with no session cookies, while structurally isolating the Auth Cockpit in `frontend/app.vue` to ensure outgoing application shell containers are completely unmounted upon navigation to `/login`.

## What Changes

- **Hardened SSR Route Guard in `frontend/middleware/auth.global.ts`**:
  - Inspect authentication cookies (`techdaily_token` and `refreshToken`) during Server-Side Rendering (`import.meta.server`).
  - If neither `techdaily_token` nor `refreshToken` is present, immediately execute `navigateTo({ path: '/login', query: { redirect: to.fullPath } })` on the server, returning an HTTP 302 Found redirect instead of rendering protected route HTML.
  - If a token cookie is present (even if expired), allow SSR to proceed so that client-side hydration can attempt silent token refresh via `POST /api/v1/auth/refresh`.

- **Auth Shell DOM Isolation in `frontend/app.vue`**:
  - Isolate the authentication surface into a distinct conditional branch (`<template v-if="isAuthPage">`) separate from the regular application layout (`<template v-else>`).
  - Completely detach the persistent `<div class="flex-1 flex overflow-hidden">` and `<main class="flex-1 overflow-y-auto">` wrappers when `isAuthPage` is true, ensuring that previous route DOM nodes cannot persist or stack with `login.vue`.
  - Maintain `AppToastContainer` at the root so feedback notifications remain visible across both authenticated and guest auth states.

- **Test Suite Updates**:
  - Update `frontend/tests/middleware/auth.spec.ts` to assert that SSR immediately redirects pure guests lacking cookies to `/login` with the proper redirect parameter, while retaining hydration deferral when session cookies are present.
  - Verify that `frontend/tests/app.spec.ts` continues to pass with full shell isolation.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Updates SSR route middleware requirements to distinguish unauthenticated guests (immediate 302 redirect) from potentially renewable sessions (hydration deferral), and enforces strict structural DOM separation between the application shell and the full-screen authentication surface in `app.vue`.

## Impact

- `frontend/middleware/auth.global.ts`: Replaces unconditional SSR bypass with cookie-aware guest check.
- `frontend/app.vue`: Restructures template to isolate `isAuthPage` from regular app shell container.
- `frontend/tests/middleware/auth.spec.ts`: Updates SSR hydration deferral tests to cover both guest and renewable session paths.
