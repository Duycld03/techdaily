# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Interactive Design System Showcase
The platform SHALL maintain an interactive living design system showcase at `/showcase` exclusively in development environments (`NODE_ENV !== 'production'`) displaying:
1. Base UI Primitives (Buttons, inputs with ⌘K badge, tags, segmented switcher)
2. Interactive Multiple-Choice Option Cards with active/correct/incorrect states
3. Production Modal Shell with sticky actions
4. Bento Metric Cards with tabular numerals
5. Code Snippet Block with clipboard copy feedback
6. Skeleton Loading Shimmers
7. Empty and Error State Cards
8. Reader Floating Selection Toolbar

The frontend build pipeline (`nuxt.config.ts`) and navigation shell (`useNavigationMenu.ts`) SHALL enforce that `/showcase` and `/playground` routes are strictly isolated to development environments:
1. **Build-Time Route Pruning**:
   - In production builds (`process.env.NODE_ENV === 'production'`), `/showcase` and `/playground` routes SHALL be stripped during build time via the Nuxt `pages:extend` hook, ensuring zero JavaScript chunks or client-side route manifest entries are emitted into production artifacts.
2. **Environment-Gated Navigation Menu**:
   - The application navigation composable (`useNavigationMenu.ts`) SHALL only include `{ name: 'nav.showcase', path: '/showcase', icon: Palette }` when running in local development mode (`import.meta.dev`), omitting it from the sidebar in production builds.
3. **Route Defense & Zero Metadata Leakage**:
   - Direct URL requests to `/showcase` or `/playground` on production deployments SHALL return standard 404 Not Found status without leaking internal design system components, source maps, or prototype state.

#### Scenario: Developer or Agent inspects design system showcase
- **WHEN** user navigates to `/showcase` in a local development environment
- **THEN** the page renders all 8 component showcases in both light and dark modes
- **AND** interactive demo states (selection, modal open, copy feedback) function seamlessly.

#### Scenario: User attempts to access design system showcase in production
- **WHEN** a user or crawler accesses `/showcase` or `/playground` on a production deployment
- **THEN** the system returns a standard 404 Not Found error
- **AND** client-side DevTools, route tables, and source maps contain zero references to showcase or playground components.
