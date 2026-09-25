# Proposal

## Why
While migrating note editing to a dedicated modal successfully eliminated dynamic row stretching during in-place editing, the cards in the `/notes` grid still exhibit significant vertical height disparity. Cards containing personal reflection notes render additional heading badges and multiple lines of text, while adjacent cards containing only quotes remain short, leaving noticeable bottom gaps in each row track. Additionally, engineers currently have no dedicated interface to read longer architectural notes or view rendered markdown formatting without entering an edit form.

## What Changes
- **Uniform Clamped Height for Note Cards in `frontend/pages/notes.vue`**:
  - Normalize card height across all grid cells using balanced text clamping:
    - Cards with personal reflection notes: clamp reflection note to 2 lines (`line-clamp-2`) and supporting excerpt quote to 1 line (`line-clamp-1`).
    - Cards with quotes only: clamp excerpt quote to 3 lines (`line-clamp-3`).
  - Standardize container flex geometry so all cards in any grid row maintain uniform height (~175px–185px) with bottom footers perfectly aligned.
- **Dual-Mode Detail & Edit Modal (`AppModal`)**:
  - Mirror the successful card inspection pattern from `frontend/pages/review.vue` (Flashcard Deck modal):
    - **Mode Switcher in Modal Header**: A tab toggle between **"Xem chi tiết / Details"** (active by default) and **"Chỉnh sửa / Edit"**.
    - **Details Mode (Default)**: Displays the full, un-clamped source excerpt, document source breadcrumb, technical tags, and the personal reflection note rendered as formatted Markdown (`markdown-it`), with quick actions to switch to Edit mode or generate an SM-2 flashcard.
    - **Edit Mode**: Provides the reflection textarea, tag input editor, Cancel, and Save actions (seamlessly syncing back to the store and details view).
- **Interactive Card Surface**:
  - Clicking on a note card body opens the modal in the default "Xem chi tiết / Details" tab, while clicking the subtle "Chỉnh sửa ghi chú" button directly opens the modal in "Chỉnh sửa / Edit" mode.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `notes`: Codify uniform card height clamping on the grid and define the dual-mode modal requirement (defaulting to Preview/Details with an Edit tab toggle).

## Impact
- **Frontend Code**: `frontend/pages/notes.vue` (card click surface, uniform line clamps, modal mode switcher, markdown rendering).
- **Dependencies**: Reuses existing `AppModal.vue`, `markdown-it`, and `useNotesStore` without new libraries.
- **Backend / API**: Zero API changes. All existing endpoints (`GET /api/v1/notes/highlights`, `PUT /api/v1/notes/highlights/{id}`) remain unchanged.
