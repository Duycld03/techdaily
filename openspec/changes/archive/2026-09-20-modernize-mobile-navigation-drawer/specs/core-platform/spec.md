# Spec Delta: Modernize Mobile Navigation Drawer

## ADDED Requirements

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
   - The drawer header SHALL display the canonical TechDaily brand mark with the Deep Iris Violet gradient: `bg-gradient-to-tr from-brand-600 via-brand-500 to-indigo-400` with white `BookOpen` icon (`w-4 h-4 :stroke-width="1.5"`).
   - The drawer brand title SHALL render `TechDaily` with the gradient text styling matching the desktop header, and SHALL NOT use hardcoded `"TechDaily Menu"` English text.
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
- **AND** the brand header renders "TechDaily" with the primary gradient
- **AND** the logout button renders localized text ("Đăng xuất") instead of hardcoded English "Log Out".
