# Tasks

## 1. Root Shell & Layout Viewport Standardization

- [x] 1.1 Update `app.vue` and `error.vue` to replace `min-h-screen` with `min-h-dvh` and verify dynamic mobile URL bar stability
- [x] 1.2 Audit `error.vue` action buttons for `whitespace-nowrap shrink-0` and responsive gap

## 2. Floating Popovers & Dropdown Primitives

- [x] 2.1 Audit `AppSelect.vue` to ensure dropdown menu clamps within 320px screens (`max-w-[calc(100vw-2rem)]`) with touch targets $\ge 44\text{px}$
- [x] 2.2 Audit `AppTimePicker.vue` for 320px mobile popover positioning and dark-mode active state styling
- [x] 2.3 Verify `AppCommandPalette.vue` modal backdrop, escape key listener, and touch dismissal using VueUse

## 3. Shared Component Hygiene & Iconography

- [x] 3.1 Audit `BasePagination.vue`, `LocaleSelector.vue`, `StreakBadge.vue`, and `ThemeToggle.vue` for typed `defineProps` and `defineEmits`
- [x] 3.2 Ensure all Lucide icons in common components use `:stroke-width="1.5"`
- [x] 3.3 Verify unit test suite passes for shared primitives via `npm test`
