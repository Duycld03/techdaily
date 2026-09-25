# Tasks

## 1. Email delivery transport

- [x] 1.1 Add the MailKit package (pinned version) to `TechDaily.Infrastructure.csproj` and verify `dotnet restore` + `dotnet build` succeed.
- [x] 1.2 Add `IEmailSender` (Application/Interfaces) and `SmtpEmailSender` (Infrastructure/Services) reading `Email:Smtp:*` (Host, Port, From, UseStartTls, Username, Password); register in `DependencyInjection.cs`. Add non-secret `Email` placeholders to `appsettings.json` and document the required secret keys in `.env`/`appsettings.Local.json` (never committed). Verify build succeeds and the sender composes a message to the configured host.
- [x] 1.3 Make `SmtpEmailSender` throw a clear error (not a silent success) when SMTP credentials are unconfigured, and ensure no credential value is logged. Verify with a unit test asserting an unconfigured sender throws and that logs contain no password.

## 2. OTP domain & persistence

- [x] 2.1 Add `OtpPurpose` enum (`EmailVerification`, `PasswordReset`) and `EmailOtp` entity (`Email`, `Purpose`, `CodeHash`, `ExpiresAt`, `ConsumedAt`, `AttemptCount`, `CreatedAt`, and nullable `PendingName`/`PendingPasswordHash`/`PendingLocale`); add `DbSet` and EF configuration with an index on `(Email, Purpose)`. Verify `dotnet build` succeeds.
- [x] 2.2 Add EF migration `AddEmailOtp`, run `dotnet ef database update`, and verify the table + index exist via `psql` against the dev database.
- [x] 2.3 Implement an OTP service that issues (6-digit CSPRNG code, SHA-256 hash at rest, 10-minute expiry), validates (single-use consume, max 5 attempts then invalidate), and enforces the 60-second resend cooldown per `(email, purpose)`. Verify unit tests cover expiry (`AUTH_OTP_EXPIRED`), five-attempt invalidation (`AUTH_OTP_MAX_ATTEMPTS`), cooldown (`AUTH_OTP_RESEND_COOLDOWN`), and consumed-code reuse (`AUTH_OTP_INVALID`).

## 3. Global session revocation

- [x] 3.1 Add `IRefreshTokenService.RevokeAllForUserAsync(Guid userId, CancellationToken)` and its implementation setting `RevokedAt` on all unrevoked tokens across every family for the user. Verify a test proving that after the call, `POST /api/v1/auth/refresh` for any of that user's prior families returns `HTTP 401`.

## 4. Auth endpoints

- [x] 4.1 Replace `POST /api/v1/auth/register` with the step-1 request (validate, `AUTH_EMAIL_EXISTS` on duplicate, store pending registration with PBKDF2 hash, email OTP, return **no** session) and add `POST /api/v1/auth/register/verify` (create user + provision + issue tokens → `AuthSessionResponse`). Verify integration tests: step-1 returns no session; verify creates the user and session; no `User` row exists until a valid code is submitted; duplicate email returns `AUTH_EMAIL_EXISTS`; wrong code increments attempts and creates no account.
- [x] 4.2 Add `POST /api/v1/auth/forgot-password` (always `200`; issue reset OTP only if the account exists) and `POST /api/v1/auth/reset-password` (validate OTP, set password hash, call `RevokeAllForUserAsync`). Verify integration tests: unknown email returns `200` and sends nothing; valid reset updates the hash and revokes all families; a Google-linked account with `hasPassword:false` gains a password (`hasPassword:true` afterward); invalid/expired code is rejected without changing the password.
- [x] 4.3 Add `POST /api/v1/auth/otp/resend` (`{email, purpose}`, subject to cooldown). Verify a test that a resend within 60 seconds returns `AUTH_OTP_RESEND_COOLDOWN`.
- [x] 4.4 Register the new OTP error codes in the error catalog and add an `OtpEndpointsPolicy` rate limiter (partitioned by normalized email + IP), applied to all OTP endpoints; generalize the global `OnRejected` detail text so the 429 body is accurate for non-AI endpoints. Verify a burst against an OTP endpoint returns `HTTP 429` with the corrected problem-details body.

## 5. Frontend two-step OTP UI

- [x] 5.1 Add `useAuthStore` methods for register-request, register-verify, forgot-password, reset-password, and resend, sending the exact documented payloads. Verify Vitest data-contract tests assert the request bodies and that register-request yields no authenticated session.
- [x] 5.2 Update `login.vue` to add a code-entry sub-step for register and forgot-password (reset also collects the new password), a resend cooldown timer, and `pendingEmail` state. Verify Vitest tests: empty/invalid inputs block submission, `isSubmitting`/disabled states toggle, and RFC 7807 error codes surface as toasts.
- [x] 5.3 Add i18n keys in `en.json` and `vi.json` for every new error code (`AUTH_OTP_*`) and new UI string. Verify both locales resolve all keys with no missing-key warnings.
- [x] 5.4 Visual gate (AGENTS Pillar 5): drive headless Chromium to capture Desktop (1440x900) and Mobile (390x844) screenshots of the register OTP step and the forgot/reset steps in both `en` and `vi`, and present the images as verification proof.

## 6. Integration verification

- [x] 6.1 Run the full backend suite (`dotnet test`) and the full frontend suite (`npm test`); verify both pass 100%.
- [x] 6.2 End-to-end smoke on the running stack with the real Gmail SMTP credentials: register a throwaway address → receive the code by email → verify → obtain a session; then run forgot-password → receive code → reset → confirm all prior sessions are revoked (refresh returns 401). Delete the throwaway user and OTP rows afterward and confirm zero residual data.
