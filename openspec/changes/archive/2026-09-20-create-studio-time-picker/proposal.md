# Proposal: Create Dev-Learning Studio AppTimePicker Component

## Why

While native HTML5 temporal inputs (`<input type="time">`) respect `color-scheme: dark;` for broad color inversion, the internal browser popup remains locked in the User-Agent Shadow DOM. In Chromium and WebKit desktop browsers, this produces an unstyled, square-cornered, blue-highlighted (`#8ab4f8`) dialog that starkly contrasts with TechDaily's sleek Dev-Learning Studio aesthetic (Obsidian `#09090b` canvas, Deep Iris Violet `#7c3aed`, rounded-2xl glassmorphism cards, and hairline borders).

Just as `AppSelect.vue` replaced raw OS `<select>` elements with a studio-grade floating listbox, TechDaily requires a dedicated `AppTimePicker.vue` component. This delivers an elegant, brand-aligned time selection experience with quick-preset shortcuts, auto-flipping viewport collision protection, and 100% drop-in `v-model` (`HH:mm`) compatibility.

## What Changes

- **New `AppTimePicker.vue` Component (`frontend/components/common/AppTimePicker.vue`)**:
  - **Trigger Button**: Styled studio pill with subtle hairline border, `Clock` icon (1.5px stroke weight), formatted time readout (`08:00 AM`), and purple active focus ring.
  - **Floating Glassmorphic Popover**: Teleported to `document.body` (`z-[60]`) with fixed viewport coordinates, smooth scale transition, and auto-flip collision avoidance when rendered near viewport boundaries.
  - **Studio Column Picker**:
    - Hours column (`01` to `12`) with smooth scrollable pill buttons.
    - Minutes column (`00` to `55` in 5-minute increments) with smooth scrollable pill buttons.
    - Period segmented control (`AM` / `PM`) with instant toggle buttons.
    - Active selection highlights rendered in Deep Iris Violet (`bg-brand-600 text-white shadow-sm`).
  - **Quick Schedule Presets**: 1-click preset chips for common developer study times (`07:00 AM`, `08:00 AM`, `08:00 PM`, `09:00 PM`).
  - **Interaction & Accessibility**: Outside-click detection via VueUse (`onClickOutside`), `Escape` key dismissal, and window scroll repositioning.
  - **Contract**: Accepts and emits standard `HH:mm` string format (e.g. `'08:00'`, `'20:00'`) via `v-model`.
- **Settings View Migration**:
  - Replace `<input type="time" v-model="preferredStudyTime" />` and `<input type="time" v-model="streakAlertTime" />` in `frontend/pages/settings.vue` with `<AppTimePicker>`.
- **Zero Breaking Changes**:
  - Completely non-breaking frontend component. No database, schema, or backend API modifications.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update Dev-Learning Studio design system requirements to specify the `AppTimePicker` component and its floating glassmorphic popover behavior.

## Impact

- **Affected Files**:
  - `frontend/components/common/AppTimePicker.vue` (new)
  - `frontend/pages/settings.vue`
- **Specs**: `openspec/specs/core-platform/spec.md`.
- **User Experience**: Completely eliminates unsightly raw browser popups, providing a unified, fluid Dev-Learning Studio experience across dark and light modes.
