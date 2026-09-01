# Proposal: Hybrid Authentication & Seamless Password Setup for Google OAuth Accounts

## 1. Why (Problem & Motivation)

Users who sign up or log in to TechDaily via **Google OAuth 2.0** on desktop browsers frequently face access hurdles when switching to other devices (such as mobile phones, tablets, or corporate networks):
1. **OAuth Origin URL Mismatch on Local/Mobile Networks:** Google OAuth requires pre-registering authorized JavaScript origins (`http://192.168.x.x:3000`, local IPs, or custom domains). When developers access the app from a mobile browser on the same Wi-Fi, Google OAuth blocks login with `Error 400: origin_mismatch`.
2. **No Initial Password for Google Accounts:** Because Google OAuth accounts have no password (`hasPassword = false`), users who cannot use Google OAuth on mobile are locked out unless they know how to initialize a password.
3. **Lack of In-App Guidance:** Users are often unaware that TechDaily allows setting a standard password on Google accounts to enable universal email/password login across all platforms.

By implementing a streamlined **Hybrid Authentication Password Setup & Mobile Handoff** system, users can easily initialize a password from their Google-linked account, allowing seamless access anywhere via standard email/password credentials.

---

## 2. What (Scope & Deliverables)

### Capability 1: In-App Password Initialization Guidance
- **Profile Security Focus (`/profile`):** When `hasPassword == false`, display an informative banner in the Security tab: *"Your account is linked with Google. Set a password to log in with email and password on any mobile or desktop device."*
- **Post-Login Mobile Handoff Banner:** After logging in via Google for the first time or when visiting `/today` without a password, display a dismissible prompt: *"Want to access TechDaily on your phone? Set a password in your Profile to log in anywhere."*

### Capability 2: Zero-Friction Password Setup & Live Store Sync
- **Dedicated Set-Password Flow:** When `hasPassword == false`, hide the `Current Password` field and provide a streamlined `New Password` + `Confirm Password` interface with strength indicators.
- **Immediate State Synchronization:** When a password is successfully created, update `useAuthStore` and `useProfileStore` (`hasPassword = true`) in real-time so all UI components immediately reflect the updated state without full page reload.

### Capability 3: Email-Based Password Setup Request for Unauthenticated Mobile Users
- **"Set Password via Email" on Login Page (`/login`):** For users stranded on a mobile device who only have Google sign-in on desktop, provide a *"First time on mobile? Set account password"* flow where they enter their Google email and receive a secure one-time password setup verification code / magic link.

### Capability 4: Bilingual Localization & Documentation
- Full i18n support in Vietnamese (`vi-VN`) and English (`en-US`).
- Update domain invariants in `docs/domain-rules.md` and user guide in `README.md`.

---

## 3. Impact & Non-Goals
- **Impact:** Eliminates mobile lockouts, allows friction-free multi-device usage, and preserves high security with PBKDF2 password hashing.
- **Non-Goals:** Bypassing authentication or allowing password creation without proving identity / account ownership.
