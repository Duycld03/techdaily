# Tasks

## 1. Frontend

- [x] 1.1 Delete directory `frontend/pages/playground/` and all its contained files (`audio-narration.vue`, `temp.vue`, `index.vue`, `dashboard.vue`, `README.md`)
- [x] 1.2 Remove playground navigation item and active link resolution logic in `frontend/composables/useNavigationMenu.ts`
- [x] 1.3 Remove `nav.playground` translation key from `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`
- [x] 1.4 Remove `/playground` route exemptions from `frontend/middleware/auth.global.ts`
- [x] 1.5 Remove `/playground/**` route rules and `/playground` prefix in `pages:extend` hook in `frontend/nuxt.config.ts`

## 2. Documentation

- [x] 2.1 Update `AGENTS.md` Pillar 2 (UI Design System & Component Governance) to remove Item 4 ("Mandatory Vue Playground Protocol")

## 3. Verification

- [x] 3.1 Update unit test assertions in `frontend/tests/composables/useNavigationMenu.spec.ts`, `frontend/tests/config/route-pruning.spec.ts`, and `frontend/tests/middleware/auth.spec.ts`
- [x] 3.2 Run `npm test` across the entire frontend test suite to ensure 100% pass rate with zero regressions
