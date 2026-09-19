# Design: Standardize UI Dropdowns and Dialog Hygiene (Batch 2)

## Context

Following Batch 1's performance and event hygiene improvements, five components and pages retain manual document/window listener bindings:
1. `pages/today.vue`: Book and slice selection menu uses manual `document.addEventListener("click")`.
2. `pages/roadmap.vue`: Curriculum track selection dropdown uses manual `window.addEventListener('click')`.
3. `components/app/AppCommandPalette.vue`: Command palette uses manual `window.addEventListener('keydown')`.
4. `components/graph/GraphControlBar.vue`: Viewport detection uses manual `window.addEventListener('resize')`.
5. `components/graph/GraphDetailDrawer.vue`: Node inspection drawer uses manual `window.addEventListener('keydown')`.

## Goals / Non-Goals

**Goals:**
- Refactor dropdown outside-click dismissal to `onClickOutside` in `pages/today.vue` and `pages/roadmap.vue`.
- Standardize keyboard shortcut listeners to `useEventListener` in `AppCommandPalette.vue` and `GraphDetailDrawer.vue`.
- Standardize viewport resize listeners to `useEventListener` in `GraphControlBar.vue`.
- Guarantee automatic event listener detachment upon component unmount and across route navigations.
- Maintain 100% test pass rate across all 55 frontend test files.

**Non-Goals:**
- Modifying UI layouts, CSS styles, animations, or DOM structures.
- Altering query parameters, route transitions, or state store methods.

## Decisions

### 1. `onClickOutside` for Navigation Dropdowns
- **Rationale**:
  - `pages/today.vue` contains `bookMenuRef` which houses the dropdown menu for switching book slices.
  - `pages/roadmap.vue` contains `trackMenuRef` which houses the curriculum track selector.
  - Both currently bind window/document click listeners in `onMounted` and unbind in `onUnmounted`.
- **Implementation**:
  ```typescript
  onClickOutside(bookMenuRef, () => {
    if (isBookMenuOpen.value) isBookMenuOpen.value = false
  })
  ```
  This removes document listener boilerplate and eliminates edge cases where fast page transitions skip `onUnmounted`.

### 2. `useEventListener` for Modals and Drawers
- **Rationale**:
  - `AppCommandPalette.vue` captures global `Cmd+K` / `Ctrl+K` and arrow keys.
  - `GraphDetailDrawer.vue` captures `Escape` to close node details.
- **Implementation**:
  ```typescript
  useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeydown)
  ```
  VueUse automatically cleans up listeners when the component unmounts, while preserving body scroll lock restoration in `onUnmounted`.

### 3. `useEventListener` for Viewport Resize in `GraphControlBar.vue`
- **Rationale**:
  - `GraphControlBar.vue` monitors window width (`window.innerWidth < 640`) to adapt filter pill layout.
- **Implementation**:
  ```typescript
  onMounted(() => {
    checkMobile()
  })
  useEventListener(typeof window !== 'undefined' ? window : null, 'resize', checkMobile)
  ```
  Retains the `isMobileScreen` ref structure for existing unit test compatibility while eliminating manual `removeEventListener`.

## Risks / Trade-offs

- **Vitest Environment Compatibility**:
  - Unit tests in `tests/pages/today.spec.ts` and `tests/pages/roadmap.spec.ts` simulate user clicks.
  - *Mitigation*: `onClickOutside` seamlessly processes synthetic events dispatched on `document.body` or external elements during test runs.
