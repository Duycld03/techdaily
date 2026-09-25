# Tasks

## 1. Frontend Implementation

- [x] 1.1 Refactor note card container and header in `frontend/pages/notes.vue` to adopt the Showcase design system archetype (top row with category tag badge pill, source document context, and responsive hover delete button).
- [x] 1.2 Update card body typography in `frontend/pages/notes.vue` to prioritize user reflection notes as the primary insight while displaying the excerpt quote as clean supporting context without heavy italic styling.
- [x] 1.3 Redesign card footer in `frontend/pages/notes.vue` with subtle ghost/outline action buttons (`Edit Note` trigger and `Flashcard SM-2` / `✓ In SM-2` badge) and tag chips.
- [x] 1.4 Integrate `AppModal.vue` as the dedicated note editing modal in `frontend/pages/notes.vue`, featuring read-only source quote reference, spacious reflection textarea, tag input, and save/cancel actions.
- [x] 1.5 Wire modal reactive state (`isEditModalOpen`, `editingHighlight`, `saveModalEditing`, `cancelModalEditing`) to `useNotesStore` and remove obsolete inline editing state variables (`editingHighlightId`).
- [x] 1.6 Add `items-start` to the notes content grid container in `frontend/pages/notes.vue` to enforce natural card heights and prevent row stretching.

## 2. Testing and Visual Verification

- [x] 2.1 Run frontend Vitest test suite (`npm test` in `frontend/`) to verify state behavior, component rendering, and ensure zero regressions.
- [x] 2.2 Verify modal editing workflow (opening modal, saving changes, keyboard `Escape` dismissal, backdrop click) and check grid stability on desktop (1080p) and mobile (390px) viewports.
