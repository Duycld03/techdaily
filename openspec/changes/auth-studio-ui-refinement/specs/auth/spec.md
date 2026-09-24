# Spec Delta: Auth Capability

## MODIFIED Requirements

### Requirement: Studio Cockpit Header & TechDaily Brand Hierarchy
The Studio Auth canvas SHALL conform to authentic TechDaily design standards, presenting clean branding and eliminating placeholder IDE/Staff+ telemetry and cluttered latency badges.

1. **Top Header Hierarchy**:
   - The top header SHALL display the TechDaily book emblem, `TechDaily` brand title, an engineering badge, and language / theme toggle controls.
   - Extraneous IDE badges, mock telemetry (`PING 18ms`), and placeholder version numbers SHALL be omitted.

2. **Sub-Header Status Bar**:
   - The sub-header SHALL feature an operational badge `• ALL SERVICES OPERATIONAL` (in a rounded pill container with an emerald indicator dot) on the left.
   - The right side of the sub-header SHALL display `⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`.
   - Cluttered or non-standard latency indicators (`ĐỘ TRỄ 14MS` / `LATENCY 14MS`) SHALL be omitted from the status bar.

#### Scenario: User arrives at authentication page
- **WHEN** user navigates to `/login`
- **THEN** the top header displays clean TechDaily branding with language and theme toggle controls
- **AND** no extraneous `PING 18ms` badge or `v2.5.0-sys` label is visible.
- **AND** the sub-header displays `• ALL SERVICES OPERATIONAL` and `⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`.
---

### Requirement: Streamlined Single-Provider Google OAuth
The 1-click developer authentication stack SHALL be restricted to Google Sign-In, eliminating placeholder OAuth providers until backend integrations are finalized.

1. **GitHub Button Removal**:
   - The placeholder GitHub OAuth button SHALL be completely removed from the authentication card.

2. **Dedicated Google Sign-In Presentation**:
   - The Google Sign-In button SHALL render as the dedicated 1-click OAuth provider (`Google ⌘L`), cleanly integrated without multi-column blowout or overflow.

#### Scenario: User views 1-click OAuth options
- **WHEN** user views the authentication card in Sign In or Register mode
- **THEN** only the Google OAuth button is displayed
- **AND** no GitHub OAuth button is rendered.

---

### Requirement: Left Mastery Stage & TechDaily Practice Telemetry
The left column SHALL showcase the TechDaily technical documentation reading and SM-2 active recall practice model with balanced metric indicators and live practice code snippet.

1. **Module Header & Telemetry**:
   - Track badges: `TECHDAILY` and `SM-2 ACTIVE RECALL`.
   - Title: `TechDaily` daily technical documentation reading and spaced learning platform in English and Vietnamese.
   - Mini-bento metric bars: `DAILY READING TARGET` with `94% HIT` and `SM-2 SPACED REPETITION` with `LONG-TERM RETENTION`.

2. **Live Code Snippet Card**:
   - Card header: `<> TechDaily_Practice.ts` with lock icon.
   - Code block: Syntax-highlighted `techDaily.getDailySlice` daily drill execution snippet.

3. **Security Footnote**:
   - Card bottom footnote SHALL display a shield icon with `SM-2 ACTIVE RECALL & PRACTICE`.

#### Scenario: Desktop visitor views curriculum preview
- **WHEN** user views `/login` on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the left stage showcases `TechDaily` active recall badges, reading progress metrics, the `TechDaily_Practice.ts` code snippet, and the SM-2 practice footnote.
