# Design: Redesign Notes Layout & Card Header

## Context

On the technical notes hub (`frontend/pages/notes.vue`), the page utilizes the `BoardLayout.vue` layout archetype. `BoardLayout.vue` currently hardcodes the `.glass-card` class on its root container, enclosing the header, filter row, content grid, and pagination within a giant rounded card. When rendered on the obsidian canvas (`bg-slate-50 dark:bg-canvas`), this creates an awkward double-card nesting pattern and large empty voids on screens with few notes.

Inside each note card, the header row currently hosts both source document metadata (`bookTitle`, `chapterTitle`) and three full action buttons ("Chỉnh sửa ghi chú", "Đã Trong SM-2", and delete). These action buttons consume $> 300\text{px}$ of horizontal width, compressing the book title and chapter title down to $< 30\text{px}$ on standard 2-to-3 column grid cards. This causes severe text truncation into unreadable clipped dots (`📖 : • \`).

## Goals / Non-Goals

**Goals:**
- Eliminate outer double-card nesting on `/notes` by introducing an optional `flat?: boolean` prop to `BoardLayout.vue` and using it in `notes.vue`.
- Separate source reference information (card header) from interactive actions (card footer).
- Expand the card header's horizontal space so book and chapter titles render with full legibility across desktop, tablet, and mobile viewports.
- Relocate primary action buttons ("Edit Note" / "Chỉnh sửa ghi chú" and "Flashcard SM-2" / "Đã Trong SM-2") to a dedicated card footer with comfortable click/tap targets.
- Maintain 100% backward compatibility for other `BoardLayout` usages and full test coverage.

**Non-Goals:**
- Modifying backend API endpoints, database entities, or DTO structures.
- Altering the SuperMemo SM-2 synthesis workflow or spaced repetition scheduling.

## Decisions

### 1. BoardLayout Flat Open-Canvas Mode

In `frontend/components/layout/BoardLayout.vue`:
- Add optional `flat?: boolean` prop (default `false`):
  ```vue
  <script setup lang="ts">
  defineProps<{
    maxHeight?: string
    flat?: boolean
  }>()
  </script>

  <template>
    <div
      :class="[
        flat
          ? 'flex min-h-0 flex-col space-y-4 w-full'
          : 'glass-card flex min-h-0 flex-col overflow-hidden'
      ]"
      :style="maxHeight ? { height: maxHeight } : {}"
    >
  ```
- When `flat` is true:
  - Header: Renders cleanly on the page without enclosed card borders.
  - Filters: Renders as a clean filter strip on the canvas.
  - Content: Renders directly on the canvas without inner scroll truncation.
  - Pagination: Renders pagination controls at the bottom of the content flow.
- In `frontend/pages/notes.vue`:
  - Adopt `<BoardLayout flat class="w-full">`.
  - This eliminates the outer "card tổng" while preserving the architectural slot structure (`#header`, `#filters`, `#content`, `#pagination`).

### 2. Highlight Card Architectural Separation (Header vs Footer)

In `frontend/pages/notes.vue`, refactor each note card template:
- **Card Header (Source Context)**:
  - Left: `BookOpen` icon (`w-4 h-4 text-brand-500 shrink-0`) + Book Title (`font-bold text-xs sm:text-sm text-slate-800 dark:text-slate-200 truncate`) + dot separator (`•`) + Chapter Title / slice (`text-xs text-slate-500 dark:text-slate-400 truncate`).
  - Right: Delete button only (`Trash2`, `p-1.5 rounded-lg text-slate-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition-colors shrink-0`).
  - Available space for titles increases from $\approx 30\text{px}$ to $> 300\text{px}$, eliminating text squishing.
- **Card Body (Excerpt & Notes)**:
  - Highlight quote blockquote with Iris Violet left border accent.
  - User reflection note container (or inline editing form when in active edit mode).
- **Card Footer (Actions & Tags)**:
  - Structured bottom action bar separated by `pt-3 border-t border-slate-100 dark:border-white/[0.04]`:
    - Tags row with `#` badges and interactive tag filtering.
    - Action buttons row:
      - Edit button: `Pencil` icon + "Edit Note" / "Chỉnh sửa ghi chú".
      - SM-2 button: `Zap` / `Check` icon + "Flashcard SM-2" / "In SM-2" (styled as green disabled badge when already converted).

### 3. Responsive Stability

- On small screens ($< 640\text{px}$):
  - Card footer stacks tags and action buttons cleanly with full touch target heights ($\ge 36\text{px}$).
- On medium to large screens ($\ge 640\text{px}$):
  - Card footer places tags on the left and action buttons on the right, maintaining visual symmetry across all cards in the grid.

## Risks / Trade-offs

- **Risk: Backward compatibility with tests expecting header buttons**:
  - *Mitigation*: The action buttons retain the same selectors and click handlers (`startEditing(item)`, `handleCreateFlashcard(item.id)`, `openDeleteModal(item.id)`). Existing unit tests querying buttons by text/icons will continue to find them in the card footer.
- **Risk: Long book and chapter titles causing overflow**:
  - *Mitigation*: Both titles use CSS `truncate` within a flex container (`flex items-center gap-1.5 min-w-0`), accompanied by HTML `title` attributes for full hover tooltips.
