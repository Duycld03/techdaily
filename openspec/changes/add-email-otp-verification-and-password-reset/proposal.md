# Proposal

## Why

The `/login` "forgot password" flow is cosmetic (the submit handler awaits `Promise.resolve()` and shows a success toast without any backend call), `/register` creates and logs in an account from any email string with no proof the address is real or reachable, and the platform has zero email-sending infrastructure. Users cannot recover an account, and unverified/typo'd emails accumulate as unusable accounts.

## What Changes

- Add transactional email delivery over SMTP using Gmail app-password credentials, behind an `IEmailSender` seam so the transport can be swapped later. Credentials are secrets (never committed).
- **OTP-first registration**: `/register` becomes a two-step, email-verified flow. Step 1 validates the input and emails a 6-digit code; the `User` row is created only after step 2 confirms the code. No unverified rows ever exist in `Users`.
- **OTP password reset**: the forgot-password flow emails a 6-digit code; confirming the code lets the user set a new password. Google-linked accounts with no password may use it to establish one.
- **Global session revocation on reset**: a successful password reset revokes all of the user's refresh-token families (every device is signed out). Email/password users re-authenticate manually with the new password; Google-linked users re-authenticate via the one-click Google button without a manual password login.
- Shared OTP policy: 6-digit numeric code, 10-minute expiry, max 5 verification attempts, 60-second resend cooldown, stored hashed at rest (SHA-256), rate-limited, and anti-enumeration on the forgot-password request (always `200`).
- New machine-readable auth error codes for OTP failures, mapped to the system i18n catalog (en/vi). Registration with an already-registered email still returns a clear `AUTH_EMAIL_EXISTS`.

## Capabilities

### New Capabilities
- `email-delivery`: Transactional email delivery via configured SMTP transport (Gmail app-password), with secret credential handling and explicit failure behavior when unconfigured.

### Modified Capabilities
- `auth`: OTP-verified registration replaces the trust-any-email registration; the existing "Email-Based Password Setup for Stranded Mobile Users" requirement becomes a concrete OTP reset flow; the machine-readable error-code requirement gains OTP codes.
- `refresh-tokens`: a successful password reset revokes all refresh-token families for the user.

## Impact

- **Backend API** (`TechDaily.Api/Endpoints/AuthEndpoints.cs`): new endpoints for register-request/verify OTP, forgot-password, reset-password, and resend; `/register` split into the OTP flow. New rate-limiter policy (reuse the existing `AddRateLimiter` in `Program.cs`).
- **Domain** (`TechDaily.Domain/Entities`): new `EmailOtp` (or equivalent) and pending-registration store; **EF Core migration** required.
- **Infrastructure**: new `IEmailSender` interface + `SmtpEmailSender` (Gmail SMTP) registered in DI; new `Email` config section in `appsettings.json` with credentials only in `appsettings.Local.json`/`.env`.
- **Frontend** (`frontend/pages/login.vue`, `stores/useAuthStore.ts`): two-step OTP UI for both register and forgot-password (code entry + resend timer); new i18n keys in `en.json`/`vi.json`.
- **Tests**: backend endpoint/OTP-policy tests; frontend data-contract tests for the two-step flows.
