# Technical Design: Shadcn-Style Segmented TimePicker

## Context

The previous timepicker suffered from:
- Duplicate display of the selected time (`08:00 PM` on trigger + `08:00 PM` badge in header + `08:00 PM` in inputs).
- Unnecessary preset chips row (`08:00 AM`, `12:00 PM`, etc.).
- Irregular pill/ellipse button shapes with stacked chevrons looking like a bomb countdown timer.

This design aligns with the proven **Shadcn / OpenStatus Segmented Time Input** pattern, utilizing consistent `rounded-xl` containers, direct typing with auto-focus advance, mouse-wheel scrub, keyboard arrows, and a horizontal segmented AM/PM control.

## Component Layout Blueprint

```
+-------------------------------------------------------------+
| (clock) Select Time                                         |
+-------------------------------------------------------------+
|                                                             |
|       HOUR               MINUTE              PERIOD         |
|  +------------+      +------------+      +---------------+  |
|  |     08     |  :   |     00     |      |  AM   | [PM]  |  |
|  +------------+      +------------+      +---------------+  |
|                                                             |
+-------------------------------------------------------------+
|                          [ Done ]                           |
+-------------------------------------------------------------+
```

## Key Decisions

1. **Header Hygiene**:
   - Delete `<span class="tabular-nums ...">{{ formattedDisplayTime }}</span>` from the popover header.
   - Retain only the subtle clock icon and localized `Select Time` label.

2. **Clean Segmented Inputs**:
   - `hourInputRef` and `minuteInputRef` inputs.
   - Sizing: `w-16 h-12 text-center font-mono text-xl font-bold rounded-xl bg-slate-50 dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08]`.
   - On 2 digits typed in Hour: `if (val.length === 2) minuteInputRef.value?.focus()`.
   - Arrow keys: `ArrowRight` -> `minuteInputRef.value?.focus()`; `ArrowLeft` -> `hourInputRef.value?.focus()`.
   - Mouse wheel: `@wheel.prevent="handleHourWheel"` and `@wheel.prevent="handleMinuteWheel"`.

3. **Horizontal Segmented Switch (AM / PM)**:
   - Container: `h-12 flex items-center p-1 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08] gap-1`.
   - Buttons: `h-full px-3.5 rounded-lg text-xs font-bold transition-all cursor-pointer`.
   - Active: `bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border border-slate-200/60 dark:border-white/[0.08]`.

4. **Removal of Redundant Elements**:
   - Completely delete the quick preset buttons row.
   - Completely delete all 6 external chevron buttons (`▲` and `▼`).

## Risks / Trade-offs

- **Risk**: Users might wonder how to step if buttons are invisible.
  - **Mitigation**: Arrow keys ($\uparrow$/$\downarrow$) and mouse wheel directly step the numbers. In-input focus outlines and native number selection affordances are familiar to developers using Shadcn/Radix components.
