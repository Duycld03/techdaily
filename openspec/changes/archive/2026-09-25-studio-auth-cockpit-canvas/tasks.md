# Tasks: Studio Auth Cockpit Canvas & Ambiance

## 1. Frontend - Ambient Canvas & Clean Studio Shell
- [x] 1.1 In `frontend/pages/login.vue`, implement the ambient background container with `.bg-grid-dots`, Iris violet radial ambient glow (`blur-[140px]`), accent glows, and hairline guide dividers matching `local://paste-3.md`
- [x] 1.2 In `frontend/pages/login.vue`, implement clean studio header with brand logo, title, and language/theme toggle controls
- [x] 1.3 Keep bottom layout clean and distraction-free without redundant telemetry lines or footer clutter

## 2. Frontend - Learning Telemetry Stage & Code Window
- [x] 2.1 In `frontend/pages/login.vue`, update the left column with platform badge `● TECHDAILY | SM-2 ACTIVE RECALL`
- [x] 2.2 In `frontend/pages/login.vue`, implement progress gauge cards for Daily Reading Goal (94%) and SM-2 Spaced Repetition (Active Recall)
- [x] 2.3 In `frontend/pages/login.vue`, implement macOS-style code window simulation for `TECHDAILY_PRACTICE.TS` with syntax-highlighted `techDaily.getDailySlice` snippet

## 3. Frontend - Auth Cockpit Card & Google SSO Glitch Fix
- [x] 3.1 In `frontend/pages/login.vue`, implement the 3-tab segmented control (`[ Sign In ]`, `[ Register ]`, `[ ↻ Recover ]`) with smooth transitions
- [x] 3.2 In `frontend/pages/login.vue`, replace the native iframe Google hover box with an integrated full-width Google button using clean SVG mark and responsive hover styling (`dark:bg-[#202024]`), completely eliminating the white box overflow defect
- [x] 3.3 In `frontend/pages/login.vue`, update form inputs with terminal `>_` email prefix, key icon password prefix, eye toggle, and `Forgot password?` label link
- [x] 3.4 In `frontend/pages/login.vue`, style the submit button with `↵ RETURN` shortcut chip and card footer with `🛡️ SECURE & ENCRYPTED AUTH`

## 4. Frontend - Localization & Verification
- [x] 4.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add all new telemetry strings, tooltips, and badges
- [x] 4.2 Update unit tests in `frontend/tests/pages/login.spec.ts` covering studio auth cockpit, code window, tab switching, and glitch-free Google button
- [x] 4.3 Run full frontend test suite (`npm test`) and production build (`npm run build`)
- [x] 4.4 Capture browser verification screenshots across desktop (1440x900) and mobile (375x812) viewports confirming zero layout shift and 100% visual fidelity
