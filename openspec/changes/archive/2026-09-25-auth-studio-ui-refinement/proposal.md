# Proposal: Refine Studio Auth Layout to Authentic Design Specification

## Why
The authentication surface recently accumulated extraneous telemetry elements—including a `PING 18ms` badge, an unstandardized `IDE v2.5.0-sys` pill, latency indicators (`ĐỘ TRỄ 14MS`), and cluttered Vietnamese footer strings—which diverge from the pristine engineering mockup (Image #1). Furthermore, a placeholder GitHub OAuth button was provisionally displayed without an active integration. This change strips away redundant noise, aligns the visual hierarchy directly to Image #1, removes the GitHub login option, and standardizes all copy and security attestations around TechDaily's core learning mission.

## What Changes
- **Remove Redundant Telemetry & Latency Clutter**:
  - Remove the `PING 18ms` badge from the top header (Image #4).
  - Remove the `IDE v2.5.0-sys` pill (Image #5); standardize header to `TechDaily` brand icon + `IDE` badge, subtle vertical divider, `v1.0` badge, and `Engineering Cockpit · Density 8/10` indicator.
  - Simplify the sub-header status line (Image #3): replace the cluttered latency string with the clean `• ALL SERVICES OPERATIONAL` badge (left) and `⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE` (right), matching Image #1.
  - Fix footer telemetry placement (Image #2): move `TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT` into the main body flow beneath the split cards, and keep the sticky bottom footer strictly for `• DISTRIBUTED COCKPIT // SECURE_AUTH_NODE` (left) and `TLS 1.3 AES-256-GCM` (right).
- **Remove GitHub OAuth Provider**:
  - Remove the GitHub OAuth button from the 1-click developer stack.
  - Render Google Sign-In as the dedicated 1-click OAuth button (`Google ⌘L`) or clean single container.
- **Align Left Mastery Stage & Code Snippet**:
  - Ensure exact styling of `STAFF+ TRACK` (`v2.4-DRILL`), `Distributed Systems Mastery`, and its mission description.
  - Rebalance the two mini-bento metric bars: `SESSION INTERVAL TARGET` (`99.4% HIT`, 94% width emerald bar) and `SM-2 SPACED DECAY` (`24.8 Hrs`, gradient bar).
  - Polish the `<> Consensus_Promise.ts` code snippet card with lock icon and syntax highlighting for `RaftQuorum`.
  - Add the `END-TO-END CRYPTOGRAPHIC ATTESTATION` footnote with shield icon.
- **Card Security & Typography Polish**:
  - Title & Subtitle: "Welcome back to TechDaily" / "Continue your personalized distributed systems deliberate practice track."
  - Maintain the 3-tab segmented control (`[ Sign In ]`, `[ Register ]`, `[ ↻ ]`).
  - Card footer: `ZERO-KNOWLEDGE AUTH` with emerald lock icon, and `Terms · Privacy` links.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Update visual layout, header telemetry, and OAuth provider requirements to strictly reflect the canonical Dev-Learning Studio Cockpit design (Image #1), eliminating redundant latency badges and restricting 1-click OAuth to Google.

## Impact
- **Frontend Code**: `frontend/pages/login.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Testing**: Update unit tests in `frontend/tests/pages/login.spec.ts` to assert the cleaned header, single Google OAuth stack, and updated telemetry structure.
- **Visual Design**: Strict 1:1 parity with Image #1.
