# Proposal: Align Graph Control Bar Action Buttons and Fix Dashboard Learned Chunks Metric

## Why

Two visual and metric inconsistencies currently degrade the user experience in the Knowledge Graph and Home Bento Dashboard:

1. **Graph Control Bar Button Misalignment**: In the Knowledge Graph studio (`/graph`), the "Fit to Screen" (*Vừa Khung Hình*) and "Reset Filters" (*Đặt Lại Bộ Lọc*) action buttons in `GraphControlBar.vue` are noticeably bulkier, taller (~38-40px), and styled with larger font/icon metrics (`text-xs sm:text-sm`, `py-2`, `w-4 h-4`) than the adjacent 2D/3D view mode toggle container (`h-8`, `py-1.5`, `text-xs font-bold`, `w-3.5 h-3.5`). This causes an uneven baseline and visual imbalance in the top HUD bar.
2. **Dashboard "Learned Slices" Metric Distortion**: In the Home Bento Dashboard (`HomeBentoDashboard.vue`), the Knowledge Constellation card (*Chòm Sao Tri Thức* / `DomainConstellationCard.vue`) displays "1320" for "Lát cắt đã học" (*Learned Slices*). This happens because line 434 passes `:chunk-count="pacer?.totalChunks || 1"`—passing the *total* slice count of the largest book instead of the slices the user has actually read and learned.

Harmonizing the graph control action button sizing to match the 2D/3D toggle and deriving the true learned slice count from user pacer progress restores visual polish and metric integrity.

## What Changes

- **Graph Control Bar Button Harmonization**:
  - Update `GraphControlBar.vue` action buttons ("Fit to Screen" and "Reset Filters") to match the exact height (`h-8`), padding (`px-2.5 py-1.5`), typography (`text-xs font-bold`), and icon dimensions (`w-3.5 h-3.5`) of the 2D/3D view mode toggle.
  - Maintain responsive collapse behaviors (hiding labels or buttons on narrow viewports as designed) while keeping a uniform horizontal baseline.
- **Accurate Dashboard Learned Slices Metric**:
  - In `HomeBentoDashboard.vue`, replace `:chunk-count="pacer?.totalChunks || 1"` with a computed metric that aggregates the user's completed slices across active and library books: `Math.max(0, currentChunkOrder - 1)` per in-progress book, and `totalChunks` for 100% completed books.
  - Fall back gracefully to `graphStore.rawData?.stats?.nodeTypeCounts?.['chunk'] ?? 0` if pacer progress is uninitialized or absent.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Update HUD control bar specifications to require uniform button height (`h-8`) and typography alignment between the 2D/3D toggle and action controls.
- `today`: Update Bento dashboard specifications so the Domain Constellation widget renders the authentic cumulative count of completed/learned document slices rather than the total capacity of the largest document.

## Impact

- **Frontend Components**:
  - `frontend/components/graph/GraphControlBar.vue`: Button sizing, classes, and icon scaling.
  - `frontend/components/dashboard/HomeBentoDashboard.vue`: Computed learned slice aggregation passed to `DomainConstellationCard`.
- **Backend / Database**: No schema or API changes required; relies on existing `pacer` and `availableBooks` summary data.
- **Tests**: Update or add unit tests for `GraphControlBar.spec.ts` and `HomeBentoDashboard.spec.ts`.
