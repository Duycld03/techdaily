# Design: Fix Google Account Password Requirement & Verification

## Context

See `proposal.md` for motivation. Currently:
1. `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs` enforces `CurrentPassword` verification on `PUT /api/v1/user/change-password` whenever `!string.IsNullOrEmpty(user.PasswordHash)`, without checking whether the user is linked to Google OAuth (`user.GoogleSubjectId`).
2. `frontend/pages/profile.vue` renders the "Current Password" field whenever `profileStore.profile?.hasPassword` is true, marking it `required`.
3. `frontend/pages/profile.vue` displays `profile.google_password_hint` ("Tài khoản liên kết Google. Bạn có thể đặt thêm mật khẩu để đăng nhập bằng Email.") for any Google account, even when `hasPassword` is true.

When a user who logs in via Google visits the Security tab, if their account has an existing password in the database (e.g. from prior testing or registration), they are prompted for a current password they may not possess or remember, and the backend rejects their request if omitted.

## Goals / Non-Goals

**Goals:**
- Allow Google-linked accounts (`user.GoogleSubjectId != null`) to set or update their email login password without entering a current password.
- Strictly maintain `CurrentPassword` verification for standard email accounts (`user.GoogleSubjectId == null && user.PasswordHash != null`).
- Omit the "Current Password" input field on `/profile` for Google-linked users.
- Differentiate Google account security banner messaging between initial password setup and existing password update.
- Ensure 100% automated test pass rate across backend xUnit and frontend Vitest suites.

**Non-Goals:**
- Removing `currentPassword` verification for standard accounts.
- Modifying the Google OAuth authentication endpoint (`/api/v1/auth/google`).
- Requiring re-authentication via external Google OAuth popup for password changes.

## Decisions

### 1. Backend Verification Bypass for Google Accounts
- **Decision**: In `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs`, check both `user.PasswordHash` and `user.GoogleSubjectId`:
  ```csharp
  // If user has existing password AND is not a Google-linked account, verify current password
  if (!string.IsNullOrEmpty(user.PasswordHash) && string.IsNullOrEmpty(user.GoogleSubjectId))
  {
      if (string.IsNullOrEmpty(request.CurrentPassword) || !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
      {
          return Results.BadRequest(new { code = Error.CurrentPasswordIncorrect.Code, error = Error.CurrentPasswordIncorrect.Message });
      }
  }
  ```
- **Rationale**: Google OAuth users are externally authenticated by Google. If a Google account already has a password hash in the database, requiring the old password locks out legitimate Google users who only authenticate via Google.

### 2. Frontend Field Visibility
- **Decision**: In `frontend/pages/profile.vue`, update the condition for rendering the "Current Password" block from:
  ```vue
  <div v-if="profileStore.profile?.hasPassword">
  ```
  to:
  ```vue
  <div v-if="!profileStore.profile?.isGoogleLinked && profileStore.profile?.hasPassword">
  ```
- **Rationale**: For Google-linked accounts, `currentPassword` is never required. Hiding it removes the blocked input and prevents validation errors.

### 3. Differentiated Google Banner Messaging
- **Decision**: Update the Google banner in `frontend/pages/profile.vue`:
  ```vue
  <div
    v-if="profileStore.profile?.isGoogleLinked"
    class="p-3.5 rounded-xl bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200/80 dark:border-blue-500/20 flex items-center gap-2.5 text-xs text-blue-700 dark:text-blue-300"
  >
    <Shield class="w-4 h-4 shrink-0 text-blue-500" />
    <span>
      {{ profileStore.profile?.hasPassword ? $t('profile.google_password_hint_linked') : $t('profile.google_password_hint') }}
    </span>
  </div>
  ```
  And add localized strings:
  - `en.json`: `"google_password_hint_linked": "Connected with Google. You can update your email password directly without entering your current password."`
  - `vi.json`: `"google_password_hint_linked": "Tài khoản liên kết Google. Bạn có thể cập nhật mật khẩu Email mà không cần nhập mật khẩu hiện tại."`
- **Rationale**: Eliminates the contradictory state where a user with an existing password is told "You can set a password to sign in via Email", making the interface clear and intuitive.

### 4. Test Suite Coverage
- **Decision**:
  - Add `GoogleUser_WithExistingPassword_CanUpdatePasswordWithoutCurrentPassword` to `backend/tests/TechDaily.Tests/Application/HybridAuthPasswordTests.cs`.
  - Update `frontend/tests/pages/profile.spec.ts` to assert that Google-linked accounts do not render `profile.current_password`.

## Risks / Trade-offs

- **Risk**: An unauthorized person with temporary access to an unlocked device could set or change the email password without knowing the previous password.
  - *Mitigation*: This aligns with industry-standard OAuth behavior (GitHub, Google, Supabase, Firebase). The active session is authenticated via JWT. For high-security environments, step-up authentication or email notifications can be introduced as future enhancements.
