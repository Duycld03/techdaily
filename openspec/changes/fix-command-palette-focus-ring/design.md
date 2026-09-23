# Design

## Context

`frontend/assets/css/main.css` applies a global accessibility focus ring on all inputs:
```css
input:focus-visible {
  outline: 2px solid #8b5cf6 !important;
  outline-offset: 2px !important;
}
```
When `AppCommandPalette.vue` opens via keyboard shortcut (Cmd+K / Ctrl+K), `searchInputRef.value?.focus()` programmatically focuses the input. Chromium automatically triggers `:focus-visible` on programmatic text input focus, causing an intrusive purple outline.

## Goals / Non-Goals

**Goals:**
- Suppress the focus-visible outline and shadow specifically on the Command Palette search input.
- Retain normal text selection, caret, and typing behavior.
- Leave global focus rings intact for all other inputs and controls.

**Non-Goals:**
- Changing the global `:focus-visible` accessibility styles in `main.css`.

## Decisions

### 1. Override via Scoped Component Styles
Tailwind utility classes like `focus-visible:outline-none` do not take precedence over an `!important` rule in external CSS.
Instead, we add a scoped `<style>` block in `AppCommandPalette.vue`:
```vue
<style scoped>
input:focus-visible {
  outline: none !important;
  box-shadow: none !important;
}
</style>
```
Vue's scoped CSS compiler rewrites the selector to `input[data-v-xxxx]:focus-visible`. With specificity `(0, 2, 1)` plus `!important`, it cleanly overrides the global `input:focus-visible` rule `(0, 1, 1)` with `!important`.

## Risks / Trade-offs

- **[Risk] Accessibility impact for keyboard-only users** → Mitigation: The Command Palette is a dedicated modal overlay where the search input is the sole auto-focused interactive element upon opening, accompanied by a search icon, placeholder text, and blinking text cursor.
