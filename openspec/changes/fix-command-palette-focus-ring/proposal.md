# Proposal

## Why

The Command Palette search input shows an intrusive purple focus ring (`outline: 2px solid #8b5cf6`) immediately upon opening, before any user interaction. This happens because a global `input:focus-visible` rule in `main.css` (lines 38–43) applies to all focused inputs with `!important`, and Chrome/Chromium always assigns `:focus-visible` to text inputs on programmatic `.focus()` since it expects keyboard usage for text entry.

## What Changes

- Suppress the global focus-visible outline on the Command Palette search input by adding `focus-visible:outline-none focus-visible:shadow-none` classes. The input already has clear visual context (search icon, dedicated container, placeholder text) making the focus ring redundant.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — single-element CSS class fix, no behavior change._

## Impact

- **`frontend/components/app/AppCommandPalette.vue`**: Add two Tailwind classes to the search input element (line 249).
- **No other components affected** — other form inputs retain their `:focus-visible` accessibility ring.
