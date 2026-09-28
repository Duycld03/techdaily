# Proposal

## Why

When a user's authentication token expires or an unauthenticated user loads a protected view like `/`, multiple concurrent asynchronous requests (`fetchTodayFocus`, `fetchDeckCards`, `fetchGraph`) return HTTP 401 Unauthorized simultaneously. In `frontend/composables/useApiClient.ts`, each 401 error independently executes `navigateTo('/login?redirect=...')` without deduplication or single-flight locking.

In Nuxt 3 / Vue Router, rapid successive `navigateTo` calls interrupt the in-flight route transition. When Vue's `<Transition>` component replacement is cancelled mid-flight, the outgoing page component (`index.vue`) fails to unmount from `<main>`, while the incoming page component (`login.vue`) is mounted directly below it. This produces a broken visual state where the entire Dashboard (streak card, knowledge constellation, reading slice) is rendered at the top, and the Login page is stacked underneath it inside the same scrollable container.

## What Changes

- **Single-Flight Redirect Guard in `useApiClient.ts`**:
  - Implement a module-scoped redirect flag (`let isRedirectingToLogin = false`) so that when concurrent 401 errors occur within milliseconds of each other, only the first request initiates `navigateTo('/login?redirect=...')`. Subsequent requests are ignored until navigation completes or resets.
- **Disable Vue Transition Interruption in `nuxt.config.ts`**:
  - Explicitly configure `app: { pageTransition: false, layoutTransition: false }` in `frontend/nuxt.config.ts`. This disables Vue `<Transition>` wrapper around `<NuxtPage />`, ensuring synchronous, deterministic DOM replacement on route changes without waiting for CSS `transitionend` events on root elements.
- **Route Key Enforcement & Auth Shell Isolation in `app.vue`**:
  - Add `:page-key="route => route.fullPath"` to `<NuxtPage />` in `frontend/app.vue` to guarantee atomic route replacement.
  - Expand `isAuthPage` check in `app.vue` to cover all guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`) with trailing-slash normalization, completely suppressing `AppHeader` and `AppSidebar`.
- **Layout Cleanliness in `login.vue`**:
  - Ensure the auth page container mounts as a clean, single-page view without vertical scroll bleeding from previous route remnants.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Enhances authentication routing and session expiration handling to guarantee that unauthenticated redirects to `/login` atomically replace the active DOM without stacking or rendering residual protected route components.

### Impact
- `frontend/composables/useApiClient.ts`: Add single-flight redirect guard on 401 responses.
- `frontend/nuxt.config.ts`: Disable page and layout transitions (`pageTransition: false`, `layoutTransition: false`).
- `frontend/app.vue`: Add `:page-key="route => route.fullPath"` and expand `isAuthPage` matching.
- `frontend/tests/`: Add unit tests verifying 401 redirect single-flight behavior and route replacement contracts.
