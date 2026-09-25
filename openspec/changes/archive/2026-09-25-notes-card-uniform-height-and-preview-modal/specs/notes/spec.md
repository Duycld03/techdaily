# Spec Delta: notes

## MODIFIED Requirements

### Requirement: Dedicated Reading Notes Management View
The `/notes` page SHALL serve as an exclusive reading notes hub, displaying user highlights with book titles, chapter titles, selected excerpt quotes, reflection notes, tags, timestamps, and persistent flashcard association state (`HasFlashcard`), adhering to the **Showcase Design System** note card archetype with uniform row geometry:

1. **Obsidian Studio Canvas & Glass Controls**:
   - The `/notes` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base).
   - The dynamic tag chip bar SHALL style the active chip with Iris Violet accents and inactive chips with dark glass styling.

2. **Showcase-Archetype Uniform Highlight Cards**:
   - Highlight cards SHALL render as clean elevation containers with uniform vertical geometry (~175px–185px) to eliminate row height disparity and empty bottom gaps in grid rows:
     - **Header**: Primary tag badge pill on the left, document source context on the right, and a responsive hover delete trigger (`Trash2`).
     - **Balanced Clamped Body**:
       - When a personal reflection note exists, the note SHALL render with prominent weight clamped to at most 2 lines (`line-clamp-2`), followed by the source excerpt quote clamped to at most 1 line (`line-clamp-1`).
       - When no personal reflection note exists, the source excerpt quote SHALL render clamped to at most 3 lines (`line-clamp-3`).
     - **Footer**: Technical tag chips on the left, subtle ghost/outline action triggers on the right (`Edit Note` / `Details` and `Flashcard SM-2` / `✓ In SM-2`).
   - Clicking on the note card body SHALL open the Note Details modal in the default "Xem chi tiết / Details" view.

#### Scenario: User views uniform height cards across grid rows
- **WHEN** an authenticated user navigates to `/notes`
- **THEN** all highlight cards in any given grid row render with uniform vertical height (~175px–185px)
- **AND** cards with personal notes clamp the note to 2 lines and excerpt to 1 line
- **AND** cards with quotes only clamp the excerpt to 3 lines
- **AND** zero jagged bottom gaps appear between adjacent cards in the same row track.

#### Scenario: User clicks card to open details view
- **WHEN** a user clicks on the body of a highlight card
- **THEN** system opens the Note Modal with the "Xem chi tiết / Details" tab selected by default
- **AND** displays the complete, un-truncated excerpt quote and formatted personal reflection note.

---

### Requirement: Highlight Reflection and Tag Updating
The system SHALL provide an accessible, dual-mode modal interface for inspecting and editing reading highlight notes and tags using `AppModal.vue`, featuring a mode switcher between "Xem chi tiết / Details" (default) and "Chỉnh sửa / Edit":

1. **Dual-Mode Modal Architecture**:
   - **Mode Switcher**: The modal header SHALL contain an interactive tab switcher:
     - `Xem chi tiết` (Details / Preview) — active by default upon opening a card.
     - `Chỉnh sửa` (Edit) — active when clicking the explicit "Edit Note" button or switching from Details mode.
   - **Details Mode (Default)**:
     - Displays the source document title and chapter breadcrumb.
     - Displays the complete, un-clamped source excerpt quote in a dedicated reference panel.
     - Displays the personal reflection note rendered as formatted Markdown (supporting bold, italics, bullet lists, and code spans via `markdown-it`).
     - Displays technical tags and an action to generate an SM-2 flashcard or switch directly to Edit mode.
   - **Edit Mode**:
     - Displays the source excerpt reference panel.
     - Displays a multi-line textarea (`rows="4"`) for drafting reflections.
     - Displays an interactive comma-separated tag input field.
     - Displays Cancel and Save buttons with active loading spinner feedback.
   - Pressing `Escape` or clicking outside the modal backdrop SHALL close the modal.

2. **Persistence and Mode Transition**:
   - Successfully saving in Edit mode SHALL invoke `PUT /api/v1/notes/highlights/{id}`, update the highlight in `useNotesStore`, display a success toast (`notes.toast_update_success`), and smoothly transition back to Details mode or close the modal.

#### Scenario: User inspects highlight details in default preview mode
- **WHEN** user clicks on a note card or clicks "Xem chi tiết"
- **THEN** system opens `AppModal` with the Details tab active
- **AND** renders the full original excerpt quote and formatted Markdown reflection note without truncation.

#### Scenario: User switches to edit mode, updates note, and saves
- **WHEN** user is in Details mode and clicks "Chỉnh sửa" (or clicks the Edit button directly from the card)
- **THEN** modal switches to Edit mode displaying the note textarea and tag input
- **WHEN** user modifies note text and clicks "Lưu"
- **THEN** client sends `PUT /api/v1/notes/highlights/{id}`
- **AND** backend saves changes
- **AND** modal updates to reflect the new note in Details mode and presents a success toast.
