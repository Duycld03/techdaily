# Proposal

## Why

The Command Palette search overlay (`AppCommandPalette.vue`) originally used `z-60` on its fixed backdrop, but Tailwind CSS's default z-index scale only defines values up to `z-50`. Because `z-60` was not generated, the backdrop lacked an explicit `z-index`, causing the search overlay to render behind page content, modals, floating menus, and other elements using `z-50`.

## What Changes

- Use Tailwind's arbitrary value syntax `z-[60]` directly on the fixed backdrop container in `frontend/components/app/AppCommandPalette.vue` (line 235). This reliably outputs `z-index: 60` without introducing complex or brittle Tailwind config extensions.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — CSS utility fix, no spec-level behavior changes._

## Impact

- **`frontend/components/app/AppCommandPalette.vue`**: Set backdrop class to `z-[60]` instead of unmapped `z-60`.
- **No other components affected**: Search overlay sits reliably above all headers (`z-40`), modals (`z-50`), and dropdown menus (`z-50`).
