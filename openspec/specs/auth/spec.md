# Auth Specification

## Purpose
Provides hybrid authentication, Google OAuth account linkage, and password initialization workflows enabling users on mobile devices or alternative environments to seamlessly authenticate with email and password.

## Requirements

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts (`isGoogleLinked: true`) to establish or update an email login password without providing an existing password (`currentPassword`).

For accounts linked to Google OAuth (`!string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint (`PUT /api/v1/user/change-password`) SHALL NOT require `currentPassword`, regardless of whether `hasPassword` is `false` or `true`.

For standard accounts without Google OAuth linkage (`string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint SHALL strictly require and verify `currentPassword` whenever `hasPassword` is `true`.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 8) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2-HMAC-SHA256 (600,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

#### Scenario: User with Google account updates existing password without current password
- **WHEN** a user with `isGoogleLinked: true` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with a valid `newPassword` (length >= 8) and null or omitted `currentPassword`
- **THEN** the system updates `user.PasswordHash` with the new hashed password and returns `200 OK`.

#### Scenario: Standard user without Google account must provide current password
- **WHEN** a user with `isGoogleLinked: false` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with null, empty, or incorrect `currentPassword`
- **THEN** the system rejects the request with `HTTP 400 Bad Request` and error code `USER_CURRENT_PASSWORD_INCORRECT`.

---

### Requirement: In-App Mobile Handoff Guidance
The system SHALL provide contextual guidance to Google OAuth users prompting them to set or update their email login password on the Profile Security tab without blocking them with a current password requirement.

The "Current Password" input field SHALL ONLY be displayed and required for standard accounts (`!isGoogleLinked && hasPassword`). For Google-linked accounts (`isGoogleLinked: true`), the "Current Password" field SHALL be omitted regardless of `hasPassword` state.

#### Scenario: User visits Profile Security tab without a password
- **WHEN** user navigates to `/profile` and selects the "Security & Password" tab with `hasPassword: false`
- **THEN** the UI displays an informative alert highlighting that setting a password allows logging in with email and password on mobile or other devices.
- **THEN** the UI hides the "Current Password" input field and changes the submit action button to "Set Password" (Thiết Lập Mật Khẩu).

#### Scenario: Google user logs in on desktop for first time
- **WHEN** user signs in via Google OAuth on desktop
- **THEN** a non-intrusive banner appears suggesting the user create a password for easy access on mobile devices.

#### Scenario: Google user visits Profile Security tab with existing password
- **WHEN** a user with `isGoogleLinked: true` and `hasPassword: true` navigates to `/profile` and selects the "Security" tab
- **THEN** the UI hides the "Current Password" input field
- **AND** the Google banner informs the user that their account is linked to Google and their email password can be updated directly without entering their current password
- **AND** the submit action button displays "Update Password" (`profile.update_password_btn`).

### Requirement: OTP-Verified Email Registration
Registration SHALL prove control of the email address before any account is created. No `User` row SHALL exist for an unverified email.

The flow has two steps:
1. **Request**: `POST /api/v1/auth/register` accepts name, email, and password (length >= 8). If an account with that email already exists, the system SHALL reject with `AUTH_EMAIL_EXISTS`. Otherwise the system SHALL hold the pending registration (email, name, password hash, locale) in transient server-side storage, issue a registration OTP (see the OTP policy requirement), and email it. The response SHALL NOT include an authenticated session.
2. **Confirm**: `POST /api/v1/auth/register/verify` accepts the email and the OTP. On a valid, unexpired, unconsumed OTP with attempts remaining, the system SHALL create the `User` from the held pending registration, run first-time provisioning, issue access and refresh tokens, and return an authenticated session identical to a successful login.

Pending registrations SHALL expire with their OTP and SHALL NEVER surface as usable accounts.

#### Scenario: New email registration requests a verification code
- **WHEN** a visitor submits a unique email, name, and valid password to `POST /api/v1/auth/register`
- **THEN** the system stores the pending registration, emails a 6-digit code, and responds without an authenticated session

#### Scenario: Registration with an existing email is rejected clearly
- **WHEN** a visitor submits an email that already has an account to `POST /api/v1/auth/register`
- **THEN** the system responds with error code `AUTH_EMAIL_EXISTS` and sends no verification email

#### Scenario: Verifying the code creates the account and signs in
- **WHEN** the visitor submits the correct OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system creates the user, provisions their starter content, and returns an access token plus a refresh-token cookie

#### Scenario: No account exists until the code is verified
- **WHEN** a visitor requests a registration OTP but never submits a valid code
- **THEN** no `User` row is created and the pending registration expires with the OTP

#### Scenario: Verification with a wrong code is rejected and counts an attempt
- **WHEN** the visitor submits an incorrect OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system rejects it with an OTP error code, increments the attempt counter, and creates no account

### Requirement: One-Time Passcode Issuance and Validation Policy
Email one-time passcodes (OTPs) used for registration and password reset SHALL follow a single issuance and validation policy:
- The code SHALL be a 6-digit numeric value generated with a cryptographically secure random source.
- The code SHALL expire 10 minutes after issuance.
- The code SHALL be stored only as a cryptographic hash (never plaintext) and compared by hash.
- A code SHALL be single-use: once a verification succeeds it SHALL be marked consumed and SHALL NOT verify again.
- The system SHALL allow at most 5 incorrect verification attempts per issued code; on the 5th failure the code SHALL be invalidated and a new request required.
- Resending a code SHALL be rate-limited by a 60-second cooldown per email and purpose; a request inside the cooldown SHALL be rejected with `AUTH_OTP_RESEND_COOLDOWN`.
- OTP request and verification endpoints SHALL be rate-limited to bound abuse.

#### Scenario: Issued code expires after ten minutes
- **WHEN** a code is issued and 10 minutes elapse before it is submitted
- **THEN** verification fails with `AUTH_OTP_EXPIRED`

#### Scenario: Code is invalidated after five wrong attempts
- **WHEN** an incorrect code is submitted 5 times for the same issued OTP
- **THEN** the OTP is invalidated and further submissions fail with `AUTH_OTP_MAX_ATTEMPTS` until a new code is requested

#### Scenario: Resend within cooldown is rejected
- **WHEN** a client requests a new code within 60 seconds of the previous request for the same email and purpose
- **THEN** the system rejects the request with `AUTH_OTP_RESEND_COOLDOWN`

#### Scenario: Consumed code cannot be reused
- **WHEN** a code that already completed a successful verification is submitted again
- **THEN** verification fails with `AUTH_OTP_INVALID`

### Requirement: Email-Based Password Setup for Stranded Mobile Users
The system SHALL let a user reset or establish an email/password credential by proving control of their registered email through a one-time passcode (OTP), without requiring an existing password. This covers both standard accounts that forgot their password and Google-linked accounts that have never set one.

The flow has two steps:
1. **Request**: `POST /api/v1/auth/forgot-password` accepts an email. The system SHALL respond `200 OK` regardless of whether the email is registered (anti-enumeration). When the email maps to an existing account, the system SHALL issue a password-reset OTP (see the OTP policy requirement) and email it to that address.
2. **Confirm**: `POST /api/v1/auth/reset-password` accepts the email, the OTP, and a new password (length >= 8). On a valid, unexpired, unconsumed OTP with attempts remaining, the system SHALL set the account's password hash, mark the OTP consumed, and revoke all of the user's refresh-token families (see `refresh-tokens`).

After a successful reset every session is invalidated: an email/password user SHALL re-authenticate manually with the new password, while a Google-linked user MAY re-authenticate via Google OAuth without a manual password login.

#### Scenario: Unauthenticated visitor requests password initialization on login page
- **WHEN** an unauthenticated visitor on `/login` submits their email via the forgot-password flow
- **THEN** the system responds `200 OK` and, if the email is registered, emails a 6-digit password-reset OTP

#### Scenario: Forgot-password request for an unknown email does not reveal existence
- **WHEN** a visitor submits an email that is not registered to `POST /api/v1/auth/forgot-password`
- **THEN** the system responds `200 OK` with no indication of whether the account exists and sends no email

#### Scenario: User completes a password reset with a valid code
- **WHEN** a user submits their email, the correct OTP, and a new password (length >= 8) to `POST /api/v1/auth/reset-password`
- **THEN** the system updates the password hash, marks the OTP consumed, and revokes all of the user's refresh-token families
- **AND** the response requires the user to sign in again with the new password

#### Scenario: Google-linked account without a password sets one via reset
- **WHEN** a Google-linked user with `hasPassword: false` completes the forgot-password OTP flow with a new password
- **THEN** the system establishes the password hash and subsequent profile requests return `hasPassword: true`
- **AND** the user MAY continue to sign in with Google without a manual password login

#### Scenario: Reset attempt with an invalid or expired code is rejected
- **WHEN** a user submits an incorrect, expired, or already-consumed OTP to `POST /api/v1/auth/reset-password`
- **THEN** the system rejects the request with the corresponding OTP error code and does not change the password

### Requirement: Machine-Readable Authentication Error Codes
The authentication and user account management endpoints (`/api/v1/auth/*` and `/api/v1/user/*`) SHALL return standardized error codes for all credential and validation failures:
- `AUTH_EMAIL_PASSWORD_REQUIRED`: Email or password omitted.
- `AUTH_PASSWORD_TOO_SHORT`: Password length less than 8 characters.
- `AUTH_EMAIL_EXISTS`: Registered account with email already present.
- `AUTH_INVALID_CREDENTIALS`: Email not found or password verification failed.
- `AUTH_GOOGLE_TOKEN_INVALID`: Google ID token invalid, expired, or signature/audience verification failed. This error code SHALL strictly represent external token validation failures and SHALL NOT be returned for internal database connection or user provisioning exceptions.
- `AUTH_GOOGLE_NOT_CONFIGURED`: Google Client ID unconfigured on server.
- `USER_CURRENT_PASSWORD_INCORRECT`: Supplied current password failed verification.
- `USER_NEW_PASSWORD_TOO_SHORT`: New password length less than 8 characters.
- `AUTH_TOKEN_REUSE_DETECTED`: A previously-rotated refresh token was reused (potential theft).
- `AUTH_REFRESH_TOKEN_EXPIRED`: The refresh token has expired.
- `AUTH_OTP_INVALID`: Submitted one-time passcode does not match an active issued code.
- `AUTH_OTP_EXPIRED`: The one-time passcode passed its expiry window.
- `AUTH_OTP_MAX_ATTEMPTS`: Too many incorrect passcode attempts; the code is invalidated and a new one must be requested.
- `AUTH_OTP_RESEND_COOLDOWN`: A new passcode was requested before the resend cooldown elapsed.

Every returned error code SHALL have a corresponding localized message in the system i18n catalog (`en` and `vi`).

#### Scenario: User attempts login with wrong password
- **WHEN** user submits invalid credentials to `POST /api/v1/auth/login`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }`

#### Scenario: User registers with an existing email
- **WHEN** user submits duplicate email to `POST /api/v1/auth/register`
- **THEN** server returns HTTP 409 with `{ "code": "AUTH_EMAIL_EXISTS", "error": "An account with this email already exists." }`

#### Scenario: Google token validation fails
- **WHEN** client sends an invalid Google ID token to `POST /api/v1/auth/google`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_GOOGLE_TOKEN_INVALID", "error": "Invalid Google token." }` without leaking internal exception details

#### Scenario: Database connection failure during Google login does not return AUTH_GOOGLE_TOKEN_INVALID
- **WHEN** client sends a valid Google ID token to `POST /api/v1/auth/google` but the database is unreachable or connection fails
- **THEN** the system does NOT return `AUTH_GOOGLE_TOKEN_INVALID`
- **AND** the failure is reported through standard exception and problem details handling.

#### Scenario: OTP verification fails with an expired code
- **WHEN** a user submits an expired one-time passcode to an OTP verification endpoint
- **THEN** the server returns HTTP 400 with `{ "code": "AUTH_OTP_EXPIRED" }` and a localized message

### Requirement: Proactive JWT Expiration Validation
The client-side authentication store SHALL decode the JWT payload `exp` claim and consider tokens expired if `exp * 1000 <= Date.now()`. When initialized or checked, an expired token SHALL be purged from cookies and local storage immediately.

#### Scenario: User visits app with an expired token
- **WHEN** user loads the application after being away for longer than the token lifetime
- **THEN** `authStore.init()` detects that `exp` is in the past, purges `techdaily_token` and `techdaily_user`, and sets `isLoggedIn = false`.
- **THEN** user avatar and protected UI elements display the unauthenticated state rather than a ghost session.

#### Scenario: User evaluates login state with active token
- **WHEN** user has a valid token where `exp * 1000 > Date.now()`
- **THEN** `authStore.isLoggedIn` evaluates to `true`.

---

### Requirement: Global 401 Session Expiration Interception
The HTTP client composable (`useApiClient`) SHALL intercept any response with HTTP status `401 Unauthorized`. It SHALL purge local session credentials, show a localized notification indicating that the session has expired, and redirect the user to `/login` preserving the current route as the redirect parameter.

#### Scenario: Authenticated request fails with 401
- **WHEN** an API call returns `HTTP 401 Unauthorized`
- **THEN** client executes session cleanup via `authStore.logout()` (or internal session reset).
- **THEN** client emits a warning notification: "Your session has expired. Please sign in again."
- **THEN** client navigates to `/login?redirect=<currentUrl>`.

---

### Requirement: Extended Access Token Lifespan Matching Curriculum
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`) SHALL issue JWT access tokens with a 60-minute lifetime. Long-lived session continuity SHALL be provided by refresh tokens (see `refresh-tokens` capability) rather than long-lived access tokens.

#### Scenario: User authenticates successfully
- **WHEN** user signs in via email/password or Google OAuth
- **THEN** the returned JWT access token has an expiration set to 60 minutes from creation time

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard (`frontend/middleware/auth.global.ts`) SHALL enforce a strict **Default-Deny** security posture: all platform routes require active authentication by default, with only explicit guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local development playgrounds (`/playground/*`, `/showcase`) exempted.

1. **Unauthenticated Access Redirection**:
   - When an unauthenticated visitor attempts to navigate to any protected route (including `/`, `/today`, `/library`, `/quiz`, `/review`, `/roadmap`, `/settings`, `/profile`, `/insights`, `/notes`), the middleware SHALL immediately redirect the visitor to `/login`.
   - The middleware SHALL preserve the attempted destination in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
   - On server-side rendering (SSR), if no authentication token is present, the server SHALL issue an immediate redirect to `/login` to prevent content flashing.

2. **Authenticated Access Redirection**:
   - When an authenticated user with a valid, non-expired token navigates to `/login`, the middleware SHALL redirect the user to the target specified by the `redirect` query parameter, or to `/today` if no redirect parameter is present.
   - If a visitor arrives at `/login` with an expired token, the middleware SHALL purge credentials and allow the visitor to stay on `/login`.

#### Scenario: User visits /login with expired token
- **WHEN** visitor navigates to `/login` with an expired token cookie
- **THEN** middleware recognizes the token as expired, clears session, and allows the visitor to stay on `/login` without redirecting back to `/today`.

#### Scenario: Active logged-in user visits /login
- **WHEN** authenticated user with a valid non-expired token navigates to `/login`
- **THEN** middleware redirects to `/today`.

#### Scenario: Unauthenticated visitor attempts to access protected platform route
- **WHEN** an unauthenticated visitor navigates directly to `/library` or `/quiz`
- **THEN** the route middleware intercepts navigation and redirects to `/login?redirect=%2Flibrary` (or respective path)
- **AND** zero protected content or layout chrome is rendered to the visitor.

#### Scenario: Unauthenticated visitor arrives at root URL
- **WHEN** an unauthenticated visitor navigates to `/`
- **THEN** the route middleware redirects to `/login`
- **AND** the executive dashboard is completely inaccessible until authentication completes.
### Requirement: Mandatory secret configuration at startup
The application SHALL fail fast during startup if the `Jwt:Secret` configuration value is missing, empty, or has fewer than 256 bits of cryptographically random entropy. The `Jwt:Secret` value MUST be generated using a CSPRNG; human-readable, example, or default secrets SHALL NOT be used. The application SHALL NOT fall back to any hardcoded default JWT signing key. The same fail-fast behavior SHALL apply to VAPID key configuration (`WebPush:PrivateKey`, `WebPush:PublicKey`).

#### Scenario: Application starts without JWT secret
- **WHEN** the application starts with `Jwt:Secret` unset or empty
- **THEN** the application throws an exception during startup and does not begin accepting HTTP requests

#### Scenario: Application starts with JWT secret configured
- **WHEN** the application starts with `Jwt:Secret` set to a CSPRNG-generated value with at least 256 bits of entropy
- **THEN** the application starts normally and uses the configured secret for JWT signing

### Requirement: Production CORS origin isolation
The CORS policy SHALL load allowed origins exclusively from configuration (`Cors:AllowedOrigins`). The policy SHALL NOT include localhost or loopback origins unless explicitly listed in the configuration for that environment.

#### Scenario: Production deployment with configured origins
- **WHEN** the application runs with `Cors:AllowedOrigins` set to `["https://techdaily.duckdns.org"]`
- **THEN** CORS preflight and actual requests from `http://localhost:3000` are rejected

#### Scenario: Development with localhost origins
- **WHEN** the application runs with `Cors:AllowedOrigins` including localhost entries
- **THEN** requests from those localhost origins are permitted

### Requirement: Google Identity Services Client Configuration & Fallback
The web frontend application SHALL resolve the Google Identity Services Client ID from runtime configuration using flexible environment variable resolution, supporting either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID`.

The system SHALL guarantee that:
1. When either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID` is set in the hosting environment (including local development runner `.env`), the frontend initializes Google Identity Services (`google.accounts.id.initialize`) with the resolved non-empty Client ID.
2. In local development environments executed via `run-dev.sh`, environment variables from `.env` are automatically propagated to the frontend development server process.
3. If neither variable is configured (empty string), the application SHALL NOT initialize Google Identity Services with an empty Client ID, preventing `400: invalid_request (Missing required parameter: client_id)` authorization errors.

#### Scenario: Local development environment with GOOGLE_CLIENT_ID defined in .env
- **WHEN** a developer starts the development stack with `run-dev.sh` and root `.env` defines `GOOGLE_CLIENT_ID`
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `GOOGLE_CLIENT_ID`
- **AND** the Google Sign-In button initializes with the configured Client ID without `client_id` missing parameter errors.

#### Scenario: Production or CI deployment with NUXT_PUBLIC_GOOGLE_CLIENT_ID defined
- **WHEN** the application runs in a container or environment where `NUXT_PUBLIC_GOOGLE_CLIENT_ID` is defined
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `NUXT_PUBLIC_GOOGLE_CLIENT_ID`.

#### Scenario: Environment without Google OAuth credentials configured
- **WHEN** neither `NUXT_PUBLIC_GOOGLE_CLIENT_ID` nor `GOOGLE_CLIENT_ID` is defined
- **THEN** the frontend recognizes the missing Client ID, avoids passing an empty string to `google.accounts.id.initialize`, and does not render a broken Google Sign-In authorization flow.

### Requirement: Responsive Viewport-Bounded Authentication Card
The authentication surface at `/login` SHALL provide a responsive, viewport-bounded authentication container adhering to the Dev-Learning Studio design system:

1. **Card Geometry & Centering**:
   - The authentication card SHALL be centered horizontally and vertically within the viewport (`min-h-[calc(100dvh-4rem)] flex items-center justify-center p-4`).
   - The card width SHALL be constrained to `w-full max-w-md` with refined glassmorphic styling (`dark:bg-canvas-subtle/80 backdrop-blur-xl border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-3xl`).
2. **Mode Switcher Zero-Shift Stability**:
   - Toggling between Login (`authMode === 'login'`) and Registration (`authMode === 'register'`) SHALL execute smoothly without vertical jumping, layout displacement, or browser scrollbar popping.
   - Mode switcher tabs SHALL utilize pill button styling with high-contrast active highlights (`bg-brand-600 text-white` / `bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300`).
3. **Responsive Input Groups & Touch Targets**:
   - Form inputs (Email, Password, Name) SHALL enforce minimum 44px touch heights (`py-2.5 sm:py-3`), comfortable leading icon clearance, and high-visibility focus rings (`focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`).
   - Form submit buttons and third-party Google OAuth buttons SHALL span 100% width with clear loading indicators during async credentials verification.
4. **Consistent Password Visibility Toggle Focus Styling**:
   - Password and Confirm Password visibility toggle buttons SHALL maintain consistent vertically centered positioning (`top-1/2 -translate-y-1/2 right-3.5`).
   - When navigated via keyboard (Tab), the focus indicator on password visibility toggle buttons SHALL render as a bounded, compact focus ring (`rounded-md focus-visible:ring-2 focus-visible:ring-brand-500`) that does not stretch across the container height or clip outside the input's rounded container.

#### Scenario: User visits login page on desktop
- **WHEN** user navigates to `/login` on desktop
- **THEN** the authentication card is vertically and horizontally centered with clean glassmorphic elevation
- **AND** the submit button and inputs fit comfortably within the viewport without requiring full-page scrolling.

#### Scenario: User switches between login and registration modes
- **WHEN** user clicks the "Register" or "Login" tab on `/login`
- **THEN** the form inputs transition smoothly without sudden height jumping or layout stutter
- **AND** existing valid email input is preserved across modes.

#### Scenario: User tabs through password field to visibility toggle
- **WHEN** user focuses the password input on `/login` and presses the Tab key to navigate to the visibility toggle button
- **THEN** the password visibility toggle button displays a clean, compact focus ring centered within the input field
- **AND** the focus ring does not stretch vertically to the top and bottom borders of the input container
- **AND** the focus ring does not produce sharp rectangular corner clipping outside the input container's rounded border.

#### Scenario: User toggles password visibility via keyboard
- **WHEN** the password visibility toggle button is focused via keyboard navigation and the user presses Enter or Space
- **THEN** the password input switches between masked (`password`) and plaintext (`text`) modes
- **AND** the `:aria-label` updates dynamically to reflect the current state (`Show password` or `Hide password`).

### Requirement: Dev-Learning Studio Authentication Surface & Shell Isolation
The platform SHALL provide a dedicated, full-screen **Studio Auth Cockpit** conforming to the Dev-Learning Studio visual language, completely isolated from internal application navigation chrome.

1. **Application Shell Isolation & Full-Screen Frame**:
   - The global layout shell (`app.vue`) SHALL detect authentication routes (`isAuthPage = computed(() => route.path === '/login')`) and completely suppress both `AppHeader.vue` and `AppSidebar.vue`.
   - The authentication surface SHALL occupy the entire viewport (`min-h-screen w-screen overflow-hidden`) with the dark obsidian background canvas (`bg-slate-50 dark:bg-canvas`).
   - The page frame SHALL feature:
     - **Top System Telemetry Bar**: System identity badge (`TECHDAILY::IDE v2.5.0-sys`), live ping latency status indicator (`● PING 18ms`), single-locale language selector (`EN | VI`), and theme toggle button (`ThemeToggle.vue`).
     - **Ambient Status Sub-Header**: Live operational status ticker (`● ALL SERVICES OPERATIONAL  LATENCY 14MS`) and the platform invariant (`⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`).
     - **Bottom Compliance Telemetry Bar**: Security framework and node compliance indicators (`TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT`, `DISTRIBUTED COCKPIT // SECURE_AUTH_NODE`, `TLS 1.3 AES-256-GCM`).

2. **Left Column (Curriculum & Telemetry Showcase Stage)**:
   - On desktop viewports ($\ge 1024\text{px}$), the left stage SHALL display:
     - Track header badge: `STAFF+ TRACK v2.4-DRILL`.
     - Platform headline and localized engineering mission statement.
     - Live learning telemetry: `SESSION INTERVAL TARGET 99.4% HIT` progress gauge and `SM-2 SPACED DECAY 24.8 Hrs` indicator.
     - Interactive code showcase card (`CONSENSUS_PROMISE.TS`) rendering syntax-highlighted monospace promise code (`await quorum.commit(...)`) with lock status.

3. **Right Column (Interactive Cockpit Auth Card)**:
   - Houses the `.glass-panel` container (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `rounded-3xl`, `shadow-2xl`) containing mode switching tabs, input forms, and actions.
   - **Strict Single-Language i18n Standard**: All UI text, tabs, labels, badges, and action buttons SHALL render exclusively in the active locale (`en` or `vi`) via `@nuxtjs/i18n`. Bilingual slash concatenation (e.g. `Đăng nhập / Sign In` or `DEV HANDLE / WORK EMAIL`) is strictly prohibited.
   - **Mode Switcher Tabs**: Segmented control switching between `login`, `register`, and `forgot-password` with zero layout shift.
   - **OAuth Providers**: GitHub and Google sign-in buttons with monospace keyboard shortcut badges (`G`, `⌘L`).
   - **Form Fields**: Monospace telemetry labels (`DEV HANDLE / WORK EMAIL`, `SECRET TOKEN / KEY`), clear localized placeholders, password visibility eye toggle, and inline "Forgot password?" trigger.
   - **Session Persistence**: Remember session checkbox (`30 days`).
   - **Primary Action Button**: Localized Iris Violet submit button (`bg-brand-600 hover:bg-brand-500 text-white font-semibold`) with `↵ RETURN` shortcut badge and loading spinner.
   - **Card Footer**: Terms and Privacy navigation links.

4. **Multi-Mode Authentication State Machine**:
   - In `login` mode, presents Email, Password, Forgot Password trigger, Remember Session checkbox, Submit button, and OAuth options.
   - In `register` mode, presents a 2-column input grid (`grid sm:grid-cols-2 gap-3.5`) for Full Name, Email, Password, and Confirm Password, reducing vertical elongation by over 40%.
   - In `forgot-password` mode, presents Email input, Reset button, and a Back to Login navigation trigger.
   - Transitions between modes SHALL execute smoothly without vertical jumping or scrollbar popping.

5. **Divider Layout Stability**:
   - The third-party OAuth divider SHALL use an absolute centering architecture (`absolute inset-0 flex items-center` with a relative centered text pill), eliminating flexbox dimension blowout and guaranteeing centered text alignment across all viewports.

#### Scenario: Guest user arrives at login on desktop monitor
- **WHEN** an unauthenticated user navigates to `/login` on a 1920x1080 display
- **THEN** the internal navigation sidebar (`AppSidebar`) and global header (`AppHeader`) are hidden
- **AND** the authentication surface displays the 2-column Studio Cockpit layout with top/bottom telemetry frames, curriculum branding on the left, and the interactive auth card on the right
- **AND** the entire authentication card fits cleanly within the viewport without requiring page scrolling.

#### Scenario: User switches to Register mode on desktop
- **WHEN** the user clicks the "Register" tab on a desktop viewport
- **THEN** the form expands into a 2-column grid displaying Name and Email on row 1, and Password and Confirm Password on row 2
- **AND** the card width remains stable (`max-w-4xl` to `max-w-5xl`) with zero layout shift or width blowout.

#### Scenario: Mobile viewport responsiveness
- **WHEN** the user views `/login` on a mobile screen (390px width)
- **THEN** the layout stacks vertically with a compact brand header
- **AND** all input fields span 100% width with touch targets of at least 44px.

#### Scenario: Guest switches to forgot password mode
- **WHEN** the user clicks "Forgot password?" on `/login`
- **THEN** the card transitions to the forgot-password form displaying an email input and "Send Reset Link" button
- **AND** a "Back to Sign In" button allows returning to the login form without reloading the page.

#### Scenario: Strict single-language display without bilingual slash text
- **WHEN** user views `/login` with active locale set to Vietnamese (`vi`)
- **THEN** all tabs, labels, placeholders, and buttons display solely Vietnamese text (e.g. `Đăng nhập`, `Đăng ký`, `Email công việc`, `Mật khẩu`, `Vào Cockpit Luyện Tập`)
- **AND** zero dual-language slash strings (such as `Đăng nhập / Sign In`) are rendered
- **WHEN** user switches the locale to English (`en`)
- **THEN** all elements update immediately to English (e.g. `Sign In`, `Register`, `Dev Handle / Work Email`, `Secret Token / Key`, `Enter Practice Cockpit`).

#### Scenario: Full-screen Studio Cockpit telemetry and code showcase rendering
- **WHEN** user loads `/login` on a desktop viewport
- **THEN** the top system bar displays `TECHDAILY::IDE` and ping latency
- **AND** the left stage renders the `CONSENSUS_PROMISE.TS` code block with monospace syntax highlighting and SM-2 spaced decay metrics
- **AND** the bottom frame displays security compliance notices (`SOC2 TYPE II & RFC-7519 JWT`).

---

### Requirement: Studio Cockpit Account Recovery & 3-Tab Segmented Mode Switcher
The interactive authentication cockpit card SHALL provide a unified 3-tab segmented control supporting direct switching between Sign In, Register, and Account Recovery (`forgot-password` mode).

1. **Segmented Mode Switcher Architecture**:
   - The top tab strip SHALL display three segmented options: `Sign In` (`auth.sign_in_tab`), `Register` (`auth.register_tab`), and a compact recovery action tab with a key/lock-reset icon (`auth.recovery_tab_title` with `KeyRound` / `RotateCcw` icon).
   - Clicking the recovery tab SHALL activate `authMode = 'forgot-password'` with zero layout shift or vertical jumping.
   - The active tab state SHALL be highlighted with high-contrast studio styling (`bg-slate-200 dark:bg-canvas-subtle text-brand-600 dark:text-brand-400 font-bold`).

2. **Dedicated Account Recovery Surface**:
   - In `forgot-password` mode, the card SHALL display a dedicated recovery header: "Recover Cockpit Access" (`auth.recover_cockpit_title`) and helper note explaining that a magic link valid for 15 minutes will be dispatched (`auth.recover_cockpit_subtitle`).
   - The form SHALL provide a single monospace-labeled email input field (`ACCOUNT REGISTRATION EMAIL` / `auth.account_email_label`) with a leading mail icon and HTML5 email validation.
   - The card SHALL present a contextual advisory notice with an informational icon informing engineers that accounts configured with GitHub OAuth or Hardware Security Keys can authenticate directly without resetting passwords (`auth.oauth_bypass_notice`).
   - The primary action button SHALL display "Send Recovery Magic Link" (`auth.send_recovery_link_btn`) with a send icon and keyboard return hint (`↵ RETURN`).
   - A dedicated secondary navigation action "Back to Sign In" (`auth.back_to_signin_btn`) SHALL restore the `authMode = 'login'` state.

3. **Cockpit Footer & Security Attestation**:
   - The card footer SHALL display an attestation badge: `ZERO-KNOWLEDGE AUTH` (`auth.zero_knowledge_badge`) with an emerald security lock icon alongside standard Terms and Privacy policy navigation links.
   - All text content SHALL be 100% localized through single-language translation keys in `en.json` and `vi.json` without bilingual slash combinations.

#### Scenario: User clicks recovery tab in segmented header
- **WHEN** user clicks the recovery tab in the top segmented switcher
- **THEN** `authMode` transitions to `forgot-password`
- **AND** the card smoothly reveals the Account Recovery form without layout blowout.

#### Scenario: User submits email for password recovery
- **WHEN** user enters a valid email address and clicks "Send Recovery Magic Link"
- **THEN** the system simulates/invokes the password recovery dispatch and notifies the user with a localized success toast.

#### Scenario: User returns to Sign In from recovery view
- **WHEN** user clicks "Back to Sign In" or the "Sign In" tab
- **THEN** `authMode` reverts to `login` and preserves any entered email address.

---

### Requirement: Studio Auth Cockpit Canvas & Ambient Ambiance
The Studio Auth canvas at `/login` SHALL render an immersive developer cockpit atmosphere utilizing multi-layered ambient elements that eliminate empty black screen voids on wide viewports.

1. **Ambient Background Layers**:
   - The canvas SHALL display an engineering dot-matrix background pattern (`rgba(255, 255, 255, 0.07)`).
   - The canvas SHALL project a deep iris violet radial ambient glow behind the central content stage.
   - Subtle hairline guide lines SHALL delineate the upper and lower boundaries of the view.

2. **Responsive Split Canvas**:
   - On desktop viewports ($\ge 1024\text{px}$), the canvas SHALL display a balanced 12-column grid (`lg:grid-cols-12`) featuring the telemetry/learning stage on the left (`lg:col-span-6`) and the elevated auth cockpit card on the right (`lg:col-span-6`).
   - On mobile/tablet viewports ($< 1024\text{px}$), the canvas SHALL stack gracefully into a single-column layout without clipping or horizontal overflow.

#### Scenario: Visitor loads login page on widescreen display
- **WHEN** user navigates to `/login` on a 1920x1080 display
- **THEN** the view renders with the engineering dot-matrix grid and iris ambient glow
- **AND** the content is presented in a balanced two-column layout without unstyled black empty space.

---

### Requirement: Clean Studio Header & Language Controls
The top navigation header on the Studio Auth canvas SHALL provide a clean, distraction-free branding bar with language and theme switches.

1. **Brand Identity**:
   - The header SHALL display the TechDaily brand mark and title linking to `/login`.

2. **User Preferences**:
   - The right side of the header SHALL provide language selection (EN / VI) and color mode (Dark / Light) toggles.

#### Scenario: Visitor inspects header
- **WHEN** user loads `/login`
- **THEN** the top header displays the TechDaily brand emblem and title alongside language and theme toggle buttons.

---

### Requirement: Left Telemetry Stage with Metrics & Code Simulation
The left column of the Studio Auth canvas SHALL showcase TechDaily's technical reading curriculum and spaced repetition practice model through live telemetry gauges and simulated code execution.

1. **Platform Identifiers**:
   - The stage SHALL display the `● TECHDAILY | SM-2 ACTIVE RECALL` pill badge and `v2.4-SYS` version tag.
   - The headline SHALL state `Daily Technical Reading & Spaced Learning` with supporting curriculum description.

2. **Progress Metrics Card**:
   - The metrics card SHALL display `DAILY READING GOAL` with `94% COMPLETED` alongside a green gradient progress bar.
   - The card SHALL display `SM-2 SPACED REPETITION` with `ACTIVE RECALL` alongside an iris purple gradient progress bar.

3. **Code Simulation Window**:
   - The code block SHALL feature an editor title bar with three macOS-style window controls, title `>_ TECHDAILY_PRACTICE.TS`, and a lock emblem.
   - The code view SHALL render the syntax-highlighted `techDaily.getDailySlice` practice invocation snippet.

#### Scenario: Desktop visitor inspects learning preview
- **WHEN** user views `/login` on desktop
- **THEN** the left stage renders the progress gauges and the code window simulation.

---

### Requirement: Glitch-Free Full-Width Google Authentication Trigger
The 1-click Google OAuth button SHALL render as an integrated, full-width element conforming to the card's visual system, eliminating native iframe hover visual artifacts and logo bounding box overflow.

1. **Hover State Stability & Clean Geometry**:
   - The Google Sign-In button SHALL span 100% width of the card's inner content area (`w-full`).
   - The Google logo SHALL render with clean SVG geometry without any protruding white background corners ("dư 1 chút ở trên và dưới") when hovered or focused.
   - The button SHALL display localized copy (`Đăng nhập với Google` in VI, `Sign in with Google` in EN).

2. **Authentication Flow Continuity**:
   - Clicking the Google button SHALL trigger Google Identity Services (GSI) or initiate the Google OAuth sign-in flow.
   - In environments where Google Client ID is configured, credential responses SHALL be transmitted to `/api/v1/auth/google`.

#### Scenario: User hovers over Google Sign-In button
- **WHEN** user hovers over the Google Sign-In button
- **THEN** the background transitions smoothly to a hover shade
- **AND** the Google logo remains cleanly bounded with zero white box clipping or corner overflow.

#### Scenario: User clicks Google Sign-In button
- **WHEN** user clicks the Google Sign-In button
- **THEN** the system triggers Google Identity Services or opens the Google account selection prompt.

---

### Requirement: Distraction-Free Canvas Footer
The Studio Auth canvas SHALL maintain a clean layout without redundant bottom telemetry or compliance clutter, keeping the focus entirely on developer authentication and spaced learning preview.

#### Scenario: Visitor views canvas bottom
- **WHEN** user views `/login`
- **THEN** the view is clean and distraction-free without bottom telemetry bars or status spam.

---

### Requirement: Remember-Me Session Persistence
The `/login` "Remember session" checkbox SHALL control whether the authenticated session survives a browser restart, end-to-end across the client and server. The email/password login and registration requests SHALL carry a `rememberMe` boolean; when the field is omitted, the server SHALL treat it as `true` to preserve backward compatibility.

1. **Persistent session (checkbox checked, the default)**:
   - The frontend SHALL persist the access token and user in a 30-day `techdaily_token` / `techdaily_user` cookie and in `localStorage`.
   - The backend SHALL issue the `refreshToken` cookie as a persistent cookie with a 30-day `Expires`/`Max-Age` (see `refresh-tokens`).
   - The session SHALL remain valid after the browser is fully closed and reopened, subject to token expiry.

2. **Session-scoped session (checkbox unchecked)**:
   - The frontend SHALL persist the access token and user in a browser **session cookie** (no `Expires`/`Max-Age`) and in `sessionStorage`, and SHALL NOT write them to `localStorage` or a persistent cookie.
   - The backend SHALL issue the `refreshToken` cookie as a browser **session cookie** (no `Expires`/`Max-Age`).
   - The session SHALL be discarded when the browser is fully closed; a reopened browser SHALL present no credentials and land on the unauthenticated state.

3. **Session clearing**: Logout and 401 session purge SHALL clear the credentials from both `localStorage` and `sessionStorage` and from the token/user cookies, regardless of the persistence mode that created them.

4. **Google OAuth**: The "Remember session" checkbox is presented only in `login` mode and not for the Google button; Google OAuth login SHALL issue a persistent session.

The refresh token's server-side absolute expiry SHALL remain 30 days in both modes; only the browser cookie persistence differs (see `refresh-tokens`).

#### Scenario: Login with Remember session checked persists across restart
- **WHEN** a user signs in via email/password with the "Remember session" checkbox checked
- **THEN** the login request body includes `"rememberMe": true`
- **AND** the client stores `techdaily_token` / `techdaily_user` in a 30-day cookie and `localStorage`
- **AND** after fully closing and reopening the browser, `authStore.init()` restores the session and `isLoggedIn` evaluates to `true` (subject to token expiry).

#### Scenario: Login with Remember session unchecked ends on browser close
- **WHEN** a user signs in via email/password with the "Remember session" checkbox unchecked
- **THEN** the login request body includes `"rememberMe": false`
- **AND** the client stores the access token and user in a session cookie and `sessionStorage`, writing neither to `localStorage` nor to a persistent cookie
- **AND** after the browser session ends, no credentials remain and the client presents the unauthenticated state.

#### Scenario: Login request omits rememberMe
- **WHEN** a client submits `POST /api/v1/auth/login` or `/register` without a `rememberMe` field
- **THEN** the server treats the request as `rememberMe: true` and issues a persistent refresh token cookie.

#### Scenario: Google OAuth login is persistent regardless of the checkbox
- **WHEN** a user authenticates via the Google button on `/login`
- **THEN** the server issues a persistent refresh token cookie and the client persists the session for 30 days.
