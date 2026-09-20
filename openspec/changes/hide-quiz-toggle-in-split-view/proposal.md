# Proposal

## Why

On desktop (md+) screens, when the Today page displays both the Reader and Challenge Dock panels side by side, the purple toggle button (Terminal icon) in the header toolbar is redundant — the quiz panel is already visible. On mobile, the tab switcher already handles panel switching, making the button equally unnecessary there. Hiding it when the dock is open declutters the header toolbar.

## What Changes

- Hide the Scenario Challenge Dock toggle button (`isChallengeDockOpen` toggle) when the dock is already open. The button remains visible only when the dock has been collapsed, allowing users to re-open it.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — UI visibility tweak for an existing toggle button, no behavior change._

## Impact

- **`frontend/pages/today.vue`**: Add `v-show="!isChallengeDockOpen"` to the toggle button (lines 314–328). No other files affected.
