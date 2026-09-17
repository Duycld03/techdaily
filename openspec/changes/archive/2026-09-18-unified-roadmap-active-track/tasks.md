# Tasks: Unified Roadmap Active Track

## 1. State Management & Track Switcher Logic

- [x] 1.1 In `frontend/pages/roadmap.vue`, introduce reactive state for track selection: `isCurriculumSelected = ref(false)`, `selectedBookId = ref<string | null>(null)`, `isTrackMenuOpen = ref(false)`, and `trackMenuRef = ref<HTMLElement | null>(null)`.
- [x] 1.2 In `frontend/pages/roadmap.vue`, refactor `onMounted` to fetch both `focusStore.fetchTodayFocus` and `roadmapStore.fetchRoadmap` in parallel, initializing `selectedBookId` with the active pacer book or falling back to curriculum mode if no pacer exists.
- [x] 1.3 In `frontend/pages/roadmap.vue`, implement `handleSelectBookTrack(bookId: string)` to update `selectedBookId`, close the dropdown, switch the active pacer book via `focusStore.switchBook()`, and load book details via `libraryStore.fetchBookById()`.
- [x] 1.4 In `frontend/pages/roadmap.vue`, implement `handleSelectCurriculumTrack()` to set `isCurriculumSelected = true`, close the dropdown, and activate the curriculum skill tree view.
- [x] 1.5 In `frontend/pages/roadmap.vue`, attach a window click listener to dismiss `isTrackMenuOpen` when users click outside `trackMenuRef`, cleaning up the event listener on unmount.

## 2. Template Refactoring & Dual-Tab Elimination

- [x] 2.1 In `frontend/pages/roadmap.vue`, remove the top-level tab bar markup (`activeMode = 'book' | 'curriculum'`) and eliminate the separate header banners from Sections A and B.
- [x] 2.2 In `frontend/pages/roadmap.vue`, build the unified header banner with the embedded Track Switcher dropdown button, animated `ChevronDown` indicator, and popover menu.
- [x] 2.3 In `frontend/pages/roadmap.vue`, render the dropdown popover sections: In-Progress Document Books with mini progress bars, 30-Day Senior Curriculum option with completion fraction, and `+ Browse Library` link (`/library`).
- [x] 2.4 In `frontend/pages/roadmap.vue`, unify the metric counter card and global progress bar to dynamically compute completed/total counts and percentage based on `isCurriculumSelected` and `focusStore.data.pacer`.

## 3. Chapter Milestones, Slices & 1-Click Action Bridges

- [x] 3.1 In `frontend/pages/roadmap.vue`, conditionally render either the Book Chapter Milestones view (`!isCurriculumSelected`) or the Curriculum Modules view (`isCurriculumSelected`) beneath the unified header.
- [x] 3.2 In `frontend/pages/roadmap.vue`, verify that chapter search filtering (`chapterSearch`) and batch accordion controls (`expandAll()`, `collapseAll()`) operate smoothly on the active book's chapter milestones.
- [x] 3.3 In `frontend/pages/roadmap.vue`, wire direct 1-click bridge actions:
  - Active slice node jumps to `/today?bookId={id}&chunkOrder={order}`.
  - Completed slice node jumps to `/today?bookId={id}&chunkOrder={order}` for drill review.
  - Ready/Upcoming slice node links to `/read/{bookId}?slice={order}` for reading.
  - Curriculum day nodes jump to `/today?day={dayOrder}`.

## 4. Bilingual Localization & Invariants Compliance

- [x] 4.1 In `frontend/i18n/locales/en.json`, add new keys under `roadmap`: `track_switcher_label`, `in_progress_tracks`, `curriculum_track`, `curriculum_track_desc`, `browse_library`, and `active_badge`.
- [x] 4.2 In `frontend/i18n/locales/vi.json`, add corresponding Vietnamese translations: `"Lộ Trình Đang Học"`, `"Tài Liệu Đang Đọc"`, `"Lộ Trình 30 Ngày Chuẩn"`, `"Cây kỹ năng kiến trúc fullstack senior"`, `"Khám phá Thư Viện"`, and `"Đang Học"`.
- [x] 4.3 In `frontend/pages/roadmap.vue`, ensure all action buttons, pills, and badges enforce `whitespace-nowrap shrink-0` to satisfy AGENTS.md invariant 37 across English and Vietnamese locales.

## 5. Automated Testing & Verification

- [x] 5.1 In `frontend/tests/pages/roadmap.spec.ts`, create a comprehensive Vitest test suite mocking `useDailyFocusStore`, `useLibraryStore`, and `useRoadmapStore`.
- [x] 5.2 In `frontend/tests/pages/roadmap.spec.ts`, assert that `/roadmap` renders a single unified header without competing view tabs.
- [x] 5.3 In `frontend/tests/pages/roadmap.spec.ts`, test the Track Switcher dropdown interaction: toggling menu open/close, displaying available in-progress books, and switching to the curriculum track.
- [x] 5.4 In `frontend/tests/pages/roadmap.spec.ts`, assert that switching a book calls `focusStore.switchBook()` and updates displayed chapter milestones.
- [x] 5.5 In `frontend/tests/pages/roadmap.spec.ts`, test 1-click action bridge routes to `/today` and `/read/[bookId]`.
- [x] 5.6 In `frontend/tests/pages/roadmap.spec.ts`, assert fallback to 30-day curriculum when no active book pacer exists.
- [x] 5.7 Run `npm --prefix frontend test` to ensure all tests pass cleanly.
- [x] 5.8 Run `openspec validate --strict unified-roadmap-active-track` to confirm 100% specification compliance.
