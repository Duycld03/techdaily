# Proposal: Standardize UI Dropdowns and Dialog Hygiene (Batch 2)

## Why

Continuing TechDaily's UI standardization according to `antfu/skills` conventions (following Batch 1 performance and event hygiene), interactive dropdowns and overlays across the application still rely on manual document/window click listeners and keydown bindings:
1. **Dropdown Outside-Click Fragility**: The book/slice selection menu in `pages/today.vue` and the curriculum track selection menu in `pages/roadmap.vue` attach `document.addEventListener('click')` and `window.addEventListener('click')` with manual cleanup. These manual bindings risk memory leaks or lingering listeners if components are unmounted during route navigation.
2. **Modal & Drawer Keyboard Listeners**: `components/app/AppCommandPalette.vue` and `components/graph/GraphDetailDrawer.vue` manually bind `window.addEventListener('keydown')` with manual removal in `onUnmounted` or `onBeforeUnmount`.
3. **Responsive Resize Listeners**: `components/graph/GraphControlBar.vue` uses manual `window.addEventListener('resize')` to calculate mobile viewport status.

Standardizing these five components with VueUse composables (`onClickOutside`, `useEventListener`) guarantees automatic lifecycle detachment and reduces boilerplate code.

## What Changes

- **Today Page Dropdown Hygiene (`pages/today.vue`)**:
  - Refactor `bookMenuRef` outside-click handling to VueUse `onClickOutside`.
  - Eliminate manual `document.addEventListener("click")` in `onMounted` and `document.removeEventListener` in `onUnmounted`.
- **Roadmap Track Menu Hygiene (`pages/roadmap.vue`)**:
  - Refactor `trackMenuRef` outside-click handling to VueUse `onClickOutside`.
  - Eliminate manual `window.addEventListener('click')` in `onMounted` and `window.removeEventListener` in `onUnmounted`.
- **Command Palette Shortcut Hygiene (`components/app/AppCommandPalette.vue`)**:
  - Refactor global shortcut listener (`keydown`) to VueUse `useEventListener`.
  - Eliminate manual `window.addEventListener` in `onMounted` and `window.removeEventListener` in `onUnmounted`.
- **Graph Control Bar Resize Hygiene (`components/graph/GraphControlBar.vue`)**:
  - Refactor window `resize` listener to VueUse `useEventListener`.
  - Eliminate manual `onMounted` and `onUnmounted` listener boilerplate.
- **Graph Detail Drawer Keyboard Hygiene (`components/graph/GraphDetailDrawer.vue`)**:
  - Refactor `Escape` key listener to VueUse `useEventListener`.
  - Eliminate manual `onMounted` and `onBeforeUnmount` boilerplate.

## Capabilities

### New Capabilities
*(None - this change standardizes event handling and component lifecycles within existing capabilities)*

### Modified Capabilities
- `core-platform`: Expands declarative DOM event hygiene requirements to cover application menus, dropdowns, command palettes, and graph drawers.

## Impact

- **API & Domain Contracts**: Zero breaking changes.
- **Visual Design**: Zero regression or styling shifts.
- **Reliability**: Clean, automated event unbinding across all navigation dropdowns and floating dialogs.
