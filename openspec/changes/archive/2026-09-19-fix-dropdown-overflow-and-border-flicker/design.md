# Design: Fix Dropdown Overflow Clipping, Tab Border Flicker, and Timezone Auto-Persist

## Context

TechDaily's UI uses custom Vue components with Tailwind CSS in dark and light modes.
Three specific issues cause layout shifts, visual twitching, and stale preferences:
1. `frontend/components/common/AppSelect.vue` renders its options popover as an in-DOM child with `position: absolute; top: 100%`. When placed inside modal containers with `overflow-y-auto` (such as the document import modal in `frontend/pages/library.vue`) or near the bottom of pages (`frontend/pages/settings.vue`), the popover extends the container's `scrollHeight`. This triggers unexpected scrollbar generation, shrinking container content width by the scrollbar track width (~15px) and causing visible layout jumps (CLS). Furthermore, the modal bounds clip the bottom of the popover.
2. `frontend/components/roadmap/RoadmapViewSwitcher.vue` toggles a 1px border only when a button is active, while the inactive button lacks a border class. Combined with `transition-all`, switching tabs triggers a 1px geometry resize and border collapse flicker.
3. In `frontend/pages/settings.vue`, `commonTimezones` is an unreactive plain array that is mutated with `.unshift()`, and changing timezone requires an explicit manual save button click unlike theme/locale which auto-persist on change.

See `proposal.md` for motivation and impact.

## Goals / Non-Goals

**Goals:**
- Upgrade `AppSelect.vue` to render its popover via `<Teleport to="body">` with dynamic fixed positioning based on `triggerRef.getBoundingClientRect()`.
- Add intelligent auto-flip placement: when clearance below the trigger is less than 260px and there is more clearance above, position the popover above the trigger.
- Keep `AppSelect` completely zero-dependency (no external libraries like Floating UI or Popper.js).
- Update click-outside detection in `AppSelect.vue` to check both `triggerRef` and the teleported `listboxRef`.
- Standardize `RoadmapViewSwitcher.vue` and global segmented button controls to use `border border-transparent` base classes and `transition-colors`, completely eliminating 1px border twitching.
- Add `scrollbar-gutter: stable` to scrollable modal dialog containers in `library.vue`.
- Make `commonTimezones` in `settings.vue` reactive and deduplicated, and auto-persist timezone changes to `profileStore.updateProfile` upon selection.

**Non-Goals:**
- Introducing heavy third-party UI/floating libraries (Radix Vue, Floating UI, Headless UI).
- Altering the visual design tokens, colors, or typography of `AppSelect` or roadmap switchers.

## Decisions

### 1. Teleport to `<body>` with Dynamic Fixed Coordinates

- **Approach**:
  When `isOpen` is toggled to `true`, calculate the trigger button's viewport bounding rect:
  ```ts
  const triggerRect = triggerRef.value.getBoundingClientRect()
  ```
  Determine vertical placement:
  ```ts
  const spaceBelow = window.innerHeight - triggerRect.bottom
  const spaceAbove = triggerRect.top
  const placeAbove = spaceBelow < 250 && spaceAbove > spaceBelow
  ```
  Compute fixed style coordinates:
  ```ts
  floatingStyle.value = {
    position: 'fixed',
    left: `${triggerRect.left}px`,
    width: `${triggerRect.width}px`,
    top: placeAbove ? 'auto' : `${triggerRect.bottom + 6}px`,
    bottom: placeAbove ? `${window.innerHeight - triggerRect.top + 6}px` : 'auto',
    zIndex: 60
  }
  ```
- **Rationale**:
  Rendering directly into `<body>` with `position: fixed` completely bypasses parent container clipping (`overflow: hidden` / `overflow-y-auto`) and never inflates the parent container's `scrollHeight`.

### 2. Scroll and Resize Event Synchronization

- **Approach**:
  When `isOpen` is true, attach window `scroll` (with `capture: true`) and `resize` listeners. If the user scrolls, recalculate coordinates or close the dropdown if the trigger scrolls out of view. Remove listeners on close or unmount.
- **Rationale**:
  Guarantees the teleported listbox remains tethered to the trigger button during fluid scrolling or window resizing.

### 3. Click-Outside Detection for Teleported DOM Node

- **Approach**:
  Update `handleClickOutside`:
  ```ts
  function handleClickOutside(event: MouseEvent) {
    if (!isOpen.value) return
    const target = event.target as Node
    const isClickInsideTrigger = triggerRef.value?.contains(target)
    const isClickInsideListbox = listboxRef.value?.contains(target)
    if (!isClickInsideTrigger && !isClickInsideListbox) {
      isOpen.value = false
    }
  }
  ```
- **Rationale**:
  Because the listbox is teleported to `document.body`, `selectRef.value.contains(target)` would falsely report true clicks inside the listbox as clicks outside. Checking both references resolves this cleanly.

### 4. Zero-Shift Segmented Controls Pattern

- **Approach**:
  Define a universal styling convention in `frontend/assets/css/main.css` or component base classes:
  - Base class: `border border-transparent transition-colors duration-150`
  - Active class: `border-slate-200/80 dark:border-white/[0.12] (or dark:border-white/[0.06]) bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm`
  - Inactive class: `border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white`
  - Never use `transition-all` on elements toggling border/padding.
- **Rationale**:
  Allocating a 1px transparent border upfront maintains a constant bounding box ($W \times H$). Restricting transitions to `transition-colors` prevents layout interpolation twitch.

### 5. Reactive Timezones & Auto-Save on Change in Settings

- **Approach**:
  In `frontend/pages/settings.vue`:
  - Define `commonTimezones` as a `ref` array with standard IANA zones.
  - In `onMounted`, prepend detected browser timezone or user profile timezone via reactive helper function `registerTimezone(tz: string, labelSuffix: string)` that avoids duplicates.
  - Watch or listen to timezone changes via `@update:model-value="handleTimezoneChange"`, immediately invoking `profileStore.updateProfile({ timeZone: newTz })` with a success toast notification. Keep the "Save Schedule" button for manual time saves.
- **Rationale**:
  Matches user expectations set by theme and locale settings (immediate persistence without requiring a secondary submit click).

## Risks / Trade-offs

- **Z-Index Layering**: Modals use `z-50`. The teleported listbox must use `z-[60]` so it renders cleanly on top of modals.
- **Testing in Vitest (Happy-DOM)**:
  `getBoundingClientRect()` in Happy-DOM returns all zeros by default. In unit tests, wrapper searches for the listbox should search `document.body` or use `attachTo: document.body`, and provide safe fallback positioning values (`rect.width || 240`).
