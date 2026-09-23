# Proposal: Isolate Showcase & UI Sandboxes to Dev-Only Environments

## Why

The interactive Design System Showcase (`/showcase`) and UI Playground Sandboxes (`/playground/*`) are critical engineering tools for developing and verifying UI primitives, layout archetypes, and component density. However, these surfaces are internal developer artifacts that must not be exposed in production builds, indexed by search engines, or bundled into client production assets where browser developer tools, route manifests, or URL probing could inspect unfinished or internal engineering components.

## What Changes

- **Build-Time Route Pruning (`nuxt.config.ts`)**:
  - Configure the Nuxt `pages:extend` hook to strip `/showcase` and `/playground` route definitions entirely when building for production (`process.env.NODE_ENV === 'production'`).
  - Guarantee zero JavaScript chunks or client router entries are emitted in production distributions.
- **Environment-Aware Sidebar Navigation (`useNavigationMenu.ts`)**:
  - Conditionally register `{ name: 'nav.showcase', path: '/showcase', icon: Palette }` only when running in development mode (`import.meta.dev`).
  - Keep production sidebar menus clean, showing only user-facing application groups.
- **Strict 404 Route Isolation**:
  - Ensure that manual URL probing to `/showcase` or `/playground` on production environments returns a standard `404 Page Not Found`, preventing route enumeration.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update `Requirement: Interactive Design System Showcase` and navigation shell invariants to specify build-time dev-only environment isolation and production bundle elimination.

## Impact

- **Frontend Bundle**: Reduces production bundle size by eliminating showcase and sandbox components (`ShowcaseSection.vue`, `PrimitivesShowcase.vue`, `LayoutArchetypesShowcase.vue`, `dashboard.vue`, etc.) from production chunks.
- **Security & Hygiene**: Completely closes route enumeration and prevents internal UI primitives from being discovered via browser DevTools.
- **Developer Workflow**: Developers retain full access to `/showcase` and `/playground` on local development (`npm run dev`) with zero workflow friction.
