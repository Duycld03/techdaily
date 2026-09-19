# Tasks

## 1. Daily Practice & Roadmap Dropdown Hygiene

- [x] 1.1 In `frontend/pages/today.vue`, refactor `bookMenuRef` outside-click dismissal to VueUse `onClickOutside` and eliminate manual document click listeners in `onMounted` / `onUnmounted`.
- [x] 1.2 In `frontend/pages/roadmap.vue`, refactor `trackMenuRef` outside-click dismissal to VueUse `onClickOutside` and eliminate manual window click listeners in `onMounted` / `onUnmounted`.

## 2. Command Palette & Graph Overlay Hygiene

- [x] 2.1 In `frontend/components/app/AppCommandPalette.vue`, refactor global `keydown` listener to VueUse `useEventListener`.
- [x] 2.2 In `frontend/components/graph/GraphControlBar.vue`, refactor viewport resize listener to VueUse `useEventListener`.
- [x] 2.3 In `frontend/components/graph/GraphDetailDrawer.vue`, refactor Escape `keydown` listener to VueUse `useEventListener`.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass across all 55 test files.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
