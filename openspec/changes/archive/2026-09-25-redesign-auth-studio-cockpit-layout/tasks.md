# Tasks: Dev-Learning Studio Cockpit Authentication Architecture

## 1. Top Telemetry Bar & Studio Ambient Framework
- [x] 1.1 In `frontend/pages/login.vue`, construct the full-screen Studio Cockpit frame with top telemetry bar (`TECHDAILY::IDE v2.5.0-sys`, `PING 18ms`, single-locale `EN | VI` toggle, and theme switch)
- [x] 1.2 Implement the ambient status sub-header with operational status ticker (`• ALL SERVICES OPERATIONAL - LATENCY 14ms`) and system invariant tagline (`⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`)
- [x] 1.3 Implement the bottom compliance telemetry bar (`DISTRIBUTED COCKPIT // SECURE_AUTH_NODE`, `TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT`, `TLS 1.3 AES-256-GCM`)

## 2. Curriculum & Learning Telemetry Showcase (Left Column Stage)
- [x] 2.1 In `frontend/pages/login.vue`, construct the left stage container with Staff+ Track badge (`>_ TechDaily Studio // STAFF+ TRACK v2.4-DRILL`) and headline (`Distributed Systems Mastery`)
- [x] 2.2 Implement live learning telemetry gauges for Session Interval Target (`99.4% HIT`) and SM-2 Spaced Decay (`24.8 Hrs`)
- [x] 2.3 Implement the `CONSENSUS_PROMISE.TS` live code showcase card displaying Raft consensus commit snippet with line numbers and monospace typography

## 3. Interactive Auth Cockpit Card & Localization Polish (Right Column Stage)
- [x] 3.1 Define single-language translation keys in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for all cockpit elements, eliminating bilingual slash text
- [x] 3.2 Refactor the interactive auth card with single-language segmented tabs (`[ Sign In ]` `[ Register ]`), GitHub (`⌘G`) and Google (`⌘L`) OAuth buttons, and absolute centered divider
- [x] 3.3 Style input fields with monospace telemetry labels (`DEV HANDLE / WORK EMAIL [SSH / OIDC]`, `SECRET TOKEN / KEY [Forgot password?]`) and dark input backgrounds
- [x] 3.4 Implement session persistence checkbox (`Remember session (30 days)`) and high-contrast Iris Violet submit button (`Enter Training Cockpit ↵ RETURN`)
- [x] 3.5 Preserve the compact 2-column register grid and forgot password reset mode with transition animations

## 4. Testing & Verification
- [x] 4.1 Update unit tests in `frontend/tests/pages/login.spec.ts` covering top telemetry bar, left curriculum showcase, monospace inputs, shortcut chips, and bottom compliance bar
- [x] 4.2 Execute frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 4.3 Capture visual test screenshots in English and Vietnamese across Desktop (1440x900) and Mobile viewports verifying balanced layout proportions, dark/light modes, and zero layout shift
