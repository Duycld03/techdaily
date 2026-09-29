# Design: Graph Control Bar Harmonization and Dashboard Learned Chunks Metric

## Context

See `proposal.md` - Why.

### 1. Graph Control Bar Layout (`GraphControlBar.vue`)
The top bar in the Knowledge Graph studio contains:
- Search input: `flex-1 min-w-[200px] sm:min-w-[260px]`
- View Mode 2D/3D toggle container: `inline-flex p-0.5 rounded-xl bg-slate-100/90 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.08]` enclosing buttons with `px-2 sm:px-2.5 py-1.5 rounded-lg text-xs font-bold` and `w-3.5 h-3.5` icons. The outer height is `h-8` (32px).
- "Fit to Screen" (*Vừa Khung Hình*): Currently styled with `px-3 py-2 text-xs sm:text-sm font-semibold` and `sm:w-4 sm:h-4` icon, rendering at ~38-40px height.
- "Reset Filters" (*Đặt Lại Bộ Lọc*): Currently styled with `px-3 py-2 text-xs sm:text-sm font-semibold` and `sm:w-4 sm:h-4` icon, also rendering at ~38-40px height.

Because the action buttons sit directly adjacent to the 2D/3D toggle, the height and font mismatch is immediately prominent.

### 2. Dashboard Domain Constellation Slices (`HomeBentoDashboard.vue`)
In `HomeBentoDashboard.vue`:
```vue
<DomainConstellationCard
  :node-count="graphStore.rawData?.stats?.totalNodes ?? graphStore.rawData?.nodes?.length ?? 148"
  :edge-count="graphStore.rawData?.stats?.totalEdges ?? graphStore.rawData?.edges?.length ?? 210"
  :card-count="reviewStats.total"
  :highlight-count="notesStore.totalAllCount || notesStore.totalCount || notesStore.highlights.length"
  :chunk-count="pacer?.totalChunks || 1"
/>
```
The prop `:chunk-count` is passed `pacer?.totalChunks || 1`. For large books (such as Atomic Habits with 1320 slices), this displays 1320 under "Lát cắt đã học" (*Learned Slices*). A user on slice 1 or slice 10 sees 1320 as if they had already completed the entire book.

## Goals / Non-Goals

**Goals:**
- Align the height of "Fit to Screen" and "Reset Filters" buttons in `GraphControlBar.vue` to `h-8` (32px) with `px-2.5 py-1.5 text-xs font-bold` and `w-3.5 h-3.5` icons, matching the 2D/3D toggle.
- Calculate and pass the authentic count of learned/completed slices across user reading pacers to `DomainConstellationCard.vue`.

**Non-Goals:**
- Modifying the underlying Cytoscape 2D or Three.js 3D camera fit algorithms.
- Modifying the schema or presentation of `DomainConstellationCard.vue` itself (it already formats `chunkCount` properly; only the input data was incorrect).
- Changing backend API contracts (frontend stores already receive `PacerInfo` with `currentChunkOrder`, `totalChunks`, `progressPercentage`, and `availableBooks`).

## Decisions

### Decision 1: Standardize Action Buttons in `GraphControlBar.vue` to `h-8`
- **Specification**:
  - Add explicit `h-8` utility class to both the "Fit to Screen" and "Reset Filters" `<button>` elements.
  - Change padding from `px-3 py-2` to `px-2.5 py-1.5`.
  - Change typography from `text-xs sm:text-sm font-semibold` to `text-xs font-bold`.
  - Scale icon elements to `w-3.5 h-3.5 shrink-0` (removing `sm:w-4 sm:h-4`).
- **Rationale**: This mirrors the exact internal button metrics of the adjacent 2D/3D toggle, establishing a shared 32px height and identical font weight across the entire control strip.

### Decision 2: Aggregate Authentic Learned Slices in `HomeBentoDashboard.vue`
- **Specification**:
  Define a computed property `learnedChunksCount` in `HomeBentoDashboard.vue`:
  ```ts
  const learnedChunksCount = computed(() => {
    const books = pacer.value?.availableBooks
    if (books && books.length > 0) {
      return books.reduce((sum, b) => {
        const completed = b.progressPercentage >= 100
          ? b.totalChunks
          : Math.max(0, (b.currentChunkOrder || 1) - 1)
        return sum + completed
      }, 0)
    }
    if (pacer.value) {
      return pacer.value.progressPercentage >= 100
        ? pacer.value.totalChunks
        : Math.max(0, (pacer.value.currentChunkOrder || 1) - 1)
    }
    return graphStore.rawData?.stats?.nodeTypeCounts?.['chunk'] ?? 0
  })
  ```
  Pass `:chunk-count="learnedChunksCount"` into `<DomainConstellationCard>`.
- **Rationale**:
  - In TechDaily's reading model, `currentChunkOrder` is 1-indexed; when the user is reading slice $K$, slices $1 \dots K-1$ are completed.
  - When a book reaches 100% progress, all `totalChunks` are counted as learned.
  - Summing across `availableBooks` captures all documents the user has engaged with, matching the cumulative nature of "Thẻ ghi nhớ SM-2" (all cards) and "Ghi chú & Trích đoạn" (all highlights).

## Risks / Trade-offs

- **Risk**: For a brand new user who has just imported a book and is on slice 1, `learnedChunksCount` evaluates to 0.
  - *Mitigation*: This is expected and semantically accurate—the user has not yet completed any slice. As they advance to slice 2, 3, etc., the metric immediately increments.
