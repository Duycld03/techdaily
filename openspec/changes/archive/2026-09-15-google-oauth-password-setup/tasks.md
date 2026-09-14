# Tasks: Hybrid Authentication & Password Setup for Google Accounts

## 1. Backend Tasks
- [x] 1.1 Verify `PUT /api/v1/user/change-password` allows setting initial password when `PasswordHash` is null without requiring `CurrentPassword`.
- [x] 1.2 Ensure `GET /api/v1/user/profile` accurately returns `hasPassword: false` and `isGoogleLinked: true` for Google OAuth users.
- [x] 1.3 Add backend unit/integration tests in `TechDaily.Tests` verifying password setup for Google accounts without existing password hash.

## 2. Frontend Tasks
- [x] 2.1 Update `pages/profile.vue` Security tab to display a dedicated informative banner for Google accounts without passwords.
- [x] 2.2 Add password strength indicator and live match validation in `pages/profile.vue`.
- [x] 2.3 Add a dismissible "Mobile Handoff" prompt banner for Google OAuth users on `/today` or `/profile`.
- [x] 2.4 Update `useProfileStore.ts` to immediately update `profile.hasPassword = true` upon successful password creation.
- [x] 2.5 Add bilingual i18n keys to `en.json` and `vi.json` (`profile.google_no_password_notice`, `profile.password_set_success`, `profile.mobile_handoff_banner`).
- [x] 2.6 Add unit tests in `frontend/tests/` for the password setup and store reactive update flow.

## 3. Verification & Documentation Tasks
- [x] 3.1 Run `dotnet test` and `npm test` to ensure all automated tests pass.
- [x] 3.2 Perform Playwright visual verification in Vietnamese (`vi`) on Mobile (390x844) and Desktop (1280x800).
- [x] 3.3 Update documentation in `docs/domain-rules.md` with Hybrid Authentication invariants.
