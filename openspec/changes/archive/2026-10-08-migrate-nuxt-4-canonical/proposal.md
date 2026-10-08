# Proposal: Migrate to Canonical Nuxt 4 Directory Structure & Tooling

## Why
Although the web frontend runs on Nuxt 4.6.0 (`nuxt: ^4.6.0`), the codebase still resides in the legacy Nuxt 3 flat root directory layout where UI code, styling, stores, pages, and infrastructure configurations are intermixed at the `frontend/` root. Furthermore, project architectural guidelines (`AGENTS.md` and `openspec/config.yaml`) still describe the frontend stack as "Nuxt 3", and the production build emits PostCSS nesting package warnings. 

Migrating to the canonical Nuxt 4 `app/` architecture resolves root pollution, establishes a clean boundary between UI client code and infrastructure tooling, ensures 100% alignment with official Nuxt 4 ecosystem conventions, and cleans up the build pipeline without breaking existing unit test suites or user experience.

## What Changes
- **Directory Restructuring**: Relocate application runtime code into `frontend/app/`:
  - `app.vue` and `error.vue`
  - `pages/`, `components/`, `composables/`, `stores/`, `middleware/`, `plugins/`, `assets/`, `utils/`, and `workers/`
- **Root Tooling Isolation**: Preserve infrastructure, build configuration, and testing files at `frontend/` root:
  - `nuxt.config.ts`, `tailwind.config.js`, `vitest.config.ts`, `tsconfig.json`, `package.json`, `Dockerfile`
  - `public/` (static assets), `config/` (environment validator), and `tests/` (Vitest suites)
- **Path Resolution & Tooling Alignment**:
  - Update `vitest.config.ts` path aliases (`~` and `@`) to resolve to `./app`, guaranteeing all 74 test suites (664 tests) continue to pass without mass test file rewrites.
  - Update `tailwind.config.js` content scanning paths to `./app/**/*.{js,vue,ts}`.
  - Update `nuxt.config.ts` CSS paths and clean up redundant `future.compatibilityVersion: 4` bridge flag.
  - Resolve PostCSS `tailwindcss/nesting` package warning to ensure 0 build warnings.
- **Architectural Documentation Synchronization**:
  - Update `AGENTS.md` and `openspec/config.yaml` to standardize on `Nuxt 4` throughout project context and engineering pillars.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update requirement `Nuxt 4 Frontend Framework Runtime & Build Pipeline` to mandate the canonical `app/` directory architecture, zero build warnings, and documentation consistency.

## Impact
- **Developer Experience**: Significantly clearer separation of concerns between runtime UI source code and build infrastructure tooling.
- **Backwards Compatibility**: Fully preserved. No breaking changes to HTTP endpoints, SSR behavior, route rules, or visual design system.
- **Testing**: All existing 664 unit tests continue to execute and pass cleanly under Vitest and Happy-DOM.
