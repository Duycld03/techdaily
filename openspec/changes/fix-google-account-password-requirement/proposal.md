# Proposal: Fix Google Account Password Requirement & Verification

## Why

When users authenticate via Google OAuth, their identity is cryptographically verified by Google. However, on the User Profile Security tab (`frontend/pages/profile.vue`), if a Google-linked account has an existing password in the database (e.g. from prior email registration, previous password setup, or environment seeding), the interface requires entering "Current Password" (`profile.current_password`), and the backend endpoint `PUT /api/v1/user/change-password` rejects updates with `USER_CURRENT_PASSWORD_INCORRECT` if `currentPassword` is omitted or invalid.

This traps Google OAuth users who do not know or remember an old email password, blocking them from establishing or updating their password to access the platform across mobile apps or devices without Google OAuth. Furthermore, the Google security banner displays "You can set a password to sign in via Email" regardless of whether a password is already configured.

## What Changes

- **Backend Password Verification Exemption for Google Accounts**: In `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs`, update the `PUT /api/v1/user/change-password` endpoint so `CurrentPassword` verification is only required for standard email accounts (`string.IsNullOrEmpty(user.GoogleSubjectId)`). Accounts linked to Google OAuth (`!string.IsNullOrEmpty(user.GoogleSubjectId)`) can set or update passwords directly using `NewPassword` without providing `CurrentPassword`.
- **Frontend Current Password Conditional Visibility**: In `frontend/pages/profile.vue`, display and require the "Current Password" input field exclusively for standard email accounts (`!profileStore.profile?.isGoogleLinked && profileStore.profile?.hasPassword`). Google-linked accounts (`profileStore.profile?.isGoogleLinked == true`) omit the "Current Password" input entirely.
- **Differentiated Google Account Banner**: In `frontend/pages/profile.vue`, adjust the Google connection banner:
  - If `hasPassword == false`: Display `profile.google_password_hint` advising that setting a password enables email sign-in. Action button displays `profile.set_password_btn` ("Set Password").
  - If `hasPassword == true`: Display `profile.google_password_hint_linked` advising that the account is linked to Google and email password can be updated directly. Action button displays `profile.update_password_btn` ("Update Password").
- **Localization Updates**: Add `profile.google_password_hint_linked` to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
- **Automated Verification**: Add backend integration unit tests in `backend/tests/TechDaily.Tests/Application/HybridAuthPasswordTests.cs` and frontend component tests in `frontend/tests/pages/profile.spec.ts`.

## Capabilities

### Modified Capabilities

- `auth`: Update requirement `Google OAuth Password Setup & State Visibility` and `In-App Mobile Handoff Guidance` to allow Google-linked users to update existing passwords without `currentPassword`, and define differentiated banner states for Google-linked accounts.

## Impact

- **API & Domain Contracts**: No breaking API contract changes. `PUT /api/v1/user/change-password` request model (`ChangePasswordRequest`) already defines `CurrentPassword` as nullable `string?`.
- **Security**: Google OAuth users have established authentication through their external OAuth provider; allowing password updates without previous password entry for OAuth-linked users matches industry standards (e.g. Supabase, Firebase Auth, GitHub). Standard email users remain strictly protected with mandatory current password verification.
