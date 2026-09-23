# Proposal

## Why

On mobile screens, the Today page uses the bottom navigation tab switcher to toggle between the Reader and Scenario Challenge views. Having an additional Scenario Challenge Dock toggle button in the header toolbar is redundant and takes up scarce header width.
On desktop (md+) screens, the Scenario Challenge Dock toggle button is essential: it allows users to collapse the dock into "Full Immersion Reader" mode or reopen it into a 2-column split view.

## What Changes

- Hide the Scenario Challenge Dock toggle button on mobile devices (`<md`) using `hidden md:flex`.
- Preserve the toggle button on desktop/laptop screens (`md:flex`) so users can freely open or collapse the scenario dock at any time.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — responsive UI visibility optimization for the header toolbar._

## Impact

- **`frontend/pages/today.vue`**: Set toggle button class to `hidden md:flex` instead of unconditional `flex`.
