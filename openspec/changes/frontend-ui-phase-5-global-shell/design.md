# Design: Frontend UI Phase 5 - Global Shell, Navigation & Authentication

## Context

See `proposal.md - Why` for motivation.

The global application shell components (`AppHeader.vue`, `AppSidebar.vue`), authentication surface (`login.vue`), and settings surfaces (`settings.vue`, `MasterDetailLayout.vue`) establish the visual framework for TechDaily. In Phase 5, we polish these persistent container surfaces to eliminate density discrepancies, uneven edge padding, and modal/drawer transition hitches across Windows 11 desktop, macOS, and mobile viewports.

## Goals / Non-Goals

**Goals:**
- Standardize `AppHeader.vue` and `AppSidebar.vue` with unified elevation, compact density, keyboard shortcut tooltips, and seamless mobile drawer transitions.
- Standardize `login.vue` with a centered, viewport-bounded glassmorphic card and zero layout shift when toggling between login and registration modes.
- Formalize `MasterDetailLayout.vue` for settings and configuration surfaces with a persistent desktop category rail (`md:w-64`) and responsive mobile tab bar degradation.
- Ensure all action buttons and badges enforce the Bilingual Responsive Layout Invariant (`whitespace-nowrap shrink-0`).

**Non-Goals:**
- Modifying backend authentication APIs, JWT token structures, or OAuth callbacks.
- Adding new settings categories or database columns.
- Altering application routing hierarchy.

## Decisions

### Decision 1: Global Navigation Chrome & Header Elevation
In `frontend/components/layout/AppHeader.vue` and `frontend/components/layout/AppSidebar.vue`:
- Standardize header height to `h-14 sm:h-15` with `sticky top-0 z-40`, glassmorphic backdrop (`bg-white/95 dark:bg-canvas/80 backdrop-blur-md`), and hairline border (`border-slate-200/80 dark:border-white/[0.08]`).
- Telemetry widgets (Streak counter, Theme switch, Locale toggle, User Avatar dropdown) maintain compact, touch-friendly padding.
- Desktop sidebar maintains `w-64 shrink-0` with semantic navigation groups (`nav.group_learn`, `nav.group_knowledge`, `nav.group_system`), active indicator pills with electric violet accents, and `whitespace-nowrap shrink-0` on all labels.
- Mobile drawer uses `z-50` with safe-area insets, body scroll lock, and ESC key listener.

### Decision 2: Centered, Viewport-Bounded Authentication Surface
In `frontend/pages/login.vue`:
- The authentication container uses `min-h-[calc(100dvh-4rem)] flex items-center justify-center p-4 sm:p-6` to guarantee vertical centering without cut-off action buttons.
- Constrain the card to `w-full max-w-md rounded-3xl p-6 sm:p-8` with glassmorphic styling (`dark:bg-canvas-subtle/80 backdrop-blur-xl border border-slate-200/90 dark:border-white/[0.08] shadow-2xl`).
- Mode switcher tab bar ("Login" vs "Register") uses pill button styling with fixed container geometry to prevent layout displacement or height jumps when toggling.

### Decision 3: MasterDetailLayout Standards & Settings Polish
In `frontend/components/layout/MasterDetailLayout.vue` and `frontend/pages/settings.vue`:
- Desktop navigation rail (`md:w-64 shrink-0`) renders category items with icons and active pills (`bg-brand-500/10 text-brand-600 dark:text-brand-400 font-bold`).
- Mobile navigation degrades to a horizontal scrollable tab bar (`flex md:hidden overflow-x-auto gap-2 p-2 border-b`) to preserve full width for form inputs.
- Settings content panel (`flex-1 min-w-0 p-4 sm:p-6`) organizes form sections into multi-column responsive grids with clear labels, helper text, and save action buttons.

## Risks / Trade-offs

- **Risk:** Mobile virtual keyboard displacing fixed header or centered auth card.
  - **Mitigation:** Use `min-h-[calc(100dvh-4rem)]` with `overflow-y-auto` so users can scroll if the soft keyboard takes screen space.
- **Risk:** Form state loss during login/register tab toggle.
  - **Mitigation:** Retain email and password form refs when toggling modes; only reset mode-specific fields (e.g. name).
