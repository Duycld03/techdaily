# Proposal

## Why

The `/login` page renders a "Remember session (30 days)" checkbox (`rememberSession`, default checked), but it is cosmetic: the value is never sent to the backend, and the session is *always* persisted for 30 days regardless. The access token is stored in a 30-day `techdaily_token` cookie plus `localStorage`, and the backend always issues a 30-day persistent `refreshToken` cookie. Unchecking the box changes nothing, so a user on a shared machine cannot opt into a session that ends when the browser closes.

## What Changes

- Wire the login "Remember session" checkbox end-to-end so it controls session persistence:
  - **Checked (default)**: persistent session — 30-day `refreshToken` cookie (backend) and 30-day `techdaily_token`/`techdaily_user` cookies + `localStorage` (frontend). Unchanged from today.
  - **Unchecked**: session-scoped — backend issues the `refreshToken` as a browser **session cookie** (no `Expires`/`Max-Age`); frontend stores the access token/user in a **session cookie** + `sessionStorage`. The session ends when the browser closes.
- Backend `POST /api/v1/auth/login` and `/register` accept a `RememberMe` boolean (default `true` for backward compatibility) and issue the refresh token with the matching persistence.
- Persist the remember choice on the refresh-token family (`RefreshToken.IsPersistent`) so `POST /api/v1/auth/refresh` re-issues a cookie with the same persistence after rotation, instead of silently promoting a session to persistent.
- The refresh token's **server-side** 30-day absolute expiry (`ExpiresAt`) is unchanged in both modes; only cookie persistence (browser retention) varies.
- Google OAuth login remains persistent (the checkbox is login-mode only and is not shown for the Google button).
- Frontend `useAuthStore.login`/`register` accept `rememberMe` and pass it in the request body; `login.vue` passes `rememberSession.value`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Add a requirement specifying that the login "Remember session" checkbox controls whether the authenticated session survives a browser restart, including the frontend token-storage scoping (persistent cookie + `localStorage` vs session cookie + `sessionStorage`) and the `RememberMe` request field.
- `refresh-tokens`: Update refresh-token issuance so the HttpOnly `refreshToken` cookie is persistent (30-day `Expires`) when `RememberMe` is true and a browser session cookie when false; the DB token keeps its 30-day absolute expiry. Update rotation so the re-issued cookie preserves the family's persistence.

## Impact

- **Backend**: `LoginRequest`/`RegisterRequest` DTOs, `AuthEndpoints` (login/register/refresh wiring, `SetRefreshTokenCookie` persistence parameter), `IRefreshTokenService`/`RefreshTokenService` (`IssueTokenAsync`/`RotateTokenAsync` persistence), `RefreshToken` entity + EF configuration + a new EF Core migration adding `IsPersistent`.
- **Frontend**: `stores/useAuthStore.ts` (login/register signatures, `setSession`/`init`/`clearSession` storage scoping), `pages/login.vue` (pass the checkbox value).
- **Security**: Users on shared machines can end their session on browser close; the refresh token remains HttpOnly/SameSite=Lax with unchanged server-side expiry.
- **Compatibility**: `RememberMe` defaults to `true` when omitted, so existing clients and the current default-checked UX keep 30-day persistence. Existing `RefreshToken` rows default to `IsPersistent = true`.
- **Dependencies**: No new packages.
