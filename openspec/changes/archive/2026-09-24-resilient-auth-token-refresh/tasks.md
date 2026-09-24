# Tasks: Resilient Auth Token Refresh Lifecycle

## 1. Api (Backend)

- [x] 1.1 Update `AuthEndpoints.cs` `SetRefreshTokenCookie` and `ClearRefreshTokenCookie` to set `Secure` dynamically based on `context.Request.IsHttps` and `X-Forwarded-Proto: https`
- [x] 1.2 Update `GenerateJwtToken` in `AuthEndpoints.cs` to read `Jwt:ExpiryMinutes` from configuration with fallback to 60

## 2. Frontend Store & API Client

- [x] 2.1 Refactor `useAuthStore.ts` `init()` to remove immediate `clearSession()` on expired tokens, preserving user state for refresh
- [x] 2.2 Implement `tryRefreshToken(): Promise<boolean>` in `useAuthStore.ts` that coordinates with `useApiClient().refreshAuthToken()`
- [x] 2.3 Verify `useApiClient.ts` in-flight refresh promise deduplication and cookie synchronization on refresh completion

## 3. Frontend Route Middleware & Navigation

- [x] 3.1 Update `frontend/middleware/auth.global.ts` to asynchronously await `authStore.tryRefreshToken()` when access token is expired before rejecting navigation
- [x] 3.2 Preserve attempted destination URL in `redirect` query parameter when refresh terminally fails and redirect to `/login` occurs

## 4. Automated Testing & Verification

- [x] 4.1 Update or add unit tests for `auth.global.ts` and `useAuthStore.ts` covering expired token refresh and non-destructive initialization
- [x] 4.2 Run backend unit tests (`dotnet test`) and frontend test suite (`npm test`) to ensure 100% pass rate
- [x] 4.3 Verify end-to-end transparent refresh in headless browser with an expired access token
