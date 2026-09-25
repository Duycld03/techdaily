# Spec Delta: notes

## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Showcase Design System** note card archetype (`LayoutArchetypesShowcase.vue`):

1. **Obsidian Studio Canvas & Glass Controls**:
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base).
   - The search input SHALL render with subtle dark glass backgrounds (`bg-white dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08]`) and hairline borders.
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents (`bg-brand-600 text-white border-transparent shadow-sm`) and inactive chips with dark glass styling (`bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200 dark:border-white/[0.08] hover:border-white/[0.16]`).

2. **Showcase-Archetype Highlight Cards**:
   - Highlight cards SHALL render as clean elevation containers (`rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-elevated hover:border-brand-400 dark:hover:border-brand-500/40 transition-colors shadow-sm`) without artificial inner voids.
   - **Header**:
     - The top row SHALL render a subtle category tag badge pill on the left (e.g. `bg-brand-500/10 text-brand-600 dark:text-brand-300 rounded-full px-2 py-0.5 text-xs font-semibold`).
     - On the right, the document source context (`Book Title • Chapter Title`) SHALL render in subdued text (`text-xs text-slate-400 dark:text-slate-500 truncate`).
     - A compact delete button (`Trash2`) SHALL be positioned in the header and subtly reveal on card hover (`opacity-0 group-hover:opacity-100 transition-opacity` on pointer devices, always visible on touch devices) to minimize visual clutter.
   - **Body (Content & Hierarchy)**:
     - When a personal reflection note exists, it SHALL render as the primary insight block with high visual prominence (`text-sm font-semibold text-slate-900 dark:text-white leading-relaxed`).
     - The source excerpt quote SHALL render as clean supporting context beneath the note (`text-xs leading-relaxed text-slate-500 dark:text-slate-400 line-clamp-3`), without heavy italic styling or thick distracting purple borders.
   - **Footer (Actions & Tags)**:
     - The card footer SHALL feature a subtle hairline divider (`pt-3 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between gap-2`).
     - Left: Technical tag chips (`text-[11px] font-medium text-slate-500 hover:text-brand-600 dark:text-slate-400 dark:hover:text-brand-300`).
     - Right: Action triggers styled as subtle ghost/outline buttons:
       - **Edit Note**: Button opening the dedicated Edit Modal (`AppModal`), using subdued styling (`text-slate-600 dark:text-slate-300 hover:text-brand-600 hover:bg-slate-100 dark:hover:bg-white/[0.06] text-xs font-medium px-2.5 py-1.5 rounded-lg border border-slate-200 dark:border-white/[0.08]`).
       - **Flashcard SM-2**: Button using subtle brand border (`text-brand-600 dark:text-brand-400 border border-brand-500/30 hover:bg-brand-500/10 text-xs font-medium px-2.5 py-1.5 rounded-lg`), transitioning to a compact green badge (`bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 text-xs font-medium px-2.5 py-1.5 rounded-lg`) when converted to SM-2.

3. **Grid Layout Stability**:
   - The notes grid SHALL maintain consistent, natural row heights (`items-start` alignment).
   - In-place editing inside the card grid SHALL be strictly prohibited, eliminating row height inflation, sibling vertical stretching, and dead white/black space.

#### Scenario: User views clean showcase note cards in reading notes hub
- **WHEN** an authenticated user opens `/notes`
- **THEN** the cards render with the clean showcase archetype: primary tag pill and document source in the header, prominent reflection insight text, supporting clean excerpt quote, and subtle footer action buttons.
- **AND** cards in the same row track do not stretch unnaturally or display empty dead vertical space.

#### Scenario: Delete button reveals on hover
- **WHEN** an engineer hovers over a note card on a desktop browser
- **THEN** the delete trash icon fades in smoothly in the card header.
- **AND** clicking the button opens the delete confirmation dialog without triggering card navigation.

---

### Requirement: Highlight Reflection and Tag Updating
The system SHALL provide an accessible, dedicated modal interface for editing reading highlight notes and tags using `AppModal.vue`, replacing in-place card expansion.

1. **Modal Architecture**:
   - Triggering "Edit Note" on any card SHALL open a teleported dialog centered on the viewport (`AppModal`, `maxWidth="max-w-xl"`).
   - The modal SHALL display:
     - Header: Modal title ("Edit Architectural Note" / "Chỉnh sửa ghi chú kiến trúc") with close (`X`) button.
     - Source Excerpt (Read-Only): The original highlighted excerpt quote displayed in a subdued context box for reference.
     - Note Textarea: A spacious multi-row textarea (`rows="4"`) with placeholder (`reader.note_placeholder`) for drafting reflections.
     - Tags Input: An interactive tag input enabling users to type and manage tags (space or Enter separated).
     - Footer: Cancel button and Save button with active loading spinner state (`isSaving`).
   - Pressing `Escape` or clicking outside the modal backdrop SHALL dismiss the modal without saving unpersisted changes.

2. **Persistence and Cache Synchronization**:
   - Submitting the modal SHALL invoke `PUT /api/v1/notes/highlights/{id}` with updated `note` and `tags`.
   - On success, the modal SHALL close, the active card in `useNotesStore` SHALL update in-place without page reload, and a success toast (`notes.toast_update_success`) SHALL display.
   - If the update fails, an error toast SHALL be displayed, and the modal SHALL remain open to prevent data loss.

#### Scenario: User opens and saves reflection note via Edit Modal
- **WHEN** user clicks "Edit Note" on a highlight card
- **THEN** system opens the dedicated `AppModal` displaying the original excerpt quote, current reflection note, and current tags.
- **AND** the underlying grid layout does not shift, expand, or stretch sibling cards.
- **WHEN** user modifies the reflection note and clicks "Save Changes"
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}`
- **AND** backend saves changes and returns HTTP 200 OK
- **AND** modal closes, the card reflects the updated note and tags, and a success toast is presented.

#### Scenario: User cancels note editing in modal
- **WHEN** user clicks "Cancel", clicks the backdrop, or presses `Escape` in the Edit Modal
- **THEN** the modal closes immediately
- **AND** any unsaved text changes in the modal are discarded without modifying the card or sending an API request.
