# Tasks

## 1. Frontend Dropdown & Canvas Hygiene

- [x] 1.1 In `frontend/components/common/AppSelect.vue`, refactor window `scroll` and `resize` repositioning to VueUse `useEventListener` and eliminate manual `addEventListener` / `removeEventListener` boilerplate.
- [x] 1.2 In `frontend/components/graph/GraphCanvas.vue`, refactor window `resize` handler to VueUse `useEventListener` and eliminate manual `removeEventListener` in `onBeforeUnmount`.

## 2. Frontend Reader & Page Navigation Hygiene

- [x] 2.1 In `frontend/components/today/DocReaderPane.vue`, refactor typography popover dismissal to VueUse `onClickOutside` and keydown/click bindings to `useEventListener`.
- [x] 2.2 In `frontend/pages/insights.vue`, refactor card navigation `keydown` listener to VueUse `useEventListener`.
- [x] 2.3 In `frontend/pages/review.vue`, refactor tab navigation `keydown` listener to VueUse `useEventListener`.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass across all 55 test files.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
