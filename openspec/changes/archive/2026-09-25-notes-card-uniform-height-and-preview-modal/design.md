# Design

## Context
See `proposal.md` for motivation. Currently, in `frontend/pages/notes.vue`, highlight cards contain varying amounts of content:
- Untagged cards with no personal note display 1–2 lines of quote text (~140px height).
- Cards with multi-line personal notes display a badge, a note, and an excerpt quote (~230px–260px height).

Because CSS Grid allocates track heights to the tallest item in each row, cards with disparate line counts create jagged bottom gaps under shorter cards. Furthermore, users currently lack an unconstrained view to read formatted reflections without entering editing inputs.

In `frontend/pages/review.vue`, an established dual-mode tab switcher exists in card modals:
```html
<div class="flex items-center gap-1 bg-slate-100 dark:bg-white/[0.06] p-1 rounded-xl text-xs font-semibold">
  <button @click="editActiveTab = 'edit'">{{ $t('review.tab_edit') }}</button>
  <button @click="editActiveTab = 'preview'">{{ $t('review.tab_preview') }}</button>
</div>
```
This design adapts this exact interaction pattern for `/notes`.

## Goals / Non-Goals

**Goals:**
- **Uniform Row Geometry**: Enforce balanced line clamping across cards with notes (`line-clamp-2` note + `line-clamp-1` quote) and cards without notes (`line-clamp-3` quote) so that all cards in any grid row have an aligned footprint (~175px–185px) and flush bottom footers.
- **Dual-Mode Detail & Edit Modal**: Upgrade `AppModal` in `frontend/pages/notes.vue` to provide a seamless switcher between **"Xem chi tiết / Preview"** (default) and **"Chỉnh sửa / Edit"**.
- **Rich Markdown Note Rendering**: Render the full personal note with `markdown-it` in the Preview tab so engineers can view formatted lists, code spans, and bold terms without truncation.
- **Natural Card Interaction**: Allow clicking anywhere on the card body to open the Details preview modal.

**Non-Goals:**
- Changing backend endpoints, DTO contracts, or database schemas.
- Altering the SM-2 flashcard synthesis algorithm or review deck scheduler.

## Decisions

### 1. Uniform Clamping Strategy on Card Grid
To achieve visual alignment without hiding essential info:
- **Cards with personal note**:
  ```html
  <div class="space-y-1">
    <div class="text-[10px] font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 flex items-center gap-1">
      <Sparkles class="w-3 h-3 shrink-0" />
      <span>Personal Note</span>
    </div>
    <p class="text-xs sm:text-sm font-semibold text-slate-900 dark:text-white leading-relaxed line-clamp-2">
      {{ item.note }}
    </p>
  </div>
  <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed line-clamp-1 pl-2.5 border-l-2 border-slate-200 dark:border-white/[0.08]">
    "{{ item.selectedText }}"
  </p>
  ```
- **Cards without personal note**:
  ```html
  <p class="text-xs sm:text-sm text-slate-800 dark:text-slate-200 leading-relaxed line-clamp-3 pl-2.5 border-l-2 border-brand-500/60 font-normal">
    "{{ item.selectedText }}"
  </p>
  ```
- **Card container**: `h-full flex flex-col justify-between`. When cards have normalized text lines (3 lines total in both states), each row in the CSS Grid maintains an even ~175px–185px height with bottom footers perfectly aligned.

### 2. Dual-Mode Modal Architecture (`AppModal.vue`)
- **Reactive State**:
  ```typescript
  const isDetailModalOpen = ref(false)
  const activeModalTab = ref<'preview' | 'edit'>('preview')
  const editingHighlight = ref<Highlight | null>(null)
  const editNoteText = ref('')
  const editTagInput = ref('')
  const isSavingEdit = ref(false)
  ```
- **Modal Header**:
  - Left: Title with icon (`BookOpen` in Preview mode, `Pencil` in Edit mode).
  - Right: Tab Switcher pills:
    ```html
    <div class="flex items-center gap-1 bg-slate-100 dark:bg-white/[0.06] p-1 rounded-xl text-xs font-semibold">
      <button @click="activeModalTab = 'preview'">Xem chi tiết</button>
      <button @click="activeModalTab = 'edit'">Chỉnh sửa</button>
    </div>
    ```
- **Preview Tab (Default)**:
  - Source Context box: Full un-clamped excerpt quote (`"{{ editingHighlight.selectedText }}"`) with document title & chapter name.
  - Personal Note box: Rendered formatted Markdown via `v-html="renderMarkdown(editingHighlight.note)"`.
  - Tags list: Interactive chips.
  - Footer: `[⚡ Flashcard SM-2]` button and `[ Đóng / Close ]` button.
- **Edit Tab**:
  - Read-only source quote reference.
  - Textarea: 5-row input with auto-focus.
  - Tag input: Comma-separated tag editor.
  - Footer: `[ Hủy / Cancel ]` and `[ ✓ Lưu / Save ]` button.

### 3. Click Target Ergonomics
- Clicking anywhere on the card container (excluding buttons and tag chips) invokes `openDetailModal(item)` with `activeModalTab = 'preview'`.
- Clicking the "Chỉnh sửa ghi chú" button in the footer directly invokes `openEditModal(item)` with `activeModalTab = 'edit'`.
- Clicking the tag chips invokes `selectTag(tag)`.

## Risks / Trade-offs

| Risk / Trade-off | Mitigation |
| :--- | :--- |
| **Card Click Event Propagation**: Clicking tags or action buttons might accidentally trigger the parent card click. | Use `.stop` event modifier on inner buttons (`@click.stop="selectTag(tag)"`, `@click.stop="openEditModal(item)"`, `@click.stop="handleCreateFlashcard(item.id)"`). |
| **Markdown XSS in User Notes**: Rendering user notes with `v-html` could open XSS vulnerabilities. | `renderMarkdown` uses MarkdownIt with `html: false`, guaranteeing user HTML tags are escaped and sanitized. |
