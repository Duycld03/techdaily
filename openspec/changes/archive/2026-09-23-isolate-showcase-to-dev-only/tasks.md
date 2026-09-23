# Tasks

## 1. Frontend Build Pipeline & Route Pruning

- [x] 1.1 Implement `hooks['pages:extend']` in `frontend/nuxt.config.ts` to prune routes starting with `/showcase` or `/playground` when `process.env.NODE_ENV === 'production'`.

## 2. Frontend Navigation Shell Gating

- [x] 2.1 Update `frontend/composables/useNavigationMenu.ts` to gate `{ name: 'nav.showcase', path: '/showcase', icon: Palette }` in `nav.group_account` behind `import.meta.dev`.

## 3. Verification & Automated Testing

- [x] 3.1 Update unit tests in `frontend/tests/composables/useNavigationMenu.spec.ts` to verify `/showcase` is present in dev mode and excluded when `import.meta.dev` is false.
- [x] 3.2 Run full frontend test suite (`npm test`) to guarantee all 60 test suites and existing assertions pass with zero regressions.
- [x] 3.3 Verify route exclusion behavior in production build mode.
