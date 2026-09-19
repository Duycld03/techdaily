# Spec Delta: Core Platform

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

4. **Modernized Application Shell & Navigation (Zero-Shift Transitions):**
   - The topbar (`AppHeader.vue`) SHALL feature:
     - Prominent TechDaily monogram/logo with subtle glowing dot indicator.
     - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
     - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
     - Locale switcher (`LocaleSelector.vue`) with smooth `transition-colors` and user profile ring.
   - The navigation sidebar (`AppSidebar.vue`) links SHALL maintain a constant 2px left border geometry across both active and inactive states (`border-l-2 border-transparent` when inactive; `border-l-2 border-brand-500` when active) and constrain animations to `transition-colors`, eliminating horizontal layout shifting and border collapse flicker when navigating between routes.

5. **Viewport Scrollbar Track Stability**:
   - The root `html` container SHALL declare `scrollbar-gutter: stable`, reserving space for the vertical scrollbar track at all times.
   - When dialogs, drawers, or command palettes lock body scrolling via `overflow: hidden`, the underlying page content SHALL remain anchored in place without horizontal layout shifting (CLS) or jumping.

6. **Keyboard Accessibility & Focus Ring Standards (WCAG 2.1 AA)**:
   - Interactive elements (`button`, `a`, `input`, `textarea`, `select`, `[tabindex]`) SHALL provide prominent, high-contrast visual focus rings when navigated via keyboard (`:focus-visible`).
   - The keyboard focus ring SHALL utilize Electric Violet (`outline: 2px solid #8b5cf6; outline-offset: 2px;`) across both light and dark themes.
   - Pointer or touch click interactions SHALL NOT produce persistent sticky focus outlines, enforced via `:focus:not(:focus-visible) { outline: none; }`.

7. **Mobile Dynamic Viewport Height Standards**:
   - Full-height reading views, studio workspaces, and viewports SHALL employ dynamic viewport height units (`h-dvh` or `min-h-[100dvh]`) rather than static `h-screen` (`100vh`), preventing viewport clipping and overflow underneath mobile browser dynamic chrome (e.g. iOS Safari bottom address bar and Android navigation bars).

8. **Semantic Overlay Z-Index Stacking Hierarchy**:
   - Overlay and floating layers SHALL adhere to a deterministic, semantic z-index scale:
     - Global Toast Notifications: `z-[9999]`
     - Global Command Palette (`⌘K`): `z-60`
     - Full-screen Modals and Teleported Drawers: `z-50`
     - Contextual Popovers, Tooltips, and Floating Menus: `z-40`
     - Sticky Header and Top Navigation Bars: `z-30`
     - In-Page Floating Action Bars and Canvas Controls: `z-10`

9. **Universal Form Select & Dropdown Styling Standards**:
   - All `<select>` form controls across the application SHALL suppress native operating system and browser default styling (`appearance: none; -webkit-appearance: none; -moz-appearance: none;`).
   - All `<select>` controls SHALL feature an embedded SVG chevron dropdown indicator positioned at `right 0.75rem center` with `no-repeat` geometry, adequate right padding (`padding-right: 2.5rem`), and pointer cursor (`cursor: pointer`).
   - All `<option>` items across all browsers SHALL render with theme-calibrated backgrounds and high-contrast text (`bg-white text-slate-900` in light mode; `bg-[#18181b] text-slate-100` in dark mode).

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

#### Scenario: Modal locks body scroll without layout shift
- **WHEN** a user opens a modal, drawer, or the Command Palette (`Cmd+K`) on a desktop screen with a visible scrollbar
- **AND** the application sets `document.body.style.overflow = 'hidden'`
- **THEN** the root `html` retains its stable scrollbar gutter
- **AND** centered page containers (e.g., `max-w-6xl mx-auto`) experience zero horizontal layout shift ($0\text{px}$ shift).

#### Scenario: Keyboard user tabs through interactive elements
- **WHEN** a user navigates interactive buttons or links using the `Tab` key
- **THEN** each active element displays an Electric Violet 2px focus ring with 2px offset (`:focus-visible`)
- **WHEN** the user clicks an element with a mouse or tap pointer
- **THEN** no persistent outline or box-shadow ring remains visible.

#### Scenario: Mobile reader renders on dynamic viewport
- **WHEN** a user opens the reader view (`/read/[bookId]`) on a mobile browser with dynamic address bars (e.g. iOS Safari)
- **THEN** the reader container scales to dynamic viewport height (`h-dvh`)
- **AND** the top navigation bar and bottom pagination footer remain fully visible within the active screen area without being concealed by the browser UI.

#### Scenario: Layer stacking order across simultaneous overlays
- **WHEN** a toast notification fires while the Command Palette and a contextual popover are visible
- **THEN** the Toast (`z-[9999]`) renders above the Command Palette (`z-60`), which renders above any standard modal or drawer (`z-50`), preventing visual collision or z-index clipping.

#### Scenario: Universal form select styling across themes
- **WHEN** a user views or interacts with any `<select>` input control across the application (e.g., target role in `/profile`, book selector in `/quiz`, timezone in `/settings`, or category in `/library`)
- **THEN** default OS/browser appearance is suppressed (`appearance: none`)
- **AND** a custom theme-calibrated SVG dropdown chevron renders on the right side without colliding with option text
- **AND** dropdown `<option>` items render with crisp contrast in both Light Mode (`#ffffff` background) and Dark Mode (`#18181b` canvas-elevated background).
