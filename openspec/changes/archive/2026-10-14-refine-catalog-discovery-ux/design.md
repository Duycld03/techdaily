# Design

## Context

See proposal.md for motivation. `frontend/app/pages/library.vue` hosts the `BoardLayout` with two tabs: "My Library" (`mine`) and "Discover Catalog" (`discover`). The current Discover tab filter slot uses 3 unconstrained `AppSelect` components which trigger Tailwind `w-full` class merging and unlocalized placeholder fallbacks. In `CatalogCard.vue` and `CatalogPreviewModal.vue`, adopted documents render a `<NuxtLink>` targeting `/library`, causing redundant page reloads instead of opening the reader. In `CatalogPreviewModal.vue`, `max-h-80` TOC height combined with header and preview elements exceeds `AppModal`'s `max-h-[60vh]` scrollable container, resulting in nested double scrollbars.

## Goals / Non-Goals

**Goals:**
- Eliminate vertically stacked, unconstrained dropdowns on the Discover tab toolbar.
- Provide a clean horizontal toolbar featuring search, category quick-filter pills, and an Advanced Filters button with active filter counter.
- Create an accessible, bilingual `CatalogFilterModal.vue` following the established `AdvancedFilterModal.vue` archetype from `/review`.
- Ensure "Open Book" directly navigates to `/read/:bookId`.
- Eliminate outer modal scrollbar in `CatalogPreviewModal.vue` by constraining internal scroll heights to `max-h-52`.

**Non-Goals:**
- Modifying backend catalog endpoints or database queries (existing API contracts already support all query parameters).
- Changing personal library ingestion or formatting pipelines.

## Decisions

### 1. CatalogFilterModal Component Architecture
We will create `frontend/app/components/catalog/CatalogFilterModal.vue` using Vue 3.5 `<script setup lang="ts">` and `<Teleport to="body">`.
- It accepts `isOpen: boolean`, `currentCategory?: number`, `currentSourceType?: number`, `currentLanguage?: string`.
- It maintains local draft state while open, supporting:
  - **All Categories**: Selectable radio-pill chips.
  - **Source Type**: All, Web, PDF, Markdown.
  - **Language**: All, English (EN), Vietnamese (VI).
  - **Footer Actions**: "Reset" (clears all) and "Apply" (emits updated criteria and closes).
- This mirrors the UX of `AdvancedFilterModal.vue` in `/review`, providing cross-application consistency.

### 2. Discover Tab Toolbar Layout
In `library.vue`, the Discover `#filters` slot will be refactored into:
- Left: Search input (`v-model="discoverSearch"` with clear button and `@keyup.enter`).
- Right: A flex-wrap row containing:
  - Popular category quick-pills (All, Frontend, Backend, Database, System Design).
  - An Advanced Filters trigger button (`SlidersHorizontal` icon, localized label, and badge counter when non-default criteria are active).

### 3. Direct Reader Navigation
In `CatalogCard.vue` and `CatalogPreviewModal.vue`:
```vue
<NuxtLink
  v-if="document.alreadyAdded || document.existingBookId"
  :to="{ path: `/read/${document.existingBookId}`, query: { from: '/library' } }"
  class="..."
>
  <CheckCircle2 class="w-4 h-4 shrink-0" />
  <span>{{ $t('library.discover.open_book') }}</span>
</NuxtLink>
```
If `document.existingBookId` is available, navigation takes the user directly to the reader slice view for that book with `query: { from: '/library' }`, matching the standard used across DeepPace (`Library.vue:1080`).

### 4. Single-Scroll Preview Modal Layout
In `CatalogPreviewModal.vue`:
- Table of Contents list container: change `max-h-64 sm:max-h-80` to `max-h-52`.
- First slice preview container: change `max-h-60 sm:max-h-72` to `max-h-52`.
- Header card: reduce padding to `p-3` and margin to `space-y-1.5`.
- Result: Total inner content height stays under 420px, well within `AppModal`'s `max-h-[60vh]`, completely preventing the outer modal scrollbar from appearing while keeping smooth internal scrolling on TOC.

## Risks / Trade-offs

- **Small screens (< 390px)**: The category quick-pills could wrap excessively on narrow screens.
  - *Mitigation*: On mobile (`sm:hidden`), the quick-pills are omitted or condensed to just the top 2 plus the Advanced Filter button, keeping the toolbar clean and preventing vertical stacking.
- **Existing BookId availability**: If an adopted document does not yet have `existingBookId` loaded, fallback gracefully to `/library`.
