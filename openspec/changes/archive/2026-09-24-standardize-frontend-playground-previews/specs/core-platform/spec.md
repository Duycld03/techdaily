# Spec Delta: core-platform (Standardize Frontend Playground Previews)

## ADDED Requirements

### Requirement: Frontend Dev Playground UI Previews
The system SHALL support integrated frontend UI prototyping and previewing through dedicated development playground pages located under `frontend/pages/playground/*.vue`. Playground pages MUST utilize local reactive mock data, share the project's canonical Tailwind CSS and Vite asset pipeline, and require no authentication during local development.

#### Scenario: Reviewing prospective UI in local development
- **WHEN** a developer or reviewer navigates to `http://localhost:3000/playground/<feature>`
- **THEN** the playground page MUST render with full design system typography (including `JetBrains Mono` and `Inter`), authentic layout archetypes (`BoardLayout`, `StudioLayout`), and reactive mock data without requiring a user login or backend API connectivity.

#### Scenario: Exclusion from production builds
- **WHEN** the frontend application is compiled for production deployment (`NODE_ENV === 'production'`)
- **THEN** all routes matching `/playground` and `/showcase` MUST be automatically stripped by Nuxt page generation hooks and excluded from the production distribution.

### Requirement: Prohibition of Disconnected Static Preview HTML Files
Developers and AI agents MUST NOT create disconnected, standalone `.html` files utilizing third-party CDN stylesheets (such as Tailwind CDN or external unbundled fonts) for UI previews. All UI evaluations and user design reviews MUST be performed directly within the Vue playground environment to guarantee 100% visual parity with production components.

#### Scenario: Prototyping a prospective UI phase
- **WHEN** preparing a visual preview for user design review
- **THEN** the preview MUST be created as a Vue Single File Component under `frontend/pages/playground/` consuming project component primitives rather than an isolated HTML file.
