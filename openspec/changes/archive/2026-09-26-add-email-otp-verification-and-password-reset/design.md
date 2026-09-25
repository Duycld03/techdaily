# Design

## Context

See proposal.md — Why. Current state, confirmed by reading the code:
- `frontend/pages/login.vue` `handleSubmit()` forgot-password branch is a stub (`await Promise.resolve()` + success toast, no network call).
- `backend/.../AuthEndpoints.cs` `/register` validates, creates the `User`, provisions, issues tokens, and returns an authenticated session in one shot — no email proof.
- No email transport exists anywhere (no SMTP/MailKit/SendGrid package or config).

Reuse points already in the codebase:
- `PasswordHasher` (PBKDF2) for hashing passwords.
- `IRefreshTokenService` for issuing/rotating/revoking refresh cookies; `RefreshTokenService.HashToken` (SHA-256 → lowercase hex) as the hashing convention for opaque secrets.
- `AddRateLimiter` in `Program.cs` (sliding-window, partitioned by user-id/IP) — currently one policy `AiEndpointsPolicy`.
- RFC 7807 problem-details + machine-readable error codes mapped to the frontend i18n catalog (`en.json`/`vi.json`).

## Goals / Non-Goals

Goals:
- Prove email control before an account exists (OTP-first register) and before a password is changed (OTP reset), using one shared OTP mechanism.
- Introduce a swappable email transport with Gmail SMTP as the first implementation.

Non-Goals:
- Magic-link reset (explicitly chose OTP to avoid a new SSR-guarded `/reset-password` route).
- Verifying email on Google OAuth login (Google already asserts the address).
- Email templating/branding beyond a plain, localized OTP message.
- A scheduled cleanup job for expired OTP rows (expiry is enforced at validation time; see Decisions).

## Decisions

### D1 — Email transport: MailKit behind `IEmailSender`
Add `IEmailSender` (Application) + `SmtpEmailSender` (Infrastructure) using **MailKit** over Gmail SMTP (`smtp.gmail.com:587`, STARTTLS). Register in DI.
- Why MailKit over `System.Net.Mail.SmtpClient`: Microsoft explicitly discourages `SmtpClient` for new code; MailKit is the maintained community standard with correct STARTTLS/timeout handling.
- The seam means swapping to a transactional API later touches only one class.

### D2 — SMTP configuration & secrets
New `Email` config section. Non-secret keys (`Email:Smtp:Host`, `Port`, `From`, `UseStartTls`) may live in `appsettings.json`; the **sender username and app password are secrets** and live only in `appsettings.Local.json` / `.env` (gitignored per repo Secrets rule). Committed files carry placeholders, never real values.
- If SMTP credentials are absent at send time, the email send throws and the OTP endpoint returns a server error (no fake "sent" success), per the `email-delivery` spec.

### D3 — Single `EmailOtp` entity carrying an optional pending registration
One table serves both flows:
- Columns: `Id`, `Email` (normalized), `Purpose` (`EmailVerification` | `PasswordReset`), `CodeHash` (SHA-256 hex — never plaintext), `ExpiresAt`, `ConsumedAt` (nullable), `AttemptCount`, `CreatedAt`.
- Registration-only nullable columns hold the **pending registration**: `PendingName`, `PendingPasswordHash` (already PBKDF2-hashed), `PendingLocale`. This is why no `User` row exists pre-verify.
- Why embed rather than a separate `PendingRegistration` table: one self-expiring row per (email, purpose); fewer moving parts; the pending data is meaningless without its OTP.
- At most one active row per `(Email, Purpose)`: a new request after the cooldown replaces the prior row (regenerate code, reset `AttemptCount`); a request inside the 60s cooldown is rejected (`AUTH_OTP_RESEND_COOLDOWN`).
- Storing a PBKDF2 hash (not plaintext) in a short-lived row is acceptable; the row is deleted on success and superseded/expired otherwise.

### D4 — Endpoint contract
- `POST /api/v1/auth/register` — **step 1 (BREAKING)**: now returns no session; validates, checks `AUTH_EMAIL_EXISTS`, stores pending registration, emails OTP.
- `POST /api/v1/auth/register/verify` — step 2: `{email, code}` → create user + provision + issue tokens → returns the existing `AuthSessionResponse`.
- `POST /api/v1/auth/forgot-password` — `{email}` → always `200`; issues reset OTP if the account exists.
- `POST /api/v1/auth/reset-password` — `{email, code, newPassword}` → set hash + revoke all families.
- `POST /api/v1/auth/otp/resend` — `{email, purpose}` → re-issue subject to cooldown.

### D5 — Revoke all sessions on reset
Add `IRefreshTokenService.RevokeAllForUserAsync(Guid userId, CancellationToken)` (existing `RevokeFamilyAsync` only revokes one family by raw token). Reset calls it so every device is signed out.
- Post-reset UX (user decision): email/password users re-authenticate manually with the new password; Google-linked users re-authenticate via the one-click Google button (always-persistent) without a manual password login.

### D6 — Rate limiting & anti-enumeration
- Add an `OtpEndpointsPolicy` (sliding window) partitioned by normalized email + IP, applied to all OTP request/verify endpoints. The current global `OnRejected` detail text is AI-specific; generalize it (or set a per-policy message) so the 429 body is accurate for OTP endpoints.
- `forgot-password` returns `200` regardless of account existence (anti-enumeration). `register` deliberately reveals `AUTH_EMAIL_EXISTS` (user decision: clear error, i18n-mapped) — registration inherently discloses existence.

### D7 — Frontend two-step, no new route
`login.vue` gains a code-entry sub-step for both register and forgot-password (reset also collects the new password), plus a resend cooldown timer and `pendingEmail` state. `useAuthStore` gains methods for request/verify/reset/resend. Chosen OTP specifically so no new SSR-guarded page is needed. New i18n keys added to `en.json` and `vi.json` for every new error code and UI string.

## Risks / Trade-offs

- Gmail SMTP caps (~500 mails/day) and ties sending to a personal account → acceptable at current scale; `IEmailSender` seam allows a provider swap without touching feature logic.
- Deliverability/spam from a personal Gmail sender → acceptable for this project; revisit if bounce/spam becomes an issue.
- 6-digit code brute force → 10-minute expiry + 5-attempt lockout + per-(email,IP) rate limit make the 1e6 space safe.
- BREAKING `/register` contract (no session on step 1) → frontend, store, and tests are updated in this same change so nothing ships half-migrated.
- Provided Gmail app password was shared in chat → it will be placed only in gitignored local config, never committed; treat it as sensitive and rotate if it leaks.

## Migration Plan

1. Add EF Core migration for the `EmailOtp` table (index on `(Email, Purpose)`); run `dotnet ef database update`.
2. Deploy config: non-secret `Email` keys in `appsettings.json`; real Gmail username + app password only in `.env` / `appsettings.Local.json`.
3. Rollback: revert the new endpoints/frontend and drop the migration. Existing users and the login/refresh/google/revoke endpoints are untouched, so rollback does not affect current sessions.

## Open Questions

- OTP email body copy/branding — plain localized text for now; visual template can be refined later without changing specs or tasks.
