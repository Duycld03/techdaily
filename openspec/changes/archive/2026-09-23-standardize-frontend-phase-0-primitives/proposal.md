# Proposal: Standardize Frontend Phase 0 — Foundation & Shared UI Primitives

## Why

The shared UI primitives in `frontend/components/common/`, `frontend/components/app/`, and root shell templates (`app.vue`, `error.vue`) form the foundational design system of TechDaily. To adhere to the newly installed `antfu/skills` standards, these core components must be strictly standardized:
1. Ensure 100% Vue 3.5 Composition API with explicit TypeScript interfaces for `defineProps` and `defineEmits`.
2. Eliminate manual DOM/window event bindings in favor of VueUse composables (`useEventListener`, `onClickOutside`).
3. Audit small viewport responsiveness ($\le 375\text{px}$ down to $320\text{px}$) for floating popovers (`AppSelect.vue`, `AppTimePicker.vue`), ensuring zero horizontal overflow, no clipping on narrow displays, and full touch-target compliance ($\ge 44\text{px}$).
4. Standardize zero-shift border transitions and clean engineering iconography (`:stroke-width="1.5"`).

## What Changes

- **Root Shell & Error Boundaries (`app.vue`, `error.vue`)**:
  - Replace legacy `min-h-screen` with `min-h-dvh` to eliminate mobile browser URL bar jumpiness on mobile Safari and Chrome.
  - Standardize error page layout and back-navigation buttons with Dev-Learning Studio tokens.
- **Floating Popovers & Dropdowns (`AppSelect.vue`, `AppTimePicker.vue`)**:
  - Verify popover bounds calculations (`floatingUi` / CSS anchor) so dropdowns clamp within viewport width on 320px devices.
  - Standardize keyboard navigation (`ArrowUp`, `ArrowDown`, `Enter`, `Escape`) via VueUse `useEventListener`.
- **Global Command Palette (`AppCommandPalette.vue`)**:
  - Audit mobile presentation: ensure full-width glass modal, clean backdrop blur, and touch-friendly close action.
  - Ensure search input autofocus and escape key handling use VueUse lifecycle.
- **Shared Primitives (`BasePagination.vue`, `LocaleSelector.vue`, `StreakBadge.vue`, `ThemeToggle.vue`, `ShikiCodeBlock.vue`, `AppToastContainer.vue`)**:
  - Replace any implicit props with strict TypeScript interfaces.
  - Ensure all action buttons use `whitespace-nowrap shrink-0` with responsive gap for EN/VI text stability.
  - Enforce `:stroke-width="1.5"` for all Lucide icons.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Standardize shared UI primitive specifications regarding 320px popover containment, zero-shift borders, and VueUse event hygiene.

## Impact

- **Affected Files**: `frontend/app.vue`, `frontend/error.vue`, `frontend/components/app/AppCommandPalette.vue`, `frontend/components/common/*.vue`.
- **Testing**: Unit tests in `frontend/tests/components/` and Playwright responsive smoke tests.
