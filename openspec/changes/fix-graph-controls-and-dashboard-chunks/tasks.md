# Tasks

## 1. Frontend (Graph Control Bar UI Alignment)

- [x] 1.1 In `frontend/components/graph/GraphControlBar.vue`, update the "Fit to Screen" button styling to `h-8 inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-bold` with icon `w-3.5 h-3.5 shrink-0`, aligning its height and baseline with the 2D/3D toggle container.
- [x] 1.2 In `frontend/components/graph/GraphControlBar.vue`, update the "Reset Filters" button styling to `h-8 inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-bold` with icon `w-3.5 h-3.5 shrink-0`, ensuring uniform button height across all action controls.

## 2. Frontend (Dashboard Learned Slices Metric Fix)

- [x] 2.1 In `frontend/components/dashboard/HomeBentoDashboard.vue`, create computed property `learnedChunksCount` that aggregates completed slices across `pacer.value?.availableBooks` (or active `pacer` progress) using `Math.max(0, currentChunkOrder - 1)` for in-progress books and `totalChunks` for 100% completed books.
- [x] 2.2 In `frontend/components/dashboard/HomeBentoDashboard.vue`, bind `:chunk-count="learnedChunksCount"` to `<DomainConstellationCard>` instead of `:chunk-count="pacer?.totalChunks || 1"`.

## 3. Verification & Testing

- [x] 3.1 Run frontend unit tests (`npm test` in `frontend/`) covering `GraphControlBar.spec.ts` and `HomeBentoDashboard.spec.ts` to ensure 100% test pass rate with no regressions.
- [x] 3.2 Perform automated headless browser visual verification on Desktop (1440x900) and Mobile (390x844) viewports to verify that the graph control buttons align cleanly with the 2D/3D toggle and that the dashboard Domain Constellation card displays the authentic learned slices count.
