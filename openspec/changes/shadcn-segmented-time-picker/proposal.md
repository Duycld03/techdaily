# Proposal: Modern Segmented TimePicker Conforming to TechDaily Design System

## Why

The previous experimental stepper timepicker suffered from design system inconsistencies and visual clutter:
1. **Redundant Duplicate Time Badges**: The trigger button already displayed `08:00 PM`, while the popover top header unnecessarily repeated `08:00 PM` in a prominent badge.
2. **Unnecessary Shortcut Chips**: Quick preset chips (`08:00 AM`, `12:00 PM`, etc.) crowded the bottom fold without user necessity.
3. **Inconsistent Component Shapes & Bomb-Game Aesthetics**: Floating chevron pills (`▲` and `▼`), tiny buttons, and an oval AM/PM pill clashed with TechDaily's clean, developer-focused design language (looking like a digital bomb-defusal device).

Replacing this with the industry-standard **Shadcn / OpenStatus Segmented Time Input** pattern provides a unified, elegant interface: direct numeric boxes with arrow-key and scroll-wheel stepper support, a clean horizontal AM/PM segmented switch, zero duplicate badges, zero chevrons, and disciplined `rounded-xl` geometry.

## What Changes

- **Remove Duplicate Time Badge**: Strip the redundant time badge from the popover top header; keep only the clean title and clock icon (`Select Time` / `Chọn Giờ`).
- **Remove Quick Presets**: Remove the bottom 4-button shortcut chips row.
- **Eliminate Cluttered Chevrons**: Remove the floating `▲` and `▼` button stacks that caused visual distortion.
- **Adopt Shadcn / OpenStatus Segmented Input Standard**:
  - Two uniform input blocks for Hour and Minute (`w-16 h-12 rounded-xl text-center font-mono text-xl font-bold bg-slate-50 dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08]`).
  - Supports keyboard arrow keys ($\uparrow$ / $\downarrow$) and mouse-wheel scrubbing directly inside the inputs.
  - Automatic focus shifting (typing 2 digits into Hour automatically advances focus to Minute; ArrowRight / ArrowLeft navigate segments).
- **Harmonious Horizontal AM/PM Segmented Control**:
  - Horizontal switch container (`h-12 rounded-xl p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08]`) with two balanced pills (`[ AM ] [ PM ]`).
- **Ultra-Compact Height**: Popover height drops to $\sim 140\text{px}$, perfectly sized and 100% compliant with TechDaily UI design standards.

## Capabilities

### Modified Capabilities
- `core-platform`: Standardize `AppTimePicker` to the Shadcn / OpenStatus segmented design archetype with zero visual redundancies.
