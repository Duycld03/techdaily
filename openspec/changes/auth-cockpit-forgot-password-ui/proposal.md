# Proposal: Dev-Learning Studio Cockpit Account Recovery & Forgot Password UI

## Why
While the Dev-Learning Studio Cockpit authentication layout establishes an engineering-first 2-column aesthetic for Sign In and Register, the account recovery (Forgot Password) flow remains basic and does not yet reflect the dark obsidian cockpit design language demonstrated in `paste-1.md`. Incorporating the dedicated recovery UI into the 3-tab segmented cockpit switcher (`[ Sign In ]`, `[ Register ]`, `[ ↻ Recover ]`) provides engineers with a seamless, high-density account recovery experience featuring clear OAuth bypass guidance, monospace telemetry input fields, zero-knowledge security badges, and bilingual parity.

## What Changes
- **3-Tab Segmented Cockpit Switcher**: Extend the interactive card's top segmented tab control with a third recovery action tab (`lock_reset` icon / `auth.recovery_tab`) that seamlessly activates `authMode = 'forgot-password'`.
- **Engineering-Grade Recovery Surface**:
  - Structured header with localized mission title ("Recover Cockpit Access" / "Khôi Phục Quyền Truy Cập Cockpit") and magic link validity note (15-minute expiration).
  - Monospace telemetry field for `ACCOUNT REGISTRATION EMAIL` (`EMAIL ĐĂNG KÝ TÀI KHOẢN`) with leading mail icon and valid autofocus.
  - Contextual authentication advisory panel highlighting direct login for GitHub OAuth or hardware credentials.
  - High-contrast Iris Violet submit button (`Send Recovery Magic Link` with `send` icon and return shortcut hint).
  - Dedicated navigation trigger: `Back to Sign In` (`Quay lại Đăng nhập`) with arrow icon.
- **Cockpit Footer & Security Badges**:
  - Add zero-knowledge security indicator (`ZERO-KNOWLEDGE AUTH` with emerald lock icon).
  - Terms & Privacy policy links compliant with bilingual typography standards.
- **Single-Language Localization**: Add dedicated translation keys in `en.json` and `vi.json` avoiding any bilingual slash text.
- **Automated & Visual Verification**: Extend unit tests in `frontend/tests/pages/login.spec.ts` covering the 3-tab switcher and recovery panel interactions, with visual screenshots in desktop/mobile and dark/light modes.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Update the authentication surface requirements to specify the 3-tab segmented cockpit switcher, the dedicated engineer recovery view with contextual security guidance, and the zero-knowledge security card footer.

## Impact
- **Frontend Code**: `frontend/pages/login.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Testing**: `frontend/tests/pages/login.spec.ts`.
- **Visual Design**: Strict adherence to the Dev-Learning Studio color palette (obsidian `#09090b`, `#131315`, `#18181b`, Iris Violet `#7c3aed`, Emerald `#34d399`) and responsive typography guidelines.
