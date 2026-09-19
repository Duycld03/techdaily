# Proposal: Unify Form Select & Dropdown Styling Standards

## Why

Following the recent Dev-Learning Studio UI overhaul, `<select>` form controls across the application (`profile.vue`, `quiz.vue`, `settings.vue`, `library.vue`) suffer from an unstyled visual regression: native operating system dropdown controls render with default beveled arrows, missing theme chevrons, inconsistent padding, and unstyled `<option>` lists in Dark Mode.

Because `@tailwindcss/forms` is not installed, Tailwind CSS Preflight does not normalize `<select>` controls, leaving them dependent on browser-default styles. This breaks the polished Obsidian Canvas & Deep Iris design aesthetic and creates jarring visual inconsistency for developers using the platform.

## What Changes

- **Global Base Select Normalization**: In `frontend/assets/css/main.css`, define comprehensive `@layer base` rules for `select`:
  - Suppress browser/OS default select styling (`appearance: none; -webkit-appearance: none; -moz-appearance: none;`).
  - Embed an inline SVG chevron indicator via `background-image` positioned at `right 0.75rem center` with `no-repeat` and size `1rem 1rem`.
  - Enforce standard right padding (`padding-right: 2.5rem`) so text never overlaps the dropdown indicator.
  - Enforce `cursor: pointer`.
  - Normalize `<option>` elements across browsers:
    - Light mode: `background-color: #ffffff; color: #0f172a;`
    - Dark mode: `background-color: #18181b; color: #f8fafc;`
- **Reusable Component Utility `.select-input`**: In `frontend/assets/css/main.css` `@layer components`, provide a `.select-input` class applying standard heights, typography, glassmorphic dark canvas backgrounds, hairline borders, and focus rings.
- **Form Controls Audit & Standardization**: Update all existing `<select>` elements across the frontend:
  - `frontend/pages/profile.vue`: Engineering role target selector.
  - `frontend/pages/quiz.vue`: Grounded in Book selector.
  - `frontend/pages/settings.vue`: Timezone preference selector.
  - `frontend/pages/library.vue`: Document import and PDF upload category selectors.
- **Automated Verification**: Add unit and regression tests verifying select styling across modified pages.

## Capabilities

### Modified Capabilities

- `core-platform`: Update requirement `Dev-Learning Studio Design System & Navigation Shell` to mandate universal form select and dropdown styling standards.

## Impact

- **API & State**: Zero changes to backend APIs, data models, or Pinia store state bindings (`v-model` remains identical).
- **Cross-Browser & Theme Reliability**: Ensures identical, elegant dropdown rendering on Chromium, Firefox, and WebKit across Linux, Windows, macOS, iOS, and Android.
