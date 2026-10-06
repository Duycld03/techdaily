# Design

## Context

TechDaily's `README.md` previously lacked the visual badge row used in other portfolio repositories like `Duycld03.github.io`. Simultaneously, the GitHub Actions CI pipeline failed at the `Frontend CI` step during `npm ci` due to an outdated lockfile where newly declared dependencies in `package.json` (`nuxt: ^4.6.0`, `@nuxtjs/i18n: ^10.6.0`) lacked synchronized tree entries (`unplugin@3.4.0`, `pinia@4.0.3`) in `package-lock.json`.

## Goals / Non-Goals

**Goals:**
- Provide a consistent, high-contrast Shields.io badge row at the top of `README.md` reflecting TechDaily's live deployment, technology stack, CI/CD health, and specification framework.
- Ensure `frontend/package-lock.json` is 100% synchronized with `frontend/package.json` so that `npm ci` succeeds without error in GitHub Actions and Docker build environments.
- Verify that `npm run build` and `npm test` execute cleanly with zero warnings or errors.

**Non-Goals:**
- Rewriting the body content or architecture diagrams of `README.md`.
- Changing application business logic or backend code.

## Decisions

### Decision 1: Standardized Shields.io Badge Architecture
- Use `style=flat-square` across all badges for visual consistency with the dark-first engineering portfolio.
- Color palette:
  - Live: `#10b981` (Emerald / Online status)
  - Backend: `#512bd4` (.NET purple)
  - Frontend: `#00DC82` (Nuxt green)
  - Database: `#4169e1` (PostgreSQL blue)
  - CI/CD: Dynamic GitHub Actions status badge
  - Workflow: `#7c3aed` (OpenSpec purple)

### Decision 2: Clean Lockfile Regeneration and Verification
- Regenerate `frontend/package-lock.json` cleanly via `npm install` on the frontend directory.
- Verify lockfile determinism by executing `npm ci` locally.
- Validate that the container build (`frontend/Dockerfile`) succeeds end-to-end.

## Risks / Trade-offs

- **Risk**: GitHub Actions cache might contain stale npm cache.
  - **Mitigation**: GitHub Actions `actions/setup-node@v4` with `cache-dependency-path: frontend/package-lock.json` automatically computes the cache key from the SHA of `package-lock.json`. Committing the updated lockfile invalidates any stale cache and guarantees a clean cache populate.
