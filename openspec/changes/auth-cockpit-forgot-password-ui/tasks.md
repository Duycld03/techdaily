# Tasks: Dev-Learning Studio Cockpit Account Recovery & Forgot Password UI

## 1. Localization & Translation Keys
- [x] 1.1 In `frontend/i18n/locales/en.json`, define recovery translation keys: `recovery_tab_title`, `recover_cockpit_title`, `recover_cockpit_subtitle`, `account_email_label`, `oauth_bypass_notice`, `send_recovery_link_btn`, `back_to_signin_btn`, `zero_knowledge_badge`, `terms_link`, and `privacy_link`
- [x] 1.2 In `frontend/i18n/locales/vi.json`, define the corresponding single-language Vietnamese translation keys preserving authentic technical terminology without slash combinations

## 2. Frontend - Segmented Switcher & Account Recovery Surface
- [x] 2.1 In `frontend/pages/login.vue`, update the top segmented switcher container to support 3 distinct tabs: `[ Sign In ]`, `[ Register ]`, and `[ ↻ Recover ]` with active tab highlights and zero layout shift
- [x] 2.2 In `frontend/pages/login.vue`, refactor the `authMode === 'forgot-password'` template section into the Dev-Learning Studio recovery cockpit panel with the mission header, helper note, and monospace `ACCOUNT REGISTRATION EMAIL` input
- [x] 2.3 In `frontend/pages/login.vue`, add the contextual OAuth and hardware security key bypass guidance notice box with amber `Info` icon
- [x] 2.4 In `frontend/pages/login.vue`, implement the high-contrast Iris Violet submit button (`Send Recovery Magic Link` with `Send` icon and `↵ RETURN` shortcut hint) and secondary `Back to Sign In` link
- [x] 2.5 In `frontend/pages/login.vue`, add the card footer with `ZERO-KNOWLEDGE AUTH` emerald security attestation and Terms/Privacy navigation links

## 3. Testing & Visual Verification
- [x] 3.1 In `frontend/tests/pages/login.spec.ts`, add unit tests covering 3-tab segmented switcher state transitions, recovery mode input rendering, and "Back to Sign In" navigation
- [x] 3.2 Execute frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.3 Capture visual test screenshots in English and Vietnamese across Desktop (1440x900) and Mobile viewports verifying zero layout shift, dark/light modes, and balanced proportions
