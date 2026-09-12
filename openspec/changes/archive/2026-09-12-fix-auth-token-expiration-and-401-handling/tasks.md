# Tasks: Fix Auth Token Expiration, Global 401 Interception, and Resilient Drill Submission

## 1. Backend Tasks
- [x] 1.1 Update `GenerateJwtToken` in `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs` to set token expiration to 30 days (`DateTime.UtcNow.AddDays(30)`).
- [x] 1.2 Verify backend auth unit/integration tests continue passing with `dotnet test`.

## 2. Frontend Tasks
- [x] 2.1 Implement `isTokenExpired(token: string | null): boolean` in `frontend/stores/useAuthStore.ts` to decode JWT `exp` timestamp.
- [x] 2.2 Update `isLoggedIn`, `isAuthenticated`, and `init()` in `frontend/stores/useAuthStore.ts` to proactively clear session when the token is expired.
- [x] 2.3 Implement global 401 handling in `frontend/composables/useApiClient.ts`: clear session, emit warning toast, and navigate to `/login?redirect=...`.
- [x] 2.4 Update `submitOption` in `frontend/stores/useDailyFocusStore.ts` to avoid overwriting `error.value`, ensuring submission failures are shown via toast without unmounting the curriculum UI.
- [x] 2.5 Add bilingual i18n keys for session expiration and drill submission errors in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
- [x] 2.6 Add unit tests in `frontend/tests/stores/auth.spec.ts` testing expired token detection and auto-cleanup.
- [x] 2.7 Verify frontend unit tests pass with `npm test`.

## 3. Verification & Documentation
- [x] 3.1 Run full test suites (`dotnet test` and `npm test`).
- [x] 3.2 Verify user experience on mobile and desktop viewports.
