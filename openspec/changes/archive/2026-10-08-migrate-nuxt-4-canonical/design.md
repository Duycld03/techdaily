# Design: Migrate to Canonical Nuxt 4 Directory Structure & Tooling

## Context
See `proposal.md` for background and motivation. The web frontend currently runs on Nuxt 4.6.0 with Nitro 2.13.4, Vite 8.3.3, and Vue 3.5.43. While `future: { compatibilityVersion: 4 }` is active in `nuxt.config.ts`, the repository still maintains the legacy Nuxt 3 flat directory structure at `frontend/`.

## Goals / Non-Goals

**Goals:**
- Move all client/SSR runtime source code into `frontend/app/`: `app.vue`, `error.vue`, `pages/`, `components/`, `composables/`, `stores/`, `middleware/`, `plugins/`, `assets/`, `utils/`, and `workers/`.
- Retain project build, configuration, and testing tooling at `frontend/` root: `nuxt.config.ts`, `tailwind.config.js`, `vitest.config.ts`, `tsconfig.json`, `package.json`, `public/`, `config/`, and `tests/`.
- Update `vitest.config.ts` aliases so `~` and `@` point to `./app`, ensuring 100% of test suites pass without individual test import rewrites.
- Update `tailwind.config.js` content paths to `./app/**/*.{js,vue,ts}`.
- Resolve the `tailwindcss/nesting` PostCSS build warning.
- Synchronize project documentation (`AGENTS.md` and `openspec/config.yaml`) to standardize on Nuxt 4.

**Non-Goals:**
- No refactoring of Vue component business logic, styling rules, or API contracts.
- No migration to Tailwind CSS v4 (TechDaily remains on Tailwind v3.4.17 LTS).
- No relocation of `tests/` into `app/` (tests remain cleanly decoupled at `frontend/tests/`).

## Decisions

### 1. File Relocation via Git Rename
- **Decision**: Relocate files using `git mv` commands to preserve file history and blame information in git.
- **Directories moved to `frontend/app/`**:
  - `app.vue`
  - `error.vue`
  - `assets/`
  - `components/`
  - `composables/`
  - `middleware/`
  - `pages/`
  - `plugins/`
  - `stores/`
  - `utils/`
  - `workers/`
- **Directories preserved at `frontend/`**:
  - `public/` (Nuxt 4 serves static assets from project root `public/`)
  - `config/` (contains build-time env validator `validateEnv.ts` used by `nuxt.config.ts`)
  - `tests/` (Vitest unit and integration test suites)

### 2. Vitest Path Aliases Parity
- **Decision**: Update `frontend/vitest.config.ts`:
  ```typescript
  resolve: {
    alias: {
      '~': fileURLToPath(new URL('./app', import.meta.url)),
      '@': fileURLToPath(new URL('./app', import.meta.url))
    }
  }
  ```
- **Rationale**: Nuxt 4 maps `~` and `@` to `app/` (and `~~` / `@@` to root). Aligning Vitest guarantees that all existing imports like `import FlashcardDeck from '~/components/review/FlashcardDeck.vue'` and `import { useAuthStore } from '~/stores/useAuthStore'` continue to resolve without changing a single line in test files.

### 3. Worker Path Consistency
- **Decision**: Move `workers/` into `frontend/app/workers/`.
- **Rationale**: In `useSliceAudio.ts`, the worker is instantiated via:
  ```typescript
  new Worker(new URL('../workers/ttsSynth.worker.ts', import.meta.url), { type: 'module' })
  ```
  Since both `composables/` and `workers/` reside as sibling directories inside `frontend/app/`, the relative path `../workers/` remains valid without modification.

### 4. PostCSS Nesting Warning Remediation
- **Decision**: Install `tailwindcss/nesting` as a development dependency in `frontend/package.json`:
  ```bash
  npm i -D tailwindcss/nesting
  ```
- **Rationale**: Nuxt 4's CSS bundling pipeline scans for nesting plugins when CSS files use nested syntax. Installing `tailwindcss/nesting` silences the `[NUXT_B5010]` and `[NUXT_B7007]` warnings during `npm run build`.

### 5. Nuxt Configuration Streamlining
- **Decision**: Remove redundant `future.compatibilityVersion: 4` in `frontend/nuxt.config.ts` (as Nuxt 4 is now the native runtime), update CSS path to `~/assets/css/main.css`, and retain `compatibilityDate: '2024-11-01'`.

## Risks / Trade-offs

- **Risk: Auto-imports mismatch in Nuxt 4**: Nuxt 4 might fail to auto-scan deeply nested composables.
  - *Mitigation*: Run `npx nuxt prepare` immediately following file relocation to rebuild virtual type declarations, and verify with `npm run build`.
- **Risk: CSS purges in Tailwind**: Tailwind might purge classes if template paths are omitted.
  - *Mitigation*: Set `content: ['./app/**/*.{js,vue,ts}']` in `tailwind.config.js` to ensure 100% template coverage.
- **Risk: Vitest config env validator import**: `tests/config/nuxt-config-env.spec.ts` imports `~/config/validateEnv`.
  - *Mitigation*: Add an explicit alias or update that single test file to import from `../config/validateEnv` or `@@/config/validateEnv`.
