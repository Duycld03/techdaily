# Tasks: Remove Roadmap Demo Track & Fix Mindmap Layout Collisions

- [x] 1.1 Update `convertCurriculumToTree` in `frontend/utils/roadmapTreeLayout.ts` to assign numeric integer index (`idx + 1`) instead of category string concatenation (`mod.category + 1`), preventing badge overflow and title collisions
- [x] 1.2 Enforce badge overflow containment (`overflow-hidden`, `shrink-0`) and text truncation in chapter branch nodes within `frontend/components/roadmap/RoadmapMindmapCanvas.vue`

## 2. Frontend - Document-First Roadmap & Track Switcher Cleanup

- [x] 2.1 Remove the legacy hardcoded demo curriculum option (`track-curriculum-option`, "Lộ Trình Mẫu: Senior Fullstack (Demo)") from the track switcher popover in `frontend/pages/roadmap.vue`
- [x] 2.2 Update roadmap page state initialization in `frontend/pages/roadmap.vue` to default directly to the active document book (`focusStore.data.pacer.bookId` or first item in `availableBookTracks`)
- [x] 2.3 Ensure an inviting empty state with a direct CTA to `/library` renders when the user has zero reading books in progress

## 3. Testing & Verification

- [x] 3.1 Update `frontend/tests/utils/roadmapTreeLayout.spec.ts` and `frontend/tests/pages/roadmap.spec.ts` to assert numeric branch index values (`1`, `2`, `3`, `4`) and document-first track switcher behavior
- [x] 3.2 Execute full frontend test suite (`npm test`) and run production build (`npm run build`) to ensure zero regressions
