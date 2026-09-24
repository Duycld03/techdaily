# Design: Dev-Learning Studio Auth Cockpit Layout

## Context

The existing `/login` page (`frontend/pages/login.vue`) implements basic shell isolation and a 2-column desktop layout. However, it lacks the full-screen mission-control aesthetic of an IDE Cockpit, and its layout does not match the reference design system requested by the user. Furthermore, the design must strictly uphold TechDaily's 100% single-language i18n standard, eliminating dual-language slash text (`Đăng nhập / Sign In`) present in the initial design reference mockup.

## Goals / Non-Goals

**Goals:**
- Implement the full-screen Studio Cockpit frame with top telemetry bar, ambient status ticker, 2-column central stage, and bottom compliance footer.
- Construct the left stage: Staff+ Track badge, Distributed Systems Mastery headline, session interval target gauge (`99.4% HIT`), SM-2 spaced decay indicator (`24.8 Hrs`), and live `CONSENSUS_PROMISE.TS` code showcase card.
- Construct the right stage: Interactive glass auth card with segmented mode tabs (`login` / `register`), OAuth buttons with keyboard badges (`G`, `⌘L`), telemetry-labeled inputs with `architect@enterprise.io` placeholder, session persistence checkbox, and Iris Violet primary CTA with `↵ RETURN` shortcut badge.
- Enforce strict single-language rendering: Every text string, tab, label, and badge renders purely in the active locale (`en` or `vi`) using `@nuxtjs/i18n` message catalogs, with zero bilingual slash concatenation.
- Guarantee responsive stacking: On mobile viewports ($< 1024\text{px}$), gracefully stack into a scrollable, touch-ergonomic ($\ge 44\text{px}$) layout.

**Non-Goals:**
- Modifying backend authentication endpoints or token contracts (`/api/v1/auth/*`).
- Introducing new authentication providers beyond GitHub and Google.
- Changing password validation rules (minimum 8 characters, confirmation matching).

## Decisions

### Decision 1: Single-Language i18n Strictness vs. Reference Mockup
- **Rationale**: The reference mockup illustrated bilingual labels (e.g. `Đăng nhập / Sign In`, `DEV HANDLE / WORK EMAIL`). The user explicitly directed: *"i18n mình có nên đừng cho cả 2 ngôn ngữ lên cùng"* (we should not show both languages together). In TechDaily, locale switching is instant and declarative via `@nuxtjs/i18n`. We preserve the telemetry/terminal uppercase styling, but map all strings to single-locale translation keys.
- **Alternatives Considered**: Keeping dual-language slashes. Rejected due to visual clutter, awkward mobile wrapping, and explicit user prohibition.

### Decision 2: Cockpit Frame Geometry & Overflow Discipline
- **Rationale**: The cockpit frame should fit standard 1080p desktop monitors without page scrollbars (`h-screen max-h-screen overflow-hidden` on desktop). On mobile devices with smaller vertical viewports, the outer frame transitions to `min-h-screen overflow-y-auto` to ensure all fields and buttons remain comfortably accessible.

### Decision 3: Telemetry Accent Details
- **Rationale**: Adding purposeful system touches (ping indicator `● PING 18ms`, operational status ticker, consensus promise code card) reinforces TechDaily's identity as a specialized distributed systems & senior engineering training platform while rejecting artificial marketing buzzwords like `ACTIVE TRUST`.

## Risks / Trade-offs

- **Small Height Screens (laptops with 768px height)**:
  - *Risk*: A fixed 100vh frame could clip the bottom footer if the central stage is too tall.
  - *Mitigation*: Use responsive padding (`py-2 sm:py-4`), compact gap spacing, and allow the central container to scroll if vertical viewport is below $720\text{px}$ (`max-h-[calc(100vh-6rem)] overflow-y-auto lg:overflow-visible`).
