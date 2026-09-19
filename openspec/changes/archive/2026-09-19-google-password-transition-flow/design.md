# Design: Google Account Password State Transition & Lifecycle Flow

## Context

See `proposal.md` for motivation. A Google OAuth account undergoes a two-stage credential lifecycle:
1. **Stage 1 (Initial Setup - `hasPassword: false`)**:
   - The user has authenticated via Google OAuth, but has never created an application-level email password.
   - The UI must inform the user that they can create an email password to log in on alternative devices.
   - The UI cannot ask for a "Current Password" because no password exists.
   - The action is "Set Password" (`profile.set_password_btn`).
2. **Stage 2 (Established Credentials - `hasPassword: true`)**:
   - The user has successfully created a password (or initially registered with an email password).
   - The onboarding tip is no longer applicable and must be hidden.
   - Changing the password requires entering the "Current Password" to prevent unauthorized modifications.
   - The action is "Update Password" (`profile.update_password_btn`).

## Goals / Non-Goals

**Goals:**
- Render the Google setup tip banner if and only if `isGoogleLinked && !hasPassword`.
- Render the "Current Password" input field if and only if `hasPassword` is true.
- Maintain reactive state synchronization: creating a password immediately transitions the UI from Stage 1 to Stage 2.
- In the backend, require `CurrentPassword` verification whenever `!string.IsNullOrEmpty(user.PasswordHash)`.
- Ensure 100% automated test pass rate across backend and frontend suites.

**Non-Goals:**
- Introducing an email-based forgot password reset link (handled separately).
- Modifying Google OAuth token verification or login endpoints.
- Database schema changes.

## Decisions

### 1. Frontend Template Conditionals in `frontend/pages/profile.vue`
- **Google Tip Banner**:
  ```vue
  <div
    v-if="profileStore.profile?.isGoogleLinked && !profileStore.profile?.hasPassword"
    class="p-3.5 rounded-xl bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200/80 dark:border-blue-500/20 flex items-center gap-2.5 text-xs text-blue-700 dark:text-blue-300"
  >
    <Shield class="w-4 h-4 shrink-0 text-blue-500" />
    <span>{{ $t('profile.google_password_hint') }}</span>
  </div>
  ```
  *Rationale*: Once a password is created, the tip is redundant. Hiding it provides a clean, focused security management screen.

- **Current Password Input Field**:
  ```vue
  <div v-if="profileStore.profile?.hasPassword">
    <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
      {{ $t('profile.current_password') }}
    </label>
    <div class="relative">
      <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
      <input
        v-model="currentPassword"
        required
        :type="showCurrentPassword ? 'text' : 'password'"
        placeholder="••••••••"
        class="..."
      />
      ...
    </div>
  </div>
  ```
  *Rationale*: Strictly maps to whether credentials exist in the system.

- **Submit Button Text**:
  ```vue
  <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
  ```

- **Password Submission Method**:
  ```ts
  const currentPwd = profileStore.profile?.hasPassword ? currentPassword.value : null
  await profileStore.changePassword(currentPwd, newPassword.value)
  ```

### 2. Backend Verification Invariant in `backend/src/TechDaily.Api/Endpoints/UserEndpoints.cs`
- Enforce:
  ```csharp
  // If user has existing password, verify current password
  if (!string.IsNullOrEmpty(user.PasswordHash))
  {
      if (string.IsNullOrEmpty(request.CurrentPassword) || !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
      {
          return Results.BadRequest(new { code = Error.CurrentPasswordIncorrect.Code, error = Error.CurrentPasswordIncorrect.Message });
      }
  }
  ```
  *Rationale*: Consistent security model across all user types. Accounts without a password can establish one; accounts with a password require verification.

### 3. Reactive State Transition in `frontend/stores/useProfileStore.ts`
- In `changePassword`:
  ```ts
  if (profile.value) {
    profile.value.hasPassword = true
  }
  ```
  And in `profile.vue`:
  ```ts
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  ```
  *Rationale*: Re-renders the component immediately upon response, eliminating stale states.

## Risks / Trade-offs

- **Risk**: A Google OAuth user who previously created a password and forgot it will be prompted for `CurrentPassword`.
  - *Mitigation*: This is the standard security guarantee expected of credential systems. Users who forgot their password can sign in with Google or reset via standard mechanisms once email password resets are added.
