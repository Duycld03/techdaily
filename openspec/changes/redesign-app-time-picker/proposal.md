# Proposal: Redesign AppTimePicker to Align with Dev-Learning Studio Design System

## Why

The current `AppTimePicker.vue` component popover dialog has an unpolished, dated appearance with sunken scroll containers, an unbalanced AM/PM column, harsh neon button highlights, and clunky action buttons that deviate significantly from TechDaily's **Dev-Learning Studio** visual language. Redesigning this primitive aligns time selection controls across the Settings page and notification scheduling with our cohesive glassmorphic aesthetic, concentric radii, micro-interaction standards, and bilingual layout invariants.

## What Changes

- **Redesign Popover Surface**: Upgrade popover container to standard `.glass-panel` with obsidian elevation (`dark:bg-canvas-elevated`), translucent hairline borders (`dark:border-white/[0.08]`), `rounded-2xl` concentric curvature, and balanced backdrop blur.
- **Unified Column Architecture**: Replace separated, sunken scrollboxes with a unified columns layout. Column headers use refined micro-typography (`text-[10px] font-bold tracking-wider text-slate-400 dark:text-slate-500 uppercase`).
- **Balanced AM/PM Segmented Selector**: Replace the awkward, half-empty vertical AM/PM column with an aligned vertical segmented control matching the height and curvature of adjacent hour/minute lists.
- **Design System Active States**: Replace harsh solid purple selection fills with refined studio brand states (`bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border border-brand-500/30` or subtle active contrast), eliminating jarring visual spikes.
- **Refined Action Footer & Done Button**: Modernize the bottom "Done" / "Xong" action button to a clean, compact studio button (`h-8 text-xs font-semibold rounded-xl`), maintaining touch target safety without excessive visual weight.
- **Trigger Button Conformance**: Ensure trigger button matches Density 8/10 baseline (`px-3.5 py-2 rounded-xl text-xs sm:text-sm font-semibold`), with monospace tabular time formatting (`font-mono tabular-nums`).
- **Living Showcase Integration**: Feature the redesigned `AppTimePicker` in `/showcase` for ongoing visual regression testing.

## Capabilities

### Modified Capabilities
- `core-platform`: Standardize time selection controls on `AppTimePicker.vue` conforming to Dev-Learning Studio glassmorphic styling, keyboard navigation, and bilingual localization.

## Impact

- `frontend/components/common/AppTimePicker.vue`: Visual structure, popover layout, active states, and action footer.
- `frontend/pages/settings.vue`: Study time and streak alert scheduling controls gain immediate visual polish.
- `frontend/pages/showcase.vue`: Time picker interactive demo added to showcase.
- `frontend/tests/components/AppTimePicker.spec.ts`: Unit tests verifying time selection, keyboard accessibility, and DOM events.
