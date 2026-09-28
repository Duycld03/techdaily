# Design

## Context

See `proposal.md` for motivation. In the earlier change `2026-09-19-fix-tab-switcher-border-transition-flicker`, top-level segment switchers and navigation sidebar links were updated to use constant border baselines and `transition-colors`. However, several category pill rows and sub-modal tab switchers were overlooked or regressed:

1. **`frontend/pages/library.vue` (Category Filters Bar)**:
   - Toggles `font-bold` (active) versus `font-medium` (inactive). Because bold glyphs have larger horizontal advance metrics than medium glyphs, selecting any tab widens it by several pixels.
   - Inside a flex row (`flex flex-wrap items-center gap-2`), widening the active button immediately pushes every sibling button to its right, causing visible layout jitter.
   - Combines this width change with `transition-all`, producing animated box expansion and border flashing.
2. **`frontend/pages/insights.vue` (Topic Filter Chips & Code Solution/Problem Tabs)**:
   - Topic filter buttons and code pane tabs continue to use `transition-all` instead of scoped `transition-colors`.
3. **`frontend/pages/notes.vue` & `review.vue` (Modal Tab Switchers & Filter Chips)**:
   - Modal headers toggle `font-bold` on active tabs under `transition-all`.

## Goals / Non-Goals

**Goals:**
- Eliminate layout shifts, text width jumps, and border flicker when switching category filter pills on `/library`.
- Enforce constant font weight (`font-semibold`) across active and inactive states in `library.vue`, `notes.vue`, and `review.vue`.
- Constrain all tab and filter chip transitions to `transition-colors duration-150`.
- Maintain exact visual contrast, active brand tokens, and dark/light mode palette fidelity.

**Non-Goals:**
- Altering filter logic, pinia store actions, URL query parameters, or API request pipelines.
- Redesigning the layout hierarchy or replacing Tailwind utility classes with external CSS libraries.

## Decisions

### 1. Invariant Font Weight on Interactive Filter Buttons
- **Decision**: Declare `font-semibold` in the base class for category filter pills and tab switchers. Omit `font-bold` or `font-medium` from dynamic state bindings.
- **Rationale**: CSS text-rendering engines calculate inline text bounding boxes strictly based on font weight glyph metrics. Keeping font weight identical across active and inactive states guarantees that button width ($W = 2 \times \text{padding} + \text{textWidth} + 2 \times \text{border}$) remains invariant, permanently preventing layout shift of adjacent flex items.

### 2. Transition Property Restriction (`transition-colors`)
- **Decision**: Replace `transition-all` with `transition-colors duration-150` on all category filter chips, segment buttons, and modal sub-tabs.
- **Rationale**: `transition-all` interpolates every animatable CSS property, including box-shadow, border-width, font-weight, padding, and margin. Restricting transitions to `transition-colors` ensures that only background, text, and border colors interpolate over 150ms, with zero geometry or subpixel rendering artifacts.

### 3. Preserved 1px Border Geometry
- **Decision**: Pre-allocate a 1px border baseline (`border`) on the base button class, overriding only border colors between active and inactive states:
  - Active: `border-slate-300 dark:border-white/[0.12] bg-slate-100 dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm`
  - Inactive: `border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:border-slate-300 dark:hover:border-white/[0.16]`
- **Rationale**: Matches the platform's Dark Mode palette (`#09080e` canvas, hairline borders) while guaranteeing no border-width collapse occurs during transitions.

## Risks / Trade-offs

- **Zero Functional Risk**: Styling changes are purely presentation-level. Event listeners (`@click="handleCategorySelect(cat.id)"`), test selectors, and accessibility attributes remain 100% intact.
- **Visual Polish**: Users experience instantaneous, stable feedback when toggling categories without distracting layout twitch or border flash.
