# Proposal: Standardize System Layout Archetypes and Custom Selects

## Why

While individual UI primitives (buttons, cards, badges) are established, full-page desktop views across TechDaily suffer from unbalanced layout density, excessive empty black voids, and narrow single-column stretching on standard 1080p displays (1920x1080, ~1680px available width). Additionally, instances of unstyled native HTML `<select>` elements bypass CSS styling and display operating system default styling with jarring highlights. Establishing three standardized system layout archetypes (`StudioLayout`, `MasterDetailLayout`, and `BoardLayout`) and enforcing custom `AppSelect` components will deliver a cohesive, cockpit-grade engineering experience across all screens.

## What Changes

- **System Layout Primitives**:
  - Introduce `StudioLayout.vue` (`frontend/components/layout/StudioLayout.vue`) featuring a 68/32 split stage/dock for interactive learning workflows, bound cleanly to the viewport without nested double-scrollbars.
  - Introduce `MasterDetailLayout.vue` (`frontend/components/layout/MasterDetailLayout.vue`) providing a persistent 256px sub-navigation rail and expansive multi-column content panel for settings and configuration surfaces.
  - Introduce `BoardLayout.vue` (`frontend/components/layout/BoardLayout.vue`) providing an integrated header filter toolbar and responsive 2-to-3 column auto-flowing grid for content-dense browsing.
- **Showcase Integration**:
  - Replace raw unstyled native `<select><option>` controls in `PrimitivesShowcase.vue` with accessible, dark-mode compliant `AppSelect.vue` dropdowns.
  - Incorporate `LayoutArchetypesShowcase.vue` as Section 09 in `frontend/pages/showcase.vue` to demonstrate all three layout archetypes interactively.
- **Page Layout Modernization**:
  - Refactor `/review` (Spaced Repetition Flashcards) to adopt `StudioLayout`, placing the active card on the main action stage and anchoring session progress, SM-2 telemetry, and keyboard hotkeys in the companion dock.
  - Standardize `/settings` and `/notes` to eliminate awkward side margins and optimize screen utilization.

## Capabilities

### New Capabilities
- `system-layout-archetypes`: Defines the core layout shells (`StudioLayout`, `MasterDetailLayout`, `BoardLayout`) enforcing responsive viewport bounding, slot-based structure, and consistent density standards across all pages.

### Modified Capabilities
- `review`: Modernizes the active flashcard practice interface to pair the active review card with a dedicated telemetry dock containing session progress, SM-2 scheduling metrics, and keyboard shortcuts.
- `core-platform`: Requires all select dropdowns across the application to utilize the custom `AppSelect` component instead of native OS-rendered select elements, and mandates Section 09 layout archetype demos in the design system showcase.

## Impact

- **Affected Code**:
  - `frontend/components/layout/StudioLayout.vue`
  - `frontend/components/layout/MasterDetailLayout.vue`
  - `frontend/components/layout/BoardLayout.vue`
  - `frontend/components/showcase/LayoutArchetypesShowcase.vue`
  - `frontend/components/showcase/PrimitivesShowcase.vue`
  - `frontend/pages/showcase.vue`
  - `frontend/pages/review.vue`
- **Dependencies**: No external dependencies added; builds upon existing Vue 3, Nuxt 4, Tailwind CSS, and Lucide Icons primitives.
- **Non-breaking**: Existing page routes and APIs remain unchanged; layout components wrap existing page components via standard slots.
