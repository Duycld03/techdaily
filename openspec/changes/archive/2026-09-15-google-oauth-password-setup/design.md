# Technical Design: Hybrid Authentication & Seamless Password Setup for Google OAuth Accounts

## 1. Architectural Overview

TechDaily uses a **Clean Architecture** backend with ASP.NET Core (.NET 10) and a **Nuxt 4 / Pinia** frontend. 
User authentication supports dual identity providers:
1. **PBKDF2 Password Authentication:** `Email` + `PasswordHash` (100,000 SHA-256 iterations).
2. **Google OAuth 2.0 Identity:** `Email` + `GoogleSubjectId` verified via Google token validation.

When a user registers via Google OAuth, `user.PasswordHash` is initially `null` or empty. This design unifies credential management so that any Google user can establish a standard password at any time.

---

## 2. Component Interactions & Data Flow

```mermaid
sequenceDiagram
    autonumber
    actor User as Engineer (Google OAuth User)
    participant UI as Nuxt 4 (Profile / Security Tab)
    participant Store as Pinia (useProfileStore / useAuthStore)
    participant API as ASP.NET Core Minimal API (/api/v1/user)
    participant DB as PostgreSQL (Users Table)

    User->>UI: Navigates to /profile -> Security Tab
    UI->>Store: fetchProfile()
    Store->>API: GET /api/v1/user/profile
    API->>DB: Query User entity
    DB-->>API: User (PasswordHash == null, GoogleSubjectId != null)
    API-->>Store: { hasPassword: false, isGoogleLinked: true }
    Store-->>UI: Displays "Set Password for Mobile Access" banner

    User->>UI: Enters New Password ("Password123!") & Confirms
    User->>UI: Clicks "Set Password" (Thiết Lập Mật Khẩu)
    UI->>Store: changePassword(null, "Password123!")
    Store->>API: PUT /api/v1/user/change-password { newPassword: "..." }
    API->>API: Validates len >= 6; Hash with PBKDF2
    API->>DB: UPDATE Users SET PasswordHash = ..., UpdatedAt = NOW()
    DB-->>API: Rows Affected: 1
    API-->>Store: 200 OK { message: "Password updated successfully." }
    Store->>Store: Set profile.hasPassword = true
    Store-->>UI: Shows success toast; UI transitions to "Update Password" mode
```

---

## 3. Detailed Component Designs

### A. Frontend Layer (`frontend/`)
1. **Profile Security Tab (`pages/profile.vue`):**
   - When `hasPassword == false`:
     - Show informational banner: *"Tài khoản liên kết Google chưa có mật khẩu. Bạn có thể thiết lập mật khẩu để đăng nhập trên các thiết bị khác hoặc mobile."*
     - Hide the `Current Password` input.
     - Button label changes dynamically to `$t('profile.set_password_btn')`.
     - Real-time password strength meter (Weak / Good / Strong) and match verification.
2. **Mobile Handoff Notification Banner (`components/today/MobileHandoffBanner.vue` or `pages/today.vue`):**
   - For Google OAuth users with `hasPassword == false`, display a subtle dismissible banner offering a quick link to set up their password.
3. **Pinia Stores (`useAuthStore.ts` & `useProfileStore.ts`):**
   - Update state reactively when password is set without requiring re-authentication.

### B. Backend Layer (`backend/`)
1. **Change/Set Password Endpoint (`UserEndpoints.cs`):**
   - `PUT /api/v1/user/change-password`
   - Check if user already has `PasswordHash`:
     - If `!string.IsNullOrEmpty(user.PasswordHash)`: require and verify `request.CurrentPassword`.
     - If `string.IsNullOrEmpty(user.PasswordHash)`: allow setting `request.NewPassword` without `CurrentPassword`.
   - Hash with `PasswordHasher.HashPassword(request.NewPassword)`.
   - Return `200 OK`.
2. **Security Invariants:**
   - Always enforce `.RequireAuthorization()` on `/api/v1/user/change-password`.
   - Enforce minimum password length of 6 characters.

---

## 4. Localization Keys (i18n)

```json
{
  "profile": {
    "google_no_password_notice": "Tài khoản của bạn đăng nhập qua Google và chưa có mật khẩu riêng. Hãy thiết lập mật khẩu để có thể đăng nhập trên mọi thiết bị và ứng dụng di động.",
    "set_password_btn": "Thiết Lập Mật Khẩu",
    "password_set_success": "Thiết lập mật khẩu thành công! Bây giờ bạn có thể đăng nhập bằng Email và Mật khẩu trên mọi thiết bị."
  }
}
```
