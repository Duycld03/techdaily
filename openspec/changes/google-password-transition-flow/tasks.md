# Tasks

## 1. Frontend (Profile View & State Transition)

- [x] 1.1 Update `frontend/pages/profile.vue` so the Google tip banner renders strictly when `profileStore.profile?.isGoogleLinked && !profileStore.profile?.hasPassword`, hiding it once `hasPassword` is true.
- [x] 1.2 Update `frontend/pages/profile.vue` so the "Current Password" input field renders whenever `profileStore.profile?.hasPassword` is true, ensuring it appears for subsequent password updates.
- [x] 1.3 Ensure `handlePasswordChange` in `frontend/pages/profile.vue` submits `currentPassword.value` when `hasPassword` is true and resets form inputs on success.

## 2. Api (Backend Verification)

- [x] 2.1 Ensure `PUT /api/v1/user/change-password` in `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs` verifies `CurrentPassword` whenever `!string.IsNullOrEmpty(user.PasswordHash)` and bypasses it when `string.IsNullOrEmpty(user.PasswordHash)`.

## 3. Automated Verification & Testing

- [x] 3.1 Update unit tests in `frontend/tests/pages/profile.spec.ts` to assert that Google accounts with `hasPassword: false` hide "Current Password" and show the tip banner, while Google accounts with `hasPassword: true` show "Current Password" and hide the tip banner.
- [x] 3.2 Run backend test suite (`dotnet test backend/tests/TechDaily.Tests`) and frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate.
- [x] 3.3 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
