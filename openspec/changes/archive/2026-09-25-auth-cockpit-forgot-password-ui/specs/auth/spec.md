# Spec Delta: Auth Capability

## ADDED Requirements

### Requirement: Studio Cockpit Account Recovery & 3-Tab Segmented Mode Switcher
The interactive authentication cockpit card SHALL provide a unified 3-tab segmented control supporting direct switching between Sign In, Register, and Account Recovery (`forgot-password` mode).

1. **Segmented Mode Switcher Architecture**:
   - The top tab strip SHALL display three segmented options: `Sign In` (`auth.sign_in_tab`), `Register` (`auth.register_tab`), and a compact recovery action tab with a key/lock-reset icon (`auth.recovery_tab_title` with `KeyRound` / `RotateCcw` icon).
   - Clicking the recovery tab SHALL activate `authMode = 'forgot-password'` with zero layout shift or vertical jumping.
   - The active tab state SHALL be highlighted with high-contrast studio styling (`bg-slate-200 dark:bg-canvas-subtle text-brand-600 dark:text-brand-400 font-bold`).

2. **Dedicated Account Recovery Surface**:
   - In `forgot-password` mode, the card SHALL display a dedicated recovery header: "Recover Cockpit Access" (`auth.recover_cockpit_title`) and helper note explaining that a magic link valid for 15 minutes will be dispatched (`auth.recover_cockpit_subtitle`).
   - The form SHALL provide a single monospace-labeled email input field (`ACCOUNT REGISTRATION EMAIL` / `auth.account_email_label`) with a leading mail icon and HTML5 email validation.
   - The card SHALL present a contextual advisory notice with an informational icon informing engineers that accounts configured with GitHub OAuth or Hardware Security Keys can authenticate directly without resetting passwords (`auth.oauth_bypass_notice`).
   - The primary action button SHALL display "Send Recovery Magic Link" (`auth.send_recovery_link_btn`) with a send icon and keyboard return hint (`↵ RETURN`).
   - A dedicated secondary navigation action "Back to Sign In" (`auth.back_to_signin_btn`) SHALL restore the `authMode = 'login'` state.

3. **Cockpit Footer & Security Attestation**:
   - The card footer SHALL display an attestation badge: `ZERO-KNOWLEDGE AUTH` (`auth.zero_knowledge_badge`) with an emerald security lock icon alongside standard Terms and Privacy policy navigation links.
   - All text content SHALL be 100% localized through single-language translation keys in `en.json` and `vi.json` without bilingual slash combinations.

#### Scenario: User clicks recovery tab in segmented header
- **WHEN** user clicks the recovery tab in the top segmented switcher
- **THEN** `authMode` transitions to `forgot-password`
- **AND** the card smoothly reveals the Account Recovery form without layout blowout.

#### Scenario: User submits email for password recovery
- **WHEN** user enters a valid email address and clicks "Send Recovery Magic Link"
- **THEN** the system simulates/invokes the password recovery dispatch and notifies the user with a localized success toast.

#### Scenario: User returns to Sign In from recovery view
- **WHEN** user clicks "Back to Sign In" or the "Sign In" tab
- **THEN** `authMode` reverts to `login` and preserves any entered email address.
