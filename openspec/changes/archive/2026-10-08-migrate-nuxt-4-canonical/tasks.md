# Tasks

## 1. Frontend Directory Restructuring & Tooling Configuration

- [x] 1.1 Create `frontend/app/` and relocate runtime UI directories via `git mv`: `app.vue`, `error.vue`, `pages/`, `components/`, `composables/`, `stores/`, `middleware/`, `plugins/`, `assets/`, `utils/`, and `workers/`.
- [x] 1.2 Update `frontend/tailwind.config.js` content array to scan `./app/**/*.{js,vue,ts}`.
- [x] 1.3 Update `frontend/vitest.config.ts` path aliases (`~` and `@`) to resolve to `./app`, and update `tests/config/nuxt-config-env.spec.ts` import path for `validateEnv`.
- [x] 1.4 Update `frontend/nuxt.config.ts` to remove redundant `future.compatibilityVersion: 4` bridge flag and verify CSS path.
- [x] 1.5 Configure PostCSS nesting in `frontend/nuxt.config.ts` to eliminate PostCSS build warnings (`NUXT_B5010` / `NUXT_B7007`).

## 2. Project Architecture & Documentation Synchronization

- [x] 2.1 Update `AGENTS.md` frontend stack definitions from `Nuxt 3` to `Nuxt 4`.
- [x] 2.2 Update `openspec/config.yaml` context from `Nuxt 3` to `Nuxt 4`.

## 3. Verification & Dual-Gate Testing

- [x] 3.1 Execute `npx nuxt prepare` in `frontend/` to generate virtual TypeScript types under the canonical directory structure.
- [x] 3.2 Execute `npm test` in `frontend/` to verify all 74 Vitest test suites (664 tests) pass 100%.
- [x] 3.3 Execute `npm run build` in `frontend/` to confirm production client and Nitro server build cleanly with zero PostCSS warnings.
- [x] 3.4 Conduct automated headless browser visual inspection on Desktop (1440x900) and Mobile (390x844) viewports to verify SSR hydration and visual integrity.
