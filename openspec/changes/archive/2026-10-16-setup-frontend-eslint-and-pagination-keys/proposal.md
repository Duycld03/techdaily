# Proposal

## Why

The frontend repository currently lacks automated static linting and template governance in `package.json`, creating blind spots for Vue 3 / Nuxt 4 anti-patterns, template security vulnerabilities, and accessibility regressions. Furthermore, existing components like `BasePagination.vue` rely on array index keys (`:key="idx"`) in `v-for` loops rendering mixed page numbers and ellipsis indicators, risking DOM recycling bugs and inconsistent focus behavior during page navigation.

## What Changes

- Add `@nuxt/eslint` to `frontend/package.json` devDependencies and register it as a Nuxt module in `frontend/nuxt.config.ts`.
- Generate and configure ESLint 9+ flat configuration (`eslint.config.mjs`) compatible with Nuxt 4 auto-imports, Vue 3.5 Composition API, and template safety standards.
- Add `lint` and `lint:fix` scripts to `frontend/package.json`.
- Refactor `frontend/app/components/common/BasePagination.vue` to use stable, contextual keys (`page-${page}` for numerical pages, `ellipsis-start` / `ellipsis-end` or `ellipsis-${idx}` for delimiters) adhering to Vue/Nuxt and ECC coding standards.
- Validate the new linting pipeline and verify zero breakages across existing component unit tests.

## Capabilities

### Existing Capabilities
- `core-platform`: Extends the verification and developer workflow requirements with automated frontend static linting gates.
- `system-layout-archetypes`: Refines base pagination rendering contracts to mandate stable DOM keying for pagination items.

## Impact

- **Developer Workflow**: Developers and CI can run `npm run lint` and `npm run lint:fix` to automatically detect and correct Vue/TypeScript styling and template security issues.
- **Component Stability**: Prevents DOM element recycling issues in pagination when navigating between ranges with differing page count and ellipsis placements.
- **Dependencies**: Adds `@nuxt/eslint` and `eslint` as development dependencies in `frontend/`. No runtime or production bundle size overhead.
