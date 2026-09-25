# Tasks

## 1. Backend persistence layer

- [x] 1.1 Add a non-nullable `bool IsPersistent` (default `true`) to `RefreshToken` (`backend/src/TechDaily.Domain/Entities/RefreshToken.cs`). Verify: `dotnet build` succeeds.
- [x] 1.2 Map `IsPersistent` in `RefreshTokenConfiguration` (`backend/src/TechDaily.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`) with a database default of `true`. Verify: build succeeds and the property appears in the model snapshot after step 1.4.
- [x] 1.3 Extend `IRefreshTokenService.IssueTokenAsync` with `bool isPersistent = true`; in `RefreshTokenService`, set `token.IsPersistent = isPersistent` on issue and pass the parent token's `IsPersistent` into the successor inside `RotateTokenAsync` (both the rotation and the grace-window successor paths return a token carrying the family's `IsPersistent`). Verify: `dotnet build`; existing refresh-token tests still pass (`dotnet test` filtered to the refresh-token suite).
- [x] 1.4 Add an EF Core migration adding `IsPersistent boolean NOT NULL DEFAULT true` (`dotnet ef migrations add AddRefreshTokenIsPersistent` in `backend/src/TechDaily.Infrastructure`). Verify: generated `Up` adds the column with default `true` and `Down` drops it; `dotnet build` succeeds.

## 2. API auth endpoints

- [x] 2.1 Add `bool RememberMe = true` to the `LoginRequest` and `RegisterRequest` records (`backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs`). Verify: build succeeds; an omitted `rememberMe` binds to `true`.
- [x] 2.2 Add a `bool persistent` parameter to `SetRefreshTokenCookie`; set `Expires = DateTimeOffset.UtcNow.AddDays(30)` only when `persistent` is true, otherwise emit no `Expires`/`Max-Age` (session cookie). Keep HttpOnly, Secure-by-scheme, SameSite=Lax, and `Path=/api/v1/auth` unchanged. Verify: build succeeds.
- [x] 2.3 Wire the endpoints: `login`/`register` call `IssueTokenAsync(user.Id, isPersistent: request.RememberMe)` and `SetRefreshTokenCookie(context, rawRefreshToken, request.RememberMe)`; `google` issues persistent (`isPersistent: true`, `SetRefreshTokenCookie(..., persistent: true)`); `/refresh` calls `SetRefreshTokenCookie(context, newRawToken, rotateResult.Value.NewToken.IsPersistent)`. Verify: build succeeds.
- [x] 2.4 Add backend tests asserting: login with `rememberMe: false` emits a `refreshToken` `Set-Cookie` with no `Max-Age`/`Expires`; login with `rememberMe: true` and with the field omitted emits `Max-Age=2592000`; Google login emits `Max-Age=2592000`; and `/refresh` on a session-scoped family re-issues a session cookie while a persistent family re-issues `Max-Age=2592000`. Verify: `dotnet test` passes for the auth and refresh-token suites.

## 3. Frontend session scoping

- [x] 3.1 In `frontend/stores/useAuthStore.ts`, add a `rememberMe` parameter to `login(email, password, rememberMe)` and `register(email, password, name?, locale?, rememberMe)` and include `rememberMe` in the request body; make `setSession(token, user, remember)` write `techdaily_token`/`techdaily_user` as 30-day cookies + `localStorage` when `remember` is true, and as session cookies (no `maxAge`) + `sessionStorage` (never `localStorage`) when false; make `init()` read the cookie first then fall back to `localStorage` or `sessionStorage`; make `clearSession()` remove both storages and both cookies. Verify: `npm run typecheck`/build succeeds.
- [x] 3.2 In `frontend/pages/login.vue` `handleSubmit`, pass `rememberSession.value` to `authStore.login(...)` and `authStore.register(...)`. Verify: the login request payload includes the checkbox value.
- [x] 3.3 Add/adjust Vitest data-contract tests (no CSS/layout assertions): login submits `{ email, password, rememberMe }`; with `rememberMe: false`, `setSession` writes to `sessionStorage` and not `localStorage`; with `rememberMe: true`, it writes to `localStorage`; `clearSession` clears both. Verify: `npm test` passes.

## 4. Integration verification

- [x] 4.1 Smoke the running stack for both modes: `POST /api/v1/auth/login` with `rememberMe: true` and `false`, inspecting the `Set-Cookie: refreshToken` attributes (`Max-Age=2592000` vs session cookie), then confirm a `POST /api/v1/auth/refresh` preserves the presented family's persistence. Verify: observed cookie attributes and storage match the `auth` and `refresh-tokens` spec scenarios. (No screenshot gate: the `login.vue` change forwards a boolean and does not alter layout.)
