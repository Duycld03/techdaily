# Tasks

## 1. Api (Backend Cookie Scope)

- [x] 1.1 Update `SetRefreshTokenCookie` and `ClearRefreshTokenCookie` in `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs` to use `Path = "/"` instead of `Path = "/api/v1/auth"`.
- [x] 1.2 Verify backend test suite integrity via `dotnet test backend/TechDaily.sln`.

## 2. Frontend (Login Auto-Restore & Verification)

- [x] 2.1 Update `frontend/pages/login.vue` `onMounted` lifecycle hook to attempt `authStore.tryRefreshToken()` when a session token or refresh cookie is present, navigating directly to `getRedirectTarget()` upon successful rotation.
- [x] 2.2 Verify full frontend test suite execution via `npm --prefix frontend test`.
