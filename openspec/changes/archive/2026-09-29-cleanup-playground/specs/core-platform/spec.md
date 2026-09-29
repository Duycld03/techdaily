# Spec Delta: Remove Playground Mechanism from Core Platform

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

The frontend build pipeline (`nuxt.config.ts`) and navigation shell (`useNavigationMenu.ts`) SHALL enforce that `/showcase` is strictly isolated to development environments:
1. **Build-Time Route Pruning**:
   - In production builds (`process.env.NODE_ENV === 'production'`), `/showcase` route definitions SHALL be stripped during build time via the Nuxt `pages:extend` hook, ensuring zero JavaScript chunks or client-side route manifest entries are emitted into production artifacts.
2. **Environment-Gated Navigation Menu**:
   - The application navigation composable (`useNavigationMenu.ts`) SHALL only include `{ name: 'nav.showcase', path: '/showcase', icon: Palette }` when running in local development mode (`import.meta.dev`), omitting it from the sidebar in production builds.
3. **Route Defense & Zero Metadata Leakage**:
   - Direct URL requests to `/showcase` on production deployments SHALL return standard 404 Not Found status without leaking internal design system components, source maps, or prototype state.

#### Scenario: Developer or Agent inspects design system showcase
- **WHEN** user navigates to `/showcase` in a local development environment
- **THEN** the page renders all 8 component showcases in both light and dark modes
- **AND** interactive demo states (selection, modal open, copy feedback) function seamlessly.

#### Scenario: User attempts to access design system showcase in production
- **WHEN** a user or crawler accesses `/showcase` on a production deployment
- **THEN** the system returns a standard 404 Not Found error
- **AND** client-side DevTools, route tables, and source maps contain zero references to showcase components.

### Requirement: Developer & Agent UI Design Governance Protocol

The project repository SHALL mandate strict engineering governance rules codified in `AGENTS.md`, organized into five foundational pillars:
1. **Pillar 1: Production Security & Auth Boundaries**:
   - Zero fake or default fallback users. Endpoints requiring auth must enforce `.RequireAuthorization()` and return `401 Unauthorized` when no valid JWT is present.
   - Zero development 1-click bypasses or local-dev banners in production code.
2. **Pillar 2: UI Design System & Component Governance**:
   - Mandatory System Layout Archetypes (`StudioLayout`, `BentoDashboardLayout`, `MasterDetailLayout`, `BoardLayout`). Unconstrained ad-hoc wrapper divs causing empty black voids on 1080p desktop viewports are strictly prohibited.
   - Strict prohibition of raw native HTML `<select>` (MUST use `AppSelect.vue`) and native browser dialogs (`alert`, `confirm`, `prompt`).
   - Responsive typography standards (≥14px on mobile, ≥16px on desktop/tablet) and bilingual layout protection (`whitespace-nowrap shrink-0` across English and Vietnamese).
3. **Pillar 3: Frontend Testing Boundaries & Visual Inspection**:
   - Automated Vitest unit tests MUST strictly defend data contracts, form serialization payloads, validation barriers, auth state, and route guards.
   - Vitest tests MUST NOT assert CSS/Tailwind classes to evaluate layout geometry.
   - Visual layout, responsiveness, and spacing MUST be verified through direct screenshot inspection.
4. **Pillar 4: AI Engine & External Ingestion**:
   - High-speed model selection (`gemini-3.5-flash-lite`, <5s latency, ≥120s proxy timeout) with user-triggered generation.
   - Balanced bracket depth scanning for LLM JSON outputs.
   - Canonical URL resolution and clean markdown extraction for web crawlers.
5. **Pillar 5: Verification, DevOps & Skills Protocol**:
   - Local-first verification before git push (`dotnet test`, `npm test`).
   - Nginx container upstream cache restart and modern ED25519 SSH keys.
   - Mandatory pre-flight reading of canonical skill documentation in `.agents/skills/`.

#### Scenario: Agent Implements a New View
- **WHEN** an AI agent or developer is instructed to create or refactor a frontend view
- **THEN** the agent selects an established layout archetype, verifies dropdowns use `AppSelect.vue`, and implements directly on production views with dual-gate verification.

#### Scenario: Agent implements a new feature or refactor
- **WHEN** an AI agent or developer is instructed to create or refactor frontend or backend code
- **THEN** the agent adheres to the Core Engineering Pillars in `AGENTS.md`
- **AND** the agent executes local verification before committing or pushing changes.

## REMOVED Requirements

### Requirement: Frontend Dev Playground UI Previews
This requirement is removed. The `/playground` sandbox environment and routes are excised from the project.

### Requirement: Prohibition of Disconnected Static Preview HTML Files
This requirement is removed.
