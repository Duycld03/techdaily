# Proposal: Google Account Password State Transition & Lifecycle Flow

## Why

When a user logs in for the first time via Google OAuth, their account has no email password configured (`hasPassword: false`). In this initial state, the User Profile Security tab (`frontend/pages/profile.vue`) must omit the "Current Password" field and display an informational tip explaining that setting a password enables email sign-in on other devices.

However, once the user creates their password (or for any account where `hasPassword: true`), the interface must immediately transition into standard password update mode: the setup tip banner must be hidden, the "Current Password" field must be displayed and required to protect existing credentials, and the submit button must reflect "Update Password" (`profile.update_password_btn`). Currently, the UI fails to transition properly—either displaying the tip indefinitely or omitting the current password field even after a password has been set.

## What Changes

- **Google Tip Banner Conditional Display**: In `frontend/pages/profile.vue`, update the Google banner condition to render strictly when an account is linked to Google AND has not yet created a password (`profileStore.profile?.isGoogleLinked && !profileStore.profile?.hasPassword`). Once `hasPassword` is true, the banner is hidden.
- **Current Password Input Field Conditional Display**: In `frontend/pages/profile.vue`, render the "Current Password" input field whenever `profileStore.profile?.hasPassword` is true (`v-if="profileStore.profile?.hasPassword"`). For initial Google OAuth users who have no password yet (`!hasPassword`), the field is omitted.
- **Backend Password Verification Lifecycle Alignment**: In `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs`, enforce `CurrentPassword` verification whenever the user already has a password (`!string.IsNullOrEmpty(user.PasswordHash)`). When `user.PasswordHash` is null or empty (initial setup), `CurrentPassword` verification is bypassed.
- **Immediate Reactive UI Transition**: Ensure that upon successful password creation via `useProfileStore.changePassword`, `profile.hasPassword` is set to `true`, clearing password form inputs and immediately switching the UI to the established password state (tip hidden, current password field visible, submit button displaying "Update Password").
- **Automated Tests**: Add and update tests in `backend/tests/TechDaily.Tests/Application/HybridAuthPasswordTests.cs` and `frontend/tests/pages/profile.spec.ts` covering both lifecycle phases.

## Capabilities

### Modified Capabilities

- `auth`: Update requirement `Google OAuth Password Setup & State Visibility` and `In-App Mobile Handoff Guidance` to specify the two-phase lifecycle:
  1. *Stage 1 (Initial Setup)*: `hasPassword == false` $\rightarrow$ Tip banner displayed, "Current Password" field hidden, button displays "Set Password".
  2. *Stage 2 (Established Credentials)*: `hasPassword == true` $\rightarrow$ Tip banner hidden, "Current Password" field displayed and required, button displays "Update Password".

## Impact

- **API Contracts**: Zero breaking changes. `PUT /api/v1/user/change-password` retains its existing request model (`ChangePasswordRequest(string? CurrentPassword, string NewPassword)`).
- **Security & UX**: Ensures consistent credential protection. Initial onboarding is frictionless (no impossible current password prompt), while subsequent updates are secured with current password verification once credentials exist.
