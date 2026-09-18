# Proposal

## Why

When switching tabs, segment controls, language options, or sidebar navigation links across the platform, the previously active element flashes a prominent light-gray border or causes a subpixel layout jitter for ~150ms before disappearing. This visual artifact is caused by asymmetric border states and unconstrained transitions:
1. Active elements apply a visible border (e.g. `border border-white/[0.12]` or `border-l-2 border-brand-500`) while inactive elements omit the border class altogether. Under `transition-all`, removing the border class causes the element's border color to momentarily revert to Tailwind's preflight default (`rgb(229, 231, 235)`) while `border-width` collapses from 1px/2px to 0px, producing a bright flash against the dark canvas.
2. Unconstrained `transition-all` on elements with dynamic box shadows (`shadow-sm`) or font weights causes intermediate rendering glitches during rapid mouse clicks and focus/blur transitions.

## What Changes

- **Constant Border Geometry**:
  - Tab switchers (`insights.vue`, `library.vue`, `quiz.vue`, `review.vue`, `profile.vue`): Maintain `border border-transparent` on base/inactive states so `border-width` is constant at 1px and only `border-color` transitions.
  - Sidebar navigation links (`AppSidebar.vue`): Maintain `border-l-2 border-transparent` on base/inactive state so `border-left-width` remains constant at 2px without 2px layout jitter.
  - Reader pacer chunk buttons (`today.vue`): Maintain `border-l-2 border-transparent` on base/inactive state.
  - Locale switcher (`LocaleSelector.vue`): Maintain constant border/shadow geometry.
- **Constrained Transitions**:
  - Replace `transition-all` with `transition-colors` across all tab switchers, sidebar links, and segment buttons, preventing accidental animation of `border-width`, `box-shadow`, `font-weight`, or layout dimensions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `insights`: Refine requirement `Tech Insights Feed Data Model & Query API` for zero-width-shift border transitions on the View Mode Switcher.
- `core-platform`: Refine requirement `Grouped Navigation Standard` and system UI controls to mandate constant border geometry and `transition-colors` on `AppSidebar.vue` and `LocaleSelector.vue`.

## Impact

- **Visual Quality**: Completely eliminates border flashing, color fallback glitches, and layout shifts during tab, route, and locale transitions.
- **Zero Breaking Changes**: No changes to APIs, stores, routes, or functional logic.
- **Test Compatibility**: Preserves 100% pass rate on all existing frontend tests.
