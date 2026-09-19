# Tasks

## 1. Frontend Interval & Debounce Modernization

- [x] 1.1 In `frontend/components/today/AISynthesisCard.vue`, refactor elapsed seconds interval to VueUse `useIntervalFn` and remove manual `onMounted`/`onUnmounted` timer boilerplate.
- [x] 1.2 In `frontend/pages/review.vue`, refactor deck search debounce to VueUse `useDebounceFn` (300ms) and eliminate manual `searchTimer`.
- [x] 1.3 In `frontend/pages/notes.vue`, refactor highlight search debounce to VueUse `useDebounceFn` (300ms) and eliminate manual `searchDebounceTimer`.
- [x] 1.4 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, refactor mindmap search query debouncing to VueUse `useDebounceFn` (150ms).

## 2. Frontend Clipboard Feedback Hygiene

- [x] 2.1 In `frontend/components/common/ShikiCodeBlock.vue`, refactor copy feedback to VueUse `useClipboard`.
- [x] 2.2 In `frontend/components/today/TermExplainerModal.vue`, refactor copy feedback to VueUse `useClipboard`.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass across all 55 test files.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
