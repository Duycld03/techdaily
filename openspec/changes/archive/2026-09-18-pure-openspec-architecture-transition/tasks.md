## 1. Documentation Retirement

- [x] 1.1 Permanently delete the legacy `docs/` directory (`features.md`, `domain-rules.md`, `api-design.md`, `database-design.md`) and verify directory removal.

## 2. Developer Guide & Agent Conventions Alignment

- [x] 2.1 In `AGENTS.md`, replace the top source-of-truth table to point exclusively to `openspec/specs/` for capability specifications and `AGENTS.md` (Sections 2 & 3) for domain invariants, removing all references to `docs/`.
- [x] 2.2 In `README.md`, remove obsolete markdown links and references to `docs/features.md`, `docs/api-design.md`, `docs/domain-rules.md`, and `docs/database-design.md`, updating the documentation table to feature `openspec/specs/` and OpenAPI/Swagger.

## 3. OpenSpec Integrity & Verification

- [x] 3.1 Validate OpenSpec change integrity by running `openspec validate pure-openspec-architecture-transition --type change` and verifying 0 errors.
- [x] 3.2 Run test suites (`dotnet test` and `npm test`) to verify clean cutover with zero regressions.
