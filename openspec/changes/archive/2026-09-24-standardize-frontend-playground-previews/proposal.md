# Proposal: Standardize Frontend Playground Previews

## Why

Creating standalone, disconnected `.html` files for UI previews relies on external Tailwind CDNs, separate Google Fonts links, and inline CSS styles. This sandbox separation causes severe visual parity drift when migrating preview code to the real application, producing issues such as missing fonts/broken Vietnamese diacritics, accidental component double-nesting (e.g., nested `glass-card` wrappers), and theme token inconsistencies.

Standardizing prospective UI previews on Vue-based dev playground pages (`frontend/pages/playground/*.vue`) guarantees 100% design system fidelity, instant Vite Hot Module Replacement (HMR), authentic Lucide icon rendering, and zero production bundle contamination (guaranteed by Nuxt's `pages:extend` production strip hook).

## What Changes

- **Canonical Vue Dev Playground Pattern**:
  - Mandate that all interactive UI previews and prospective feature iterations MUST be developed as temporary Vue Single File Components under `frontend/pages/playground/<feature-name>.vue`.
  - Preview pages use local hardcoded mock reactive state (no backend API or auth prerequisites), allowing immediate user review at `http://localhost:3000/playground/<feature-name>`.
  - Preview pages consume authentic project components (`BoardLayout`, `StudioLayout`, `ShikiCodeBlock`, `OptionCard`), verifying layout boundaries before production cutover.
- **Update Developer & Agent Rules (`AGENTS.md`)**:
  - Codify Rule 19 in `AGENTS.md`: **Never Use Disconnected HTML Files for UI Previews**. Previews must always reside in `frontend/pages/playground/*.vue` sharing the canonical Vite/Tailwind/Vue toolchain.
- **Dev-Only Route Protection & Auth Bypass**:
  - Verify that `frontend/middleware/auth.global.ts` exempts `/playground` routes from authentication barriers during local development.
  - Verify that `frontend/nuxt.config.ts` `pages:extend` hook strips `/playground` routes in production builds, preventing test code leakage.

## Capabilities

### Modified Capabilities

- `core-platform`:
  - Formalize the developer tooling standard for frontend playground previews and design system parity verification.

## Impact

- **Affected Files**: `AGENTS.md`, `frontend/pages/playground/`, `frontend/middleware/auth.global.ts`, `frontend/nuxt.config.ts`.
- **Dependencies**: None. Leverages existing Nuxt 4 compatibility hooks and Tailwind configuration.
- **Breaking Changes**: None.
