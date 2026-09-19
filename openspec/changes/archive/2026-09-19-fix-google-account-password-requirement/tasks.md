# Tasks

## 1. Api (Backend Endpoint)

- [x] 1.1 Update `PUT /api/v1/user/change-password` in `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs` to only enforce `CurrentPassword` verification when `string.IsNullOrEmpty(user.GoogleSubjectId)`.
- [x] 1.2 Add automated unit test `GoogleUser_WithExistingPassword_CanUpdatePasswordWithoutCurrentPassword` to `backend/tests/TechDaily.Tests/Application/HybridAuthPasswordTests.cs`.

## 2. Frontend (Profile View & Localization)

- [x] 2.1 Add `profile.google_password_hint_linked` localization key to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
- [x] 2.2 Update `frontend/pages/profile.vue` so the "Current Password" input field only renders for standard accounts (`!profileStore.profile?.isGoogleLinked && profileStore.profile?.hasPassword`).
- [x] 2.3 Update Google connection banner in `frontend/pages/profile.vue` to dynamically render `profile.google_password_hint_linked` when `hasPassword` is true.

## 3. Automated Verification & Testing

- [x] 3.1 Update frontend component unit tests in `frontend/tests/pages/profile.spec.ts` to assert that Google-linked accounts omit `profile.current_password`.
- [x] 3.2 Run backend test suite (`dotnet test backend/tests/TechDaily.Tests`) and frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate.
- [x] 3.3 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
