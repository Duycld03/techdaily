# Design

## Context

See `proposal.md` for motivation. On dark themes (OLED Obsidian Canvas `#09080e`), several key interactive components conditionally toggle border and layout classes between active and inactive states:
- Tab switcher buttons (`insights.vue`, `library.vue`, `quiz.vue`, `review.vue`, `profile.vue`): Active states apply a 1px border while inactive states omit border classes.
- Navigation links (`AppSidebar.vue`): Active states apply `border-l-2 border-brand-500` while inactive states omit any border class.
- Reading pacer chunks (`today.vue`): Active state applies `border-l-2 border-brand-500` while inactive states omit border classes.
- Language selector (`LocaleSelector.vue`): Buttons use `transition-all`, animating shadows, font weights, and colors during focus/blur transitions.

When switching states, removing these border classes under `transition-all` causes Tailwind's preflight default border color (`rgb(229, 231, 235)`) to show while `border-width` collapses from 1px/2px to 0px over the 150ms animation duration. On dark canvas, this renders as a bright, unpolished light-gray border flash and causes subpixel layout shifts.

## Goals / Non-Goals

**Goals:**
- Eliminate the 150ms border flash and border-width collapse when switching tabs on `insights.vue`, `library.vue`, `quiz.vue`, `review.vue`, and `profile.vue`.
- Enforce constant border geometry across both active and inactive states:
  - 1px border controls: Base class holds `border border-transparent`; active state overrides border color (`border-slate-200/80 dark:border-white/[0.12]`).
  - 2px left border controls: Base class holds `border-l-2 border-transparent`; active state overrides border color (`border-brand-500`).
- Replace `transition-all` with `transition-colors` on tab switcher buttons, sidebar links, pacer chunk buttons, and locale switcher buttons.

**Non-Goals:**
- Changing reactive state management, route destinations, or event handlers.
- Modifying visual active styling or color tokens themselves.

## Decisions

### 1. Constant Border Geometry Patterns
- **Full 1px Border (Tab Switchers & Segment Controls)**:
  Base class defines `border border-transparent`. Active state applies `border-slate-200/80 dark:border-white/[0.12]` (or appropriate active border color). Inactive state applies `border-transparent`.
- **Left 2px Border (Sidebar & Pacer Slices)**:
  Base class defines `border-l-2 border-transparent`. Active state applies `border-brand-500`. Inactive state applies `border-transparent`.

### 2. Transition Property Constriction
Replace `transition-all` with `transition-colors` across:
- `frontend/pages/insights.vue` (View Mode Switcher)
- `frontend/pages/library.vue` (Import Modal Tabs)
- `frontend/components/layout/AppSidebar.vue` (Nav link items)
- `frontend/components/common/LocaleSelector.vue` (EN / VI toggle buttons)
- `frontend/pages/today.vue` (Pacer chunk slice buttons)
- `frontend/pages/quiz.vue` (Quiz stage tabs)
- `frontend/pages/review.vue` (Review stage tabs)
- `frontend/pages/profile.vue` (Personal/Security tabs)

## Risks / Trade-offs

- **Zero Risk**: Active and inactive states retain their exact visual appearance. Only the transient state change during the 150ms transition window is stabilized, preventing layout shifts and border color flashes.
