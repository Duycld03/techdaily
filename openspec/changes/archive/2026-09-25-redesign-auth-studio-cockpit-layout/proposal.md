# Proposal

## Why

The current authentication surface at `/login` provides a functional 2-column layout, but lacks the high-fidelity **IDE Cockpit** atmosphere of the Dev-Learning Studio design system. Upgrading to the full-screen Studio Cockpit layout introduces ambient system telemetry bars, live distributed consensus code showcases, spaced repetition decay monitors, and refined form ergonomics.

Crucially, the new layout eliminates bilingual slash clutter (`Đăng nhập / Sign In`) in favor of clean single-language rendering powered by TechDaily's existing `@nuxtjs/i18n` catalog, giving Senior Engineers a breathtaking, IDE-grade first impression that feels like mission control.

## What Changes

- **Full-Screen Studio Cockpit Shell**:
  - Top System Telemetry Bar: Houses the system identity badge (`TECHDAILY::IDE v2.5.0-sys`), live ping latency status (`● PING 18ms`), interactive single-locale language toggle (`EN | VI`), and color mode toggle.
  - Ambient Status Sub-Header: Displays operational telemetry (`● ALL SERVICES OPERATIONAL LATENCY 14MS`) and the platform invariant (`⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`).
  - Bottom Compliance Telemetry Bar: Features security and compliance notices (`TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT`, `DISTRIBUTED COCKPIT // SECURE_AUTH_NODE`, `TLS 1.3 AES-256-GCM`).
- **Curriculum & Telemetry Showcase (Left Stage)**:
  - Staff+ Track badge and Distributed Systems Mastery curriculum headline.
  - Live learning telemetry: `SESSION INTERVAL TARGET 99.4% HIT` progress gauge and `SM-2 SPACED DECAY 24.8 Hrs` indicator.
  - Glass code showcase card (`CONSENSUS_PROMISE.TS`) rendering monospace consensus promise logic (`await quorum.commit(...)`) with lock status.
- **Interactive Cockpit Auth Card (Right Stage)**:
  - **Single-Language i18n Strictness**: Form labels, mode tabs, buttons, and badges strictly display either English OR Vietnamese based on active locale, never dual-language slash concatenation.
  - Segmented mode switcher tabs (`Login` / `Register` / `Forgot Password`) with instant tab switching.
  - OAuth Action Buttons: GitHub and Google buttons equipped with monospace keyboard shortcut chips (`G`, `⌘L`).
  - Form Fields with Studio Telemetry: Monospace field labels (`DEV HANDLE / WORK EMAIL`, `SECRET TOKEN / KEY`), secure password visibility toggle, and inline "Forgot password?" trigger.
  - Session Persistence: Checkbox for 30-day session preservation.
  - Primary Iris Violet CTA Button: Localized action text with `↵ RETURN` keyboard badge.
  - Card Footer: Terms and Privacy navigation links.
- **Responsive Stacking**:
  - On desktop ($\ge 1024\text{px}$), renders the balanced 2-column cockpit stage.
  - On mobile/tablet ($< 1024\text{px}$), stacks smoothly with mobile-friendly touch targets ($\ge 44\text{px}$) and scrollable container.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Update the `Dev-Learning Studio Authentication Surface & Shell Isolation` requirement to define the full-screen Studio Cockpit shell architecture, top/bottom telemetry frames, live code showcase stage, and strict single-language i18n layout.

## Impact

- **Frontend**: `frontend/pages/login.vue` (and child components/locales if applicable).
- **Localization**: Uses and extends keys in `frontend/i18n/locales/en.json` and `vi.json` to ensure 100% single-language rendering without bilingual slash text.
- **Tests**: `frontend/tests/pages/login.spec.ts` updated to assert cockpit telemetry elements, single-language display, and responsive behaviors.
