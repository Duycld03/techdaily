# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Shadcn-Style Segmented TimePicker without Redundant Clutter
The custom timepicker component (`AppTimePicker.vue`) SHALL implement a clean, segmented time-entry interface conforming strictly to the TechDaily Design System standards.

1. **Header Clutter Elimination**:
   - The popover header SHALL display only the component title and icon (`Clock` + `Select Time`).
   - The popover header SHALL NOT display any redundant time badges duplicating the trigger button value.

2. **Unified Segmented Inputs**:
   - The component SHALL render two uniform numeric input fields for Hours and Minutes (`w-16 h-12 rounded-xl text-xl font-bold font-mono`).
   - Typing 2 digits into the Hours input SHALL automatically shift focus to the Minutes input.
   - Pressing ArrowRight from Hours SHALL shift focus to Minutes. Pressing ArrowLeft from Minutes SHALL shift focus to Hours.
   - Pressing ArrowUp / ArrowDown or scrolling the mouse wheel on an input SHALL increment / decrement its value with wrap-around boundaries.

3. **Horizontal Segmented Period Control**:
   - The Period selection SHALL be rendered as a horizontal segmented control (`h-12 rounded-xl p-1`) featuring two symmetrical buttons (`AM` and `PM`).
   - The active period button SHALL be styled with active elevation and contrast (`bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border border-slate-200/60 dark:border-white/[0.08]`).

4. **Zero Visual Noise**:
   - The component SHALL NOT render external chevron stacks (`▲` / `▼`) or preset shortcut chips.
   - Total popover height SHALL measure $\le 160\text{px}$.

#### Scenario: User navigates time segments with keyboard
- **WHEN** user focuses the Hours input and types `0` then `9`
- **THEN** Hours is set to `09` and focus automatically moves to Minutes
- **AND** user can type `45` to complete setting `09:45`.

#### Scenario: User toggles AM/PM via horizontal segmented switch
- **WHEN** current period is `AM` and user clicks `PM`
- **THEN** period switches to `PM`
- **AND** the active highlight smoothly updates without layout shift.
