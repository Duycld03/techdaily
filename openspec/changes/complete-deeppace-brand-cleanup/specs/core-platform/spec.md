# Core Platform Spec Delta

## MODIFIED Requirements

### Requirement: Timezone-Aware Background Push Dispatch Worker
The system SHALL run a background service (`DailyPushNotificationWorker`) executing every 15 minutes to evaluate study reminder and streak preservation alert windows against each user's local timezone.

#### Scenario: Morning study reminder dispatch within user's local window
- **WHEN** the worker evaluates a user whose local time in their configured `TimeZone` matches their `PreferredStudyTime` (within the current 15-minute slot), and the user has not completed today's reading or quiz
- **THEN** the worker dispatches a push notification with title `"DeepPace Study Time 📚"` and deep link to `/today`, recording the dispatch to prevent duplicate messages on subsequent ticks.

#### Scenario: Evening streak preservation alert
- **WHEN** the worker evaluates a user whose local time matches their `StreakAlertTime`, and the user has an active streak (`CurrentStreak > 0`) but has not yet studied today
- **THEN** the worker dispatches an urgent push alert `"Keep your {streak}-day streak alive! 🔥"` warning that the streak will expire at local midnight.

#### Scenario: Skip notification when daily activity is already completed
- **WHEN** the evaluation window matches but the user has already completed today's reading slice and challenge
- **THEN** the worker suppresses the notification, preventing unnecessary notification spam.


### Requirement: Zero-Repo-Footprint Live Production E2E Verification
The platform verification harness SHALL support running live end-to-end headless browser test suites against the production deployment (`https://deeppace.duckdns.org`) using an ephemeral runner outside the git repository (`/tmp/deeppace-live-e2e.mjs`) leveraging host pre-cached Chromium binaries without committing test scripts, configuration files, or temporary artifacts to the source repository.

The test suite SHALL validate critical production user journeys including:
1. **S1:** Authentication & Navigation Shell
2. **S2:** Reader & AI Term Explainer Modal (bilingual layout, geometry bounds, zero overflow)
3. **S3:** Notes & SM-2 Flashcard Creation (zero `[vue-i18n]` injection crash, success toast, review queue check)
4. **S4:** Settings & Web Push (schedule controls, timezone dropdown, Brave push service guidance)
5. **S5:** Decluttered Profile Form Save (absence of schedule inputs, absence of settings link, pure identity payload)

#### Scenario: Live execution of end-to-end critical journey test suite
- **WHEN** the ephemeral test runner is invoked from `/tmp/deeppace-live-e2e.mjs` against `https://deeppace.duckdns.org`
- **THEN** it executes scenarios S1 through S5 in headless Chromium resolved from host caches
- **AND** captures timestamped logs and failure screenshots in `/tmp/` upon any assertion failure
- **AND** generates an execution summary report in `/tmp/deeppace-live-e2e-report.json`.

#### Scenario: Repository cleanliness after live verification execution
- **WHEN** the live E2E test execution finishes (either passing or failing)
- **THEN** `git status` in the DeepPace repository demonstrates zero untracked test scripts, zero modified application files, and zero temporary test artifacts.

### Requirement: Pure OpenSpec Single Source of Truth Architecture
The platform SHALL maintain specifications, business constraints, and acceptance criteria exclusively within modular domain capability files in `openspec/specs/<capability>/spec.md` and active lifecycle changes in `openspec/changes/`. Monolithic legacy documentation directories (`docs/`) SHALL be completely retired, preventing desynchronization, dual sources of truth, and outdated developer context.

#### Scenario: Developer or AI agent inspects platform specifications
- **WHEN** a developer or AI agent queries platform capabilities, business rules, or acceptance criteria
- **THEN** specifications are resolved exclusively from modular domain files in `openspec/specs/` and active changes in `openspec/changes/`
- **AND** static legacy documentation directories such as `docs/` are absent from the repository.

#### Scenario: Developer or AI agent consults architecture invariants
- **WHEN** an AI agent or developer inspects non-negotiable architectural rules and anti-patterns
- **THEN** invariants are resolved from `AGENTS.md` (Sections 2 and 3) without requiring or loading monolithic markdown files (`docs/features.md`, `docs/api-design.md`, `docs/database-design.md`).

#### Scenario: Executable contracts serve as database and API truth
- **WHEN** an engineer or agent inspects endpoint signatures or data models
- **THEN** strongly-typed code in `DeepPace.Api/Endpoints/`, OpenAPI/Swagger specifications, EF Core Entity models, and database migrations serve as the definitive, executable implementation contracts.

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
     - Prominent DeepPace monogram/logo with subtle glowing dot indicator.
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
     - Floating Dropdown Popovers (`AppSelect.vue`, `AppTimePicker.vue`): `z-[60]`
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

12. **Native Form Controls & Color-Scheme Alignment**:
    - The application root (`html.dark`, `.dark`) and native input controls (`input[type="time"]`, `input[type="date"]`, `input[type="datetime-local"]`) SHALL declare `color-scheme: dark;` when operating in dark mode, ensuring that native browser popup dialogs and User-Agent Shadow DOM pickers render with dark background surfaces and high-contrast text.
    - Light mode (`html:not(.dark)`) SHALL declare `color-scheme: light;`.
    - Native calendar and clock picker indicators (`::-webkit-calendar-picker-indicator`) SHALL render with high contrast, brand accent styling, and `cursor: pointer` in dark mode.
    - All workspace loading states SHALL utilize dedicated `Loader2` spinners (`class="animate-spin"`) with 1.5px stroke weight, eliminating decorative sparkle animations for loading processes.

13. **Studio Native Time Input Architecture (`AppTimePicker.vue`)**:
    - The application SHALL provide a dedicated `AppTimePicker.vue` component rendering as an in-place native time input (`<input type="time">`) styled according to DeepPace form control standards, eliminating dropdown popovers, floating overlays, and duplicate time displays.
    - The input container SHALL adhere to standard input dimensions (`h-11`, `rounded-xl`, `border-slate-200/90 dark:border-white/[0.08]`, `bg-white dark:bg-canvas-subtle`) with an inset 1.5px stroke `Clock` icon on the left.
    - The component SHALL accept `modelValue: string | null | undefined`, sanitize input strings (`HH:mm`), and emit `update:modelValue` and `change` with 24-hour formatted time upon selection.
    - The native calendar/picker indicator icon SHALL remain clickable and adapt cleanly to Dark Mode via invert filter.
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

#### Scenario: User changes time via native AppTimePicker control
- **WHEN** user selects or types a new time `20:00` into the native time input on `/settings`
- **THEN** the component emits `update:modelValue` with `20:00`
- **AND** the input value reflects the update immediately in-place.

#### Scenario: User navigates native AppTimePicker on mobile device
- **WHEN** user taps the time input on a touch screen
- **THEN** the device's native time picker interface appears
- **AND** confirming the selection updates the model value with zero layout shifting.

### Requirement: Application Shell Navigation & Brand Logo Routing
The application shell topbar (`AppHeader.vue`) SHALL provide intuitive, direct navigation back to the user's primary dashboard overview. Clicking the primary DeepPace brand logo and monogram in the top header SHALL navigate to the root Home Bento Dashboard (`/`) rather than an internal practice subroute.

1. **Brand Identity & Emblem**:
   - The topbar (`AppHeader.vue`) and mobile navigation drawer SHALL render the **Stitch Developer Emblem**:
     - Squircle boundary with dark obsidian gradient (`#151419` to `#08080a`) and subtle violet border stroke.
     - Symmetrical regular hexagon ring with electric violet gradient (`#c4b5fd` to `#5b21b6`).
     - Inner concentric hairline hexagon for architectural precision.
     - Symmetrical left and right code brackets (`< >`) flanking the core.
     - Central glowing memory nucleus node with soft ambient radial halo.
   - The brand title SHALL display "DeepPace" with high-contrast gradient text.

#### Scenario: Brand logo navigation returns user to home dashboard
- **WHEN** a user clicks the DeepPace brand logo in the top application header (`AppHeader.vue`)
- **THEN** the application navigates to the root Home Bento Dashboard (`/`) instead of `/today`.
- **AND** the emblem renders the Stitch Developer Emblem with crisp vector lines.

### Requirement: Modernized Mobile Navigation Drawer & Cross-Device Parity
The mobile slide-out navigation drawer (`AppHeader.vue`) SHALL provide 100% visual and functional parity with the desktop sidebar (`AppSidebar.vue`), adhering to the **Dev-Learning Studio** design tokens, zero-shift active link geometry, unified route structure, and complete localization across all supported locales:

1. **Navigation Structure & Route Completeness**:
   - The mobile drawer SHALL render the identical structured navigation groups as the desktop sidebar:
     - **Practice (`nav.group_practice`)**: Dashboard (`/`, `LayoutGrid`), Today's Focus (`/today`, `Target`), Roadmap (`/roadmap`, `Map`), Quiz (`/quiz`, `HelpCircle`), Spaced Review (`/review`, `Layers`).
     - **Knowledge (`nav.group_knowledge`)**: Insights (`/insights`, `Compass`), Document Library (`/library`, `BookOpen`), Study Notes (`/notes`, `Highlighter`), Knowledge Graph (`/graph`, `Network`).
     - **Account (`nav.group_account`)**: Profile (`/profile`, `User`), Settings (`/settings`, `Settings`).
   - The mobile drawer SHALL NEVER omit the root Dashboard (`/`) route.

2. **Accurate Active Route Matching**:
   - The route active check (`isLinkActive`) SHALL distinguish between `/` and `/today`:
     - When route path is `/`, only the Dashboard link SHALL be marked active.
     - The Today link SHALL NOT be marked active when the user is on the root path `/`.
     - Subroutes for `/today`, `/library` (including `/read`), and `/graph` SHALL remain active when viewing child content.

3. **Design Tokens & Surface Elevation**:
   - The mobile drawer container SHALL use unified theme tokens:
     - Drawer container: `bg-white/95 dark:bg-canvas-subtle/95 backdrop-blur-md` with `border-r border-slate-200/80 dark:border-white/[0.08]`.
     - Drawer overlay: `bg-slate-950/75 backdrop-blur-sm touch-none`.
     - Backdrop and content transitions SHALL use smooth hardware-accelerated animations (`animate-in slide-in-from-left duration-200`).

4. **Brand Header Realignment**:
   - The drawer header SHALL display the canonical DeepPace brand mark with the Deep Iris Violet gradient: `bg-gradient-to-tr from-brand-600 via-brand-500 to-indigo-400` with white `BookOpen` icon (`w-4 h-4 :stroke-width="1.5"`).
   - The drawer brand title SHALL render `DeepPace` with the gradient text styling matching the desktop header, and SHALL NOT use hardcoded `"DeepPace Menu"` English text.
   - The close button (`X`) SHALL provide accessible hover states (`hover:bg-slate-100 dark:hover:bg-canvas-elevated`).

5. **Zero-Shift Active Link Geometry**:
   - Navigation links in the mobile drawer SHALL maintain a constant 2px left border geometry across both active and inactive states:
     - Inactive: `border-l-2 border-transparent text-slate-600 dark:text-slate-400 hover:bg-slate-100/70 dark:hover:bg-white/[0.04] hover:text-slate-900 dark:hover:text-slate-100 font-medium`.
     - Active: `border-l-2 border-brand-500 bg-brand-500/10 dark:bg-white/[0.06] text-brand-600 dark:text-white font-semibold shadow-sm`.
   - Active link icons SHALL use `text-brand-600 dark:text-brand-400`; inactive icons SHALL use `text-slate-400 dark:text-slate-500`.
   - All navigation icons SHALL enforce sleek `:stroke-width="1.5"`.

6. **Localized User Profile Footer Card**:
   - When authenticated, the pinned bottom card SHALL use `bg-slate-100/90 dark:bg-canvas-elevated/80 border border-slate-200/80 dark:border-white/[0.06] rounded-2xl p-3`:
     - User initial avatar in a circular brand container.
     - User display name with truncate and high-contrast text (`text-slate-900 dark:text-white`).
     - Log Out action button localized via `$t('nav.logout')` (e.g. "Đăng xuất" in Vietnamese), with a clean icon or styled action button instead of raw hardcoded English text.
   - When unauthenticated, the bottom card SHALL render a full-width login button localized via `$t('nav.login')`.

7. **Lifecycle & Viewport Hygiene**:
   - Clicking any navigation item or close button SHALL immediately dismiss the mobile drawer and restore body scroll without horizontal shift.
   - Drawer container SHALL respect mobile dynamic viewport and safe area insets: `min-h-[100dvh] pb-[max(1rem,env(safe-area-inset-bottom))]`.

#### Scenario: Mobile drawer displays Dashboard as first item and highlights correctly
- **WHEN** a user on a mobile viewport (< 768px) navigates to `/` and opens the mobile navigation drawer
- **THEN** the first item under `nav.group_practice` is Dashboard (`nav.dashboard`)
- **AND** the Dashboard link is active with `border-l-2 border-brand-500`
- **AND** the Today link is inactive.

#### Scenario: Mobile drawer displays localized brand header and logout button in Vietnamese
- **WHEN** a user with locale set to `vi-VN` opens the mobile drawer
- **THEN** navigation groups render in Vietnamese ("LUYỆN TẬP", "TRI THỨC & GHI NHỚ", "HỆ THỐNG")
- **AND** the brand header renders "DeepPace" with the primary gradient
- **AND** the logout button renders localized text ("Đăng xuất") instead of hardcoded English "Log Out".

### Requirement: Dev-Learning Studio Visual Language & Responsive Density
The web frontend SHALL implement the Dev-Learning Studio visual language with platform-independent visual density and uniform typography metrics:
1. **Font Metrics Parity**: The application SHALL load the `Inter` webfont across all supported browsers and platforms, preventing font metric discrepancies between operating systems (such as wider `Segoe UI` tracking on Windows versus condensed `Ubuntu` on Linux).
2. **Decoupled Typography Hierarchy**:
   - **Reading Prose Scale**: Long-form curriculum document text and article paragraphs SHALL use `text-base md:text-lg` (16px–18px) with `leading-relaxed` for reading ergonomics.
   - **Interactive Control Scale**: Interactive elements, scenario options, form inputs, buttons, card headers, and UI widgets SHALL use `text-sm md:text-base` (14px–16px), strictly preventing `text-lg` from bloating interactive controls.
3. **Card & Surface Spacing Tokens**: Standard card surfaces (`.glass-card`, `.glass-panel`) SHALL use balanced padding (`p-4 sm:p-5`) with standard radius (`rounded-2xl`), eliminating disproportionate padding (`p-7`, `p-8`, `md:p-10`).
4. **Natural Viewport Flow**: Primary dashboard and studio views SHALL utilize natural vertical scrolling (`min-h-[calc(100dvh-3.5rem)]`) and SHALL NOT lock container height with `overflow-hidden` on desktop displays, ensuring all widgets remain accessible when viewports are constrained by browser chrome and taskbars.

#### Scenario: Inter font loaded uniformly across operating systems
- **WHEN** a user accesses DeepPace from any desktop operating system (Windows 11, Linux, macOS)
- **THEN** the browser loads and renders the `Inter` webfont family with consistent letter spacing, character width, and x-height metrics.

#### Scenario: Interactive controls adhere to UI typography scale
- **WHEN** viewing interactive controls, option choices, buttons, and form inputs on desktop viewports
- **THEN** text is styled between `text-sm` (14px) and `text-base` (16px) rather than scaling up to `text-lg` (18px).

#### Scenario: Dashboard widgets remain accessible on constrained desktop viewports
- **WHEN** the dashboard is viewed in a browser with bookmarks bar and OS taskbar visible (available height $\le 860\text{px}$)
- **THEN** all Bento cards (including knowledge constellation) are reachable via smooth vertical scrolling without overflow clipping.

### Requirement: Native Time Input Conforming to Studio Design System
The custom timepicker component (`AppTimePicker.vue`) SHALL render as an in-place native time input (`<input type="time">`) styled according to DeepPace form control standards, eliminating dropdown popovers and duplicate time displays.

1. **In-Place Input Presentation**:
   - The component SHALL render directly in the form layout without triggering floating popovers, dropdowns, or modal dialogs.
   - The input SHALL display the time in 24-hour (`HH:mm`) format internally, allowing the user's browser/OS locale to format the presentation (e.g. 12h AM/PM on US/VN systems).
   - An inset `Clock` icon SHALL be rendered inside the left edge of the input container.

2. **Form Interaction & Events**:
   - The component SHALL accept `modelValue: string | null | undefined` and safely sanitize input strings (e.g. `08:00:00` or `08:00`).
   - Editing the time SHALL emit `update:modelValue` and `change` with the updated 24-hour string format (`HH:mm`).
   - The component SHALL support a `disabled` property that renders the input in an inactive, non-interactive state.

3. **Styling & Theme Uniformity**:
   - The input container SHALL adhere to the standard DeepPace input dimensions: `h-11`, `rounded-xl`, `border-slate-200/90 dark:border-white/[0.08]`, `bg-white dark:bg-canvas-subtle`.
   - The native calendar/picker indicator icon SHALL remain clickable and adapt cleanly to Dark Mode via invert filter.

#### Scenario: User changes time via native control
- **WHEN** user selects or types a new time `20:00` into the native time input
- **THEN** the component emits `update:modelValue` with `20:00`
- **AND** the input value reflects the update immediately in-place.

#### Scenario: User navigates on mobile device
- **WHEN** user taps the time input on a touch screen
- **THEN** the device's native time picker interface appears
- **AND** confirming the selection updates the model value with zero layout shifting.


### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) and verification workflow SHALL enforce a strict separation between behavioral data contracts and visual layout verification, actively purging test theater assertions and mandating direct headless browser screenshot inspection:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('flex')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`) under the premise of verifying visual presentation or layout integrity.
   - All tests in the frontend test suite asserting styling classes under the pretense of UI layout verification SHALL be pruned or replaced with behavioral state assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states during async requests, and RFC 7807 error problem details.

3. **Mandatory Automated Browser Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct headless browser rendering and screenshot previews.
   - For all frontend UI modifications (pages, layouts, components), the agent or developer SHALL execute automated browser inspection before marking tasks complete:
     - Open headless Chromium via the `browser` device.
     - Inject authentic user session cookies/tokens (`deeppace_token`, `deeppace_user`) when navigating authenticated views.
     - Capture visual screenshots at both **Desktop (1440x900 or 1920x1080)** and **Mobile (390x844)** viewports.
     - Present the captured screenshots directly in the verification output.

4. **Dual-Gate Verification Invariant**:
   - Marking a frontend UI task complete (`- [x]`) in planning tasks or declaring "Implementation Complete" SHALL strictly require passing both verification gates:
     - **Gate 1 (Behavioral Data Contract):** Vitest test suite (`npm test`) passes with zero regression.
     - **Gate 2 (Visual Integrity Gate):** Headless browser screenshots captured and verified across both desktop and mobile viewports.
   - Declaring a UI task complete based solely on Gate 1 without Gate 2 is strictly prohibited.

#### Scenario: Dual-Gate verification requirement for frontend UI changes
- **WHEN** an agent completes coding changes to a Vue page, component, or layout
- **THEN** the agent executes `npm test` in `frontend/` to satisfy Gate 1 (Behavioral Data Contract)
- **AND** the agent executes automated browser screenshot capture for Desktop and Mobile viewports to satisfy Gate 2 (Visual Integrity)
- **AND** the task is only marked complete (`- [x]`) after both gates have succeeded.

#### Scenario: Agent captures automated browser screenshots across Desktop and Mobile viewports
- **WHEN** an agent performs visual verification on an updated route (e.g. `/notes`)
- **THEN** the agent launches headless Chromium, navigates to the route (injecting auth cookies if required), and takes screenshots at 1440x900 (Desktop) and 390x844 (Mobile)
- **AND** inspects the resulting images for text wrapping, layout shift, and element collision across locales before yielding to the user.

#### Scenario: Rejection of premature UI task sign-off without visual inspection
- **WHEN** an agent passes all Vitest unit tests but has not captured browser screenshots for modified UI surfaces
- **THEN** the agent SHALL NOT mark the visual verification task complete
- **AND** SHALL NOT yield "Implementation Complete" until the automated visual inspection is executed and presented.

### Requirement: Universal Scalable Vector Favicon
The application SHALL serve a high-fidelity scalable vector favicon (`/favicon.svg`) derived from the Stitch Developer Emblem to provide immediate brand recognition across browser tabs, bookmarks, and mobile PWA home screens.

1. **Vector Geometry & Legibility**:
   - The favicon SHALL use SVG vector definitions optimized for multi-resolution rendering (16x16, 32x32, 48x48, and 512x512).
   - The graphic elements (brackets, hexagon, glowing node) SHALL maintain high contrast against both dark and light browser chrome backgrounds.

#### Scenario: Browser loads site favicon
- **WHEN** any page of DeepPace is loaded in a web browser
- **THEN** the browser tab displays the Stitch Developer Emblem favicon (`/favicon.svg`).
- **AND** the icon is crisp and clearly identifiable on both dark and light browser tab bars.


### Requirement: Project Showcase Badge Presentation in Repository Documentation
The repository `README.md` SHALL feature a standardized Shields.io badge banner immediately beneath the primary document title, matching the portfolio showcase conventions. The banner SHALL display:
1. Live Production deployment URL badge (`deeppace.duckdns.org`)
2. Backend runtime badge (`ASP.NET Core .NET 10`)
3. Frontend framework badge (`Nuxt 4 / Vue 3.5`)
4. Database engine badge (`PostgreSQL 17 (pgvector)`)
5. CI/CD workflow status badge linked to GitHub Actions
6. Spec-Driven Development badge linked to OpenSpec

#### Scenario: Developer visits TechDaily GitHub repository
- **WHEN** any visitor or reviewer navigates to the root `README.md` of the repository
- **THEN** the badge row renders without broken links or missing images
- **AND** clicking the Live badge directs to `https://deeppace.duckdns.org`
- **AND** clicking the CI/CD badge directs to GitHub Actions workflows.


## ADDED Requirements

### Requirement: Canonical runtime identifiers and scoped legacy exceptions
Current application copy, test fixtures and developer/agent guidance SHALL use DeepPace and its canonical identifiers. TechDaily spellings SHALL remain only in focused migration code/tests, references to actual retained operational resources, archived history or authoritative source excerpts. Cleanup SHALL preserve executable repository/deployment links and SHALL NOT bulk-rewrite infrastructure identities.

#### Scenario: Current product and test guidance
- **WHEN** a developer follows current login, browser QA or application guidance
- **THEN** the examples use canonical DeepPace session identifiers and product terminology
- **AND** ordinary fixtures do not create a dependency on legacy migration behavior.

#### Scenario: Retained operational and historical references
- **WHEN** a reference identifies the existing GitHub repository, checkout/VPS path, compatibility hostname, database restoration source or archived change
- **THEN** cleanup retains the real identifier or historical content
- **AND** repository badges and deployment instructions still resolve to their actual resources.

### Requirement: Canonical notification defaults
Web Push payloads without an explicit tag SHALL use `deeppace-daily`; default product titles SHALL identify DeepPace. VAPID contact SHALL resolve from the supported Web Push configuration/defaults without a TechDaily address fallback. Explicit notification tags, subscriptions, VAPID keys, localization, scheduling and deep-link behavior SHALL retain their existing contracts.

#### Scenario: Notification uses default tag
- **WHEN** a notification is created without a custom tag
- **THEN** its payload uses `deeppace-daily` and its product fallback title uses DeepPace.

#### Scenario: Configured sender contact
- **WHEN** Web Push resolves its VAPID contact
- **THEN** it uses the supported configured value or canonical options default
- **AND** cleanup neither invents an unverified contact address nor changes keys or subscriptions.
