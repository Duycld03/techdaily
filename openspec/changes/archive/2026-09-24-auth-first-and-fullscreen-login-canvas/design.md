# Design: Auth-First Routing & Full-Screen Studio Auth Canvas

## Context

TechDaily is transitioning from a hybrid browsing model to a strict **Auth-First** architecture. Currently:
1. `frontend/middleware/auth.global.ts` uses an explicit allowlist to protect routes. Any unlisted route defaults to public access, and unauthenticated users visiting `/` can view the home dashboard.
2. `frontend/app.vue` renders `AppHeader` across all routes (even `/login`), creating visual noise and cluttering guest authentication with internal learning telemetry (e.g. streak flame, command palette trigger).
3. `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs` leaves `GET /books`, `GET /books/{id}`, `GET /books/{id}/status`, and slice reading endpoints open to anonymous calls.

## Goals / Non-Goals

**Goals:**
- Enforce **Default-Deny** routing in `frontend/middleware/auth.global.ts`: all routes require authentication except explicit guest auth pathways (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local dev routes (`/playground/*`, `/showcase`).
- Redirect unauthenticated visitors attempting to access any protected route (including `/`) to `/login?redirect=...`.
- Completely isolate `/login` in `frontend/app.vue` by suppressing both `AppHeader.vue` and `AppSidebar.vue`.
- Re-architect `frontend/pages/login.vue` as a standalone full-screen Studio Auth Canvas (`h-[100dvh] w-screen overflow-hidden`) with self-contained `ThemeToggle` and `LocaleSelector` utilities in the top-right corner.
- Support `login`, `register`, and `forgot-password` modes within `frontend/pages/login.vue` with smooth transitions and zero layout shift.
- Secure backend library endpoints with `.RequireAuthorization()`, returning `401 Unauthorized` for anonymous requests.
- Adhere strictly to **Pillar 3** in `AGENTS.md`: automated unit tests in Vitest strictly test data contracts, payloads, and redirects; visual verification is conducted through direct browser screenshot inspection.

**Non-Goals:**
- Integrating a real external email delivery provider (SMTP/SendGrid) for password reset emails (UI mode transition and mock payload dispatch is provided; backend email worker is a separate capability).
- Modifying domain entities or database migrations.

## Decisions

### Decision 1: Default-Deny Frontend Middleware
Instead of maintaining an allowlist of protected routes, the middleware will invert the check:
```ts
const isPublicGuestRoute = to.path === '/login' || to.path === '/register' || to.path === '/forgot-password' || to.path === '/reset-password'
const isDevExempt = import.meta.dev && (to.path.startsWith('/playground') || to.path.startsWith('/showcase'))

if (!authStore.isLoggedIn && !isPublicGuestRoute && !isDevExempt) {
  // If SSR or client has no token or refresh failed:
  return navigateTo({
    path: '/login',
    query: { redirect: to.fullPath }
  })
}
```
If an already authenticated user navigates to `/login`, they are automatically redirected to `route.query.redirect` or `/today`.

### Decision 2: App Shell Header & Sidebar Suppression
In `frontend/app.vue`:
```vue
<AppHeader v-if="!isReaderMode && !isAuthPage" />
<main class="flex-1 flex overflow-hidden">
  <AppSidebar v-if="!isReaderMode && !isAuthPage" />
  <div class="flex-1 overflow-y-auto">
    <NuxtPage />
  </div>
</main>
```
When `isAuthPage` is true, the page content fills 100% of the screen width and height.

### Decision 3: Studio Auth Canvas Geometry & Layout
`frontend/pages/login.vue` will render:
1. **Top Ambient Bar**: TechDaily emblem and title on top-left, `LocaleSelector` and `ThemeToggle` on top-right.
2. **Main Stage**: Centered 2-column Studio Auth container (`max-w-5xl mx-auto grid lg:grid-cols-12 gap-8 lg:gap-12 items-center`):
   - **Left Column (`lg:col-span-5 hidden lg:block`)**: Brand emblem, mission statement, 3 feature pillars (Scenario Drills, SM-2 Recall, Daily Slices).
   - **Right Column (`lg:col-span-7 w-full`)**: Interactive glass card (`.glass-panel`) housing the mode switcher tabs, Google OAuth button, and form inputs.
3. **Form Modes**:
   - `login`: Email, Password (eye toggle), Forgot Password link, Submit.
   - `register`: 2-column input grid (Full Name + Email, Password + Confirm Password with eye toggles), Submit.
   - `forgot-password`: Email, Submit reset request, Back to Sign In button.

### Decision 4: Backend Library Endpoints Authorization
In `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`:
Apply `group.RequireAuthorization()` at the group level or explicitly to `GET /books`, `GET /books/{id}`, `GET /books/{id}/status`, `GET /books/{id}/slices/{order}`, and `POST /books/{id}/slices/{order}/curate`. All anonymous requests will be rejected by ASP.NET Core JwtBearer middleware with `401 Unauthorized`.

## Risks / Trade-offs

- **Risk**: Existing automated tests might navigate anonymously to `/` or `/library` and expect 200 OK.
  - **Mitigation**: Update test mocks to provide an authenticated session for protected routes, and add explicit tests verifying that anonymous access redirects to `/login` on frontend and returns 401 on backend.
