# Design

## Context

See `proposal.md` for motivation.
TechDaily's frontend layout is managed in `frontend/app.vue`, which renders:
- `AppHeader.vue` (when not in reader or auth mode)
- `AppSidebar.vue` (when not in reader or auth mode)
- `<main class="flex-1 overflow-y-auto"><NuxtPage /></main>`

When an unauthenticated session loads `/`, multiple store methods execute concurrently on mount (`fetchTodayFocus` in `index.vue`, `fetchDeckCards` and `fetchGraph` in `HomeBentoDashboard.vue`). When these return 401 Unauthorized, each response triggers an unthrottled `navigateTo('/login?redirect=/')` in `frontend/composables/useApiClient.ts`. In Vue Router, concurrent rapid navigation calls abort the active route transition, causing Vue's `<Transition>` to retain the outgoing `index.vue` DOM node while mounting `login.vue` underneath it, resulting in the two pages stacking inside `<main>`.

## Goals / Non-Goals

**Goals:**
- Guarantee that redirecting to `/login` from any protected page atomically destroys the active view and mounts the login view as the sole content of `<main>`.
- Eliminate duplicate concurrent `navigateTo` calls upon 401 responses via single-flight debouncing in `useApiClient.ts`.
- Disable Vue `<Transition>` page animations in `nuxt.config.ts` to prevent DOM retention race conditions during interrupted navigations.
- Enforce unique route keying on `<NuxtPage />` in `app.vue` using `:page-key="route => route.fullPath"`.
- Broaden `isAuthPage` detection in `app.vue` to reliably hide `AppHeader` and `AppSidebar` across all auth routes and path variants.

**Non-Goals:**
- Changing backend authentication endpoints or token expiration policies.
- Altering the visual design or form inputs of `login.vue`.
- Modifying reader mode layout rules.

## Decisions

### 1. Single-Flight Redirect Guard in `useApiClient.ts`

```ts
let isRedirectingToLogin = false

// Inside 401 response interceptor:
if (typeof window !== 'undefined') {
  const currentPath = window.location.pathname + window.location.search
  if (!isRedirectingToLogin && !window.location.pathname.startsWith('/login') && typeof navigateTo === 'function') {
    isRedirectingToLogin = true
    navigateTo({
      path: '/login',
      query: { redirect: currentPath }
    }).finally(() => {
      // Allow subsequent navigation attempts after a brief cooldown
      setTimeout(() => {
        isRedirectingToLogin = false
      }, 500)
    })
  }
}
```
- **Rationale**: When 3–5 parallel requests fail with 401 on initial load, only the first request initiates navigation. Subsequent 401 errors are caught and toasted without firing conflicting router transitions.

### 2. Disabling Page and Layout Transitions in `nuxt.config.ts`

```ts
app: {
  pageTransition: false,
  layoutTransition: false,
  head: { ... }
}
```
- **Rationale**: Nuxt 3 wraps `<NuxtPage />` in a Vue `<Transition>` element. Root elements in `index.vue` and `login.vue` utilize Tailwind utility classes `transition-colors duration-200`. When a route change is interrupted or cancelled, Vue's transition listener can wait indefinitely for `transitionend`, causing the leaving component to stay mounted in `<main>`. Explicitly disabling transitions guarantees immediate, deterministic DOM replacement.

### 3. Route Keying and Auth Path Normalization in `app.vue`

```vue
<template>
  <div class="min-h-dvh flex flex-col bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 antialiased transition-colors duration-200">
    <AppHeader v-if="!isReaderMode && !isAuthPage" />
    <AppCommandPalette />
    <AppToastContainer />
    <div class="flex-1 flex overflow-hidden">
      <AppSidebar v-if="!isReaderMode && !isAuthPage" />
      <main class="flex-1 overflow-y-auto">
        <NuxtPage :page-key="route => route.fullPath" />
      </main>
    </div>
  </div>
</template>
```
- **Rationale**:
  - Binding `:page-key="route => route.fullPath"` instructs Vue Router to treat every path + query change as a clean, separate instance, preventing component reuse collisions.
  - Normalizing `isAuthPage` ensures that `/login/`, `/register`, `/forgot-password`, and `/reset-password` all cleanly suppress the app shell.

## Risks / Trade-offs

- **Risk**: Disabling `pageTransition` removes cross-page fade animations.
  - **Mitigation**: Snappy, zero-latency component swaps are preferred for high-density developer cockpits. The Studio design language prioritizes crisp responsiveness over sluggish page fades.
- **Risk**: `isRedirectingToLogin` flag could block legitimate user navigation if a redirect hangs.
  - **Mitigation**: The flag is scoped only to 401 responses in `useApiClient.ts` and resets automatically after 500ms in a `.finally()` block.
