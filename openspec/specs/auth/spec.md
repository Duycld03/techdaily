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

### Requirement: Email-Based Password Setup for Stranded Mobile Users
The system SHALL allow users on devices where Google OAuth cannot be used to request a secure password setup link or code via their registered Google email.

#### Scenario: Unauthenticated visitor requests password initialization on login page
- **WHEN** an unauthenticated visitor on `/login` submits their Google email via the password setup flow
- **THEN** the system verifies the email exists and generates a password setup instruction token.

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

#### Scenario: User attempts login with wrong password
- **WHEN** user submits invalid credentials to `POST /api/v1/auth/login`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }`

#### Scenario: User registers with an existing email
- **WHEN** user submits duplicate email to `POST /api/v1/auth/register`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_EMAIL_EXISTS", "error": "An account with this email already exists." }`

#### Scenario: Google token validation fails
- **WHEN** client sends an invalid Google ID token to `POST /api/v1/auth/google`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_GOOGLE_TOKEN_INVALID", "error": "Invalid Google token." }` without leaking internal exception details

#### Scenario: Database connection failure during Google login does not return AUTH_GOOGLE_TOKEN_INVALID
- **WHEN** client sends a valid Google ID token to `POST /api/v1/auth/google` but the database is unreachable or connection fails
- **THEN** the system does NOT return `AUTH_GOOGLE_TOKEN_INVALID`
- **AND** the failure is reported through standard exception and problem details handling.

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
The client-side route guard SHALL check token validity against expiration time before allowing access to protected routes or redirecting away from guest-only pages like `/login`. A raw unvalidated cookie string SHALL NOT be considered proof of active authentication.

#### Scenario: User visits /login with expired token
- **WHEN** visitor navigates to `/login` with an expired token cookie
- **THEN** middleware recognizes the token as expired, clears session, and allows the visitor to stay on `/login` without redirecting back to `/today`.

#### Scenario: Active logged-in user visits /login
- **WHEN** authenticated user with a valid non-expired token navigates to `/login`
- **THEN** middleware redirects to `/today`.

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

#### Scenario: User visits login page on desktop
- **WHEN** user navigates to `/login` on desktop
- **THEN** the authentication card is vertically and horizontally centered with clean glassmorphic elevation
- **AND** the submit button and inputs fit comfortably within the viewport without requiring full-page scrolling.

#### Scenario: User switches between login and registration modes
- **WHEN** user clicks the "Register" or "Login" tab on `/login`
- **THEN** the form inputs transition smoothly without sudden height jumping or layout stutter
- **AND** existing valid email input is preserved across modes.

### Requirement: Dev-Learning Studio Authentication Surface & Shell Isolation
The platform SHALL provide a dedicated, balanced authentication surface conforming to the **Dev-Learning Studio** visual language, isolating guest authentication flows from internal application navigation chrome.

1. **Application Shell Isolation**:
   - The global layout shell (`app.vue`) SHALL detect authentication routes (`/login`) and conditionally suppress the internal application sidebar (`AppSidebar.vue`).
   - The top header (`AppHeader.vue`) on `/login` SHALL render in a streamlined guest state, omitting internal navigation drawer triggers while retaining theme toggle, language switcher, and brand home link.

2. **Studio Auth 2-Column Archetype**:
   - On desktop viewports ($\ge 1024\text{px}$), the authentication view (`pages/login.vue`) SHALL render within a balanced container (`max-w-4xl` to `max-w-5xl`) organized into two complementary columns:
     - **Left Column (Platform Value Stage)**: Displays TechDaily's engineering brand identity, mission statement ("Master Senior Software Engineering Daily"), and 3 core learning pillars (Scenario Architecture Drills, SM-2 Spaced Mastery, Daily System Design Doses) with subtle studio badges.
     - **Right Column (Interactive Auth Card)**: Houses the `.glass-panel` container (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `rounded-3xl`, `shadow-2xl`) containing mode switching tabs, input forms, and actions.
   - On mobile and tablet viewports ($< 1024\text{px}$), the layout SHALL gracefully stack, placing a compact brand header above the authentication card.

3. **Responsive 2-Column Registration Form**:
   - When in Register mode (`authMode === 'register'`), input fields SHALL arrange into a responsive 2-column grid on screens $\ge 640\text{px}$ (`grid sm:grid-cols-2 gap-3.5`):
     - Field 1: Full Name (`name`)
     - Field 2: Email Address (`email`)
     - Field 3: Password (`password`) with visibility toggle
     - Field 4: Confirm Password (`confirmPassword`) with visibility toggle
   - The 2-column layout SHALL reduce vertical card height by at least 40%, ensuring all form controls and action buttons remain visible above the fold on standard $1080\text{p}$ monitors.
   - On mobile screens ($< 640\text{px}$), fields SHALL stack vertically with standard $44\text{px}$ touch targets.

4. **Divider Layout Stability**:
   - The third-party OAuth divider SHALL use an absolute centering architecture (`absolute inset-0 flex items-center` with a relative centered text pill), eliminating flexbox dimension blowout and guaranteeing centered text alignment across all viewports.

#### Scenario: Guest user arrives at login on desktop monitor
- **WHEN** an unauthenticated user navigates to `/login` on a 1920x1080 display
- **THEN** the internal navigation sidebar (`AppSidebar`) is hidden
- **AND** the authentication surface displays the 2-column Studio Auth layout with the brand value stage on the left and the interactive card on the right
- **AND** the entire authentication card fits cleanly within the viewport without requiring page scrolling.

#### Scenario: User switches to Register mode on desktop
- **WHEN** the user clicks the "Register" tab on a desktop viewport
- **THEN** the form expands into a 2-column grid displaying Name and Email on row 1, and Password and Confirm Password on row 2
- **AND** the card width remains stable (`max-w-4xl` to `max-w-5xl`) with zero layout shift or width blowout.

#### Scenario: Mobile viewport responsiveness
- **WHEN** the user views `/login` on a mobile screen (390px width)
- **THEN** the layout stacks vertically with a compact brand header
- **AND** all input fields span 100% width with touch targets of at least 44px.
