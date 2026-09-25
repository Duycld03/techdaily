# Design

## Context
See `proposal.md` for motivation. Currently, `frontend/pages/notes.vue` renders highlight cards inside a 3-column CSS Grid. When an engineer clicks "Edit Note", an inline form expands inside that single card, expanding the row track height and forcing adjacent cards in that row to stretch vertically. Inside those adjacent cards, `flex flex-col justify-between` causes the footers to be pushed to the bottom, leaving an awkward empty void in the middle of the card.

Furthermore, the card's visual styling contains visual noise (thick purple border accents, long truncated book/chapter headers, and large solid purple action buttons) that contrasts with the clean design system established in `frontend/components/showcase/LayoutArchetypesShowcase.vue`.

The project already contains a robust, accessible modal primitive (`frontend/components/ui/AppModal.vue`) supporting Teleport, focus management, `Escape` key handling, backdrop blur, and animated transitions.

## Goals / Non-Goals

**Goals:**
- **Zero Row Inflation**: Completely eliminate row stretching and layout shifts by moving the edit form from inside the card grid into a dedicated `AppModal`.
- **Showcase Archetype Consistency**: Refactor the card layout in `frontend/pages/notes.vue` to mirror the clean Showcase Demo 3 structure (tag pill, clear source metadata, prominent user note insight, subtle excerpt quote, and subtle action buttons).
- **Responsive Hygiene**: Ensure the delete button is visible on mobile touch devices (`opacity-100 sm:opacity-0 sm:group-hover:opacity-100`) while staying minimal and unobtrusive on desktop.
- **Maintain Full Contract & State Integrity**: Keep all Pinia store actions (`saveEditing`, `openDeleteModal`, `handleCreateFlashcard`), query filters, and tag selection interactions intact.

**Non-Goals:**
- Changing backend endpoints, DTO contracts, or database migrations (all changes are strictly frontend UI/UX and presentation).
- Altering the SM-2 flashcard generation algorithm or backend fail-fast semantics.

## Decisions

### 1. Card Layout & Visual Hierarchy
- **Header**:
  - Left: Primary tag pill badge (`bg-brand-500/10 text-brand-600 dark:text-brand-300 font-semibold px-2 py-0.5 rounded-full text-xs`). If the highlight has tags, the first tag is displayed here; if untagged, displays a default category indicator or "Highlight".
  - Right: Clean source label (`text-xs text-slate-400 dark:text-slate-500 truncate max-w-[200px]`) displaying `item.bookTitle • item.chapterTitle`.
  - Actions: Delete icon (`Trash2`) on the far right with responsive visibility (`opacity-100 sm:opacity-0 sm:group-hover:opacity-100 transition-opacity`) to avoid visual noise until hovered on desktop.
- **Body**:
  - When `item.note` exists: Displayed with primary font weight (`text-sm font-semibold text-slate-900 dark:text-white leading-relaxed line-clamp-3`), serving as the key takeaway.
  - Excerpt quote: Styled as supporting documentation context (`text-xs text-slate-500 dark:text-slate-400 leading-relaxed line-clamp-3`), eliminating heavy italic blocks and thick purple left rails.
- **Footer**:
  - Left: Tag chips for remaining tags (`text-[11px] text-slate-500 dark:text-slate-400 hover:text-brand-500`).
  - Right:
    - `Edit Note`: Subtle ghost button (`border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.06] text-xs px-2.5 py-1.5 rounded-lg text-slate-600 dark:text-slate-300`).
    - `Flashcard SM-2`: Subtle outline button (`border border-brand-500/30 text-brand-600 dark:text-brand-400 hover:bg-brand-500/10 text-xs px-2.5 py-1.5 rounded-lg`) or emerald badge (`✓ In SM-2`).

### 2. Modal Architecture with `AppModal.vue`
- Integrate `AppModal` with `maxWidth="max-w-xl"` and reactive state:
  ```typescript
  const isEditModalOpen = ref(false)
  const editingHighlight = ref<HighlightDto | null>(null)
  const editNoteText = ref('')
  const editTagInput = ref('')
  const isSavingEdit = ref(false)
  ```
- **Modal Content Layout**:
  - Context Box: Displays the read-only excerpt quote (`text-xs text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-canvas-subtle p-3 rounded-xl border border-slate-200/80 dark:border-white/[0.08] line-clamp-4`) so the user can easily reference the original text while writing their reflection.
  - Textarea: Spacious 4-row input (`rows="4"`) with comfortable padding and dark mode styling.
  - Tag Input: Tag management field allowing quick tag editing.
  - Modal Footer: Cancel button and Save button with active loading spinner and disabled state.

### 3. Grid Alignment Stability
- Update the grid container in `frontend/pages/notes.vue`:
  ```html
  <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 items-start">
  ```
  Adding `items-start` guarantees that even if cards have slight variations in content length, cards never stretch their row neighbors, completely preventing internal whitespace voids.

## Risks / Trade-offs

| Risk / Trade-off | Mitigation |
| :--- | :--- |
| **Touch Devices Hover Hidden Button**: If the delete button is `opacity-0 group-hover:opacity-100`, mobile users without mouse hover cannot see or tap it. | Use responsive classes: `opacity-100 sm:opacity-0 sm:group-hover:opacity-100`. On mobile screens (<640px), the delete icon is always visible. On desktop (sm+), it cleanly fades in on hover. |
| **Accidental Dismissal of Long Reflections**: User accidentally clicks backdrop or presses Escape while writing a long reflection note. | Modal maintains local state during the session. If desired, prompt or only discard upon explicit Cancel click or successful Save. |
| **Tag List Overflow**: A card with numerous tags might wrap and inflate card height. | Use `flex-wrap gap-1.5` with a `max-h` or subtle scroll, or display the primary tag in header and secondary tags in footer. |
