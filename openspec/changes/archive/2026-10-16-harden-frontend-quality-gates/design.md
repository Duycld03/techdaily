# Design

## Context

See `proposal.md` for motivation and `specs/core-platform/spec.md` for the revised contracts.

The repository already has the official Nuxt lint module, flat ESLint configuration, `vue-tsc`, a native typecheck command, and the four Nuxt TypeScript references. The extra layer is a custom CLI with four helper modules, an accepted-debt JSON ledger, candidate reports, gate-only tests, and CI Git-comparison setup.

The prior review observed native typechecking failing on existing project diagnostics. That is not evidence that the compiler is unavailable or that its failure can safely be converted into success. This design removes the acceptance mechanism rather than repairing or replacing it. The old task completion marks describe the implementation being removed, not completion of this revision.

## Goals / Non-Goals

**Goals:**
- Let each native tool own diagnostics and exit status; keep CI step boundaries responsible for failure propagation.
- Make the removal atomic across package commands, scripts, tests, artifacts, CI callers, and documentation.
- Retain authored-code coverage, explicit dependencies, generated-output exclusions, and existing application verification.

**Non-Goals:**
- No new coordinator, diagnostic parser, baseline format, warning ceiling, automated checker graph validator, or compatibility aliases.
- No broad rule/style migration, application type-debt cleanup, dependency upgrade campaign, or authentication/security remediation.
- No generated Nuxt configuration edits, new testing/browser infrastructure, or changes to backend/deployment architecture.

## Decisions

### 1. Keep the native Nuxt toolchain

Keep `lint: eslint .`, `lint:fix: eslint . --fix`, and `typecheck: nuxt typecheck --checker vue-tsc`; retain `nuxt prepare` in the existing install lifecycle. Preserve the root references to `.nuxt/tsconfig.app.json`, `.nuxt/tsconfig.server.json`, `.nuxt/tsconfig.shared.json`, and `.nuxt/tsconfig.node.json`. Do not introduce dev/build-time typechecking as a substitute.

Retain compatible locked package versions. Because `eslint.config.mjs` imports `globals`, add it explicitly to `devDependencies` and update the lockfile using the already-resolved compatible package rather than depending on another package to hoist it. Keep the existing Node 22 CI family on a Nuxt-supported patch.

**Alternative rejected:** A combined validation command would recreate orchestration and status semantics that separate native commands and CI steps already provide.

### 2. Preserve coverage with small authored configuration overrides

Keep the current rule severities and Nuxt auto-import support. Retain scoped Node/browser globals for authored tooling/E2E JavaScript and service-worker globals for `public/sw.js`. Preserve the existing workaround for generated Nuxt public-directory ignores so the authored service worker is actually linted; do not edit `.nuxt/` files.

Remove configuration entries whose only purpose was the deleted gate or its reports. Keep generated declarations, bundles, and screenshots excluded. In particular, a declaration lint exclusion must not remove authored declarations from the prepared TypeScript project.

**Alternative rejected:** Blanket E2E/public exclusions make lint look cleaner by removing coverage; unrelated mass auto-fixing expands this change without improving the tooling contract.

### 3. Use direct CI stages and state the typecheck limitation

After the existing clean installation, frontend CI runs `npm run lint`, then the existing `npm test`, then the existing `npm run build`, each once. Preserve `ONNXRUNTIME_NODE_INSTALL=skip`, Node/npm setup, backend checks, and publication/deployment dependencies. Remove the custom quality step, comparison-SHA resolution/fetching, and `quality-base` dispatch input while retaining manual dispatch.

ESLint errors and configuration failures block the job through native nonzero status. Warnings remain in logs and do not fail a warning-only run. There is no warning budget or guarantee against increasing warning/type debt.

Keep raw typechecking as a documented standalone command, outside the required CI stages for this change. Record its actual implementation-time result, including known existing failures, separately from lint/tests/build. Do not add `continue-on-error`, `|| true`, a reduced compiler graph, suppressions, or a passing debt label.

**Alternatives rejected:** Making full typechecking a required stage now would require application remediation outside the approved scope. Running it as a softened CI stage retains noise and ambiguous pass semantics. Both are avoided by explicitly documenting that CI does not enforce it yet.

### 4. Remove the custom feature completely

Delete the custom files and artifact directory enumerated in the proposal, their package commands, gate-only test file, and report-specific ignore entries. Do not migrate the accepted-debt ledger or retain dormant helpers. Preserve `frontend/config/validateEnv.ts`, unrelated scripts/tests, and normal generated-output handling.

The old debt-ratchet and wrapper-failure requirements exist only in this change's unsynced delta. Replace them there; do not add misleading `REMOVED` operations against requirements absent from the main capability spec.

**Alternative rejected:** Repairing the prior review's wrapper defects or keeping an opt-in legacy path maintains an unnecessary second toolchain and its callers.

### 5. Consolidate guidance and verify actual commands

Fold a short command/CI policy reference into README, then remove the separate frontend-quality guide and both README links. Explain that targeted lint still uses the project's applicable lint configuration, typechecking uses the full prepared project, and final validation is not inferred from a targeted iteration run. Keep the existing canonical OpenSpec/agent instructions authoritative for behavioral tests and authentic bilingual UI verification.

Retain a brief unresolved security/dependency handoff from the removed guide without credentials, copied secret values, or claims that this simplification fixes those findings.

Verify with clean installation, real ESLint/checker fixtures, native commands, and the existing test suite. Use disposable fixtures for a valid/error lint path, E2E/service-worker coverage, an incompatible Vue prop, authored declarations, and compiler-configuration failure; remove fixtures afterward. Do not commit source-text/config-shape assertions, mocked-tool echo tests, or new tests for the deleted gate. Do not execute credential-bearing E2E workflows merely to establish lint coverage.

**Alternative rejected:** Retaining a second operations guide duplicates the policy, while permanent wiring tests or unrelated screenshots do not prove the changed executable path.

## Risks / Trade-offs

- [Existing type errors remain and CI no longer enforces non-growing diagnostic debt] -> Keep the raw command and its nonzero result visible; document the limitation rather than claiming a fully type-safe or warning-free build.
- [Lint coverage is accidentally narrowed during deletion] -> Exercise actual E2E/service-worker lint diagnostics and a Vue prop/declaration fixture while preserving Nuxt configuration and all four references.
- [A stale command or Git-base dependency breaks CI] -> Remove every known caller in the same cutover, exercise lint failure propagation locally, and preserve the existing publication dependency graph.
- [Removing the guide loses an unresolved security handoff] -> Move a short sanitized handoff into README before deleting the guide; leave remediation explicitly unresolved.
- [Documentation overstates validation] -> Report only commands actually run. Local CI-path checks are not a remote Actions run; tooling checks are not UI or security certification.

## Migration Plan

1. Preserve the native toolchain and make `globals` explicit without upgrading unrelated packages.
2. Cut over package commands and CI together; remove the custom implementation, its tests, ledger, candidate reports, and gate-specific exclusions. Preserve unrelated files and checks.
3. Consolidate README guidance and its sanitized unresolved handoff before removing the guide and its links.
4. On supported Node 22, run a clean dependency installation, disposable real-tool smoke checks, one raw project typecheck, and the final lint/test/build pass. Record actual outcomes, obtain code review, and validate this OpenSpec delta before marking tasks complete.

There is no application-data or schema migration. If the cutover breaks validation, revert the reviewed tooling change as a unit; do not add compatibility commands, silently accept failures, or restore an unverified ledger as a fallback. Deployment rollback, if needed, uses the previous validated application artifact rather than changing application authentication or database behavior.
