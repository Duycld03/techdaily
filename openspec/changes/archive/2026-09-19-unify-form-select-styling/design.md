# Design: Unify Form Select & Dropdown Styling Standards

## Context

See `proposal.md` for motivation. In Tailwind CSS without the optional `@tailwindcss/forms` plugin, Preflight does not normalize `<select>` controls. As a result, browsers apply native platform rendering:
- Windows and Linux browsers display beveled, high-contrast OS select controls.
- Dropdown arrows are native OS widgets rather than theme-consistent icons.
- In Dark Mode, option menus frequently render white backgrounds with unreadable text or mismatched borders.
- Padding on the right is uncalibrated, causing option labels to collide with the arrow.

Four views contain `<select>` elements:
1. `frontend/pages/profile.vue`: Career target role (`targetRole`).
2. `frontend/pages/quiz.vue`: Grounded in Book selector (`selectedBookId`).
3. `frontend/pages/settings.vue`: Timezone preference (`timeZone`).
4. `frontend/pages/library.vue`: Technical category selectors (`importCategory`, `pdfCategory`) in 3 modal dialogs.

## Goals / Non-Goals

**Goals:**
- Suppress native OS select chrome globally across all `<select>` elements.
- Embed a theme-consistent SVG chevron indicator via `background-image` in CSS.
- Ensure light and dark mode styling for dropdown `<option>` items across all desktop and mobile browsers.
- Provide a reusable `.select-input` utility class in `@layer components` matching text input styles.
- Audit and standardize all select controls across `profile.vue`, `quiz.vue`, `settings.vue`, and `library.vue`.
- Ensure 100% automated test pass rate across Vitest suites.

**Non-Goals:**
- Replacing native `<select>` with custom headless dropdowns or simulated divs (native selects preserve mobile keyboard/touch ergonomics, screen reader accessibility, and lightweight DOM).
- Adding `@tailwindcss/forms` as a heavy external dependency.

## Decisions

### 1. Global Select Normalization via CSS `@layer base`
In `frontend/assets/css/main.css`:
```css
@layer base {
  select {
    appearance: none;
    -webkit-appearance: none;
    -moz-appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='%2394a3b8' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
    background-position: right 0.75rem center;
    background-repeat: no-repeat;
    background-size: 1rem 1rem;
    padding-right: 2.5rem;
    cursor: pointer;
  }

  select option {
    background-color: #ffffff;
    color: #0f172a;
  }

  .dark select option {
    background-color: #18181b;
    color: #f8fafc;
  }
}
```
- **Rationale**: Base-layer normalization guarantees that all `<select>` elements in the app—even those without dedicated utility classes—automatically inherit the chevron, right padding, pointer cursor, and dark/light option contrast.

### 2. Reusable Component Utility `.select-input`
In `frontend/assets/css/main.css`:
```css
@layer components {
  .select-input {
    @apply w-full py-2.5 pl-3.5 pr-10 bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors cursor-pointer;
  }
}
```
- **Rationale**: Ensures exact visual harmony with `.glass-card`, text inputs, and focus ring standards.

### 3. Page Audit & Styling Updates
- `frontend/pages/profile.vue`: Apply `.select-input pl-9` to `targetRole` (accommodating the left `Briefcase` icon).
- `frontend/pages/quiz.vue`: Apply `.select-input py-2 text-xs sm:text-sm` to `selectedBookId`.
- `frontend/pages/settings.vue`: Apply `.select-input py-2 text-xs sm:text-sm` to `timeZone`.
- `frontend/pages/library.vue`: Apply `.select-input px-4 py-3` to all 3 category select elements in import and upload modals.

## Risks / Trade-offs

- **Risk**: Custom SVG chevron in data URI adds minor bytes to `main.css`.
  - *Mitigation*: The inline SVG is ~200 bytes, compressed and cached with `main.css`, requiring zero additional network requests or external icon libraries.
