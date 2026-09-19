# Proposal: Fix Dropdown Overflow Clipping, Tab Border Flicker, and Timezone Auto-Persist

## Why

Two critical UI instability defects and a preference persistence issue degrade the user experience on desktop and mobile:
1. **Dropdown Overflow Clipping & Layout Shift**: In `frontend/pages/settings.vue` (timezone selector) and `frontend/pages/library.vue` (import document modal, PDF tab), opening `AppSelect` renders an inline absolute popover that expands the scrollable height of the parent container. This causes modal cards with `overflow-y-auto` and the main viewport to suddenly sprout vertical scrollbars, causing layout shifts ("bị giật 1 cái do tạo ra scroll") and clipping the bottom of the dropdown list behind modal bounds.
2. **Segmented Switcher Border Twitch & Flicker**: In `frontend/components/roadmap/RoadmapViewSwitcher.vue` (switching between "Dạng Dòng Thời Gian" and "Dạng Sơ Đồ Tư Duy") and related tab switchers, the active button dynamically applies a 1px border while the inactive button lacks a border class, combined with `transition-all`. This causes a noticeable 1px layout twitch and visual flicker upon every click.
3. **Timezone Preference Persistence & Reactivity**: In `frontend/pages/settings.vue`, selecting a timezone via `AppSelect` or modifying study/streak times does not automatically persist to the server (unlike theme/locale), and `commonTimezones` is an unreactive array mutated with `unshift`.

Fixing these issues globally ensures solid layout stability, zero Cumulative Layout Shift (CLS), seamless Dev-Learning Studio micro-interactions, and instant preference persistence.

## What Changes

- **Teleported Floating Popover in `AppSelect.vue`**:
  - Teleport the dropdown popover to `<body>` (`<Teleport to="body">`) with dynamic fixed positioning calculated from the trigger's `getBoundingClientRect()`.
  - Prevent the popover from participating in the parent container's `scrollHeight`, eliminating spurious scrollbar generation in modals (`library.vue`) and pages (`settings.vue`).
  - Implement auto-flip detection: dynamically open the popover upwards when the distance between the trigger bottom and the viewport bottom is insufficient (< 260px) and there is more room above.
  - Synchronize floating position on window scroll/resize and maintain existing ARIA keyboard navigation and click-outside dismissal.
- **Zero-Shift Segmented Controls & Tab Switchers**:
  - Update `RoadmapViewSwitcher.vue` to always include `border border-transparent` on the base button class, switching only the border color (`dark:border-white/[0.06]`) on active state, and replace `transition-all` with `transition-colors`.
  - Audit and standardize all segmented controls and tab buttons across the app (`RoadmapViewSwitcher.vue`, `pages/library.vue`, `pages/review.vue`, `pages/quiz.vue`, `pages/profile.vue`) to guarantee pre-allocated 1px borders and `transition-colors`.
- **Global Modal Scrollbar Stability**:
  - Add `scrollbar-gutter: stable` to scrollable modal dialog containers and define global utility `.segmented-tab-btn` in `frontend/assets/css/main.css` to prevent layout shifts.
- **Timezone & Schedule Auto-Persist in `settings.vue`**:
  - Convert `commonTimezones` to a reactive `ref` array with deduplication when local/saved timezones are detected.
  - Automatically persist timezone selection to the server (`profileStore.updateProfile({ timeZone: val })`) upon selection change, keeping the manual save button as fallback.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update the `Dev-Learning Studio Design System & Navigation Shell` and `System Settings, Notification Scheduling & Timezone Configuration` requirements to mandate teleported floating dropdown popovers with auto-flip collision detection, universal zero-shift borders on segmented control buttons, and automatic timezone selection persistence.

## Impact

- **API & Domain Contracts**: Zero changes. Purely frontend UI/UX, styling stability, and reactive store calls.
- **Frontend Components**: `frontend/components/common/AppSelect.vue`, `frontend/components/roadmap/RoadmapViewSwitcher.vue`, `frontend/pages/settings.vue`, `frontend/assets/css/main.css`, and modal containers.
- **Unit Tests**: Update and expand tests in `frontend/tests/components/AppSelect.spec.ts`, `frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts`, and `frontend/tests/pages/settings.spec.ts`.
