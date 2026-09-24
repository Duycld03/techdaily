# Design: Studio Auth Cockpit Canvas & Ambiance

## Context
See `proposal.md` for motivation. The design specification is anchored directly by Image #1 and `local://paste-3.md`, establishing an authentic developer cockpit atmosphere for `/login`. The existing implementation in `frontend/pages/login.vue` has a functional core but exhibits empty screen voids and a Google Sign-In hover clipping defect where the native logo container overflows with an unsightly white background ("dư 1 chút ở trên và dưới", Image #1).

## Goals / Non-Goals

**Goals:**
- Deliver exact visual fidelity matching Image #1 across Desktop (1920x1080) and Mobile viewports.
- Fix the Google Sign-In hover defect using an integrated full-width button with clean SVG mark and responsive hover shading (`dark:bg-[#202024]`).
- Enrich the canvas with ambient background layers (dot-matrix grid, iris radial ambient glow, subtle hairlines) to eliminate black voids.
- Provide a clean, focused header with brand mark, title, and language/theme switches.
- Implement left-column telemetry cards (Daily Reading Goal, SM-2 Spaced Repetition) and code window simulation (`TECHDAILY_PRACTICE.TS`).
- Maintain full bilingual localization (`en.json` and `vi.json`) and light/dark color mode support.

**Non-Goals:**
- Modifying backend authentication endpoints or database schemas (APIs in `TechDaily.Api/Endpoints/AuthEndpoints.cs` remain unchanged).
- Altering internal authenticated page layouts (e.g. `/today`, `/library`).

## Decisions

### 1. Custom Integrated Google Sign-In Button with Native Trigger
- **Problem**: When `gsi.renderButton` renders an iframe, the internal markup wraps the Google G logo in a fixed white square container. When the parent button is hovered, dark background shading highlights the white square's bounding box edges, resulting in the visible overflow glitch reported by the user ("dư 1 chút ở trên và dưới", Image #1).
- **Solution**: Replace the native iframe hover surface with an integrated, full-width custom button matching `local://paste-2.md`:
  ```html
  <button 
    @click="triggerGoogleSignIn"
    class="w-full flex items-center justify-center gap-3 py-2.5 px-4 rounded-xl bg-zinc-850 hover:bg-zinc-800 text-sm font-medium text-zinc-200 border border-white/[0.08] transition shadow-sm hover:border-white/15" 
    type="button"
  >
    <svg class="w-4 h-4 shrink-0" viewBox="0 0 24 24"><!-- Official 4-color Google G --></svg>
    <span>{{ $t('auth.google_sign_in_label') }}</span>
  </button>
  ```
  The button uses clean SVG rendering without any white background box, completely resolving the hover defect. When clicked, `triggerGoogleSignIn()` invokes `google.accounts.id.prompt()` or triggers the standard Google OAuth authorization flow.

### 2. Multi-Layer Ambient Background Architecture
- **Structure**: Rendered inside a `fixed inset-0 pointer-events-none z-0` container:
  - Engineering dot-matrix grid: `.bg-grid-dots` (`background-size: 24px 24px; radial-gradient(rgba(255, 255, 255, 0.07) 1px, transparent 1px)`).
  - Center Iris Violet Radial Glow: `w-[900px] h-[650px] bg-iris-600/15 rounded-full blur-[140px]`.
  - Accent ambient glows: `emerald-500/5 blur-[120px]` top-right, `blue-600/10 blur-[130px]` bottom-left.
  - Subtle hairline borders: Top hairline (`top-16 h-px bg-gradient-to-r from-transparent via-white/[0.08] to-transparent`) and side margin vertical hairlines.

### 3. Left Stage Learning Telemetry & Code Simulation
- **Badges**: `● TECHDAILY | SM-2 ACTIVE RECALL` pill and `v2.4-SYS` badge.
- **Metric Cards**:
  - `DAILY READING GOAL` with `94% COMPLETED` (emerald gradient progress bar).
  - `SM-2 SPACED REPETITION` with `ACTIVE RECALL` (iris purple gradient progress bar).
- **Code Simulation Window**: macOS window controls (red/amber/green dots), title `>_ TECHDAILY_PRACTICE.TS`, lock icon, and syntax-highlighted `techDaily.getDailySlice` practice code.
- **Micro-feature Chips**: Three pills below the editor: `⚡ Zero-alloc Queue`, `🧠 Ebbinghaus SM-2`, and `🛡️ Deliberate Drill`.

### 4. Right Cockpit Form Ergonomics & Mode Transition
- Segmented mode switcher with 3 tabs: `[ Sign In ]`, `[ Register ]`, `[ ↻ Recover ]`.
- Inputs with terminal prefix `>_` for email/username, lock/key icon for password, eye toggle button, and inline `Forgot password?` link.
- Action button: Full-width purple button with `↵ RETURN` shortcut badge.
- Card footer: `🛡️ SECURE & ENCRYPTED AUTH` on left, `Terms · Privacy` on right.

### 5. Clean Studio Shell
- Streamlined top header with brand emblem, title, and language/theme switches matching Image #1.
- Distraction-free canvas footer eliminating clutter and redundant telemetry lines.
## Risks / Trade-offs

- **Google OAuth Popup / Prompt**: Custom button click must invoke Google OAuth reliably. We retain `google.accounts.id.prompt()` and direct OAuth fallback to guarantee 100% login success.
- **Viewport Height on Small Screens**: The layout includes ambient layers and header/footer bars; on smaller mobile screens, the page body flows naturally with smooth scrolling while fixed ambient glows remain non-intrusive (`pointer-events-none`).
