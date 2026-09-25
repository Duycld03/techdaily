# Design

## Context

See proposal.md - Why. Today authentication is unconditionally persistent: `AuthEndpoints` issues a 30-day `refreshToken` cookie (`SetRefreshTokenCookie`, `Expires = UtcNow.AddDays(30)`) and the frontend `useAuthStore` stores the access token in a 30-day `techdaily_token` cookie plus `localStorage`. `RefreshTokenService.RotateTokenAsync` re-issues on `/refresh`. The `LoginRequest`/`RegisterRequest` DTOs carry only email/password; `rememberSession` in `login.vue` is bound but never transmitted.

Constraints:
- SSR: the route middleware (`middleware/auth.global.ts`) reads the token cookie during server rendering to decide the pre-hydration redirect, so the client cannot rely on web storage alone.
- Refresh rotation is stateless with respect to the caller; `/refresh` only has the incoming cookie, whose opaque value reveals nothing about the original persistence choice.
- `refresh-tokens` capability mandates HttpOnly, SameSite=Lax, `Path=/api/v1/auth`, and a 30-day server-side absolute expiry.

## Goals / Non-Goals

**Goals:**
- Make the "Remember session" checkbox control session survival across browser restarts, end-to-end.
- Preserve the remember choice across refresh-token rotation.
- Keep SSR middleware and the existing refresh/rotation/revocation security model intact.

**Non-Goals:**
- Changing the JWT access-token lifetime (stays 60 min) or the refresh token's 30-day server-side absolute expiry.
- Adding a per-session "remember" toggle for Google OAuth (checkbox is login-mode only; Google stays persistent).
- Reworking multi-device/family semantics.

## Decisions

### 1. Carry persistence on the token family via `RefreshToken.IsPersistent`
Add a non-nullable `IsPersistent` boolean (default `true`) to the `RefreshToken` entity, EF configuration, and a new migration. `IssueTokenAsync(userId, familyId?, isPersistent = true, ct)` sets it; `RotateTokenAsync` copies the parent token's `IsPersistent` into the successor and surfaces it so `/refresh` can re-issue a matching cookie.
- **Why:** `/refresh` has no other trustworthy source for the original choice. The family row is already loaded during rotation.
- **Alternatives:** Encode persistence in the cookie value (tamperable, needs signing); a second non-HttpOnly hint cookie (client-tamperable, splits source of truth). Rejected.
- Default `true` keeps existing rows and any omitted `rememberMe` persistent (backward compatible).

### 2. `SetRefreshTokenCookie(context, token, bool persistent)`
Add a `persistent` parameter: `true` → `Expires = UtcNow.AddDays(30)` (current behavior); `false` → omit `Expires`/`Max-Age`, yielding a browser **session cookie**. All other attributes (HttpOnly, Secure-by-scheme, SameSite=Lax, Path) are unchanged. `login`/`register` pass `request.RememberMe`; `google` passes `true`; `/refresh` passes `newToken.IsPersistent`.

### 3. `RememberMe` request field defaults to `true`
Extend `LoginRequest`/`RegisterRequest` records with `bool RememberMe = true`. Omitted field ⇒ persistent, matching today's behavior and the default-checked UI.

### 4. Frontend: cookie is the SSR source of truth; storage scope follows the flag
`useAuthStore.login(email, password, rememberMe)` and `register(..., rememberMe)` send `rememberMe`; `login.vue` passes `rememberSession.value`. `setSession(token, user, remember)` writes credentials with persistence chosen at set time:
- **remember = true:** `techdaily_token`/`techdaily_user` as 30-day cookies (`useCookie(..., { maxAge: 60*60*24*30, path: '/' })`) and `localStorage` (current behavior).
- **remember = false:** the same cookies created **without** `maxAge` (a session cookie) and mirrored to `sessionStorage`; nothing written to `localStorage`.

`init()` reads the cookie first (works under SSR), then falls back on the client to `localStorage` **or** `sessionStorage`. `clearSession()` removes both `localStorage` and `sessionStorage` keys and both cookies.
- **Why keep a cookie in session mode:** dropping the token cookie would blind SSR middleware and flash `/login`. A session cookie stays visible to SSR during the session yet is discarded on browser close, matching the intended behavior; it is also shared across tabs within the session, while `sessionStorage` is the per-tab cache.
- **Alternative:** web-storage-only (no cookie) — rejected; breaks SSR redirect.

## Risks / Trade-offs

- **`useCookie` option immutability / setup context** → The persistence must be chosen at write time, and Nuxt `useCookie` fixes options at creation. Mitigation: construct the cookie ref with the correct options inside `setSession` (client path guarded by `import.meta.client`), so persistent vs session is decided per login rather than at store construction.
- **Session token still valid server-side after browser close** → In session mode the DB token lives 30 days but its cookie is gone, so it is unreachable; no security downgrade (unreachable ⇒ effectively ended). Trade-off accepted to avoid variable server-side lifetimes.
- **Existing active sessions** → All existing `RefreshToken` rows default `IsPersistent = true`; no forced logout, no behavior change until next login.
- **Cross-tab in session mode** → New tabs share the session cookie (browser-session scoped), so auth is preserved within the browser session; `sessionStorage` is a secondary per-tab cache only.

## Migration Plan

1. Add EF Core migration: `RefreshToken.IsPersistent boolean NOT NULL DEFAULT true` (additive; existing rows become persistent).
2. Deploy backend (accepts `rememberMe`, defaults true; older frontend that omits it keeps working).
3. Deploy frontend (sends `rememberMe`, scopes storage).
- **Rollback:** revert frontend and/or backend; the `IsPersistent` column is additive and harmless if unused. The migration `Down` drops the column.
