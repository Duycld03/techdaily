# Proposal: Enforce UI Governance and System Layout Adoption

## Why

Without strict developer and AI agent governance, frontend feature development frequently introduces ad-hoc `max-w-*` wrappers, unstyled native HTML `<select>` controls, and unverified layout regressions that cause empty black voids, uneven column heights, and bloated single-column stacking on standard 1080p desktop screens. By enforcing a 4-tier governance architecture—combining `AGENTS.md` inviolable rules, an isolated `frontend/pages/playground/` sandbox for visual review before production merging, the creation of `<BentoDashboardLayout>`, and system-wide layout adoption across `/`, `/settings`, `/notes`, and `/profile`—TechDaily guarantees consistent, cockpit-grade visual density across all views.

## What Changes

- **AI Agent & Developer Governance (`AGENTS.md`)**:
  - Enshrine Rule 19: Mandatory System Layout Archetypes (`StudioLayout`, `MasterDetailLayout`, `BoardLayout`, `BentoDashboardLayout`). Arbitrary unconstrained wrappers causing desktop voids are strictly prohibited.
  - Enshrine Rule 20: Strict Prohibition of Native HTML `<select>`. All dropdowns must use `AppSelect.vue`.
  - Enshrine Rule 21: Sandbox Playground & Screenshot Preview Protocol. Non-trivial UI refactors or new views must be drafted in `frontend/pages/playground/`, visually verified via 1080p screenshot, and approved before production route cutover.
- **Layout System Completion**:
  - Introduce `BentoDashboardLayout.vue` (`frontend/components/layout/BentoDashboardLayout.vue`) featuring a top header slot, asymmetric 2-to-1 Bento grid, equalized vertical column heights, and responsive mobile stacking.
- **System-wide Layout Adoption**:
  - Refactor `/` (`HomeBentoDashboard.vue`) to adopt `BentoDashboardLayout`, tightening internal card margins and balancing left action tiles with right telemetry tiles.
  - Refactor `/settings` (`frontend/pages/settings.vue`) to adopt `MasterDetailLayout`, eliminating 900px of wasted side margins with a 256px sub-nav rail and 2-column form grids.
  - Refactor `/notes` (`frontend/pages/notes.vue`) to adopt `BoardLayout`, replacing the single-column list with an auto-flowing 2-to-3 column card grid with sticky search and tag filters.
  - Refactor `/profile` (`frontend/pages/profile.vue`) to equalize Tier 3 heights, eliminating awkward bottom voids.
- **Playground Infrastructure**:
  - Create `frontend/pages/playground/` with documentation and an initial interactive playground template for rapid component experimentation.

## Capabilities

### Modified Capabilities
- `system-layout-archetypes`: Extends the layout system to incorporate the 4th archetype (`BentoDashboardLayout`) and formalizes the Sandbox Playground Prototyping Invariant.
- `core-platform`: Mandates strict layout archetypes and `AppSelect` invariants across `/` and `/settings`, and documents developer UI governance rules.
- `notes`: Mandates the `BoardLayout` archetype for technical highlights and quote archives.

## Impact

- **Affected Code**:
  - `AGENTS.md`
  - `frontend/components/layout/BentoDashboardLayout.vue`
  - `frontend/pages/playground/README.md`
  - `frontend/components/dashboard/HomeBentoDashboard.vue`
  - `frontend/pages/settings.vue`
  - `frontend/pages/notes.vue`
  - `frontend/pages/profile.vue`
- **Dependencies**: No external npm packages required; leverages existing Vue 3, Nuxt 4, Tailwind CSS, and Lucide icons.
- **Breaking Changes**: None; existing routes, API endpoints, and user data models remain intact.
