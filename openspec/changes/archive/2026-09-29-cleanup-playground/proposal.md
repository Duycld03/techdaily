# Proposal: Remove Playground Mechanism and Cleanup Dead Files

## Why

The development playground sandbox (`frontend/pages/playground/*`) was originally introduced as a temporary prototyping environment for UI layouts. In practice, it has become dead weight: creating orphan files, cluttering the navigation bar, complicating route pruning and auth guards, and duplicating testing effort.

By removing the playground mechanism completely, TechDaily simplifies its frontend architecture, eliminates obsolete prototype code, and aligns development directly with authentic production views verified via automated headless browser inspection.

## What Changes

- **Delete Playground Directory and Files**:
  - Delete `frontend/pages/playground/` and all contained files (`audio-narration.vue`, `temp.vue`, `index.vue`, `dashboard.vue`, `README.md`).
- **Clean Configuration & Routing (`frontend/nuxt.config.ts`)**:
  - Remove the `/playground/**` COOP/COEP route rule.
  - Remove `/playground` from the `pages:extend` production route pruning hook.
- **Clean Navigation Shell (`frontend/composables/useNavigationMenu.ts`)**:
  - Remove the `nav.playground` menu entry and active link resolution logic.
  - Retain `/showcase` for design system component inspection.
- **Clean Localization (`frontend/i18n/locales/en.json` & `vi.json`)**:
  - Remove the `nav.playground` translation key from English and Vietnamese catalogs.
- **Clean Auth Middleware (`frontend/middleware/auth.global.ts`)**:
  - Remove `/playground` route prefix from development auth exemptions.
- **Update Unit Tests**:
  - Update `frontend/tests/composables/useNavigationMenu.spec.ts`, `frontend/tests/config/route-pruning.spec.ts`, and `frontend/tests/middleware/auth.spec.ts` to remove playground expectations.
- **Update Developer Governance (`AGENTS.md`)**:
  - Remove Pillar 2 Item 4 ("Mandatory Vue Playground Protocol") from `AGENTS.md`.
- **Update OpenSpec Core Platform Specs**:
  - Update `openspec/specs/core-platform/spec.md` and `openspec/specs/system-layout-archetypes/spec.md` to remove playground sandbox requirements.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Remove the development playground prototyping requirement and update agent workflow rules.
- `system-layout-archetypes`: Remove the playground sandbox prototyping invariant.

## Impact

- **Affected Files**:
  - `AGENTS.md`
  - `frontend/pages/playground/*` (removed)
  - `frontend/nuxt.config.ts`
  - `frontend/composables/useNavigationMenu.ts`
  - `frontend/i18n/locales/en.json`
  - `frontend/i18n/locales/vi.json`
  - `frontend/middleware/auth.global.ts`
  - `frontend/tests/composables/useNavigationMenu.spec.ts`
  - `frontend/tests/config/route-pruning.spec.ts`
  - `frontend/tests/middleware/auth.spec.ts`
- **Breaking Changes**: None for end users (playground was a dev-only route).
