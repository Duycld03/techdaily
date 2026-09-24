# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Native Time Input Conforming to Studio Design System
The custom timepicker component (`AppTimePicker.vue`) SHALL render as an in-place native time input (`<input type="time">`) styled according to TechDaily form control standards, eliminating dropdown popovers and duplicate time displays.

1. **In-Place Input Presentation**:
   - The component SHALL render directly in the form layout without triggering floating popovers, dropdowns, or modal dialogs.
   - The input SHALL display the time in 24-hour (`HH:mm`) format internally, allowing the user's browser/OS locale to format the presentation (e.g. 12h AM/PM on US/VN systems).
   - An inset `Clock` icon SHALL be rendered inside the left edge of the input container.

2. **Form Interaction & Events**:
   - The component SHALL accept `modelValue: string | null | undefined` and safely sanitize input strings (e.g. `08:00:00` or `08:00`).
   - Editing the time SHALL emit `update:modelValue` and `change` with the updated 24-hour string format (`HH:mm`).
   - The component SHALL support a `disabled` property that renders the input in an inactive, non-interactive state.

3. **Styling & Theme Uniformity**:
   - The input container SHALL adhere to the standard TechDaily input dimensions: `h-11`, `rounded-xl`, `border-slate-200/90 dark:border-white/[0.08]`, `bg-white dark:bg-canvas-subtle`.
   - The native calendar/picker indicator icon SHALL remain clickable and adapt cleanly to Dark Mode via invert filter.

#### Scenario: User changes time via native control
- **WHEN** user selects or types a new time `20:00` into the native time input
- **THEN** the component emits `update:modelValue` with `20:00`
- **AND** the input value reflects the update immediately in-place.

#### Scenario: User navigates on mobile device
- **WHEN** user taps the time input on a touch screen
- **THEN** the device's native time picker interface appears
- **AND** confirming the selection updates the model value with zero layout shifting.
