# Spec Delta

## MODIFIED Requirements

### Requirement: Frontend Static Type Checking
The frontend SHALL expose a standalone `npm run typecheck` for authored Vue SFCs, TypeScript, declarations, and project configuration using the prepared application/server/shared/tooling contexts (`vue-tsc --noEmit`). Authored application files, components, stores, and composables SHALL compile with zero type diagnostics (0 errors). The command SHALL preserve strictness, aliases, diagnostics, and the compiler's native exit status without an accepted-debt mode or blanket suppressions. Lint, unit tests, and bundling SHALL NOT substitute for typechecking.

#### Scenario: Clean authored code typecheck execution
- **WHEN** developer or CI executes `npm run typecheck` in the `frontend/` directory
- **THEN** `vue-tsc` executes against all authored Vue SFCs, TypeScript files, stores, composables, and configuration
- **AND** reports exactly 0 diagnostics and exits with code 0.

#### Scenario: A component receives an incompatible prop
- **WHEN** an application component receives a prop incompatible with its declared type
- **THEN** typechecking reports the mismatch and returns nonzero even if bundling succeeds.

#### Scenario: Existing semantic type diagnostics are visible
- **WHEN** the project contains existing semantic type errors
- **THEN** the command displays them and retains its nonzero exit status
- **AND** no ledger, suppression added for this cutover, or wrapper reclassifies that execution as passing.

#### Scenario: Invalid project configuration is reported
- **WHEN** the compiler rejects project configuration
- **THEN** its diagnostics and nonzero exit status remain visible rather than becoming accepted type debt.

### Requirement: Frontend Automated Static Linting & Quality Gates
The frontend codebase SHALL provide automated linting scripts (`npm run lint` and `npm run lint:fix`) integrated with `@nuxt/eslint` and flat configuration (`eslint.config.mjs`) targeting Nuxt 4, Vue 3.5 Composition API, and TypeScript. The linting rules SHALL enforce Vue template safety rules (`vue/no-v-html`, `vue/no-template-target-blank`), check statically lintable component and prop declarations, eliminate unused variables and dead imports, and restrict unconstrained `any` types while preserving Nuxt auto-imports without false-positive `no-undef` violations. Authored codebase SHALL pass with zero lint errors and zero unhandled lint warnings. Legitimate Markdown-it/KaTeX and Shiki rendering points utilizing `v-html` SHALL be explicitly audited and accompanied by scoped eslint-disable directives explaining their actual client-side HTML/URL protections or code escaping, without claiming server sanitization. Full component, prop, and application type validation SHALL be performed by the separate frontend typechecking command rather than inferred from lint or build success.

#### Scenario: Running frontend lint command
- **WHEN** developer or CI executes `npm run lint` in the `frontend/` directory
- **THEN** ESLint executes across authored Vue and TypeScript files, reports violations, and retains its native exit status without changing source files.

#### Scenario: Running frontend lint auto-fix command
- **WHEN** developer executes `npm run lint:fix` in the `frontend/` directory
- **THEN** ESLint automatically corrects fixable style and formatting violations across authored Vue and TypeScript files without introducing behavioral regressions.

#### Scenario: Audited v-html render points document their actual trust boundary
- **WHEN** a component renders markdown or syntax-highlighted HTML using `v-html`
- **THEN** the usage carries an explicit scoped eslint-disable comment explaining the applicable client Markdown-it raw-HTML and URL protections or Shiki code escaping, without claiming server sanitization
- **AND** untrusted raw HTML and unsafe link URLs are not inserted as executable markup.

#### Scenario: Lint warnings remain observable
- **WHEN** lint completes without errors but reports warnings
- **THEN** warnings remain visible even when ESLint exits zero
- **AND** the result is not described as warning-free or checked against a warning allowance, count ceiling, or debt ratchet.

#### Scenario: Lint encounters an error or invalid configuration
- **WHEN** ESLint reports an error, parsing failure, or invalid configuration
- **THEN** the lint command retains the native nonzero exit status instead of converting the failure into accepted debt.

#### Scenario: Custom quality commands are retired
- **WHEN** a developer inspects the available frontend validation commands after the cutover
- **THEN** native lint, lint auto-fix, typecheck, test, and build commands remain available
- **AND** `quality:check` and `quality:baseline` are absent without compatibility aliases or replacement accepted-debt commands.
