# Design

## Context

See `proposal.md` for motivation. TechDaily's shared components in `frontend/components/common/`, `frontend/components/app/`, `app.vue`, and `error.vue` serve as foundational blocks across all 7 platform domains. Under `antfu/skills`, these components must be audited for TypeScript interface compliance, `min-h-dvh` viewport adaptation, VueUse event hygiene, and 320px popover containment.

## Goals / Non-Goals

**Goals:**
- Enforce strict TypeScript typing (`defineProps<{ ... }>()`, `defineEmits<{ ... }>()`) across all shared primitives.
- Replace any lingering manual DOM event listeners with VueUse `useEventListener` and `onClickOutside`.
- Ensure all dropdowns and popovers (`AppSelect`, `AppTimePicker`) clamp safely within 320px screen width with touch targets $\ge 44\text{px}$.
- Replace `min-h-screen` in root layouts with `min-h-dvh` to eliminate mobile browser navigation bar jumps.
- Enforce `:stroke-width="1.5"` across all icons in common components.

**Non-Goals:**
- Modifying feature-specific business logic or stores (handled in subsequent phases).
- Redesigning color palette tokens or theme semantics (already standardized to Dev-Learning Studio).

## Decisions

### 1. Dynamic Viewport Sizing (`min-h-dvh`)
- *Rationale*: Mobile Safari and Chrome dynamically collapse URL address bars, causing `100vh` to produce vertical clipping or jumpy re-layouts. Standardizing on `min-h-dvh` ensures stable rendering.

### 2. VueUse Event Hygiene
- *Rationale*: Vue 3 components must not leave orphaned window/document listeners. All floating components will use `useEventListener(document, 'keydown', ...)` and `onClickOutside(targetRef, ...)`.

### 3. Popover Clamping on $320\text{px}$ Screens
- *Rationale*: Dropdown menus using fixed widths (e.g. `w-64`) overflow screens smaller than 375px. Popovers will use `max-w-[calc(100vw-2rem)]` or `w-full` with fixed boundary clamping.

## Risks / Trade-offs

- **Risk**: Popover positioning regressions on desktop if absolute positioning coordinates are modified carelessly.
  - **Mitigation**: Verify on both mobile (320px, 375px) and desktop (1440px) viewports.
