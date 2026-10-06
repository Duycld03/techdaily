# Spec Delta

## ADDED Requirements

### Requirement: Project Showcase Badge Presentation in Repository Documentation
The repository `README.md` SHALL feature a standardized Shields.io badge banner immediately beneath the primary document title, matching the portfolio showcase conventions. The banner SHALL display:
1. Live Production deployment URL badge (`techdaily.duckdns.org`)
2. Backend runtime badge (`ASP.NET Core .NET 10`)
3. Frontend framework badge (`Nuxt 4 / Vue 3.5`)
4. Database engine badge (`PostgreSQL 17 (pgvector)`)
5. CI/CD workflow status badge linked to GitHub Actions
6. Spec-Driven Development badge linked to OpenSpec

#### Scenario: Developer visits TechDaily GitHub repository
- **WHEN** any visitor or reviewer navigates to the root `README.md` of the repository
- **THEN** the badge row renders without broken links or missing images
- **AND** clicking the Live badge directs to `https://techdaily.duckdns.org`
- **AND** clicking the CI/CD badge directs to GitHub Actions workflows.

---

### Requirement: Deterministic Frontend Dependency Installation in CI Pipeline
The frontend dependency configuration (`package.json` and `package-lock.json`) SHALL maintain deterministic synchronization under the project's supported Node.js LTS runtime (Node 22) such that executing `npm ci` installs 100% of required dependencies without error.

#### Scenario: GitHub Actions CI executes frontend dependency installation
- **GIVEN** a push to branch `main` or pull request
- **WHEN** the `Frontend CI` job runs `npm ci` in `frontend/`
- **THEN** npm resolves and installs packages cleanly without `EUSAGE` or missing package mismatch errors
- **AND** downstream build and test steps execute successfully.
