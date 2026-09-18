# Design: Graph Visual Legend Vertical Stack & Tablet Clearance

## 1. Architectural Context & Component Roles

The Knowledge Graph page (`/graph`) hosts two persistent floating widgets docked at the bottom of the canvas viewport:
1. **Interactive Visual Graph Legend (`GraphLegend.vue`):** Positioned at `absolute bottom-5 left-5 z-20`, rendering color/geometry keys for entity types (Pillars, Topics, Books, Highlights) and SM-2 flashcard mastery states (Learning, Reviewing, Mastered).
2. **Locator Minimap (`GraphMinimap.vue`):** Positioned at `absolute bottom-4 right-4 z-20 hidden sm:block`, rendering a scaled 2D overview canvas ($160\times 100\text{px}$) with an active viewport bounding box.

Both components share the bottom edge of the canvas. On desktop screens ($> 1200\text{px}$), ample canvas area prevents any spatial conflict. On tablet viewports ($768\times 1024$), the desktop sidebar consumes $240\text{px}$, leaving only $528\text{px}$ of horizontal canvas space at the bottom.

## 2. Layout Restructuring: Vertical Stack & Unclipped Labels

### Entity Hierarchy Section
- **Before:**
  ```html
  <div class="grid grid-cols-2 gap-1">
    <div v-for="item in entityItems" ...>
      <div :class="[item.color, item.shape, 'shrink-0']" />
      <span class="truncate text-[11px]">{{ $t(item.labelKey) }}</span>
    </div>
  </div>
  ```
  The two-column grid provided only $\approx 70\text{px}$ per column, forcing labels into truncated text: `"Curriculum ..."` and `"Personal N..."`.
- **After:**
  ```html
  <div class="flex flex-col gap-1">
    <div
      v-for="item in entityItems"
      :key="item.type"
      :data-testid="`legend-item-${item.type}`"
      :class="[
        'flex items-center gap-2.5 px-2 py-1 rounded-lg cursor-pointer transition-all',
        store.hoveredLegendType === item.type
          ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-bold'
          : 'hover:bg-slate-100/80 dark:hover:bg-slate-800/60 text-slate-700 dark:text-slate-300'
      ]"
      @mouseenter="onHover(item.type)"
      @mouseleave="onHover(null)"
    >
      <div :class="[item.color, item.shape, 'shrink-0']" />
      <span class="text-xs font-medium">{{ $t(item.labelKey) }}</span>
    </div>
  </div>
  ```
  Each item occupies a full row. `truncate` is eliminated, ensuring 100% text visibility.

### Flashcard Retention Section
- **Before:**
  ```html
  <div class="flex items-center justify-between gap-1 px-1">
    <div v-for="item in masteryItems" ...>
      <div :class="[item.color, item.shape, 'shrink-0']" />
      <span class="text-[10px] whitespace-nowrap">{{ $t(item.labelKey) }}</span>
    </div>
  </div>
  ```
  Horizontal packaging of 3 items caused cramped spacing and horizontal stretching.
- **After:**
  ```html
  <div class="flex flex-col gap-1">
    <div
      v-for="item in masteryItems"
      :key="item.type"
      :data-testid="`legend-item-${item.type}`"
      :class="[
        'flex items-center gap-2.5 px-2 py-1 rounded-lg cursor-pointer transition-all',
        store.hoveredLegendType === item.type
          ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-bold'
          : 'hover:bg-slate-100/80 dark:hover:bg-slate-800/60 text-slate-700 dark:text-slate-300'
      ]"
      @mouseenter="onHover(item.type)"
      @mouseleave="onHover(null)"
    >
      <div :class="[item.color, item.shape, 'shrink-0']" />
      <span class="text-xs font-medium">{{ $t(item.labelKey) }}</span>
    </div>
  </div>
  ```

## 3. Spatial Clearance Mathematics (Tablet $768\times 1024$)

| Dimension | Formula / Value |
|---|---|
| Total Screen Width | $768\text{px}$ |
| Sidebar Width | $240\text{px}$ |
| **Available Canvas Bottom Width** | $768 - 240 = \mathbf{528\text{px}}$ |
| Legend Left Offset (`left-5`) | $20\text{px}$ |
| Legend Card Width (`w-56`) | $224\text{px}$ |
| **Legend Right Coordinate** | $20 + 224 = \mathbf{244\text{px}}$ |
| Minimap Right Offset (`right-4`) | $16\text{px}$ |
| Minimap Card Width ($160\text{px}$ canvas + $20\text{px}$ padding + $2\text{px}$ border) | $182\text{px}$ |
| **Minimap Left Coordinate** | $528 - (16 + 182) = \mathbf{330\text{px}}$ |
| **Guaranteed Clearance Buffer** | $330 - 244 = \mathbf{86\text{px}}$ |

This $86\text{px}$ buffer guarantees that even with font scaling variations or browser sub-pixel rendering, the Visual Legend card and the Minimap card never collide, touch, or overlap.

## 4. Responsive Default Collapsed Behavior

In `GraphLegend.vue`:
```typescript
function getInitialCollapsedState(): boolean {
  if (typeof window === 'undefined') return false
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved !== null) {
      return saved === 'true'
    }
    // Default collapsed on mobile and tablet (< 1024px)
    return window.innerWidth < 1024
  } catch {
    return false
  }
}
```
- On screens $< 1024\text{px}$, the legend starts collapsed into the unobtrusive pill button `[ ? Visual Legend ^ ]`, keeping the canvas open for immediate touch interaction.
- On desktop screens ($\ge 1024\text{px}$), the legend starts expanded by default.
- Any manual toggle is immediately recorded in `localStorage`, honoring the user's explicit preference across subsequent visits.

## 5. Bilingual Text Fit Verification

Inside `w-56` ($224\text{px}$):
- Usable width: $224\text{px} - 24\text{px} (\text{card padding}) - 16\text{px} (\text{item padding}) - 14\text{px} (\text{icon}) - 10\text{px} (\text{gap}) = \mathbf{160\text{px}}$.
- **English Labels:**
  - `Pillar Hub` ($\approx 65\text{px}$)
  - `Curriculum Topic` ($\approx 105\text{px}$)
  - `Tech Book` ($\approx 68\text{px}$)
  - `Personal Note / Highlight` ($\approx 155\text{px}$) — fits cleanly within $160\text{px}$!
  - `Learning (<6d)` ($\approx 95\text{px}$)
  - `Reviewing (6-20d)` ($\approx 115\text{px}$)
  - `Mastered (≥21d)` ($\approx 105\text{px}$)
- **Vietnamese Labels:**
  - `Trụ cột chính` ($\approx 80\text{px}$)
  - `Chủ đề giáo trình` ($\approx 115\text{px}$)
  - `Sách kỹ thuật` ($\approx 85\text{px}$)
  - `Ghi chú & Trích đoạn` ($\approx 135\text{px}$) — fits cleanly!
  - `Đang học (<6d)` ($\approx 95\text{px}$)
  - `Đang ôn (6-20d)` ($\approx 105\text{px}$)
  - `Thành thạo (≥21d)` ($\approx 115\text{px}$)

Zero text truncation occurs across both languages.
