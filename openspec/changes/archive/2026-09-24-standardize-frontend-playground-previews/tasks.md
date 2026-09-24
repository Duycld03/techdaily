# Tasks: Standardize Frontend Playground Previews

## 1. Documentation & Invariants

- [x] 1.1 Update `AGENTS.md` Section 3 (*Strict Rules & Anti-Patterns to NEVER Repeat*) to add **Rule 19: Never Use Disconnected HTML Files for UI Previews**, mandating that all UI evaluations and previews use `frontend/pages/playground/*.vue` sharing the canonical Tailwind, Vite, and component system.
- [x] 1.2 Document the Vue Playground preview workflow and mock data conventions in `AGENTS.md`, establishing clear procedures for scaffolding dev previews and promoting approved designs to production pages.

## 2. Infrastructure & Clean-Up

- [x] 2.1 Verify `frontend/middleware/auth.global.ts` ensures `/playground` and `/showcase` paths bypass authentication barriers during local development.
- [x] 2.2 Verify `frontend/nuxt.config.ts` `pages:extend` hook reliably excises `/playground` and `/showcase` routes from production builds when `NODE_ENV === 'production'`.
- [x] 2.3 Delete obsolete standalone `preview-phase1-board-layout.html` to eliminate disconnected legacy preview files and enforce the unified Vue Playground standard.

## 3. Verification & Automated Testing

- [x] 3.1 Verify dev playground route accessibility and verify full frontend regression test suite (`npm test`) passes with 0 regressions.
