# Design

## Context

See `proposal.md` for motivation.

When users read technical documentation on TechDaily and click the top-left `< Quay Lại` back button in `ReaderHeaderBar.vue`, the application occasionally jumps to `/today` instead of the originating dashboard (`/`), rapidly redirecting to `/login?redirect=/today` with Vue hydration node mismatch warnings in the developer console.

Three architectural boundaries interact here:
1. **Browser Navigation & Origin Isolation**: The reader route (`/read/**`) uses `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` to enable multi-threaded WebAssembly in worker threads (for on-device TTS narration). Because client-side SPA routing (`navigateTo`) does not re-issue top-level HTTP response headers, `pages/read/[bookId].vue` executes a reload when `!window.crossOriginIsolated`. Using `window.location.replace` erases the referring route from the browser history stack.
2. **SSR vs Client Route Guard Asymmetry**: `middleware/auth.global.ts` currently defers authentication verification during SSR whenever a cookie string is present, without checking whether the JWT access token is expired. SSR renders the protected layout (`<AppHeader>` and `<AppSidebar>`). During client hydration, the client-side store computes `isLoggedIn === false` due to token expiration, triggers `tryRefreshToken()`, fails to find a valid refresh cookie across localhost ports, calls `clearSession()`, and redirects to `/login`. Vue attempts to hydrate the server-rendered protected shell with the client's auth page shell, causing DOM tree mismatches.
3. **Template Condition Gap on `/today`**: `pages/today.vue` contains sequential `v-if` / `v-else-if` blocks for `isLoading`, `error`, `hasActiveBook === false`, and `data`. During initial render (SSR and pre-mounted client state), `data` is null, `error` is null, and `isLoading` is false. None of the branches match, leaving an unstyled black screen void.

## Goals / Non-Goals

**Goals:**
- Provide deterministic return navigation from the reader: clicking `< Quay Lại` must always return to the originating page (`/`, `/library`, or `/today`) without destroying navigation history.
- Eliminate Vue hydration node mismatches when navigating with expired credentials by validating JWT expiration during SSR and issuing a clean HTTP 302 redirect.
- Prevent spurious session invalidation in `useAuthStore` when transient refresh requests encounter network or local cross-origin port restrictions.
- Ensure `/today` displays a standardized loading indicator whenever data has not yet resolved, eliminating black canvas voids.

**Non-Goals:**
- Removing Cross-Origin Isolation or WebAssembly multi-threading from the reader (on-device Kokoro TTS requires `crossOriginIsolated`).
- Changing backend JWT issuance duration (60 minutes remains standard).
- Refactoring the entire routing system or layout architecture.

## Decisions

### Decision 1: Explicit `from` Route Query & Non-Destructive Isolation Reload
- **Approach**:
  1. Callers navigating to `/read/:bookId` (such as `HomeBentoDashboard.vue`, `DocReaderPane.vue`, and library book cards) pass a query parameter `from` indicating the originating path:
     ```typescript
     navigateTo({
       path: `/read/${bookId}`,
       query: { slice: chunkOrder.toString(), from: route.fullPath }
     })
     ```
  2. In `pages/read/[bookId].vue`, if a hard reload is required to establish `crossOriginIsolated`, use `window.location.assign(to.fullPath)` (or `window.location.href = to.fullPath`) instead of `window.location.replace(to.fullPath)`. This preserves the preceding navigation history entry.
  3. In `ReaderHeaderBar.vue`, `handleBackNavigation` first checks `route.query.from`:
     ```typescript
     function handleBackNavigation() {
       const from = route.query.from as string | undefined
       if (from && from.startsWith('/') && !from.startsWith('/read')) {
         navigateTo(from)
         return
       }
       if (typeof window !== 'undefined' && window.history.length > 1) {
         router.back()
       } else {
         navigateTo('/')
       }
     }
     ```
- **Rationale**: An explicit `from` parameter guarantees accurate navigation regardless of whether the user clicked through the app or hard-reloaded. Falling back to `router.back()` and then `/` provides resilient multi-tier fallback.

### Decision 2: Server-Side JWT Expiration Validation in `auth.global.ts`
- **Approach**:
  1. In `frontend/middleware/auth.global.ts`, decode and inspect the `exp` claim of `tokenCookie.value` on the server (`import.meta.server`):
     ```typescript
     function isJwtExpired(token: string | null): boolean {
       if (!token) return true
       try {
         const parts = token.split('.')
         if (parts.length < 2) return true
         const base64Url = parts[1]
         const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
         const json = typeof Buffer !== 'undefined'
           ? Buffer.from(base64, 'base64').toString('utf8')
           : atob(base64)
         const payload = JSON.parse(json)
         return typeof payload.exp === 'number' ? payload.exp * 1000 <= Date.now() : true
       } catch {
         return true
       }
     }
     ```
  2. If `techdaily_token` is expired or invalid AND no valid `refreshToken` cookie exists, the SSR middleware immediately returns:
     ```typescript
     return navigateTo({
       path: '/login',
       query: { redirect: to.fullPath }
     })
     ```
- **Rationale**: By issuing the redirect at the HTTP level during SSR, Nitro returns a 302 response and renders `/login` directly. The browser never receives `/today` HTML with `<AppHeader>` and `<AppSidebar>`, eliminating DOM reconciliation conflicts and hydration errors.

### Decision 3: Deterministic Workspace Loading Fallback on `/today`
- **Approach**:
  Update `pages/today.vue`:
  ```html
  <!-- Loading State: Active fetching or initial pre-mount unresolved state -->
  <div
    v-if="focusStore.isLoading || (!focusStore.data && !focusStore.error)"
    class="flex-1 flex flex-col items-center justify-center p-6 sm:p-8 text-center my-auto"
  >
    <div class="w-12 h-12 rounded-2xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center shadow-sm mb-4">
      <Loader2 class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin" :stroke-width="1.5" />
    </div>
    <p class="text-sm sm:text-base font-semibold text-slate-700 dark:text-slate-300 max-w-sm sm:max-w-md mx-auto leading-relaxed">
      {{ $t("pacer.ai_synthesis_desc") }}
    </p>
  </div>
  ```
- **Rationale**: Guarantees that at all times when data is not yet available and no error has occurred, a centered studio loading card with spinner is visible.

## Risks / Trade-offs

- **Risk**: Hard reload via `window.location.assign` still causes an extra page roundtrip if `window.crossOriginIsolated` is not initially true.
  - **Mitigation**: The reload only triggers once per session (`reader_isolated_reload` session guard) and only when navigating into the reader from an unisolated page. Preserving the history stack ensures the user can always navigate backward.
- **Risk**: Clock skew between client and server when checking JWT `exp` during SSR.
  - **Mitigation**: Add a 30-second grace buffer to SSR expiration validation (`payload.exp * 1000 <= Date.now() + 30000`) matching standard token rotation tolerances.
