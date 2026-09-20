# Proposal

## Why

The Command Palette search overlay (`AppCommandPalette.vue`) uses `z-60` on its fixed backdrop, but Tailwind CSS's default z-index scale only defines values up to `z-50`. The `z-60` class is not recognized and produces no `z-index` declaration, causing the search overlay to render behind page content, modals, floating menus, and other elements that correctly use `z-50`.

## What Changes

- Add `z-60` to the Tailwind config `zIndex` extension so the class produces `z-index: 60`.
- Also add `z-60` for the two inline `zIndex: '60'` usages in `AppSelect.vue` and `AppTimePicker.vue` to make them consistent with the Tailwind utility approach.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — this is a CSS utility configuration fix. No spec-level behavior changes._

## Impact

- **`frontend/tailwind.config.js`**: Add `zIndex: { 60: '60' }` to `theme.extend`.
- **`frontend/components/app/AppCommandPalette.vue`**: No change needed — `z-60` will now work.
- **`frontend/components/common/AppSelect.vue`** and **`AppTimePicker.vue`**: Optionally migrate inline `zIndex: '60'` to `z-60` class for consistency.
- **No visual changes** to other components — all existing `z-40` and `z-50` usages remain valid and lower than the search overlay's `z-60`.
