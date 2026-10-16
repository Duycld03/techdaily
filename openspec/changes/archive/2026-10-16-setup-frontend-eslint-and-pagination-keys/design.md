# Design

## Context

The DeepPace frontend is built with Nuxt 4 (`app/` directory convention, Vue 3.5, TypeScript 5.7, Pinia, Tailwind CSS). While Vitest unit tests and `vue-tsc` type-checks run during development, there is no ESLint package or configuration in `frontend/package.json`. Consequently, Vue-specific linting (such as `eslint-plugin-vue` rules, template safety, and a11y checks) cannot be verified mechanically. Additionally, `BasePagination.vue` renders pages and ellipsis markers in a single `v-for` loop keyed by array index (`:key="idx"`), triggering Vue warnings and risking DOM recycling defects when shifting between page sets.

## Goals / Non-Goals

**Goals:**
- Integrate `@nuxt/eslint` module in `frontend/nuxt.config.ts` and add `eslint` dependencies.
- Provide a standard ESLint 9+ flat configuration (`eslint.config.mjs`) integrating with Nuxt's auto-generated types and auto-import globals.
- Expose `npm run lint` and `npm run lint:fix` scripts in `frontend/package.json`.
- Refactor `BasePagination.vue` to compute stable, collision-free keys for every rendered element without using array index alone.
- Guarantee that all existing Vitest test suites (103 files, 970+ tests) continue to pass without regression.

**Non-Goals:**
- Rewriting or reformatting clean, existing components solely for stylistic churn.
- Enforcing overly aggressive stylistic rules that conflict with Nuxt 4 auto-imports or Tailwind CSS inline utility patterns.

## Decisions

### Decision 1: Adopt `@nuxt/eslint` (Option A) over Manual `eslint-plugin-vue` Setup
- **Rationale**: Nuxt 4's official `@nuxt/eslint` module deeply integrates with Nuxt's module container. It automatically compiles auto-imports (`ref`, `computed`, `useRoute`, `useApiClient`, `defineStore`) into `.nuxt/eslint.config.mjs`. This prevents hundreds of false-positive `'ref' is not defined (no-undef)` errors that occur when using vanilla `eslint-plugin-vue` without manual globals declarations.
- **Alternatives Considered**:
  - Standalone ESLint 9 Flat Config: Requires manually maintaining globals or writing an extra script to extract Nuxt imports, creating maintenance overhead on Nuxt upgrades.

### Decision 2: ESLint Rules Profile
- **Configuration**:
  ```js
  // eslint.config.mjs
  import withNuxt from './.nuxt/eslint.config.mjs'

  export default withNuxt({
    rules: {
      'vue/no-v-html': 'warn',
      'vue/no-template-target-blank': 'error',
      'vue/multi-word-component-names': 'off', // Nuxt pages/components use single words by convention
      '@typescript-eslint/no-explicit-any': 'warn',
      'no-console': ['warn', { allow: ['warn', 'error'] }]
    }
  })
  ```
- **Rationale**: Balances strict template safety and security with practical Nuxt conventions.

### Decision 3: Stable DOM Keying in `BasePagination.vue`
- **Current Pattern**:
  ```vue
  <template v-for="(page, idx) in visiblePages" :key="idx">
  ```
- **Target Pattern**:
  ```vue
  <template v-for="(page, idx) in visiblePages" :key="typeof page === 'number' ? `page-${page}` : `ellipsis-${idx}`">
  ```
- **Rationale**: Numerical page items get stable keys like `page-1`, `page-2`. Ellipsis markers get unique contextual keys `ellipsis-1` or `ellipsis-5`. When user advances pages, Vue's virtual DOM diffing cleanly transitions numbers and ellipsis without stale component state or DOM recycling issues.

## Risks / Trade-offs

- **Risk: Initial ESLint execution may flag pre-existing styling warnings across older components.**
  - *Mitigation*: Run `npm run lint:fix` on affected files and set non-security stylistic rules to `warn` initially so existing CI/CD gates remain stable.
- **Risk: `@nuxt/eslint` requires `nuxt prepare` to generate `.nuxt/eslint.config.mjs`.**
  - *Mitigation*: Ensure `postinstall: "nuxt prepare"` generates the required `.nuxt` artifacts prior to running `npm run lint`.
