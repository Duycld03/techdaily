# Proposal

## Why

The original goal was standard Nuxt-aware linting and an assessment of any missing tooling, not a custom diagnostic-debt framework. Remove the extra gate and keep native ESLint and Vue typechecking commands so results remain transparent and the project has less tooling to maintain.

## What Changes

- Retain the official `@nuxt/eslint` module, ESLint flat configuration, `lint`, and the explicitly mutating `lint:fix`; preserve existing rule severities rather than introducing another preset or formatter.
- Retain `vue-tsc` and `typecheck: nuxt typecheck --checker vue-tsc` with the standard Nuxt app/server/shared/node project references. Existing type errors remain visible and return nonzero; fixing application type debt is outside this change.
- Keep authored application, configuration, test, E2E JavaScript, and service-worker lint coverage with scoped globals. Declare `globals` directly as a development dependency because the configuration imports it; do not rely on dependency hoisting.
- **BREAKING (developer tooling only):** remove `quality:check`, `quality:baseline`, their scripts/helpers and gate-only tests, the accepted-debt ledger, candidate reports, and associated ignore entries. Provide no aliases, replacement wrapper, baseline migration, or warning budget.
- Replace CI's custom gate and Git comparison/bootstrap setup with one direct `npm run lint` before the existing tests and build. Preserve installation, ONNX handling, Node 22, and the backend/frontend publication barrier.
- Let ESLint errors fail CI while warnings remain observable under native ESLint behavior. Keep standalone typechecking outside the blocking CI stages in this change; do not run a softened typecheck or label an unperformed typecheck as passing. Non-growing warning/type debt is explicitly no longer enforced.
- Consolidate concise command, iteration, and CI policy guidance in README; remove the separate frontend-quality guide and its links after retaining its unresolved security handoff. Preserve the existing bilingual UI dual gate and canonical authentic-session guidance without duplicating them into another guide.

## Capabilities

### New Capabilities

None. Existing `core-platform` owns frontend static-analysis and verification governance.

### Modified Capabilities

- `core-platform`: Separate native lint/type results, retain authored-code coverage, require direct lint CI enforcement without a debt ratchet, and preserve scope-proportional behavioral/visual verification.

## Impact

- Implementation targets: `frontend/package.json`, `frontend/package-lock.json`, `frontend/eslint.config.mjs`, `frontend/tsconfig.json`, `.github/workflows/ci.yml`, `.gitignore`, and README.
- Removal targets: `frontend/scripts/check-quality.mjs`, `frontend/scripts/quality/`, `frontend/config/quality-baseline.json`, `frontend/tests/config/quality-gate.spec.ts`, `frontend/.quality-reports/`, and `docs/operations/frontend-quality.md`. Preserve unrelated scripts, configuration, application tests, and normal generated `.nuxt/` and `.output/` handling.
- Dependencies: reuse the installed Nuxt/ESLint/TypeScript/Vue/checker stack; explicitly declare the already-used `globals` package. No formatter, hook framework, scanner, coverage provider, or new browser runner.
- This revision supersedes the earlier baseline-oriented plan, which has not been synced into the main capability spec. Rewrite its pending delta rather than adding removals for requirements that do not exist in the main spec; replace its completed task list with unchecked work for this cutover.
- Planning changes do not implement removals, alter CI, accept type debt, remediate historical credential/dependency findings, or authorize production operations. Application behavior, authentication/storage, backend, database, and rendered UI remain out of scope.
