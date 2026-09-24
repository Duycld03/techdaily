# Technical Design: Native Studio Time Input

## Context

Previous iterations used custom popover floating components. This led to:
- Duplicate time displays (time shown on trigger button, and again inside the popover).
- Visual noise from non-standard button clusters.
- Awkward floating z-index and collision mechanics.

This design implements the **Shadcn Native Time Input** pattern: an in-place `<input type="time">` styled with Tailwind CSS, delivering native platform speed, zero popup overhead, and identical styling to all other form inputs in TechDaily.

## Architecture

```
+-----------------------------------------------------------+
| (clock icon)   08:00 PM                                   |
+-----------------------------------------------------------+
```

## Key Decisions

1. **Native HTML5 Time Input (`type="time"`)**:
   - Browser natively manages segmentation, direct typing, arrow key increments, and AM/PM transitions.
   - Mobile browsers (Safari on iOS, Chrome on Android) automatically provide native wheel/clock pickers.
   - On Windows 11 Chrome/Edge, clicking the clock indicator opens the native Windows flyout.

2. **Styling & Design System Consistency**:
   - Height: `h-11` (matches all text inputs in `/settings` and `/login`).
   - Border radius: `rounded-xl` (12px).
   - Inset icon: Lucide `Clock` placed at `left-3.5` with `pointer-events-none`.
   - Padding: `pl-10 pr-3.5`.
   - Dark mode webkit indicator: `dark:[&::-webkit-calendar-picker-indicator]:invert` ensures the native clock indicator is clearly visible on dark canvas.

3. **Value Sanitization & Emits**:
   - `modelValue` accepts `HH:mm` or `HH:mm:ss`.
   - Computed `formattedTime` normalizes any input to `HH:mm` (e.g., `'08:00:00'` -> `'08:00'`).
   - `@input` and `@change` emit `update:modelValue` and `change` with valid `HH:mm` strings.

## Risks / Trade-offs

- **Risk**: Native time input appearances vary slightly across browser engines (Chromium vs Firefox vs WebKit).
  - **Mitigation**: Standard Tailwind input styling normalizes padding, borders, typography, and background across all browsers; internal spin/picker controls conform to each OS's native guidelines.
