# Tasks

## 1. Dependencies and Nuxt Configuration

- [x] 1.1 Add `@nuxt/eslint` and `eslint` to `devDependencies` in `frontend/package.json`.
- [x] 1.2 Register `@nuxt/eslint` in `modules` array within `frontend/nuxt.config.ts`.
- [x] 1.3 Add `"lint": "eslint ."` and `"lint:fix": "eslint . --fix"` scripts to `frontend/package.json`.

## 2. ESLint Flat Configuration

- [x] 2.1 Create `frontend/eslint.config.mjs` using `withNuxt` from `./.nuxt/eslint.config.mjs` with rules for template security (`vue/no-v-html`, `vue/no-template-target-blank`) and Nuxt 4 auto-imports.
- [x] 2.2 Run `npm run lint` and resolve any initial configuration or parsing issues.

## 3. BasePagination Stable Key Refactoring

- [x] 3.1 Refactor `frontend/app/components/common/BasePagination.vue` `v-for` loop to key elements with `typeof page === 'number' ? `page-${page}` : `ellipsis-${idx}`` instead of `:key="idx"`.
- [x] 3.2 Update or add unit tests in `frontend/tests/components/BasePagination.spec.ts` asserting stable, unique keying across initial, intermediate, and terminal page ranges.

## 4. Verification and Quality Gate

- [x] 4.1 Run `npm run lint` in `frontend/` to confirm zero critical errors.
- [x] 4.2 Run full `npm test` in `frontend/` to verify all 103 test files (970+ tests) pass without regression.
