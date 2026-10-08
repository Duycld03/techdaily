# Proposal

## Why

In the AI Topic Synthesis dialog on `/insights` (and across other text inputs in the application), focusing an `<input>` or opening an autofocused modal produces an unintended, jarring visual artifact: a thick neon purple detached rectangle floating 2px outside the rounded input box. This occurs because `frontend/assets/css/main.css` applies a global `outline: 2px solid #8b5cf6 !important; outline-offset: 2px !important; box-shadow: 0 0 0 4px rgba(139, 92, 246, 0.2) !important;` rule to `input:focus-visible:not(.bg-transparent)`. This rule overrides component-level Tailwind classes (`focus:outline-none`), creating an unsightly double-border with an empty gap around rounded containers. Additionally, the modal's "Cancel" button remains hardcoded in English.

## What Changes

- **Refine Global Focus Ring Rules (`main.css`)**:
  - Remove `input:focus-visible:not(.bg-transparent)` and `textarea:focus-visible:not(.bg-transparent)` from the detached `outline: 2px solid #8b5cf6 !important; outline-offset: 2px !important;` rule.
  - Retain the high-contrast studio outline exclusively for discrete interactive elements (`button:focus-visible`, `a:focus-visible`, `[role="button"]:focus-visible`, and `select:focus-visible`).
  - Standardize text input and textarea focus states to use flush, border-radius-conforming ring tokens (`focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`).
- **Clean Ingestion & Generation Modal Input Focus (`insights.vue`)**:
  - Update the custom topic input in the AI generator modal on `insights.vue` to apply flush `focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20` styling without outer detached outline gap.
  - Localize the modal's "Cancel" action button using `$t('insights.cancel')` in both Vietnamese (`Hủy Bỏ`) and English (`Cancel`).

## Capabilities

### Modified Capabilities
- `insights`: Remove detached outline glitch on topic generator input and localize the modal dismiss button.

## Impact

- **Frontend**: `frontend/assets/css/main.css`, `frontend/pages/insights.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Zero Breaking Changes**: Does not alter any API contract, domain logic, or database schema. WCAG 2.1 AA keyboard accessibility is preserved via flush focus rings.
