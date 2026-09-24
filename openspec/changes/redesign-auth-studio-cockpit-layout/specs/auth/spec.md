# Spec Delta: auth

## MODIFIED Requirements

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
