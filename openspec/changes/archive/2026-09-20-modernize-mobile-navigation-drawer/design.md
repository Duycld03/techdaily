# Design: Modernize Mobile Navigation Drawer

## Context

The TechDaily application provides responsive navigation across desktop ($\ge 768\text{px}$) via `AppSidebar.vue` and mobile viewports ($< 768\text{px}$) via a teleported slide-out drawer inside `AppHeader.vue`. 

During past iterations, `AppSidebar.vue` was modernized with the **Dev-Learning Studio** visual standard (`canvas-subtle` backgrounds, `border-l-2 border-brand-500` zero-shift active indicators, and explicit `/` Dashboard support). However, `AppHeader.vue` retained a legacy, divergent implementation:
- It uses obsolete color tokens (`bg-slate-900`, `border-slate-800`, `emerald-400` gradient).
- It omits the Dashboard (`/`) entry in `navGroups`.
- Its `isLinkActive` method contains a bug (`linkPath === '/today'` matches `/today` or `/`), causing "Today's Focus" to be highlighted when viewing the Dashboard.
- Header and footer strings ("TechDaily Menu", "Log Out") are hardcoded in English.

See `proposal.md` for motivation and high-level requirements.

## Goals / Non-Goals

**Goals:**
- Provide a single source of truth for navigation groups and route active detection via a dedicated composable (`useNavigationMenu.ts`), ensuring `AppSidebar.vue` and `AppHeader.vue` never drift.
- Modernize the mobile drawer container, backdrop, brand header, active link indicators, and user profile footer with unified Dev-Learning Studio tokens (`canvas-subtle`, `canvas-elevated`, `border-white/[0.08]`).
- Localize all text within the drawer (brand mark, navigation groups, and logout action).
- Maintain zero layout shift and smooth touch/scroll behavior across mobile viewports.
- Update automated E2E test suites to validate mobile navigation across both English and Vietnamese locales.

**Non-Goals:**
- Altering the desktop sidebar layout or behavior (`AppSidebar.vue` already conforms to design standards).
- Adding new navigation routes or changing application information architecture.
- Altering authentication logic or backend user profile endpoints.

## Decisions

### 1. Unified Navigation Composable (`useNavigationMenu.ts`)
- **Decision**: Extract `navGroups` definition and `isLinkActive(path)` into `frontend/composables/useNavigationMenu.ts`.
- **Rationale**: `AppSidebar.vue` and `AppHeader.vue` previously maintained duplicate link structures and separate active checking logic. By centralizing them in `useNavigationMenu()`, adding or updating routes in the future will automatically apply to both desktop and mobile navigation without code duplication.
- **Alternative Considered**: Leaving separate definitions and just copying the missing `/` link to `AppHeader.vue`. Rejected because duplicate arrays will inevitably drift again during future feature additions.

### 2. Zero-Shift Left-Accent Active Link Styling
- **Decision**: Apply the desktop sidebar's constant 2px border geometry (`border-l-2 border-transparent` inactive vs `border-l-2 border-brand-500` active) with `bg-brand-500/10 dark:bg-white/[0.06]` to mobile drawer links.
- **Rationale**: Eliminates the outdated heavy solid-block highlight and provides identical visual language between mobile and desktop navigation.
- **Icon Sizing**: Use `w-4 h-4` with clean `:stroke-width="1.5"`, colored `text-brand-600 dark:text-brand-400` when active.

### 3. Drawer Header Branding Realignment
- **Decision**: Replace the hardcoded `"TechDaily Menu"` and legacy emerald gradient with the official TechDaily brand mark:
  - Gradient icon badge: `bg-gradient-to-tr from-brand-600 via-brand-500 to-indigo-400` with white `BookOpen` icon (`w-4 h-4 :stroke-width="1.5"`).
  - Brand name: `TechDaily` with gradient clip text matching `AppHeader.vue`.
  - Accessible close button with hover ring: `p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-canvas-elevated`.

### 4. Localized Profile Footer Hub
- **Decision**: Style the pinned footer user passport using `.glass-card` elevation:
  - Container: `bg-slate-100/90 dark:bg-canvas-elevated/80 border border-slate-200/80 dark:border-white/[0.06] rounded-2xl p-3`.
  - User initial avatar: `w-8 h-8 rounded-full bg-brand-600 text-white flex items-center justify-center font-bold text-xs shadow-sm`.
  - Localized logout trigger: A dedicated action button utilizing `$t('nav.logout')` (with optional `LogOut` icon) instead of unlocalized English "Log Out".
  - Unauthenticated state: A full-width login button using `$t('nav.login')` and brand gradient shadow.

### 5. E2E Test Assertion Migration
- **Decision**: Update `frontend/e2e/test-navigation-locales.mjs` to target the updated drawer title / brand heading and assert navigation to both Dashboard (`/`) and Today (`/today`) on mobile viewports.

## Risks / Trade-offs

- **E2E Test Breakage**: Existing tests specifically look for `'TechDaily Menu'`.
  - *Mitigation*: Update `frontend/e2e/test-navigation-locales.mjs` line 96 to assert on the modernized brand text and add a stable `data-testid="mobile-nav-drawer"` attribute.
- **Touch / Scroll Lock on Mobile Devices**: Locking `document.body.style.overflow` can sometimes cause scroll jumps if not cleaned up properly.
  - *Mitigation*: Use VueUse `useEventListener` or existing safe watch lifecycle hooks with `overscroll-contain` and dynamic safe-area insets (`min-h-[100dvh] pb-[max(1rem,env(safe-area-inset-bottom))]`).
