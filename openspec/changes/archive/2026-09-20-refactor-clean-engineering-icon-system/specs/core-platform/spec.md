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
   - The palette SHALL support real-time fuzzy filtering of navigation destinations across core platform capabilities.

4. **Modernized Application Shell & Navigation (Zero-Shift Transitions):**
   - The topbar (`AppHeader.vue`) SHALL feature:
     - Prominent TechDaily monogram/logo with subtle glowing dot indicator.
     - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
     - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
     - Locale switcher (`LocaleSelector.vue`) with smooth `transition-colors` and user profile ring.
   - The navigation sidebar (`AppSidebar.vue`) links SHALL maintain a constant 2px left border geometry across both active and inactive states (`border-l-2 border-transparent` when inactive; `border-l-2 border-brand-500` when active) and constrain animations to `transition-colors`, eliminating horizontal layout shifting and border collapse flicker when navigating between routes.

5. **Universal Zero-Shift Segmented Controls & Tab Switchers:**
   - All segmented view switchers, tab bars, and mode toggles (`RoadmapViewSwitcher.vue`, `review.vue`, `quiz.vue`, `library.vue`, `profile.vue`) SHALL maintain a constant 1px border geometry across both active and inactive states (`border border-transparent` when inactive; `border border-slate-200/80 dark:border-white/[0.12]` or `dark:border-white/[0.06]` when active).
   - Inactive buttons in segmented controls SHALL pre-allocate `border border-transparent`, and transitions SHALL be strictly restricted to `transition-colors` (duration 150ms).
   - The system SHALL NEVER apply `transition-all` to segmented controls or tab switchers where border appearance or padding could be animated, completely eliminating the 1px twitch, flicker, and layout jump when switching views (e.g., between "Dạng Dòng Thời Gian" and "Dạng Sơ Đồ Tư Duy" in the roadmap).

6. **Floating Dropdown Isolation & Auto-Flip Collision Prevention:**
   - Floating dropdown popovers (`AppSelect.vue`) SHALL render via `<Teleport to="body">` with fixed positioning calculated from trigger bounding rect coordinates, rendering outside parent scroll containers (`overflow-y-auto`, `overflow: hidden`, `max-h-[90vh]`).
   - The dropdown popover SHALL NOT expand the scrollable height (`scrollHeight`) of its parent container or modal, preventing modal dialogs (`library.vue`) and page views (`settings.vue`) from spawning sudden vertical scrollbars or causing horizontal content jumps.
   - The dropdown popover SHALL automatically detect vertical viewport clearance: when the available space between the trigger bottom and the viewport bottom is insufficient (< 260px) and there is more space above, the popover SHALL flip upwards above the trigger, eliminating bottom clipping and viewport overflow.
   - When open, the floating popover SHALL update coordinates on window `scroll` (capture mode) and dismiss seamlessly on click-outside and `Escape`.

7. **Viewport & Modal Scrollbar Track Stability**:
   - The root `html` container SHALL declare `scrollbar-gutter: stable`, reserving space for the vertical scrollbar track at all times.
   - Scrollable modal dialog bodies and drawers (`overflow-y-auto`) SHALL include `scrollbar-gutter: stable`, ensuring that internal content additions or tab transitions do not produce horizontal layout shifts or jarring content reflows.

8. **Keyboard Accessibility & Focus Ring Standards (WCAG 2.1 AA)**:
   - Interactive elements (`button`, `a`, `input`, `textarea`, `select`, `[tabindex]`) SHALL provide prominent, high-contrast visual focus rings when navigated via keyboard (`:focus-visible`).
   - The keyboard focus ring SHALL utilize Electric Violet (`outline: 2px solid #8b5cf6; outline-offset: 2px;`) across both light and dark themes.
   - Pointer or touch click interactions SHALL NOT produce persistent sticky focus outlines, enforced via `:focus:not(:focus-visible) { outline: none; }`.

9. **Mobile Dynamic Viewport Height Standards**:
   - Full-height reading views, studio workspaces, and viewports SHALL employ dynamic viewport height units (`h-dvh` or `min-h-[100dvh]`) rather than static `h-screen` (`100vh`), preventing viewport clipping and overflow underneath mobile browser dynamic chrome.

10. **Semantic Overlay Z-Index Stacking Hierarchy**:
    - Overlay and floating layers SHALL adhere to a deterministic, semantic z-index scale:
      - Global Toast Notifications: `z-[9999]`
      - Global Command Palette (`⌘K`): `z-60`
      - Floating Dropdown Popovers (`AppSelect.vue`): `z-[60]`
      - Full-screen Modals and Teleported Drawers: `z-50`
      - Contextual Tooltips and In-Page Menus: `z-40`
      - Sticky Header and Top Navigation Bars: `z-30`
      - In-Page Floating Action Bars and Canvas Controls: `z-10`

11. **Clean Engineering Iconography Standard**:
    - The application iconography SHALL standardize on `lucide-vue-next` following clean engineering aesthetics:
      - **Stroke Width**: Icons on navigation bars, Bento cards, action buttons, and input controls SHALL enforce a sleek 1.5px stroke weight (`:stroke-width="1.5"`), replacing clunky default 2px lines.
      - **3-Tier Sizing**: Micro metadata and inline tags SHALL use `w-3.5 h-3.5` (14px); interactive controls, inputs, and tabs SHALL use `w-4 h-4` (16px); feature tiles and studio section headers SHALL use `w-5 h-5` (20px).
      - **Standardized Icon Tile Container**: Feature cards and section banners SHALL house prominent icons within a standard glass tile container (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400 shrink-0`).
      - **Semantic Purpose**: The `Sparkles` icon SHALL NOT be used as a generic loading spinner or catch-all decoration. Loading states SHALL use dedicated `Loader2 class="animate-spin"`. Navigation and insights features SHALL use purposeful domain icons (e.g. `Compass` for Insights).
      - **Unified Palette**: Secondary metadata icons SHALL use neutral slate tones (`text-slate-400 dark:text-slate-500`), avoiding disparate rainbow icon fills across single cards.

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

#### Scenario: User opens custom dropdown inside an overflow-y-auto modal
- **WHEN** user clicks an `AppSelect` dropdown inside the document import modal (`library.vue`)
- **THEN** the options listbox is teleported to `document.body` with fixed viewport coordinates directly aligned with the trigger button
- **AND** the modal's `scrollHeight` does not expand and no new vertical scrollbar is spawned
- **AND** the options listbox is completely visible above the modal overlay without being clipped by the modal's bottom border.

#### Scenario: User opens dropdown near the bottom of the viewport
- **WHEN** user clicks the timezone `AppSelect` near the bottom of `settings.vue` where bottom clearance is less than 260px
- **THEN** the popover automatically flips upwards above the trigger button
- **AND** the page does not expand downwards or trigger a browser scrollbar jump.

#### Scenario: User switches views in Roadmap view switcher
- **WHEN** user clicks between "Dạng Dòng Thời Gian" (Timeline) and "Dạng Sơ Đồ Tư Duy" (Mindmap)
- **THEN** both buttons maintain identical 1px border geometry (`border border-transparent` when inactive, `border dark:border-white/[0.06]` when active)
- **AND** color transitions occur via `transition-colors` without any 1px layout twitch, geometry shift, or visual flicker.

#### Scenario: User opens custom AppSelect dropdown in dark mode
- **WHEN** user clicks or taps the custom `AppSelect` trigger on `/profile`, `/settings`, `/quiz`, or `/library`
- **THEN** a floating listbox popover smoothly opens anchored below the trigger
- **AND** the popover renders with studio glass-panel elevation (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `shadow-2xl`)
- **AND** options render as styled studio cards without delegating rendering to the host operating system window manager.

#### Scenario: User navigates and selects option via keyboard
- **WHEN** the `AppSelect` dropdown is focused and user presses `ArrowDown` or `ArrowUp`
- **THEN** visual highlight moves sequentially between options with `:focus-visible` studio tokens
- **WHEN** user presses `Enter` or `Space` on an option
- **THEN** the value is updated via `v-model`, the active selection shows a checkmark indicator, and the dropdown closes.

#### Scenario: User dismisses AppSelect dropdown via Escape or outside click
- **WHEN** the `AppSelect` dropdown is open and the user presses `Escape` or clicks anywhere outside the component
- **THEN** the dropdown closes immediately and returns focus cleanly to the trigger button.

#### Scenario: Universal code block terminal surface consistency across routes
- **WHEN** a user views a code block in the GitBook reader (`/read/[bookId]`), daily focus reader (`/today`), or interview scenario challenge
- **THEN** the code block renders inside a `dark:bg-canvas-subtle` container with `dark:border-white/[0.08]` hairline border
- **AND** displays the glassmorphic terminal header with traffic-light window dots, Deep Iris Violet language telemetry, and glassmorphic copy button
- **AND** syntax highlighting renders on a transparent background matching the container obsidian canvas.

#### Scenario: Universal form select styling across themes
- **WHEN** a user views or interacts with any `<select>` input control across the application (e.g., target role in `/profile`, book selector in `/quiz`, timezone in `/settings`, or category in `/library`)
- **THEN** default OS/browser appearance is suppressed (`appearance: none`)
- **AND** a custom theme-calibrated SVG dropdown chevron renders on the right side without colliding with option text
- **AND** dropdown `<option>` items render with crisp contrast in both Light Mode (`#ffffff` background) and Dark Mode (`#18181b` canvas-elevated background).

#### Scenario: Icons render with refined 1.5px stroke weight and semantic loading spinners
- **WHEN** a user navigates between studio workspaces (Today, Quiz, Insights, Library, Reader)
- **THEN** interactive icons render with a sleek 1.5px stroke weight
- **AND** loading states display dedicated `Loader2` spinners without using spinning sparkle icons.

#### Scenario: User opens native time picker in dark mode
- **WHEN** a user clicks on a native time input control (such as preferred study time in `/settings`) while the application is in dark mode
- **THEN** the native browser dropdown picker renders in dark theme with dark canvas background and high-contrast text
- **AND** the picker does not flash a stark white (`#ffffff`) background.

#### Scenario: Daily focus workspace renders clean engineering loading state
- **WHEN** a user visits the daily focus workspace (`/today`) while daily topics are loading
- **THEN** the loading container displays a `Loader2` spinner with 1.5px stroke weight rather than a spinning sparkle icon.

#### Scenario: User opens custom AppTimePicker dropdown in dark mode
- **WHEN** user clicks the time picker trigger for preferred study time on `/settings`
- **THEN** a floating glassmorphic popover opens anchored to the trigger button
- **AND** the popover renders with studio dark elevation (`dark:bg-canvas-elevated`, `border-white/[0.08]`, `backdrop-blur-md`) without using unstyled OS browser dialogs.

#### Scenario: User selects study time via column selection
- **WHEN** user selects hour `08`, minute `00`, and period `AM` in the time picker popover
- **THEN** the active selections highlight in Deep Iris Violet (`bg-brand-600 text-white`)
- **AND** the component emits `update:modelValue` with `'08:00'`
- **AND** the trigger button immediately updates to display `08:00 AM`.

#### Scenario: User dismisses AppTimePicker via outside click or Escape
- **WHEN** the time picker popover is open and the user clicks outside or presses `Escape`
- **THEN** the popover smoothly closes without modifying the unconfirmed time value.
