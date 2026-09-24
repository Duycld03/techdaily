# Spec Delta: auth

## MODIFIED Requirements

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

---

### Requirement: Dev-Learning Studio Authentication Surface & Shell Isolation
The platform SHALL provide a dedicated, full-screen **Studio Auth Canvas** conforming to the Dev-Learning Studio visual language, completely isolated from internal application navigation chrome.

1. **Application Shell Isolation**:
   - The global layout shell (`app.vue`) SHALL detect authentication routes (`isAuthPage = computed(() => route.path === '/login')`) and completely suppress both `AppHeader.vue` and `AppSidebar.vue`.
   - The authentication surface SHALL occupy the entire viewport (`min-h-screen w-screen overflow-hidden`) with the dark obsidian background canvas (`bg-slate-50 dark:bg-canvas`).

2. **Ambient Utilities on Auth Canvas**:
   - The top-right corner of the Studio Auth Canvas SHALL provide direct, self-contained controls for `ThemeToggle.vue` and `LocaleSelector.vue`, enabling visitors to switch color schemes and languages without depending on the internal application header.

3. **Studio Auth 2-Column Desktop Archetype**:
   - On desktop viewports ($\ge 1024\text{px}$), the authentication view (`pages/login.vue`) SHALL render within a balanced container (`max-w-4xl` to `max-w-5xl`) organized into two complementary columns:
     - **Left Column (Platform Value Stage)**: Displays TechDaily's brand emblem, mission statement ("Master Senior Software Engineering Daily"), and 3 core learning pillars (Scenario Architecture Drills, SM-2 Spaced Mastery, Daily System Design Doses) with subtle studio badges.
     - **Right Column (Interactive Auth Card)**: Houses the `.glass-panel` container (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `rounded-3xl`, `shadow-2xl`) containing mode switching tabs, input forms, and actions.
   - On mobile and tablet viewports ($< 1024\text{px}$), the layout SHALL gracefully stack, placing a compact brand header above the authentication card.

4. **Multi-Mode Authentication State Machine**:
   - The authentication card SHALL support modes: `login`, `register`, and `forgot-password`.
   - In `login` mode, the card presents Email, Password (with visibility eye toggle), Forgot Password link, Submit button, and Google OAuth button.
   - In `register` mode, the card presents a 2-column input grid (`grid sm:grid-cols-2 gap-3.5`) for Full Name, Email, Password, and Confirm Password, reducing vertical elongation by over 40%.
   - In `forgot-password` mode, the card presents Email input, Reset button, and a Back to Login navigation trigger.
   - Transitions between modes SHALL execute smoothly with zero vertical jumping or scrollbar popping.

5. **Divider Layout Stability**:
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

#### Scenario: Guest switches to forgot password mode
- **WHEN** the user clicks "Forgot password?" on `/login`
- **THEN** the card transitions to the forgot-password form displaying an email input and "Send Reset Link" button
- **AND** a "Back to Sign In" button allows returning to the login form without reloading the page.
