# Tasks

## 1. Backend Token & Service Enhancements

- [x] 1.1 In `backend/src/TechDaily.Api/Program.cs`, configure `ClockSkew = TimeSpan.FromMinutes(1)` in `TokenValidationParameters` to absorb minor client-server clock drift.
- [x] 1.2 In `backend/src/TechDaily.Infrastructure/Services/RefreshTokenService.cs`, widen the multi-tab concurrent refresh token reuse grace window from 10 seconds to 60 seconds (`TimeSpan.FromSeconds(60)`).

## 2. Frontend Interceptors & Middleware Enhancements

- [x] 2.1 In `frontend/composables/useApiClient.ts`, widen the proactive refresh lead time from 30 seconds to 5 minutes (`300_000 ms`), and ensure transient network errors (`Failed to fetch`, 502/503) do not invoke `authStore.clearSession()`.
- [x] 2.2 In `frontend/middleware/auth.global.ts`, update the SSR navigation guard to defer redirecting to `/login` to client-side hydration, allowing stored credentials and silent refresh to execute without premature session ejection.
- [x] 2.3 In `frontend/stores/useAuthStore.ts`, verify consistent cookie and storage synchronization on session restoration across page reloads.

## 3. Testing & Dual-Gate Verification

- [x] 3.1 Update backend unit tests in `backend/tests/TechDaily.Tests/` to verify the 60-second multi-tab rotation grace window and 1-minute clock skew tolerance.
- [x] 3.2 Update frontend unit tests in `frontend/tests/composables/useApiClient.spec.ts` and `frontend/tests/middleware/auth.spec.ts` to verify proactive refresh lead time, transient network resilience, and client-hydration route guard handling.
- [x] 3.3 Run full test suites (`dotnet test backend/` and `npm test`) to verify 100% test pass rate for Gate 1.
- [x] 3.4 Programmatically drive headless Chromium via `browser` in `eval` across Desktop and Mobile viewports to verify persistent session survival across simulated reloads and navigation (Gate 2).
