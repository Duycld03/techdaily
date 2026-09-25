# Proposal

## Why
Editing a note inline inside the `/notes` grid dynamically inflates the active card's vertical height, which forces sibling cards in the same CSS Grid row track to stretch via `align-items: stretch`. Combined with `flex flex-col justify-between`, this creates unsightly dead negative space and disorienting layout shifts across the entire row. Furthermore, the current note cards suffer from heavy visual clutter (high-contrast purple accent borders, raw quotes in italics, and bulky solid action buttons taking up ~35% of the card area), whereas the official Showcase design system (`LayoutArchetypesShowcase.vue`) provides a significantly cleaner, typography-driven note card pattern.

## What Changes
- **Replace Inline Editing with Edit Modal**: Retire the in-place expandable edit form inside individual note cards on `/notes`. Replace it with a dedicated teleported modal using `AppModal.vue` (`EditNoteModal`) that provides ample writing space for architectural takeaways and interactive tag management without altering grid row dimensions.
- **Adopt Showcase Card Visual Hierarchy**: Refactor note cards in `frontend/pages/notes.vue` to adopt the visual structure of `LayoutArchetypesShowcase.vue` (Demo 3):
  - **Header**: Clean category tag badge pill on the left, subtle document source context (`bookTitle • chapterTitle`) on the right, and an unobtrusive delete action (`Trash2`) revealing on card hover/focus.
  - **Body**: Prominent reflection note typography as the core focal point when present, paired with a clean excerpt quote without overbearing italic formatting or thick purple accent rails.
  - **Footer**: Subtle ghost/outline action buttons (`Edit Note` and `Flashcard SM-2` / `✓ In SM-2`) and tag chips that do not visually dominate the content.
- **Maintain Full Backward Compatibility**: Keep all existing store actions (`useNotesStore`), pagination, tag filtering, SM-2 flashcard synthesis, and deletion workflows completely intact.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `notes`: Modernize the reading highlight card visual hierarchy to match the Showcase design system standard, and replace the in-card inline editing requirement with a dedicated edit modal (`AppModal`).

## Impact
- **Frontend Code**: `frontend/pages/notes.vue` (card markup, modal integration, state cleanup of inline editing variables).
- **Dependencies & Components**: Reuses existing `frontend/components/ui/AppModal.vue` without requiring new external libraries.
- **Backend / API**: Zero API changes. All existing endpoints (`PUT /api/v1/notes/highlights/{id}`, `DELETE /api/v1/notes/highlights/{id}`, `POST /api/v1/review/cards/from-highlight`) remain unchanged.
