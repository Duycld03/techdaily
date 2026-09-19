# Proposal: Standardize Dark Mode Color-Scheme and Native Form Pickers

## Why

When users interact with native HTML5 form pickers (such as `<input type="time">` and `<input type="date">` in `/settings`) while TechDaily is in dark mode, Chromium and WebKit browsers render the picker dropdown popup in glaring light mode with a stark white (`#ffffff`) background. This happens because the browser's User-Agent Shadow DOM renders internal pickers according to the CSS `color-scheme` property; without explicit `color-scheme: dark;` on `html.dark` or the input element, browsers default to rendering native OS/browser dialogs in light mode.

Additionally, the daily focus workspace (`frontend/pages/today.vue`) still uses a decorative spinning `<Sparkles>` icon for its initial study session loading state instead of the unified clean engineering `<Loader2>` spinner.

Standardizing `color-scheme` at the global stylesheet level resolves native date/time pickers across the entire application with 0kb bundle overhead, preserves native mobile wheel pickers, and completes the clean engineering loading standard.

## What Changes

- **Global Dark Mode Color-Scheme Integration**:
  - In `frontend/assets/css/main.css`, define `color-scheme: dark;` on `html.dark` and all native temporal input types (`input[type="time"]`, `input[type="date"]`, `input[type="datetime-local"]`).
  - Define `color-scheme: light;` for light mode (`html:not(.dark)`).
  - Style `::-webkit-calendar-picker-indicator` to ensure native calendar and clock icons render with crisp contrast in dark mode, proper brand accent color, and `cursor: pointer`.
- **Settings Time Picker Refinement**:
  - Verify that `preferredStudyTime` and `streakAlertTime` in `frontend/pages/settings.vue` render cleanly with consistent focus rings and seamless dark obsidian palette integration.
- **Daily Focus Studio Loader Realignment**:
  - In `frontend/pages/today.vue` (lines 13 & 339-341), replace the spinning `<Sparkles>` animation with the standardized `<Loader2 class="animate-spin" :stroke-width="1.5">`.
- **Zero Breaking Changes**:
  - 100% backward compatible. No backend, API, or database changes required.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update Dev-Learning Studio design system requirements to mandate `color-scheme` alignment for native browser controls and modal pickers.

## Impact

- **Affected Files**:
  - `frontend/assets/css/main.css`
  - `frontend/pages/settings.vue`
  - `frontend/pages/today.vue`
- **Specs**: `openspec/specs/core-platform/spec.md`.
- **User Experience**: Completely eliminates blinding white popups when choosing study hours in dark mode, while maintaining lightweight native performance.
