# Proposal: Compact AppTimePicker Popover Dimensions

## Why

The `AppTimePicker.vue` popover currently stretches to an excessive vertical height (>350px) because the column containers use `h-40 flex-1` without strict height clamping, displaying up to 10 hour items simultaneously and extending all the way to the bottom edge of the browser viewport. Compacting the popover height to ~210px aligns with TechDaily's Density 8/10 desktop ergonomics, prevents viewport clipping, and provides an elegant, quick-glance time selection experience.

## What Changes

- **Compact Scroll Wells**: Constrain Hour and Minute scroll tracks to a strict `h-28 max-h-28` (112px / 7rem), displaying ~3.5 to 4 items simultaneously with smooth mouse wheel and touch scrolling.
- **Symmetrical AM/PM Segmented Control**: Size the Period column container to `h-28` with two balanced pill buttons (`h-[50px]` each), preserving harmonious vertical symmetry with adjacent columns.
- **Tightened Studio Spacing**: Reduce popover inner padding to `p-3 space-y-2.5`, header divider padding to `pb-1.5`, and confirmation button height to `h-7.5 text-xs font-semibold rounded-xl`.
- **Adaptive Floating Bounds**: Adjust collision detection threshold in `updateFloatingPosition()` to `spaceBelow < 230` to smoothly position the popover above the trigger when near the bottom of the viewport.
- **Total Height Reduction**: Reduces overall popover height by ~40% (from ~350px down to ~210px), eliminating awkward vertical overflow on 1080p and smaller monitors.

## Capabilities

### Modified Capabilities
- `core-platform`: Mandate compact height constraints ($\le 220\text{px}$) and Density 8/10 vertical rhythm for timepicker primitives.

## Impact

- `frontend/components/common/AppTimePicker.vue`: Column height classes, floating offset calculation, and container padding.
- `frontend/tests/components/AppTimePicker.spec.ts`: Unit test assertions for compact classes.
