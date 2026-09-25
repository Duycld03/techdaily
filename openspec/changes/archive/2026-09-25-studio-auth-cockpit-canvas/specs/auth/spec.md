# Spec Delta: Auth Capability

## MODIFIED Requirements

### Requirement: Studio Auth Cockpit Canvas & Ambient Ambiance
The Studio Auth canvas at `/login` SHALL render an immersive developer cockpit atmosphere utilizing multi-layered ambient elements that eliminate empty black screen voids on wide viewports.

1. **Ambient Background Layers**:
   - The canvas SHALL display an engineering dot-matrix background pattern (`rgba(255, 255, 255, 0.07)`).
   - The canvas SHALL project a deep iris violet radial ambient glow behind the central content stage.
   - Subtle hairline guide lines SHALL delineate the upper and lower boundaries of the view.

2. **Responsive Split Canvas**:
   - On desktop viewports ($\ge 1024\text{px}$), the canvas SHALL display a balanced 12-column grid (`lg:grid-cols-12`) featuring the telemetry/learning stage on the left (`lg:col-span-6`) and the elevated auth cockpit card on the right (`lg:col-span-6`).
   - On mobile/tablet viewports ($< 1024\text{px}$), the canvas SHALL stack gracefully into a single-column layout without clipping or horizontal overflow.

#### Scenario: Visitor loads login page on widescreen display
- **WHEN** user navigates to `/login` on a 1920x1080 display
- **THEN** the view renders with the engineering dot-matrix grid and iris ambient glow
- **AND** the content is presented in a balanced two-column layout without unstyled black empty space.

---

### Requirement: Clean Studio Header & Language Controls
The top navigation header on the Studio Auth canvas SHALL provide a clean, distraction-free branding bar with language and theme switches.

1. **Brand Identity**:
   - The header SHALL display the TechDaily brand mark and title linking to `/login`.

2. **User Preferences**:
   - The right side of the header SHALL provide language selection (EN / VI) and color mode (Dark / Light) toggles.

#### Scenario: Visitor inspects header
- **WHEN** user loads `/login`
- **THEN** the top header displays the TechDaily brand emblem and title alongside language and theme toggle buttons.
### Requirement: Left Telemetry Stage with Metrics & Code Simulation
The left column of the Studio Auth canvas SHALL showcase TechDaily's technical reading curriculum and spaced repetition practice model through live telemetry gauges and simulated code execution.

1. **Platform Identifiers**:
   - The stage SHALL display the `● TECHDAILY | SM-2 ACTIVE RECALL` pill badge and `v2.4-SYS` version tag.
   - The headline SHALL state `Daily Technical Reading & Spaced Learning` with supporting curriculum description.

2. **Progress Metrics Card**:
   - The metrics card SHALL display `DAILY READING GOAL` with `94% COMPLETED` alongside a green gradient progress bar.
   - The card SHALL display `SM-2 SPACED REPETITION` with `ACTIVE RECALL` alongside an iris purple gradient progress bar.

3. **Code Simulation Window**:
   - The code block SHALL feature an editor title bar with three macOS-style window controls, title `>_ TECHDAILY_PRACTICE.TS`, and a lock emblem.
   - The code view SHALL render the syntax-highlighted `techDaily.getDailySlice` practice invocation snippet.

#### Scenario: Desktop visitor inspects learning preview
- **WHEN** user views `/login` on desktop
- **THEN** the left stage renders the progress gauges and the code window simulation.
---

### Requirement: Glitch-Free Full-Width Google Authentication Trigger
The 1-click Google OAuth button SHALL render as an integrated, full-width element conforming to the card's visual system, eliminating native iframe hover visual artifacts and logo bounding box overflow.

1. **Hover State Stability & Clean Geometry**:
   - The Google Sign-In button SHALL span 100% width of the card's inner content area (`w-full`).
   - The Google logo SHALL render with clean SVG geometry without any protruding white background corners ("dư 1 chút ở trên và dưới") when hovered or focused.
   - The button SHALL display localized copy (`Đăng nhập với Google` in VI, `Sign in with Google` in EN).

2. **Authentication Flow Continuity**:
   - Clicking the Google button SHALL trigger Google Identity Services (GSI) or initiate the Google OAuth sign-in flow.
   - In environments where Google Client ID is configured, credential responses SHALL be transmitted to `/api/v1/auth/google`.

#### Scenario: User hovers over Google Sign-In button
- **WHEN** user hovers over the Google Sign-In button
- **THEN** the background transitions smoothly to a hover shade
- **AND** the Google logo remains cleanly bounded with zero white box clipping or corner overflow.

#### Scenario: User clicks Google Sign-In button
- **WHEN** user clicks the Google Sign-In button
- **THEN** the system triggers Google Identity Services or opens the Google account selection prompt.

---

### Requirement: Distraction-Free Canvas Footer
The Studio Auth canvas SHALL maintain a clean layout without redundant bottom telemetry or compliance clutter, keeping the focus entirely on developer authentication and spaced learning preview.

#### Scenario: Visitor views canvas bottom
- **WHEN** user views `/login`
- **THEN** the view is clean and distraction-free without bottom telemetry bars or status spam.
