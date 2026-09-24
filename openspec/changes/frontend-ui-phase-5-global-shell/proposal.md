# Proposal: Frontend UI Phase 5 - Global Shell, Navigation & Authentication

## Why

Global shell components (`AppHeader.vue`, `AppSidebar.vue`), authentication surfaces (`login.vue`), and user profile settings panels currently have minor density discrepancies, uneven edge padding, and inconsistent modal/drawer transitions across Windows 11 desktop and mobile viewports. Standardizing the global shell and adopting `MasterDetailLayout` for profile settings provides a polished, cohesive end-to-end user experience.

## What Changes

- **Global Navigation Chrome (`frontend/components/layout/AppHeader.vue`, `frontend/components/layout/AppSidebar.vue`)**:
  - Unify elevation, compact density, keyboard shortcut tooltips, and seamless mobile drawer transitions.
- **Authentication Surface (`frontend/pages/login.vue`)**:
  - Standardize centered, viewport-bounded card aesthetics and responsive authentication flows.
- **Profile & Settings (`frontend/components/profile/*`)**:
  - Standardize profile surfaces and settings configuration panels using `MasterDetailLayout` with a persistent category rail and expansive multi-column settings panel.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Formalize `MasterDetailLayout` requirements for multi-category settings and global shell density boundaries.
- `auth`:
  - Formalize viewport bounding specifications for auth cards.

## Impact

- **Affected Surfaces**: `frontend/components/layout/AppHeader.vue`, `frontend/components/layout/AppSidebar.vue`, `frontend/pages/login.vue`, `frontend/components/profile/*`.
- **Dependencies**: None.
- **Breaking Changes**: None.
