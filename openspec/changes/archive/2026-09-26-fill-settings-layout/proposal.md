# Proposal

## Why

The `/settings` page (`frontend/pages/settings.vue`) does not fill its viewport region: on a 1080p desktop the `MasterDetailLayout` card is capped at `max-w-5xl` (1024px) and only grows as tall as its content, leaving a large empty black void below the card and horizontal dead margins beside it. This violates the existing `system-layout-archetypes` intent ("eliminate `> 400px` dead margins", "no awkward horizontal empty margins") and, more visibly, leaves an unbounded vertical void the current spec never addressed.

## What Changes

- Make the settings page wrapper a full-height flex column so `MasterDetailLayout`'s existing `flex-1` actually stretches the card to fill the available viewport region (no bottom void). The app shell (`app.vue`) already renders pages inside `<main class="flex-1 overflow-y-auto">`, which supplies a definite height for the card to fill.
- Remove the `max-w-5xl mx-auto` horizontal cap on the settings container so the card spans the full content region (full-width / `max-w-7xl` fluid bounds), eliminating side voids on wide desktops.
- Keep settings content anchored to the top-left of the content panel; additional content simply appends downward inside the panel's existing `overflow-y-auto` scroll region.
- Preserve readable form-field width inside the panel (content is anchored top-left, not stretched edge-to-edge); the remaining empty space sits inside the filled card rather than as dead canvas outside it.
- Tighten the `system-layout-archetypes` spec so the Master-Detail archetype explicitly requires vertical viewport fill (no bottom void), not only horizontal void elimination.

No backend, API, data-model, or dependency changes. No breaking changes to public contracts.

## Capabilities

### New Capabilities
<!-- None. This change modifies an existing capability's requirements. -->

### Modified Capabilities
- `system-layout-archetypes`: The Master-Detail Layout archetype requirements gain an explicit viewport-height fill clause — a `MasterDetailLayout` page container SHALL occupy the full available viewport region (full height and full-width / `max-w-7xl` fluid bounds) so the card eliminates both horizontal dead margins and the bottom vertical void, with content anchored top-left in a scrollable content panel.

## Impact

- `frontend/pages/settings.vue`: page wrapper and container utility classes (viewport-fill + drop `max-w-5xl` cap); the reference consumer of the `MasterDetailLayout` archetype.
- `frontend/components/layout/MasterDetailLayout.vue`: no structural change required (`flex-1` already present); verified as the mechanism that stretches once the parent is a full-height flex column.
- `openspec/specs/system-layout-archetypes/spec.md`: requirement text + scenario for the Master-Detail archetype.
- Visual verification only (headless Chromium screenshots, Desktop 1080p + Mobile 390px, both `en`/`vi`); no data-contract behavior changes, so no new Vitest suites are required.
