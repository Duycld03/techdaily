# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Developer & Agent UI Design Governance Protocol
The project repository SHALL mandate strict engineering governance rules codified in `AGENTS.md`, organized into five foundational pillars:
1. **Pillar 1: Production Security & Auth Boundaries**:
   - Zero fake or default fallback users. Endpoints requiring auth must enforce `.RequireAuthorization()` and return `401 Unauthorized` when no valid JWT is present.
   - Zero development 1-click bypasses or local-dev banners in production code.
2. **Pillar 2: UI Design System & Component Governance**:
   - Mandatory System Layout Archetypes (`StudioLayout`, `BentoDashboardLayout`, `MasterDetailLayout`, `BoardLayout`). Unconstrained ad-hoc wrapper divs causing empty black voids on 1080p desktop viewports are strictly prohibited.
   - Strict prohibition of raw native HTML `<select>` (MUST use `AppSelect.vue`) and native browser dialogs (`alert`, `confirm`, `prompt`).
   - Responsive typography standards ($\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop/tablet) and bilingual layout protection (`whitespace-nowrap shrink-0` across English and Vietnamese).
   - Mandatory Vue playground protocol (`frontend/pages/playground/<feature>.vue`) for UI previews, completely prohibiting isolated static HTML files.
3. **Pillar 3: Frontend Testing Boundaries & Visual Inspection**:
   - Automated Vitest unit tests MUST strictly defend data contracts, form serialization payloads, validation barriers, auth state, and route guards.
   - Vitest tests MUST NOT assert CSS/Tailwind classes to evaluate layout geometry.
   - Visual layout, responsiveness, and spacing MUST be verified through direct screenshot inspection.
4. **Pillar 4: AI Engine & External Ingestion**:
   - High-speed model selection (`gemini-3.5-flash-lite`, <5s latency, $\ge 120\text{s}$ proxy timeout) with user-triggered generation.
   - Balanced bracket depth scanning for LLM JSON outputs.
   - Canonical URL resolution and clean markdown extraction for web crawlers.
5. **Pillar 5: Verification, DevOps & Skills Protocol**:
   - Local-first verification before git push (`dotnet test`, `npm test`).
   - Nginx container upstream cache restart and modern ED25519 SSH keys.
   - Mandatory pre-flight reading of canonical skill documentation in `.agents/skills/`.

#### Scenario: Agent Implements a New View
- **WHEN** an AI agent or developer is instructed to create or refactor a frontend view
- **THEN** the agent selects an established layout archetype, verifies dropdowns use `AppSelect.vue`, and prototypes in `frontend/pages/playground/` with visual screenshot proof before touching production routes.

#### Scenario: Agent implements a new feature or refactor
- **WHEN** an AI agent or developer is instructed to create or refactor frontend or backend code
- **THEN** the agent adheres to the 5 Core Engineering Pillars in `AGENTS.md`
- **AND** the agent executes local verification before committing or pushing changes.

---

## ADDED Requirements

### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) SHALL enforce a strict separation between behavioral data contracts and visual layout verification:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`) under the premise of verifying visual presentation or layout integrity.
   - Tests asserting styling classes under the pretense of UI verification SHALL be treated as invalid test theater and deleted or replaced with behavioral assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states during async requests, and RFC 7807 error problem details.

3. **Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct browser rendering and screenshot previews (e.g. at 1920x1080 desktop and 390x844 mobile viewports).

#### Scenario: Agent writes unit tests for an interactive form
- **WHEN** an agent writes tests for an authentication or settings form
- **THEN** tests assert input value binding, submission payload data, and validation error messages
- **AND** tests do NOT assert CSS classes like `max-w-*`, `flex`, `grid`, or `p-*`.

#### Scenario: Verifying responsive layout across viewports
- **WHEN** an agent develops or modifies a page component
- **THEN** layout stability and responsive presentation are verified through direct browser rendering or screenshot inspection at desktop and mobile breakpoints
- **AND** no Happy-DOM unit tests are written to verify pixel geometry.
