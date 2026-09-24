# Proposal

## Why

On the Technical Notes archive (`/notes`, `frontend/pages/notes.vue`), the entire page—including page header, search and tag filter rows, content grid, and pagination—is enclosed within `BoardLayout`'s outer `.glass-card` container ("card tổng"). This produces unnecessary double-card nesting (cards inside a giant card) and leaves jarring empty voids when only a few notes exist.

Furthermore, within each highlight card, three text-heavy action buttons ("Chỉnh sửa ghi chú", "Đã Trong SM-2", and delete) are crammed horizontally into the top reference bar alongside the book and chapter titles. On standard 2-to-3 column grid viewports, these buttons consume $> 300\text{px}$ of horizontal width, compressing the left-side book and chapter titles down to $< 30\text{px}$ and causing severe text truncation into unreadable clipped dots (`📖 : • \`).

Removing the redundant outer wrapper card and restructuring the highlight card layout establishes a spacious, open canvas layout (matching `/library`) while giving the book and chapter metadata full horizontal breathing room in the header and placing action controls in a dedicated, balanced card footer.

## What Changes

- **Remove Outer "Card Tổng" Enclosure on `/notes`**:
  - Extend `BoardLayout.vue` with a `flat?: boolean` prop (default `false`) that omits the outer `.glass-card` border, background, and overflow constraints, allowing the page to flow naturally on the open canvas.
  - In `frontend/pages/notes.vue`, activate the `flat` layout mode, eliminating double-card nesting and aligning page structure with `/library` and `/review`.
- **Redesign Highlight Card Header & Action Hierarchy**:
  - **Card Header**: Dedicated exclusively to source document context:
    - Left: `BookOpen` icon, prominent Book Title, and Chapter Title with clean delimiter styling.
    - Right: Compact delete icon button (`Trash2`, `p-1.5 rounded-lg text-slate-400 hover:text-rose-600`).
    - Eliminates the horizontal button squeeze and restores full readability to book and chapter titles.
  - **Card Body**: Displays the quote blockquote with Iris Violet accent border, followed by personal reflection notes and inline editor.
  - **Card Footer**: A dedicated bottom action dock with a subtle top border divider (`pt-3 border-t border-slate-100 dark:border-white/[0.04]`):
    - Left: Tag badges (`#1`, `#Vue`, `#Architecture`) with tag selection triggers.
    - Right: Primary action buttons ("Edit Note" / "Chỉnh sửa ghi chú" and "Flashcard SM-2" / "Đã Trong SM-2") with full tap targets and clear status badges.
- **Bilingual & Responsive Stability**:
  - Ensure book and chapter titles, tag badges, and action buttons render without awkward word wrapping, clipping, or horizontal overflow across Desktop (1920x1080), Tablet (768px), and Mobile (390px) viewports in both English and Vietnamese locales.

## Capabilities

### Modified Capabilities
- `notes`: Update `/notes` requirements to specify the open canvas layout architecture (removal of outer enclosing card) and define the redesigned highlight card visual hierarchy (header reference bar isolated from footer action controls).

## Impact

- **Frontend**:
  - `frontend/components/layout/BoardLayout.vue`: Add `flat` property for open canvas rendering.
  - `frontend/pages/notes.vue`: Adopt `flat` layout on `BoardLayout`, refactor highlight card template structure to separate header source reference from footer action controls.
  - `frontend/tests/components/LayoutArchetypes.spec.ts`: Add unit test asserting `flat` prop behavior on `BoardLayout`.
  - `frontend/tests/pages/notes.spec.ts`: Update unit tests to verify redesigned card header rendering, footer actions, and tag selection.
- **Backend / Database**:
  - Zero changes required. The API endpoints (`GET /api/v1/notes/highlights`, `PUT /api/v1/notes/highlights/{id}`, `DELETE /api/v1/notes/highlights/{id}`) already deliver all required data.
