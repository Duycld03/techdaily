# Design: Standardize Dark Mode Color-Scheme and Native Form Pickers

## Context

In `frontend/assets/css/main.css`, base styles configure typography, colors, and WCAG focus rings, but completely omit the standard CSS `color-scheme` property.

Modern browsers (Chromium, WebKit, Firefox) isolate the internal popup dialogs of `<input type="time">`, `<input type="date">`, and `<input type="datetime-local">` within User-Agent Shadow DOM and native operating system UI. Standard CSS cascading rules (like `.dark body { background: ... }`) do not pierce this Shadow DOM boundary. Instead, the browser relies on `color-scheme: dark;` to render the internal calendar/clock pickers in dark mode. Without it, browsers default to `color-scheme: light;`, rendering a glaring white (`#ffffff`) popup even when the page background is `#09090b`.

In addition, `frontend/pages/today.vue:339-341` still utilizes a spinning `<Sparkles>` animation for its daily study focus initial loading state.

## Goals / Non-Goals

**Goals:**
- Globally declare `color-scheme: dark;` on `html.dark`, `.dark`, and native date/time input controls in `frontend/assets/css/main.css`.
- Declare `color-scheme: light;` on `html:not(.dark)`.
- Configure `::-webkit-calendar-picker-indicator` to ensure native clock and calendar icons have appropriate contrast and cursor pointer affordance in dark mode.
- In `frontend/pages/today.vue`, replace the spinning `<Sparkles>` icon with `<Loader2 class="animate-spin" :stroke-width="1.5">`.

**Non-Goals:**
- Introducing external Vue datepicker/timepicker npm libraries (e.g. `@vuepic/vue-datepicker` adds 50+ KB JS/CSS bundle size, breaks native mobile iOS/Android wheel picker ergonomics, and introduces unnecessary maintenance overhead).

## Decisions

### 1. Global CSS `color-scheme: dark;` vs. External Component Library

- **Decision**: Use standard W3C CSS `color-scheme: dark;` in `frontend/assets/css/main.css`.
- **Rationale**:
  - The white popup in dark mode is a well-known browser behavior: native HTML5 inputs respect `color-scheme` to theme their internal User-Agent Shadow DOM.
  - Adding `color-scheme: dark;` to `html.dark` immediately instructs Chrome, Edge, Safari, and Firefox to render the picker popup with a dark canvas (`#1f1f1f`/`#242424`), white typography, and dark-themed selection pills.
  - **Zero Bundle Overhead**: 0 KB of JavaScript added to the application.
  - **Superior Mobile UX**: Preserves native iOS and Android wheel/clock pickers, which are much faster and more accessible on touch devices than custom JavaScript dropdowns.

```css
@layer base {
  html.dark,
  .dark {
    color-scheme: dark;
  }

  html:not(.dark) {
    color-scheme: light;
  }

  .dark input[type="time"],
  .dark input[type="date"],
  .dark input[type="datetime-local"] {
    color-scheme: dark;
  }

  .dark input[type="time"]::-webkit-calendar-picker-indicator,
  .dark input[type="date"]::-webkit-calendar-picker-indicator,
  .dark input[type="datetime-local"]::-webkit-calendar-picker-indicator {
    filter: invert(0.8) brightness(1.2);
    cursor: pointer;
  }
}
```

### 2. Settings Time Picker Verification

- **Decision**: Keep the clean `<input type="time" v-model="preferredStudyTime" />` and `<input type="time" v-model="streakAlertTime" />` in `frontend/pages/settings.vue`.
- **Rationale**: With the global `color-scheme: dark;` rule in place, these inputs automatically render their picker popups with dark obsidian-compatible backgrounds, eliminating the need to rewrite the settings form logic.

### 3. Daily Focus Workspace Loader Realignment

- **Decision**: In `frontend/pages/today.vue`:
  - Import `Loader2` from `lucide-vue-next`.
  - Replace `<Sparkles class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin" />` with `<Loader2 class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin" :stroke-width="1.5" />`.
- **Rationale**: Concludes the clean engineering visual overhaul by eliminating the last spinning sparkle in core focus views.

## Risks / Trade-offs

- **Browser Differences in Native Pickers**: While Chrome/Edge renders a column-based time picker and Safari renders an inline time stepper, both strictly honor `color-scheme: dark;` and render dark themed dialogs. This delivers consistent dark aesthetics without sacrificing native performance.
