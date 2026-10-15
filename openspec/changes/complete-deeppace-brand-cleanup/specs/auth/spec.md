# Auth Spec Delta

## MODIFIED Requirements

### Requirement: Proactive JWT Expiration Validation
The client-side authentication store SHALL decode the JWT payload `exp` claim and consider tokens expired if `exp * 1000 <= Date.now()`. When initialized or checked, an expired token SHALL be purged from cookies and local storage immediately.

#### Scenario: User visits app with an expired token
- **WHEN** user loads the application after being away for longer than the token lifetime
- **THEN** `authStore.init()` detects that `exp` is in the past, purges `deeppace_token` and `deeppace_user`, and sets `isLoggedIn = false`.
- **THEN** user avatar and protected UI elements display the unauthenticated state rather than a ghost session.

#### Scenario: User evaluates login state with active token
- **WHEN** user has a valid token where `exp * 1000 > Date.now()`
- **THEN** `authStore.isLoggedIn` evaluates to `true`.

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard (`frontend/app/middleware/auth.global.ts`) SHALL enforce a strict **Default-Deny** security posture: all platform routes require active authentication by default, with only explicit guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`, `/admin/login`) and local development playgrounds (`/playground/*`, `/showcase`) exempted.

The numbered rules and existing scenarios below govern learner routes. Admin routes SHALL follow the administrator-specific rules at the end of this requirement. `/admin/login` SHALL remain accessible to authenticated non-admin accounts and SHALL NOT use the learner guest-page redirect.

1. **Server-Side Rendering (SSR) Route Guard & Expiration Verification**:
   - On Server-Side Rendering (`import.meta.server`), when an incoming HTTP request targets any protected route (such as `/library`, `/today`, `/notes`), the middleware SHALL inspect request cookies for session credentials (`deeppace_token` and `refreshToken`).
   - If **neither** `deeppace_token` nor `refreshToken` cookie is present and the shared migration boundary reports no unresolved legacy cookie candidate, the SSR middleware SHALL immediately redirect the HTTP request to `/login` with `redirect={targetUrl}`.
   - If only legacy cookie evidence is present, SSR SHALL use the neutral admission boundary from `client-storage` until browser storage resolves canonical identity; it SHALL NOT authorize that legacy token or render protected markup. Normal canonical admission resumes after that boundary resolves.
   - If `deeppace_token` is present, the SSR middleware SHALL decode the JWT payload `exp` claim. If `exp * 1000 <= Date.now()` (the token has expired) and no `refreshToken` cookie is available to renew it, the SSR middleware SHALL immediately issue an HTTP 302 redirect to `/login` with `redirect={targetUrl}` before rendering the protected application shell (`<AppHeader>`, `<AppSidebar>`).
   - Under no circumstances SHALL the server render a protected application shell with user telemetry and then delegate expired token rejection to client-side hydration, which triggers DOM hydration mismatch collisions.

2. **Client-Side Hydration & Resilient Token Refresh**:
   - When an unauthenticated visitor attempts to navigate to any protected route (including `/`, `/today`, `/library`, `/quiz`, `/review`, `/roadmap`, `/settings`, `/profile`, `/insights`, `/notes`), the middleware SHALL immediately redirect the visitor to `/login`.
   - The middleware SHALL preserve the attempted destination in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
   - When an authenticated user with a valid, non-expired token navigates to `/login`, the middleware SHALL redirect the user to the target specified by the `redirect` query parameter, or to `/today` if no redirect parameter is present.
   - If a visitor arrives at `/login` with an expired token, the middleware SHALL purge credentials and allow the visitor to stay on `/login`.
   - If a route requires authentication and `authStore.isLoggedIn` is false:
     - The middleware SHALL attempt silent background renewal via `authStore.tryRefreshToken()`.
     - If refresh fails due to transient network disconnection, server restart (HTTP 5xx), or local cross-origin cookie restrictions, the platform SHALL NOT wipe valid user credentials or access tokens from local storage.
     - The user SHALL only be redirected to `/login` with cleared session credentials if the refresh request is explicitly rejected by the authentication authority with HTTP 401 or 403, or if no stored token exists.

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

#### Scenario: Pure guest visits protected route directly on SSR
- **WHEN** an unauthenticated visitor without any session cookies makes a direct HTTP request to `/library` or `/today`
- **THEN** the SSR route middleware immediately issues an HTTP 302 redirect to `/login` with the `redirect` query parameter set to the requested path
- **AND** zero HTML of the protected page or its layout container is rendered in the server response.

#### Scenario: Returning user with expired access token visits protected route on SSR
- **WHEN** a visitor with an expired `deeppace_token` cookie visits `/library`
- **THEN** the SSR route middleware decodes JWT expiration on the server, issues an immediate HTTP 302 redirect to `/login?redirect=/library` if no refresh token is present, and suppresses rendering the protected application shell.

#### Scenario: Server-side request with an expired access token
- **WHEN** a visitor makes an HTTP request to `/today` with an expired `deeppace_token` cookie and no refresh token cookie
- **THEN** the server SHALL respond with an HTTP 302 redirect to `/login?redirect=/today`
- **AND** the server SHALL NOT render `<AppHeader>` or `<AppSidebar>` into the HTML response body
- **AND** client-side hydration SHALL mount cleanly on `/login` with zero hydration node mismatch errors.

#### Scenario: Silent token refresh across local development ports
- **WHEN** client-side route navigation occurs with an expired access token in a local environment where refresh cookies cannot be sent across ports
- **AND** the refresh attempt fails with an unauthorized status
- **THEN** the client SHALL redirect cleanly to `/login?redirect={targetUrl}` without corrupting DOM tree structures.

#### Scenario: Administrator route admission on server and client
- **WHEN** a caller visits `/admin` or any `/admin/*` route except `/admin/login`
- **THEN** server-confirmed administrator permission is required before rendering or fetching privileged content
- **AND** anonymous or denied callers are redirected to `/admin/login` with a validated local admin return path.

#### Scenario: Administrator login remains reachable
- **WHEN** a regular signed-in account is redirected to `/admin/login`
- **THEN** the page remains visible with a localized permission message and allows signing in with another account without a redirect loop.

#### Scenario: Permission verification cannot complete
- **WHEN** an SSR or client permission check encounters a transient failure
- **THEN** protected UI remains hidden, a neutral retry state is shown and valid session credentials are retained.

#### Scenario: Canonical identity requires browser migration
- **WHEN** only legacy cookie evidence is visible to SSR and browser storage must resolve canonical identity
- **THEN** the shared migration boundary shows neutral admission without protected shell/content or privileged requests
- **AND** client migration completes before canonical route checks, refresh and permission verification continue.

### Requirement: Google Identity Services Client Configuration & Fallback
The web frontend application SHALL resolve the Google Client ID from runtime configuration using flexible environment variable resolution, supporting either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID`.

The system SHALL guarantee that:
1. When either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID` is set in the hosting environment (including local development runner `.env`), the frontend initializes Google OAuth2 token client (`google.accounts.oauth2.initTokenClient`) with the resolved Client ID and `openid email profile` scopes.
2. In local development environments executed via `run-dev.sh`, environment variables from `.env` are automatically propagated to the frontend development server process.
3. The Google Sign-In action SHALL render as a custom-styled button adhering to the Dev-Learning Studio design system (`bg-slate-100 dark:bg-[#202024]`, centered Google SVG icon, localized label) without iframe overlays, clipping containers, or white icon-box rendering anomalies.
4. Clicking the Google Sign-In button SHALL trigger the OAuth2 account selection popup via `tokenClient.requestAccessToken({ prompt: 'select_account' })`.
5. The backend endpoint `POST /api/v1/auth/google` SHALL accept both Google OAuth2 Access Tokens (`AccessToken`) and standard JWT ID Tokens (`IdToken`), validating Access Tokens against Google's `userinfo` endpoint and ID Tokens against Google's token signature verifier, returning an authenticated DeepPace session upon verification.
6. If neither variable is configured (empty string), the application SHALL NOT initialize Google OAuth2 services with an empty Client ID, preventing `400: invalid_request (Missing required parameter: client_id)` authorization errors.

#### Scenario: Local development environment with GOOGLE_CLIENT_ID defined in .env
- **WHEN** a developer starts the development stack with `run-dev.sh` and root `.env` defines `GOOGLE_CLIENT_ID`
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `GOOGLE_CLIENT_ID`
- **AND** the Google Sign-In button initializes with the configured Client ID without `client_id` missing parameter errors.

#### Scenario: Production or CI deployment with NUXT_PUBLIC_GOOGLE_CLIENT_ID defined
- **WHEN** the application runs in a container or environment where `NUXT_PUBLIC_GOOGLE_CLIENT_ID` is defined
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `NUXT_PUBLIC_GOOGLE_CLIENT_ID`.

#### Scenario: Environment without Google OAuth credentials configured
- **WHEN** neither `NUXT_PUBLIC_GOOGLE_CLIENT_ID` nor `GOOGLE_CLIENT_ID` is defined
- **THEN** the frontend recognizes the missing Client ID, avoids passing an empty string to Google OAuth2 initialization, and does not render a broken Google Sign-In authorization flow.

#### Scenario: User clicks custom Google Sign-In button
- **WHEN** an unauthenticated visitor clicks the custom-styled "Sign in with Google" button on `/login`
- **THEN** the system triggers Google's OAuth2 account selection popup directly
- **AND** zero iframe visual clipping or anti-clickjacking event blockage occurs.

#### Scenario: Backend authenticates OAuth2 Access Token
- **WHEN** the frontend submits an OAuth2 access token (`ya29...`) to `POST /api/v1/auth/google`
- **THEN** the server retrieves user profile information (`email`, `name`, `picture`, `sub`) from Google's userinfo endpoint
- **AND** creates or updates the user account and returns an authenticated session with JWT and refresh token cookies.

#### Scenario: Backend authenticates JWT ID Token
- **WHEN** a client submits a standard JWT ID token to `POST /api/v1/auth/google`
- **THEN** the server validates the signature and audience against Google's token validator and returns an authenticated session.

### Requirement: Dev-Learning Studio Authentication Surface & Shell Isolation
The platform SHALL provide a dedicated, full-screen **Studio Auth Cockpit** conforming to the Dev-Learning Studio visual language, completely isolated from internal application navigation chrome.

1. **Application Shell Isolation & Full-Screen Frame**:
   - The global layout shell (`app.vue`) SHALL detect authentication routes across all guest authentication paths (`/login`, `/register`, `/forgot-password`, `/reset-password`, with optional trailing slashes).
   - The template in `app.vue` SHALL isolate the authentication route outlet into an independent conditional branch (`<template v-if="isAuthPage">`) that mounts directly into the viewport root without wrapping containers.
   - The standard application shell (including `<AppHeader>`, `<AppCommandPalette>`, `<AppSidebar>`, and `<main class="flex-1 overflow-y-auto">`) SHALL reside in a mutually exclusive branch (`<template v-else>`).
   - When navigating to any guest authentication route (`isAuthPage = true`), the entire application shell DOM hierarchy (including `<main>` and any leftover server-rendered page nodes) SHALL be completely unmounted from the DOM tree, guaranteeing that outgoing route components are cleanly destroyed and eliminating visual page stacking.
   - The global notification container (`<AppToastContainer />`) SHALL remain mounted at the root level so that system toasts remain functional across both authentication and application routes.
   - The route outlet `<NuxtPage />` in `app.vue` SHALL enforce explicit full-path route keying (`:page-key="route => route.fullPath"`), and the application SHALL disable Vue page and layout transitions (`pageTransition: false`, `layoutTransition: false` in `nuxt.config.ts`), guaranteeing that outgoing route components are cleanly and atomically destroyed before incoming route components are mounted.
   - The authentication surface SHALL occupy the entire viewport (`min-h-screen w-screen overflow-hidden`) with the dark obsidian background canvas (`bg-slate-50 dark:bg-canvas`), without vertical scroll bleeding or residual DOM elements from previous routes.
   - The page frame SHALL feature:
     - **Top System Telemetry Bar**: System identity badge (`DEEPPACE::IDE v2.5.0-sys`), live ping latency status indicator (`● PING 18ms`), single-locale language selector (`EN | VI`), and theme toggle button (`ThemeToggle.vue`).
     - **Ambient Status Sub-Header**: Live operational status ticker (`● ALL SERVICES OPERATIONAL  LATENCY 14MS`) and the platform invariant (`⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`).
     - **Bottom Compliance Telemetry Bar**: Security framework and node compliance indicators (`DEEPPACE COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT`, `DISTRIBUTED COCKPIT // SECURE_AUTH_NODE`, `TLS 1.3 AES-256-GCM`).
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
- **THEN** the top system bar displays `DEEPPACE::IDE` and ping latency
- **AND** the left stage renders the `CONSENSUS_PROMISE.TS` code block with monospace syntax highlighting and SM-2 spaced decay metrics
- **AND** the bottom frame displays security compliance notices (`SOC2 TYPE II & RFC-7519 JWT`).

#### Scenario: Unauthenticated redirect to login completely replaces previous page view
- **GIVEN** the user is viewing the Dashboard at `/` when session expiration occurs
- **WHEN** the client initiates navigation to `/login?redirect=/`
- **THEN** the Dashboard component (`index.vue`) is cleanly unmounted from the DOM
- **AND** the Login component (`login.vue`) mounts as the sole child of the application container
- **AND** zero Dashboard widgets (streak card, knowledge constellation, reading slice) remain visible or stacked above the login surface.

#### Scenario: Client redirect to login cleanly unmounts application shell DOM
- **WHEN** a user or client middleware navigates from a protected route to `/login`
- **THEN** `isAuthPage` transitions to `true`
- **AND** the entire application shell branch including `<AppHeader>`, `<AppSidebar>`, and `<main class="flex-1 overflow-y-auto">` is completely unmounted from the DOM
- **AND** the authentication page mounts into an isolated root container without residual or stacked elements from the previous route.

### Requirement: Clean Studio Header & Language Controls
The top navigation header on the Studio Auth canvas SHALL provide a clean, distraction-free branding bar with language and theme switches.

1. **Brand Identity**:
   - The header SHALL display the DeepPace brand mark and title linking to `/login`.

2. **User Preferences**:
   - The right side of the header SHALL provide language selection (EN / VI) and color mode (Dark / Light) toggles.

#### Scenario: Visitor inspects header
- **WHEN** user loads `/login`
- **THEN** the top header displays the DeepPace brand emblem and title alongside language and theme toggle buttons.

### Requirement: Left Telemetry Stage with Metrics & Code Simulation
The left column of the Studio Auth canvas SHALL showcase DeepPace's technical reading curriculum and spaced repetition practice model through live telemetry gauges and simulated code execution.

1. **Platform Identifiers**:
   - The stage SHALL display the `● DEEPPACE | SM-2 ACTIVE RECALL` pill badge and `v2.4-SYS` version tag.
   - The headline SHALL state `Daily Technical Reading & Spaced Learning` with supporting curriculum description.

2. **Progress Metrics Card**:
   - The metrics card SHALL display `DAILY READING GOAL` with `94% COMPLETED` alongside a green gradient progress bar.
   - The card SHALL display `SM-2 SPACED REPETITION` with `ACTIVE RECALL` alongside an iris purple gradient progress bar.

3. **Code Simulation Window**:
   - The code block SHALL feature an editor title bar with three macOS-style window controls, title `>_ DEEPPACE_PRACTICE.TS`, and a lock emblem.
   - The code view SHALL render the syntax-highlighted `deepPace.getDailySlice` practice invocation snippet.

#### Scenario: Desktop visitor inspects learning preview
- **WHEN** user views `/login` on desktop
- **THEN** the left stage renders the progress gauges and the code window simulation.

### Requirement: Remember-Me Session Persistence
The `/login` "Remember session" checkbox SHALL control whether the authenticated session survives a browser restart, end-to-end across the client and server. The email/password login and registration requests SHALL carry a `rememberMe` boolean; when the field is omitted, the server SHALL treat it as `true` to preserve backward compatibility.

1. **Persistent session (checkbox checked, the default)**:
   - The frontend SHALL persist the access token and user in a 30-day `deeppace_token` / `deeppace_user` cookie and in `localStorage`.
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
- **AND** the client stores `deeppace_token` / `deeppace_user` in a 30-day cookie and `localStorage`
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

### Requirement: DeepPace Client Session Persistence & Token Storage Migration
The web frontend and authentication service SHALL standardize session credentials on `deeppace_*` keys through the versioned `client-storage` migration contract:

1. **Cookie & LocalStorage Identifiers**:
   - The primary access token SHALL use `deeppace_token` and the active profile SHALL use `deeppace_user` in cookies and the appropriate local/session storage according to the session's persistence mode.
   - Secondary reader and roadmap preferences SHALL use registered `deeppace_*` identifiers.
2. **Transparent Session Migration**:
   - Known legacy credentials SHALL be imported only through the centralized migration boundary before canonical session consumers initialize.
   - Existing canonical credentials SHALL take precedence; token/profile identity and known persistence mode SHALL remain consistent.
   - Successful transfer SHALL remove known legacy entries. Completed identity migration and logout SHALL prevent subsequent legacy fallback or session resurrection.
   - Invalid or expired legacy credentials SHALL use existing refresh/login recovery without weakening backend JWT or permission checks.
3. **JWT Configuration Invariants**:
   - The default JWT Issuer SHALL remain `"DeepPace"`, and the default JWT Audience SHALL remain `"DeepPaceUsers"`.
   - The existing HTTP-only `refreshToken` cookie contract SHALL remain unchanged.

#### Scenario: Existing user loads application with legacy cookies
- **GIVEN** a browser holding valid `techdaily_token` and `techdaily_user` but no canonical session credentials
- **WHEN** client admission completes the recognized legacy transfer
- **THEN** the migration boundary creates consistent `deeppace_token` and `deeppace_user` entries using safe session persistence
- **AND** removes their legacy entries after verifying the canonical writes
- **AND** the existing authenticated flow continues without forcing a credential reset.

#### Scenario: New user signs in and receives DeepPace credentials
- **WHEN** a user logs in via `POST /api/v1/auth/login`
- **THEN** the client stores the access token into `deeppace_token`
- **AND** the token payload reflects Issuer `"DeepPace"`.

#### Scenario: Canonical identity conflicts with legacy data
- **WHEN** canonical and legacy credentials identify different accounts
- **THEN** canonical identity wins and legacy profile data is not merged into it.

#### Scenario: Logout occurs before migration resolves
- **WHEN** logout or terminal session cleanup invalidates the current session during migration
- **THEN** delayed work cannot restore credentials and completed cleanup prevents future legacy fallback.

#### Scenario: Session-only legacy credentials
- **WHEN** legacy credentials came from a known session-only sign-in
- **THEN** canonical migration writes no persistent access token or profile.
