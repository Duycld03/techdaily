# Proposal

## Why

The Command Palette search input shows an intrusive purple focus ring (`outline: 2px solid #8b5cf6`) immediately upon opening, before any user interaction. This happens because a global `input:focus-visible` rule in `main.css` (lines 38–43) applies to all focused inputs with `!important`, and Chromium always assigns `:focus-visible` to text inputs on programmatic `.focus()`. Standard Tailwind utility classes (`focus-visible:outline-none`) cannot override a global rule marked `!important`.

## What Changes

- Suppress the global focus-visible outline on the Command Palette search input by adding a scoped style block in `frontend/components/app/AppCommandPalette.vue`:
  ```vue
  <style scoped>
  input:focus-visible {
    outline: none !important;
    box-shadow: none !important;
  }
  </style>
  ```
  The scoped attribute selector (`input[data-v-xxxx]:focus-visible`) provides higher specificity (0,2,1 vs 0,1,1) with `!important`, successfully suppressing the intrusive ring without affecting any other inputs across the application.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — visual styling override, no behavior change._

## Impact

- **`frontend/components/app/AppCommandPalette.vue`**: Add `<style scoped>` block targeting `input:focus-visible`.
- **No other components affected** — other form inputs retain their global accessibility focus ring.
