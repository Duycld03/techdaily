# Proposal: Fix Auth Token Expiration, Global 401 Interception, and Resilient Drill Submission

## 1. Why (Problem & Motivation)

Users who do not visit TechDaily for several days encounter a broken experience when returning to complete their daily learning curriculum (such as Day 5):
1. **Ghost Session State:** The frontend displays an active logged-in state (user avatar, streak count, user profile) because `useAuthStore.isLoggedIn` only checks for string existence of `token.value`. However, the backend JWT has already expired (`Expires = DateTime.UtcNow.AddDays(7)` vs frontend cookie `maxAge = 30 days` and indefinite `localStorage`). The client never validates the JWT `exp` timestamp.
2. **Abrupt Unhandled 401 Failures:** When the user attempts to submit a scenario drill or interact with any protected endpoint (`.RequireAuthorization()`), the backend rejects the expired token with `HTTP 401 Unauthorized`.
3. **No 401 Interceptor / Recovery Flow:** `useApiClient` simply throws `new Error('HTTP Error 401')`. It does not clear stale credentials, does not alert the user with a helpful notification ("Session expired. Please sign in again"), and does not redirect to `/login?redirect=...`.
4. **Destructive UI Error Boundary in `today.vue`:** In `useDailyFocusStore`, `submitOption` catches the 401 error and writes to `error.value`. This causes `today.vue` to unmount the entire curriculum reading pane and scenario challenge, replacing the screen with a stark red box: `HTTP Error 401 [Retry Day 5]`. Clicking "Retry Day 5" triggers a guest preview fetch that reloads the question, but subsequent submissions fail again with 401, trapping the user in a dead-end loop.

Resolving these issues ensures smooth session continuity, proactive cleanup of expired tokens, friendly user recovery, and resilient UI behavior.

---

## 2. What (Scope & Deliverables)

### Capability 1: Proactive Client-Side JWT Expiration Check
- Implement `isTokenExpired(token: string): boolean` in `useAuthStore` by parsing the standard JWT `exp` payload claim.
- In `authStore.init()` and `authStore.isLoggedIn`, verify that the token has not expired (`exp * 1000 > Date.now()`).
- If an expired token is detected on app load, purge stale cookies and `localStorage` to prevent "ghost session" states.

### Capability 2: Global 401 Interceptor in `useApiClient`
- When any API call receives `HTTP 401 Unauthorized`:
  - Automatically invoke `authStore.logout()` (or purge stored session tokens).
  - Trigger a prominent warning toast using `useToast().warning($t('auth.session_expired'))` informing the user that their session expired.
  - Automatically redirect to `/login?redirect=<currentPath>` so the user can re-authenticate without losing their context.

### Capability 3: Align Backend Token Lifetime with Curriculum
- Update `AuthEndpoints.cs` to issue JWT tokens with a 30-day lifetime (`Expires = DateTime.UtcNow.AddDays(30)`), aligning backend token validity with the 30-day curriculum duration and the frontend cookie `maxAge` (30 days).

### Capability 4: Resilient Drill Submission Error Handling
- Decouple drill submission errors from the initial page loading error in `useDailyFocusStore`.
- Handle submission errors via contextual toasts (`useToast().error(...)`) instead of setting `focusStore.error`.
- Ensure the user's selected choice and reading view remain visible and accessible during temporary submission errors.

### Capability 5: Bilingual Localization & Automated Testing
- Add localized strings for session expiration and submission errors in `en.json` and `vi.json`.
- Add unit tests in `frontend/tests/stores/auth.spec.ts` for expired token handling and in backend test suites.

---

## 3. Impact & Non-Goals
- **Impact:** Eliminates ghost sessions, provides clean session recovery on 401 errors, stops UI unmounting on submission failures, and prevents multi-day learner lockouts.
- **Non-Goals:** Building a complex refresh-token rotation server architecture at this stage; aligning token lifetimes to 30 days combined with client-side expiration checks and 401 interception provides robust, clean, and production-grade session management for this platform.
