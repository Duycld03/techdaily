# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Nuxt 4 Frontend Framework Runtime & Build Pipeline
The web frontend SHALL operate on the official Nuxt 4 framework runtime (`nuxt: ^4.6.0`) with Vue 3.5, Vite builder, and SSR/PWA capabilities, adhering strictly to the canonical Nuxt 4 directory architecture:

1. **Canonical `app/` Directory Layout**:
   - The web frontend application SHALL locate all client and SSR runtime source code within `frontend/app/`:
     - Root shells: `app.vue` and `error.vue`
     - Views and UI components: `pages/`, `components/`, and `layouts/`
     - Application state and reactivity: `stores/` and `composables/`
     - Navigation lifecycle and plugins: `middleware/` and `plugins/`
     - Visual design and utility primitives: `assets/`, `utils/`, and `workers/`
   - The `frontend/` root SHALL be reserved exclusively for project tooling, environment configuration, and test harnesses:
     - Configuration: `nuxt.config.ts`, `tailwind.config.js`, `vitest.config.ts`, `tsconfig.json`, `package.json`, `Dockerfile`
     - Static assets: `public/`
     - Environment validation: `config/`
     - Test suites: `tests/`
2. **Path Resolution & Test Runner Parity**:
   - Path aliases `~` and `@` SHALL resolve directly to the `./app` directory across both the Nuxt build pipeline and Vitest runner configuration (`vitest.config.ts`), guaranteeing seamless import resolution for both runtime code and test suites without requiring file-by-file path changes.
3. **Clean Build Pipeline & Zero Compiler Warnings**:
   - The frontend build process (`npm run build`) SHALL compile both client and Nitro server bundles cleanly with zero PostCSS or styling package warnings (`tailwindcss/nesting`).
4. **Security & Route Isolation Invariants**:
   - The build pipeline SHALL generate valid TypeScript runtime shims (`npx nuxt prepare`) and preserve existing route rules, including strict cross-origin isolation headers on `/read/**` routes (`Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: credentialless`) required for on-device multi-threaded WASM neural narration.
5. **Project Governance & Documentation Parity**:
   - Project engineering rules and developer guides (`AGENTS.md`, `openspec/config.yaml`) SHALL accurately reflect `Nuxt 4` across all frontend tech stack definitions.

#### Scenario: Application builds with canonical app folder structure and zero PostCSS warnings
- **WHEN** an engineer executes `npm run build` in `frontend/`
- **THEN** Vite and Nitro successfully compile all pages, components, and assets residing in `frontend/app/`
- **AND** the build terminal output emits zero PostCSS package missing warnings (`NUXT_B5010` / `NUXT_B7007`)
- **AND** generates the production server bundle at `frontend/.output/server/index.mjs`.

#### Scenario: Automated test suite resolves components and stores from canonical app directory
- **WHEN** an engineer runs `npm test` (`vitest run`) in `frontend/`
- **THEN** Vitest resolves all `~/components/...`, `~/stores/...`, and `~/composables/...` imports to `frontend/app/`
- **AND** all 74 test suites (664 tests) pass 100% without path resolution failures.

#### Scenario: Production build outputs server entry with unchanged cross-origin isolation headers
- **WHEN** the production Nitro server is started via `node .output/server/index.mjs`
- **THEN** HTTP responses for `/read/**` routes include `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless`
- **AND** standard routes such as `/login` and `/` remain unaffected.
