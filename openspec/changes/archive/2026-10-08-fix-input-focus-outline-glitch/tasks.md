# Tasks

## 1. Frontend Component

- [x] 1.1 Update `frontend/assets/css/main.css` to remove detached focus outline rules from `input:focus-visible` and `textarea:focus-visible`, reserving the studio outline exclusively for discrete interactive controls (`button`, `a`, `select`, `[role="button"]`).
- [x] 1.2 Update `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` to add `"cancel"` key under the `insights` dictionary.
- [x] 1.3 Update `frontend/pages/insights.vue` to apply flush focus ring styling (`focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`) to the custom topic generator input and localize the modal dismiss button with `$t('insights.cancel')`.

## 2. Verification & Testing

- [x] 2.1 Run the frontend test suite via `npm test` to verify zero regressions across all test files.
- [x] 2.2 Execute headless browser automated visual verification for Desktop (1440x900) and Mobile (390x844) viewports on `/insights` to confirm the detached purple outline artifact is eliminated.
