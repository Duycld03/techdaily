# Spec Delta: Auth

## MODIFIED Requirements

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard (`frontend/middleware/auth.global.ts`) SHALL enforce a strict **Default-Deny** security posture: all platform routes require active authentication by default, with only explicit guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local development playgrounds (`/playground/*`, `/showcase`) exempted.

1. **Server-Side Rendering (SSR) Route Guard**:
   - On Server-Side Rendering (`import.meta.server`), when an incoming HTTP request targets any protected route (such as `/library`, `/today`, `/notes`), the middleware SHALL inspect request cookies for session credentials (`techdaily_token` and `refreshToken`).
   - If **neither** `techdaily_token` nor `refreshToken` cookie is present in the incoming request, the server SHALL immediately terminate the request and issue an HTTP 302 Found redirect to `/login?redirect=<encoded-path>`, preventing any protected page HTML or component templates from being rendered by the server.
   - If an authentication token cookie is present in the request (even if the access token has passed its expiration timestamp), the server SHALL allow SSR rendering to proceed and defer active credential verification to client-side hydration, allowing client-side background silent refresh to execute before deciding whether a redirect is required.

2. **Client-Side Hydration and Active Route Interception**:
   - When an unauthenticated visitor attempts to navigate to any protected route (including `/`, `/today`, `/library`, `/quiz`, `/review`, `/roadmap`, `/settings`, `/profile`, `/insights`, `/notes`), the middleware SHALL immediately redirect the visitor to `/login`.
   - The middleware SHALL preserve the attempted destination in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
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

#### Scenario: Pure guest visits protected route directly on SSR
- **WHEN** an unauthenticated visitor without any session cookies makes a direct HTTP request to `/library` or `/today`
- **THEN** the SSR route middleware immediately issues an HTTP 302 redirect to `/login` with the `redirect` query parameter set to the requested path
- **AND** zero HTML of the protected page or its layout container is rendered in the server response.

#### Scenario: Returning user with expired access token visits protected route on SSR
- **WHEN** a visitor with an expired `techdaily_token` cookie visits `/library`
- **THEN** the SSR route middleware defers redirect to client hydration
- **AND** on client hydration, `tryRefreshToken()` executes silently to renew the session without forcing an abrupt redirect to `/login`.

---

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
