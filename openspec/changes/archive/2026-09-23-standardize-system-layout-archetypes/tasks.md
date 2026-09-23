# Tasks

## 1. Frontend Layout Primitives

- [x] 1.1 Integrate and refine `StudioLayout.vue` (`frontend/components/layout/StudioLayout.vue`) with responsive 68/32 split, mobile stack, and named slots (`#header`, `#main`, `#dock`, `#footer`).
- [x] 1.2 Integrate and refine `MasterDetailLayout.vue` (`frontend/components/layout/MasterDetailLayout.vue`) with persistent 256px sub-nav rail and expansive responsive content surface.
- [x] 1.3 Integrate and refine `BoardLayout.vue` (`frontend/components/layout/BoardLayout.vue`) with header filter toolbar and 2-to-3 column auto-flowing responsive grid.

## 2. Design System Showcase & Select Invariant

- [x] 2.1 Replace unstyled native `<select><option>` elements in `frontend/components/showcase/PrimitivesShowcase.vue` with accessible `AppSelect.vue` custom dropdowns.
- [x] 2.2 Integrate `LayoutArchetypesShowcase.vue` (`frontend/components/showcase/LayoutArchetypesShowcase.vue`) and wire Section 09 ("System Layout Archetypes") into `frontend/pages/showcase.vue`.

## 3. Flashcards Review Studio Integration

- [x] 3.1 Refactor `/review` active session (`frontend/pages/review.vue`) to adopt `StudioLayout`, positioning `FlashcardDeck.vue` in `#main`.
- [x] 3.2 Build companion telemetry dock in `/review` featuring session progress, SM-2 metrics card, and keyboard shortcuts cheatsheet card in `#dock`.

## 4. Verification & Testing

- [x] 4.1 Verify layout responsiveness, slot rendering, and custom select interactions with automated frontend tests (`npm test`).
- [x] 4.2 Verify full application build (`npm test` and frontend typecheck) and visual stability across viewports.
