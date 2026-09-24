# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Stepper and Direct Input Time Selection for AppTimePicker
The `AppTimePicker.vue` component SHALL provide a digital stepper and direct-input interface for selecting hours, minutes, and AM/PM periods, eliminating scrollable columns and wheel-based scrubbing.

1. **Digital Segment Inputs**:
   - The component SHALL display two editable numeric inputs: one for Hours (1 to 12) and one for Minutes (00 to 59), alongside a Period toggle button (AM or PM).
   - Typing into the Hours field SHALL restrict input to digits and clamp/wrap values to $[1, 12]$ on blur or enter, auto-padding with leading zero.
   - Typing into the Minutes field SHALL restrict input to digits and clamp/wrap values to $[0, 59]$ on blur or enter, auto-padding with leading zero.
   - Clicking on the Period button SHALL toggle between `AM` and `PM`.

2. **Stepper Increment / Decrement Controls**:
   - Each segment SHALL have dedicated Up and Down stepper buttons.
   - Clicking Hour Up SHALL increment the hour by 1 (wrapping from 12 to 1). Clicking Hour Down SHALL decrement by 1 (wrapping from 1 to 12).
   - Clicking Minute Up SHALL increment by 5 minutes (wrapping from 55 to 00). Clicking Minute Down SHALL decrement by 5 minutes (wrapping from 00 to 55).
   - Pressing ArrowUp or ArrowDown keys while an input is focused SHALL execute the respective increment/decrement action.

3. **Quick Presets**:
   - The popover SHALL render quick preset buttons for standard study times (`08:00 AM`, `12:00 PM`, `08:00 PM`, `10:00 PM`).
   - Clicking a preset SHALL immediately update the time model.

4. **Compact Dimensions**:
   - The popover SHALL NOT contain any scrollbars or overflow scroll containers.
   - Total popover height SHALL measure $\le 200\text{px}$ (target ~175px).

#### Scenario: User clicks Stepper Up on Hour
- **WHEN** current time is `08:00 AM` and the user clicks the Hour Up button
- **THEN** time updates to `09:00 AM`
- **AND** the display and model value reflect the change.

#### Scenario: User types directly into Minutes input
- **WHEN** user focuses the Minutes input and types `30`
- **THEN** time updates to `08:30 AM` immediately or upon blur.

#### Scenario: User selects a quick preset chip
- **WHEN** user clicks `12:00 PM` preset
- **THEN** time is set to `12:00 PM` and active preset is visually highlighted.
