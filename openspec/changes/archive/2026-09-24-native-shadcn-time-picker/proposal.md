# Proposal: Native Studio Time Input Conforming to Shadcn & TechDaily Design System

## Why

Custom popover-based timepickers introduced multiple UX flaws:
1. **Redundant Duplicate Time**: Displaying the time on the trigger button and repeating it immediately inside a popover dropdown.
2. **Artificial Floating Overlays**: Popover collision detection, z-index overlays, and "Done" buttons create unnecessary interaction friction.
3. **Inconsistent Component Aesthetics**: Floating custom stepper buttons clash with standard form input controls.

Adopting the standard **Native Time Input** pattern wrapped in Shadcn/Tailwind UI styling (`<input type="time">`) eliminates all popovers and duplicates, leverages native OS platform controls (Windows 11 flyout, iOS/Android wheel, macOS keyboard segments), and harmonizes 100% with the TechDaily Design System.

## What Changes

- **Replace Popover Architecture with Native Studio Input**:
  - Transform `AppTimePicker.vue` from a custom popover dropdown into an in-place native time input.
  - Wrap `<input type="time">` with studio-grade classes: `h-11 rounded-xl font-mono text-sm font-semibold pl-10 pr-3.5 border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle`.
  - Add an inset Lucide `Clock` icon with group-hover brand tint.
- **Zero Duplicate Time Display**:
  - The input directly displays and edits the time in-place with zero popup clutter.
- **Full Cross-Platform Native Accessibility**:
  - Windows 11 Chrome/Edge: Native segmented spin control and calendar flyout.
  - iOS / Android: Native high-performance mobile wheel picker.
  - Desktop keyboard: Full arrow key increment/decrement and direct numeric typing.
- **Zero Popover Overhead**:
  - Eliminates Teleport, Transition, floating positioning math, outside click listeners, and scroll listeners.

## Capabilities

### Modified Capabilities
- `core-platform`: Standardize `AppTimePicker` to an in-place native time input styled to Shadcn & TechDaily design specifications.

## Impact

- `frontend/components/common/AppTimePicker.vue`: Refactor to native styled time input.
- `frontend/tests/components/AppTimePicker.spec.ts`: Update test assertions for native time input events and attributes.
