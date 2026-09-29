# Design: Remove Playground Mechanism and Dead Files

## Context

The repository contains a legacy dev-only sandbox located at `frontend/pages/playground/`. It includes obsolete prototyping files (`audio-narration.vue`, `temp.vue`, `index.vue`, `dashboard.vue`, `README.md`) and configuration hooks spread across `nuxt.config.ts`, `useNavigationMenu.ts`, `auth.global.ts`, and translation files.

Additionally, `AGENTS.md` Pillar 2 Item 4 currently mandates that developers and agents prototype UI in the playground before touching production components. With dual-gate verification (Vitest data contracts + automated headless browser visual inspection), this playground requirement is redundant and creates leftover file sprawl.

## Goals / Non-Goals

**Goals:**
- Completely delete the `frontend/pages/playground/` directory and all files within it.
- Remove all `/playground` references from navigation menus, auth middleware, and Nuxt configuration.
- Retain `/showcase` (`frontend/pages/showcase.vue`) as the dedicated design system showcase.
- Update `AGENTS.md` (Pillar 2) to eliminate the mandatory playground protocol.
- Update unit tests so `npm test` passes with 100% success rate and zero warnings.

**Non-Goals:**
- Removing or altering `/showcase` (the Living Design System showcase remains active in development).
- Modifying production pages or layout components (`StudioLayout`, `BentoDashboardLayout`, etc.).

## Decisions

### Decision 1: File Deletion
Delete the entire directory:
- `frontend/pages/playground/audio-narration.vue`
- `frontend/pages/playground/temp.vue`
- `frontend/pages/playground/index.vue`
- `frontend/pages/playground/dashboard.vue`
- `frontend/pages/playground/README.md`

### Decision 2: Navigation & Localization Cleanup
In `frontend/composables/useNavigationMenu.ts`:
- Remove `{ name: 'nav.playground', path: '/playground/temp', icon: FlaskConical }`.
- Remove the `isLinkActive` matching branch for `/playground/temp`.
- Retain `{ name: 'nav.showcase', path: '/showcase', icon: Palette }`.

In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`:
- Remove `"playground": "UI Playground"` under `"nav"`.

### Decision 3: Middleware & Nuxt Configuration Cleanup
In `frontend/middleware/auth.global.ts`:
- Simplify `isDevExempt` to check only `to.path.startsWith('/showcase')`.

In `frontend/nuxt.config.ts`:
- Remove the `/playground/**` COOP/COEP headers block under `routeRules`.
- In `pages:extend` hook, change `const devPrefixes = ['/showcase', '/playground']` to `const devPrefixes = ['/showcase']`.

### Decision 4: Developer Governance Update (`AGENTS.md`)
In `AGENTS.md` (Section 3, Pillar 2: UI Design System & Component Governance):
- Delete Item 4: "Mandatory Vue Playground Protocol (No Static HTML Files)".
- Re-number or adjust remaining items to enforce direct component editing verified by Pillar 3 (Dual-Gate Verification: Vitest + Headless Browser screenshots).

### Decision 5: Test Suite Updates
- `frontend/tests/composables/useNavigationMenu.spec.ts`: Update navigation group expectations to only test `/showcase` in dev mode.
- `frontend/tests/config/route-pruning.spec.ts`: Remove `/playground` entries from mocked page route array and assertions.
- `frontend/tests/middleware/auth.spec.ts`: Remove `/playground` test cases, retaining `/showcase`.

## Risks / Trade-offs

- **Direct Hit 404**: Any existing browser tabs pointing to `/playground/*` will return a standard 404 Not Found error. This is intentional.
