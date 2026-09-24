# Proposal: Stepper & Direct-Input Architecture for AppTimePicker

## Why

The 3-column scroll well timepicker requires continuous mouse wheel or touch scrolling, which feels sluggish for keyboard-first developers and occupies vertical space even when compacted. Transitioning to a digital stepper and direct-input architecture (Click-to-Type + Up/Down buttons + Arrow Key navigation + Quick Preset Chips) provides immediate time entry, removes all scrollbars, and shrinks popover height to ~175px.

## What Changes

- **Direct Editable Numeric Inputs**: Replace scroll containers with 3 dedicated interactive input segments:
  - **Hours box**: Two-digit mono text input (01–12), click to edit, validates on blur/enter with auto-padding.
  - **Minutes box**: Two-digit mono text input (00–59), click to edit, allows any minute granularity.
  - **Period toggle**: Toggle button (AM / PM), click to switch or press Up/Down/Space.
- **Increment/Decrement Stepper Controls**:
  - Add sleek Up (`▲` / `ChevronUp`) and Down (`▼` / `ChevronDown`) buttons above and below each box.
  - Support keyboard Up/Down arrow navigation when focusing inputs.
  - Hours wrap naturally ($12 \leftrightarrow 1$). Minutes increment/decrement by 5 minutes (or 1 minute via direct input) and wrap ($55 \leftrightarrow 00$).
- **Quick Preset Chips**: Add a row of quick preset study time shortcuts (`08:00 AM`, `12:00 PM`, `08:00 PM`, `10:00 PM`) for instant 1-click selection.
- **Ultra-Compact Footprint**: Popover height drops to $\sim 175\text{px}$ (from 233px), with zero scrollbars, zero layout shift, and 100% viewport safety.

## Capabilities

### Modified Capabilities
- `core-platform`: Modernize `AppTimePicker` interaction paradigm from scroll-selection to digital stepper & direct keyboard input.

## Impact

- `frontend/components/common/AppTimePicker.vue`: Replace scroll wells with stepper inputs and preset chips.
- `frontend/tests/components/AppTimePicker.spec.ts`: Update test suite to verify direct typing, stepper button clicks, and keyboard arrow controls.
