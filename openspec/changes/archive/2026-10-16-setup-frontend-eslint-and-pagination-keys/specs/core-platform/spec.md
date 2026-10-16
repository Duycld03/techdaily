# Spec Delta: Core Platform

## ADDED Requirements

### Requirement: Frontend Automated Static Linting & Quality Gates
The frontend codebase SHALL provide automated linting scripts (`npm run lint` and `npm run lint:fix`) integrated with `@nuxt/eslint` and flat configuration (`eslint.config.mjs`) targeting Nuxt 4, Vue 3.5 Composition API, and TypeScript. The linting rules SHALL enforce Vue template safety rules (`vue/no-v-html`, `vue/no-template-target-blank`), verify component and prop typing, and prevent unused variables while preserving Nuxt auto-imports without false-positive `no-undef` violations.

#### Scenario: Running frontend lint command
- **WHEN** developer or CI executes `npm run lint` in the `frontend/` directory
- **THEN** ESLint executes across all `.vue` and `.ts` files and reports any rule violations or passes with exit code 0 when clean.

#### Scenario: Running frontend lint auto-fix command
- **WHEN** developer executes `npm run lint:fix` in the `frontend/` directory
- **THEN** ESLint automatically corrects fixable style and formatting violations across `.vue` and `.ts` files.

### Requirement: Stable Pagination DOM Keying Contract
Shared pagination navigation components (`BasePagination.vue`) rendering mixed sequences of numerical page triggers and ellipsis indicators SHALL assign distinct, stable DOM keys to each rendered item. Keying MUST NOT use raw array indices alone, preventing element state and focus desynchronization across pagination page updates.

#### Scenario: Navigating between page ranges with shifting ellipses
- **WHEN** user transitions from an initial page range (e.g. `[1, 2, 3, 4, '...', 10]`) to an intermediate page range (e.g. `[1, '...', 5, 6, 7, '...', 10]`)
- **THEN** numerical page buttons retain keys derived from their page numbers (`page-1`, `page-5`), and ellipsis indicators receive contextual keys distinguishing leading from trailing ellipsis (`ellipsis-start`, `ellipsis-end`), ensuring DOM nodes are cleanly replaced rather than mismatched across re-renders.
