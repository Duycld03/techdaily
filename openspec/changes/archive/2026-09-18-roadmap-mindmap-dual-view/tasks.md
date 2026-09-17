# Tasks: Roadmap & Mindmap Dual-View Switcher

## 1. View Switcher Component (RoadmapViewSwitcher.vue)

- [x] 1.1 Create `frontend/components/roadmap/RoadmapViewSwitcher.vue` with `modelValue: 'timeline' | 'mindmap'` prop and `update:modelValue` emit, rendering segmented buttons with Lucide icons (`ListOrdered`, `GitFork`) and `whitespace-nowrap shrink-0`. Verify component mounts with active button styles matching `modelValue`.
- [x] 1.2 In `frontend/components/roadmap/RoadmapViewSwitcher.vue`, implement ARIA accessibility attributes (`role="tablist"`, `role="tab"`, `aria-selected`) and keyboard arrow navigation between tabs. Verify accessibility attributes in rendered DOM.
- [x] 1.3 Create `frontend/composables/useRoadmapViewMode.ts` to manage reactive `viewMode` state with `localStorage` persistence under key `techdaily_roadmap_view_mode`, validating stored values and defaulting to `'timeline'`. Verify persistence across simulated page reloads.

## 2. Mindmap Canvas Component (RoadmapMindmapCanvas.vue)

- [x] 2.1 In `frontend/utils/roadmapTreeLayout.ts`, implement client-side hierarchical tree layout calculation computing $(x, y)$ coordinates for Root, Chapter branch, and Slice leaf nodes with dynamic branch heights based on expand/collapse states. Verify coordinate math via unit tests.
- [x] 2.2 Create `frontend/components/roadmap/RoadmapMindmapCanvas.vue` accepting `selectedBook` or `ChapterMilestone[]` and `roadmapData` as props, initializing the tree layout with the active today's chapter expanded by default. Verify tree data parses and renders root and chapter nodes.
- [x] 2.3 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement SVG declarative vector rendering with smooth cubic Bezier connector paths connecting parent nodes to child nodes. Verify SVG path `d` strings generate valid curve connections.
- [x] 2.4 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement chapter branch expand/collapse toggle interaction, updating `expandedChapterIds` set and dynamically recalculating tree layout positions. Verify clicking a chapter node toggles visibility of child slice leaves.
- [x] 2.5 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement viewport pan (mouse drag & touch gesture) and zoom (mouse wheel clamped between 0.25x and 2.0x) using CSS transform on the SVG canvas group. Verify dragging and wheel events adjust canvas transform.
- [x] 2.6 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, build the floating control toolbar featuring `Zoom In`, `Zoom Out`, `Fit to Screen`, `Expand All`, and `Collapse All` buttons. Verify clicking `Fit to Screen` calculates visible node bounding box and centers the tree within viewport dimensions.
- [x] 2.7 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement node status styling: Completed (emerald green with checkmark), Active Today (amber/gold with pulse animation and flame icon), and Upcoming (slate with clock icon). Verify CSS classes match node completion status.
- [x] 2.8 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, wire 1-click action bridges on slice leaf click: active slice jumps to `/today?bookId={id}&chunkOrder={order}`, and completed/upcoming slices trigger quick preview with `/read/{bookId}?slice={order}` jump. Verify router navigation on leaf click.
- [x] 2.9 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, ensure dark and light theme reactivity via Nuxt's `useColorMode()` and Tailwind reactive classes without manual canvas redraws. Verify styling updates on color mode change.

## 3. Page Integration in roadmap.vue

- [x] 3.1 In `frontend/pages/roadmap.vue`, import and mount `RoadmapViewSwitcher.vue` directly beneath the unified header banner. Verify the switcher is rendered on `/roadmap` for both book pacer and curriculum tracks.
- [x] 3.2 In `frontend/pages/roadmap.vue`, conditionally render either the linear milestone timeline (`viewMode === 'timeline'`) or `RoadmapMindmapCanvas.vue` (`viewMode === 'mindmap'`) within a smooth transition container. Verify clicking the switcher toggles between views without page reloads.
- [x] 3.3 In `frontend/pages/roadmap.vue`, ensure track switcher changes (switching active book or switching to curriculum track) reactively update the dataset passed into `RoadmapMindmapCanvas.vue`. Verify the mindmap tree updates when a different track is selected.
- [x] 3.4 In `frontend/pages/roadmap.vue`, verify responsive layout across mobile, tablet, and desktop viewports, enforcing `whitespace-nowrap shrink-0` on all interactive elements in accordance with AGENTS.md invariant 37. Verify layout does not wrap or collide on narrow screens.

## 4. Localization & Bilingual Support

- [x] 4.1 In `frontend/i18n/locales/en.json`, add translation keys under `roadmap`: `timeline_view`, `mindmap_view`, and `mindmap` sub-keys (`zoom_in`, `zoom_out`, `fit_screen`, `expand_all`, `collapse_all`, `active_badge`, `completed_badge`, `upcoming_badge`, `read_slice`, `review_drill`, `start_drill`, `chapters_count`, `slices_count`). Verify keys exist in `en.json`.
- [x] 4.2 In `frontend/i18n/locales/vi.json`, add corresponding Vietnamese translations: `"Dạng Dòng Thời Gian"`, `"Dạng Sơ Đồ Tư Duy"`, `"Phóng to"`, `"Thu nhỏ"`, `"Vừa màn hình"`, `"Mở rộng tất cả"`, `"Thu gọn tất cả"`, `"Đang học hôm nay"`, `"Đã hoàn thành"`, `"Sắp tới"`, `"Đọc bài học"`, `"Xem lại thử thách"`, `"Bắt đầu thử thách hôm nay"`, `"{count} chương"`, `"{count} lát cắt"`. Verify keys exist in `vi.json`.
- [x] 4.3 Verify that all switcher buttons, toolbar controls, and node badges enforce `whitespace-nowrap shrink-0` and render cleanly without text clipping in both English and Vietnamese.

## 5. Automated Testing & Verification

- [x] 5.1 In `frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts`, write a Vitest test suite verifying prop binding, event emitting, active styling, keyboard accessibility, and localStorage persistence. Run `npx vitest run frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts` and verify all tests pass.
- [x] 5.2 In `frontend/tests/utils/roadmapTreeLayout.spec.ts`, write a unit test suite verifying horizontal tree node positioning, branch heights for expanded vs collapsed states, and Bezier curve path string generation. Run `npx vitest run frontend/tests/utils/roadmapTreeLayout.spec.ts` and verify all tests pass.
- [x] 5.3 In `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`, write a Vitest test suite verifying tree rendering from mock book milestones, branch collapse/expand toggling, zoom/pan controls, fit-to-screen calculation, and 1-click bridge actions. Run `npx vitest run frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` and verify all tests pass.
- [x] 5.4 In `frontend/tests/pages/roadmap.spec.ts`, update page integration tests to verify dual-view switcher rendering, view switching transitions, and track change reactivity. Run `npx vitest run frontend/tests/pages/roadmap.spec.ts` and verify all tests pass.
- [x] 5.5 Run `openspec validate --strict roadmap-mindmap-dual-view` to confirm 100% specification compliance across all artifacts.
