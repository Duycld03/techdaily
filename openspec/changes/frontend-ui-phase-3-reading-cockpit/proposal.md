# Proposal: Frontend UI Phase 3 - Reading Cockpit & Document Studio

## Why

The technical document reading interface (`read/[bookId].vue`) is currently a monolithic file exceeding 1,500 lines that tightly couples document fetching, navigation controls, typography popovers, TOC sidebars, and reading article prose. On Windows 11 desktop and 2K screens, typography and sidebar margins exhibit irregular spacing. De-monolithizing the reader into dedicated sub-components and standardizing the daily cockpit (`today.vue`) will improve readability, maintainability, and layout symmetry.

## What Changes

- **Reader De-monolithization (`frontend/pages/read/[bookId].vue`)**:
  - Extract `frontend/components/reader/ReaderHeaderBar.vue` for document title, slice progress, novel-style typography controls, theme toggles, and slice navigation.
  - Extract `frontend/components/reader/ReaderTocSidebar.vue` for desktop collapsible TOC sidebar and mobile touch-dismiss drawer with section completion indicators.
  - Center reading prose in `#main` (`max-w-3xl mx-auto`) with responsive typography scaling.
- **Daily Reading Cockpit (`frontend/pages/today.vue`)**:
  - Standardize cockpit split spacing, eliminating dead margins between daily recommendations, streak status, and quick-action widgets.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Codify reading cockpit layout specifications and maximum prose width constraints (`max-w-3xl`).
- `reader`:
  - Formalize modular reader chrome component requirements.

## Impact

- **Affected Surfaces**: `frontend/pages/read/[bookId].vue`, `frontend/components/reader/*`, `frontend/pages/today.vue`.
- **Dependencies**: None. Leverages existing `useMarkdownRenderer` and `@vueuse/core`.
- **Breaking Changes**: None.
