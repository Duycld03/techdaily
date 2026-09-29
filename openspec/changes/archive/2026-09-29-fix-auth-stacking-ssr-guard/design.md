# Design

## Context

See `proposal.md` for motivation.

TechDaily uses Nuxt 3 with Server-Side Rendering (SSR) and Pinia. Commit `900f9f7` added an unconditional bypass for all protected routes on SSR:
```typescript
if ((import.meta.server || (process as any)?.server) && isAuthRequired) {
  return
}
```
This was intended to prevent users from being logged out before client-side silent refresh ran. However, it caused SSR to render full HTML of protected pages (`/library`, `/today`) for 100% unauthenticated visitors. When the browser hydrated and client middleware redirected to `/login`, Vue 3's hydration reconciler failed to clean up the existing DOM nodes inside `<main class="flex-1 overflow-y-auto">`, resulting in page stacking (`/library` above, `/login` below).

## Goals / Non-Goals

**Goals:**
- Eliminate page stacking on unauthenticated navigation across all entry points (direct URL entry, tab restoration, or expired session redirect).
- Issue an immediate HTTP 302 redirect from SSR when an incoming request has no authentication cookies, preventing any protected HTML from reaching the browser.
- Preserve transparent client-side silent refresh for returning users with expired access tokens whose 30-day refresh token or session cookie is still present.
- Guarantee that navigating to any guest authentication route (`/login`, `/register`, `/forgot-password`, `/reset-password`) completely unmounts the application shell DOM hierarchy (`<AppHeader>`, `<AppSidebar>`, and `<main>`).

**Non-Goals:**
- Modifying backend JWT lifespan (retained strictly at 60 minutes) or refresh token service configuration.
- Migrating to Nuxt layouts directory (`frontend/layouts/`), preserving the established single `app.vue` root shell pattern.

## Decisions

### 1. Cookie-Aware SSR Route Guard in `frontend/middleware/auth.global.ts`
- **Decision**: Inspect `useCookie('techdaily_token')` and `useCookie('refreshToken')` on SSR. If neither cookie is present, immediately execute:
  ```typescript
  if ((import.meta.server || (process as any)?.server) && isAuthRequired) {
    const tokenCookie = useCookie('techdaily_token')
    const refreshCookie = useCookie('refreshToken')

    if (!tokenCookie.value && !refreshCookie.value) {
      return navigateTo({
        path: '/login',
        query: { redirect: to.fullPath }
      })
    }
    return
  }
  ```
- **Rationale & RFC 6265 Boundary**:
  - The backend sets `refreshToken` with `Path = "/api/v1/auth"`. Per RFC 6265 §5.1.4, browsers never send this cookie to `/library` or `/today`.
  - However, `techdaily_token` is set with `Path = "/"` and `maxAge = 30 days`. When a user previously logged in, `techdaily_token` is always sent to SSR even if the JWT payload's internal `exp` is in the past.
  - A pure guest has neither cookie (`!tokenCookie.value && !refreshCookie.value` is `true`). SSR immediately issues an HTTP 302 redirect to `/login`. No protected page HTML is generated or transmitted.
  - A returning user has `tokenCookie.value` present. SSR returns and defers validation to client hydration, where `tryRefreshToken()` runs silent refresh against `/api/v1/auth/refresh` (which receives the `refreshToken` cookie).

### 2. Structural DOM Isolation in `frontend/app.vue`
- **Decision**: Isolate the authentication surface and standard application shell into mutually exclusive template branches:
  ```vue
  <template>
    <div class="min-h-dvh flex flex-col bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 antialiased transition-colors duration-200">
      <AppToastContainer />

      <!-- Auth Cockpit Shell: Isolated from internal navigation chrome & persistent containers -->
      <template v-if="isAuthPage">
        <NuxtPage :page-key="route => route.fullPath" />
      </template>

      <!-- Standard Application Shell -->
      <template v-else>
        <AppHeader v-if="!isReaderMode" />
        <AppCommandPalette />
        <div class="flex-1 flex overflow-hidden">
          <AppSidebar v-if="!isReaderMode" />
          <main class="flex-1 overflow-y-auto">
            <NuxtPage :page-key="route => route.fullPath" />
          </main>
        </div>
      </template>
    </div>
  </template>
  ```
- **Rationale**:
  - In Vue 3, switching between `<template v-if="isAuthPage">` and `<template v-else>` completely tears down the outgoing DOM tree (`parent.removeChild(node)`).
  - The persistent `<main class="flex-1 overflow-y-auto">` container that previously held both `/library` and `/login` is eliminated during the transition.
  - Even in the edge case where client-side silent refresh fails and redirects to `/login`, any server-rendered or partially hydrated nodes from the protected route are destroyed, making visual stacking impossible.

### 3. Persistent Global Toast Container at Root
- **Decision**: Keep `<AppToastContainer />` mounted at the root level outside the conditional branches.
- **Rationale**: Authentication pages require toast notifications (e.g., successful registration verification, password reset links sent, OAuth failure alerts). Placing it outside `v-if/v-else` guarantees uninterrupted toast lifecycles during auth route transitions.

## Risks / Trade-offs

- **Risk: Multiple `<NuxtPage />` in template**:
  - *Mitigation*: The two `<NuxtPage />` instances are wrapped in mutually exclusive `v-if` / `v-else` branches. At any given point in time, exactly one `<NuxtPage />` is mounted in the active VNode tree. This is fully supported by Vue Router and Nuxt 3.
- **Risk: Returning user whose refresh token is revoked after SSR pass**:
  - *Mitigation*: If `tokenCookie.value` was present on SSR but `tryRefreshToken()` subsequently fails on client hydration, client navigates to `/login`. Because `app.vue` isolates the DOM shells, the application shell branch is cleanly unmounted, preventing page stacking.
