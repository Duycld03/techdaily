# Technical Design: Auth Token Expiration, Global 401 Interception, and Resilient Drill Submission

## 1. Architectural Overview

Authentication in TechDaily relies on JWT tokens issued by ASP.NET Core and persisted on the Nuxt 4 client in cookies (`techdaily_token`) and `localStorage`.

Previously, four vulnerabilities interacted to degrade user experience:
1. **Token Lifetime Discrepancy:** Backend generated JWT with `AddDays(7)`, but frontend cookie specified `maxAge: 30 days`.
2. **Blind Token Presence:** `useAuthStore` checked `!!token.value` without inspecting `exp`.
3. **No 401 Interceptor:** `useApiClient` threw unhandled errors on 401 instead of logging out and redirecting.
4. **Coarse Error State:** `useDailyFocusStore` wrote submission errors into `focusStore.error`, causing `today.vue` to unmount the entire curriculum UI into a dead-end error card.

This design introduces multi-layer resilience: client-side proactive expiry checks, a centralized 401 interceptor, backend token alignment, and isolated action-level error handling.

---

## 2. Component Interactions & Data Flow

```mermaid
sequenceDiagram
    autonumber
    actor User as Engineer
    participant UI as Nuxt 4 (today.vue)
    participant Store as Pinia (useAuthStore / useDailyFocusStore)
    participant Client as Composable (useApiClient)
    participant Toast as Composable (useToast)
    participant API as ASP.NET Core (.NET 10)

    alt Scenario A: Returning After Long Absence (Proactive Expiry)
        User->>UI: Opens /today after > 30 days
        UI->>Store: authStore.init()
        Store->>Store: isTokenExpired(token) -> TRUE (exp < now)
        Store->>Store: Clear token & user from cookie and localStorage
        Store-->>UI: isLoggedIn = false (Renders Guest / Sign-in prompt)
    else Scenario B: Mid-Session 401 Interception (Reactive Recovery)
        User->>UI: Submits Scenario Drill
        UI->>Store: focusStore.submitOption(index)
        Store->>Client: POST /api/v1/daily/drills/{id}/submit
        Client->>API: HTTP POST with stale/invalid token
        API-->>Client: HTTP 401 Unauthorized
        Note over Client: Global 401 Interceptor triggers
        Client->>Store: authStore.logout() (or session reset)
        Client->>Toast: warning("Session expired. Please sign in again.")
        Client->>UI: navigateTo('/login?redirect=/today')
        Note over Store: UI state intact; focusStore.error is NOT contaminated
    end
```

---

## 3. Detailed Component Designs

### A. Frontend Layer (`frontend/`)

#### 1. `stores/useAuthStore.ts`
- Implement `isTokenExpired(token: string | null): boolean`:
  - Parse the JWT base64 payload.
  - Read `payload.exp`. If `!payload.exp`, treat as valid or inspect format.
  - Return `payload.exp * 1000 <= Date.now()`.
- Update `isLoggedIn`:
  ```typescript
  const isLoggedIn = computed(() => !!token.value && !isTokenExpired(token.value))
  ```
- In `init()`:
  - If `token.value && isTokenExpired(token.value)`: call `clearSession()` or purge stored values.
- Expose `clearSession()` without forced redirect (for headless client interceptor calls).

#### 2. `composables/useApiClient.ts`
- Enhance `request<T>` error handling:
  - Check `response.status === 401`.
  - When 401 occurs:
    - Attempt to avoid recursive loops on `/api/v1/auth/login`.
    - Purge token storage.
    - Dispatch a toast warning via `useToast().warning(...)`.
    - If in browser environment and current route is not `/login`, trigger `navigateTo({ path: '/login', query: { redirect: window.location.pathname + window.location.search } })`.
  - Throw a descriptive error.

#### 3. `stores/useDailyFocusStore.ts` & `pages/today.vue`
- In `submitOption`:
  - Do NOT set `error.value = err.message`.
  - Instead, display errors via `useToast().error(err.message || 'Failed to submit scenario option.')` or dedicated `submitError` ref.
  - The reader pane and question options remain mounted and visible.

---

### B. Backend Layer (`backend/`)

#### 1. `TechDaily.Api/Endpoints/AuthEndpoints.cs`
- In `GenerateJwtToken(User user, string secret, string issuer, string audience)`:
  - Change `Expires = DateTime.UtcNow.AddDays(7)` to `DateTime.UtcNow.AddDays(30)`.
  - Matches the 30-day curriculum duration and frontend cookie `maxAge`.

---

## 4. Localization Keys (i18n)

```json
{
  "auth": {
    "session_expired": "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
    "session_expired_en": "Your session has expired. Please sign in again."
  },
  "today": {
    "submit_failed": "Nộp đáp án thất bại. Vui lòng thử lại.",
    "submit_failed_en": "Failed to submit answer. Please try again."
  }
}
```
