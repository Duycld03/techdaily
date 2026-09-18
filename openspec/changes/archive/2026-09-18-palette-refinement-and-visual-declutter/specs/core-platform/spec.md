# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Dev-Learning Studio Design System & Navigation Shell
The web frontend SHALL implement the **Dev-Learning Studio** visual language, replacing default Nuxt/VitePress documentation styles with a neutral obsidian dark-mode-first aesthetic, Deep Iris Violet branding, translucent hairline borders, decluttered glassmorphic surface elevations, and a keyboard-driven command navigation shell following the 60-30-10 color hierarchy:

1. **Design Tokens & Color Palette:**
   - The application theme in `tailwind.config.js` SHALL define semantic layers:
     - `canvas`: `#09090b` (main neutral dark obsidian background), `subtle: '#121215'` (primary card surface), `elevated: '#18181b'` (modals, dropdowns, popovers), `border: 'rgba(255, 255, 255, 0.08)'` (translucent hairline divider).
     - `brand`: Deep Iris Violet spectrum (`brand-500: '#7c3aed'`, `brand-400: '#a78bfa'`, `brand-600: '#6d28d9'`, `brand-glow: 'rgba(124, 58, 237, 0.35)'`).
     - `streak`: Ember Orange (`amber: '#f59e0b'`, `glow: 'rgba(245, 158, 11, 0.4)'`).
     - `cyber`: Cyber Cyan (`cyber-400: '#22d3ee'`, `cyber-500: '#06b6d4'`).
   - The typography SHALL standardize on crisp font tracking (`tracking-tight`), modern monospace accents, and responsive body scales following project typography invariants (body $\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop/tablet).

2. **Hairline Borders & Elevation Utilities:**
   - Card and panel components SHALL utilize hairline borders (`border border-white/[0.08]` in dark mode, `border-slate-200/80` in light mode) and glassmorphism backdrop blur (`backdrop-blur-md` / `backdrop-blur-lg`) to provide visual separation and depth without muddy opaque backgrounds or ambient radial blur glows.

3. **Global Command Palette Navigation (`AppCommandPalette.vue`):**
   - The application shell SHALL include a global Command Palette modal accessible via keyboard shortcut (`Cmd+K` on macOS, `Ctrl+K` on Windows/Linux) or by clicking the topbar search input.
   - The palette SHALL support real-time fuzzy filtering of navigation destinations across core platform capabilities: Today's Reading Slice (`/`), Spaced Repetition Review (`/review`), Interview Quiz (`/quiz`), Architecture Knowledge Graph (`/graph`), Architecture Roadmap (`/roadmap`), Document Library (`/library`), Highlight Notes (`/notes`), and Settings (`/settings`).
   - The palette SHALL support keyboard navigation (`ArrowDown`, `ArrowUp`, `Enter` to navigate, `Escape` to dismiss) and touch tap on mobile devices.
   - When opened, the palette SHALL focus the search input automatically and prevent background page scrolling.

4. **Modernized Application Shell & Navigation:**
   - The topbar (`AppNavbar.vue` or header in `default.vue`) SHALL feature:
     - Prominent TechDaily monogram/logo with subtle glowing dot indicator.
     - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
     - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
     - Locale switcher (EN / VI) and user profile ring.
   - The navigation sidebar SHALL display sleek icon rail geometry, smooth collapsible state transitions, and a subtle neutral glass highlight (`bg-white/[0.06] text-white border-l-2 border-brand-500`) for the active route.

#### Scenario: User opens application in dark mode with new design tokens
- **WHEN** a user visits any page in dark mode
- **THEN** the body background is rendered with neutral dark obsidian `#09090b`
- **AND** primary buttons and active indicators display Deep Iris Violet `#7c3aed`
- **AND** card borders display translucent hairline styling (`border-white/[0.08]`) rather than solid gray borders.

#### Scenario: User triggers Command Palette via keyboard shortcut
- **WHEN** a user presses `Cmd+K` (on macOS) or `Ctrl+K` (on Windows/Linux) while on any page
- **THEN** the global Command Palette modal smoothly teleports into view
- **AND** the search input is focused immediately with the cursor ready
- **AND** background page scrolling is temporarily disabled.

#### Scenario: User searches and navigates via Command Palette
- **WHEN** the Command Palette is open and the user types `"graph"`
- **THEN** the results list filters instantaneously to show the Knowledge Graph destination
- **WHEN** the user presses `Enter` or clicks the result
- **THEN** the Command Palette closes
- **AND** the router navigates immediately to `/graph`.

#### Scenario: User closes Command Palette via Escape or backdrop click
- **WHEN** the Command Palette is open and the user presses `Escape` or clicks outside the modal
- **THEN** the modal closes cleanly and restores page focus without errors.

#### Scenario: Topbar search trigger on mobile viewport
- **WHEN** a user views the application on a mobile screen ($< 640\text{px}$)
- **THEN** the topbar renders a compact search icon trigger button
- **WHEN** tapped, it opens the full-screen or centered Command Palette.

---

### Requirement: Home Command Center Dashboard & Zero-Scroll Desktop Layout
The root route `/` SHALL host the primary **Home Command Center Dashboard** (`frontend/pages/index.vue`), presenting an executive overview of daily momentum, active reading slice, scenario drill, retention metrics, 7-day consistency, and knowledge cosmos connectivity with clean visual decluttering:

1. **Information Architecture & Contextual AI Restraint:**
   - **Welcome Banner:** Displays personalized greeting (`"Welcome Back, {Name}!"`) and curriculum progress pill (`"Curriculum Day {N} / 30"`). The banner SHALL NOT include an "Ask AI Explainer" button; AI explanation is strictly reserved for in-context reading text selection and scenario problem solving. The banner SHALL NOT display ambient radial blur glows.
   - **Active Reading Hero:** Displays active curriculum slice title, summary, reading time estimate, progress percentage, and a prominent `"Continue Reading →"` CTA button navigating to `/today`. The category badge SHALL render in neutral slate monospace (`text-slate-400 font-mono text-[11px]`) and icon wrapper SHALL use neutral dark glass (`bg-white/[0.04] text-slate-400`).
   - **Scenario Challenge Card:** Displays the architectural interview scenario teaser, score reward (`+10 Points`), and a `"Solve Challenge →"` CTA button navigating to `/today`.
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Knowledge Graph Radar:** Displays connected concept counts and active relation counts with a direct link to the 3D Cosmos (`/graph`).

2. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container and column flexboxes SHALL be top-aligned (`justify-start`) with consistent, snug vertical gaps (`gap-3.5 sm:gap-4`), preventing cards from scattering or dispersing to the vertical extremes on tall displays while fitting entirely within the viewport (`h-[calc(100vh-3.5rem)]`) with zero required scrolling.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

3. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.

#### Scenario: Authenticated user visits root route /
- **WHEN** an authenticated user navigates to `/`
- **THEN** the system renders the Home Command Center Dashboard
- **AND** the Welcome Banner displays the user's greeting without an AI explainer button
- **AND** all metrics and active curriculum cards populate.

#### Scenario: Single-screen desktop presentation
- **WHEN** the dashboard is viewed on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the entire dashboard container fits within the viewport height without vertical scrolling
- **AND** the Concentric Metric Card, 7-Day Consistency Matrix, and Knowledge Graph Radar are simultaneously visible above the fold.

#### Scenario: User clicks Continue Reading on Home Dashboard
- **WHEN** the user clicks "Continue Reading" on the active reading slice card
- **THEN** the router navigates to `/today`
- **AND** the Focus Studio reading pane renders the active slice.
