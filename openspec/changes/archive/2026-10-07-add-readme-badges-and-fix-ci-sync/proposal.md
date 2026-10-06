# Proposal

## Why

The portfolio project (`Duycld03.github.io`) features a standardized Shields.io badge header showcasing its live URL, framework stack, styling engine, and OpenSpec workflow, whereas the TechDaily `README.md` currently lacks this standard visual presentation header. Furthermore, the GitHub Actions CI workflow recently encountered an `npm ci` lockfile desynchronization failure during the frontend build step, preventing the automated build pipeline and badge from passing green.

## What Changes

- Add a standardized Shields.io badge banner to `README.md` featuring:
  - **Live Production** (`https://techdaily.duckdns.org`)
  - **Backend Stack** (ASP.NET Core .NET 10)
  - **Frontend Stack** (Nuxt 4 / Vue 3.5)
  - **Database Engine** (PostgreSQL 17 + pgvector)
  - **CI/CD Status** (GitHub Actions `TechDaily CI` badge)
  - **Spec-Driven Development** (OpenSpec Workflow)
- Synchronize and regenerate the frontend `package-lock.json` cleanly under Node 22/npm 10 to ensure `npm ci` runs deterministically without missing package errors in both Docker builds and GitHub Actions CI runner.
- Validate that the frontend build and Vitest test suite succeed within the CI environment.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update build pipeline and project documentation standards to include standardized showcase badges in `README.md` and enforce clean lockfile synchronization for `npm ci`.

## Impact

- **Documentation**: Improves visual presentation and recruiter/developer clarity on GitHub with direct status and stack badges matching the portfolio aesthetic.
- **CI/CD Pipeline**: Fixes broken GitHub Actions runner (`Frontend CI (Nuxt 4 / Vue 3)` job) caused by npm lockfile mismatch, restoring green build status.
- **Developers**: Guarantees zero friction when executing `npm ci` in Docker containers and local development.
