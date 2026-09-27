# Tasks

## 1. Backend / Api

- [x] 1.1 Update `GoogleAuthRequest` in `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs` to accept optional `IdToken` and `AccessToken`.
- [x] 1.2 Implement dual-token authentication logic in `MapPost("/google", ...)` supporting OAuth2 Access Tokens (`ya29...`) via `https://www.googleapis.com/oauth2/v3/userinfo` alongside JWT ID Token validation.
- [x] 1.3 Service-locate `IHttpClientFactory` via `HttpContext.RequestServices.GetService<IHttpClientFactory>()` with fallback instantiation to preserve unit test execution in lightweight test hosts.
- [x] 1.4 Register base `AddHttpClient()` in `backend/src/TechDaily.Infrastructure/DependencyInjection.cs` to guarantee factory availability across all production scopes.

## 2. Frontend

- [x] 2.1 Restore the custom Google Sign-In button markup in `frontend/pages/login.vue` adhering to Studio layout aesthetics (`bg-slate-100 dark:bg-[#202024]`, centered Google SVG icon, localized label).
- [x] 2.2 Wire client-side authentication initialization in `frontend/pages/login.vue` using `google.accounts.oauth2.initTokenClient` with `openid email profile` scopes.
- [x] 2.3 Implement `triggerGoogleSignIn()` method invoking `tokenClient.requestAccessToken({ prompt: 'select_account' })` on direct user click.
- [x] 2.4 Update `authStore.googleLogin()` in `frontend/stores/useAuthStore.ts` to transmit either `accessToken` or `idToken` payload transparently.

## 3. Verification

- [x] 3.1 Execute `dotnet build backend/TechDaily.sln` to confirm zero compilation warnings or errors.
- [x] 3.2 Execute `dotnet test backend/TechDaily.sln` to verify 100% test pass rate across all API and Domain test suites.
- [x] 3.3 Execute `npm test` in `frontend/` to verify zero test regressions.
- [x] 3.4 Verify Google Sign-In button appearance and account selection popup behavior in headless Chromium browser.
