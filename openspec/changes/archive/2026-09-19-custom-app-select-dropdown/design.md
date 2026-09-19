# Design: Custom AppSelect Dropdown Component for Dev-Learning Studio UI

## Context

See `proposal.md` for motivation.

Native HTML `<select>` controls cannot be custom-styled for dropdown options on Chromium, Firefox, and WebKit on desktop OSs (Linux, Windows, macOS). While previous changes applied CSS utility classes (`.select-input`) to the closed button trigger, opening the menu launches the operating system's native window popup manager, which ignores CSS properties like `border-radius`, custom colors, shadows, and hover animations.

This design document establishes the architecture for `frontend/components/common/AppSelect.vue`, a headless-inspired, fully styled custom select component that replaces all native `<select>` controls across TechDaily.

## Goals / Non-Goals

**Goals:**
- Provide a standardized, reusable `AppSelect.vue` component matching the Dev-Learning Studio aesthetic (`dark:bg-canvas-elevated`, hairline borders `dark:border-white/[0.08]`, Deep Iris Violet active highlights).
- Guarantee full accessibility compliance: ARIA combobox/listbox roles, screen reader labels, and keyboard navigation (`ArrowDown`, `ArrowUp`, `Enter`, `Space`, `Escape`, `Tab`).
- Support leading icon slots/props (`Briefcase`, `Globe`, `BookOpen`) to maintain rich visual cues on form inputs.
- Migrate all existing native `<select>` callsites in `profile.vue`, `settings.vue`, `quiz.vue`, and `library.vue`.

**Non-Goals:**
- Multi-select or tag chips inside the select (current requirements only need single-select).
- Asynchronous remote search/filtering (all current select options are finite local lists).

## Decisions

### 1. Component Architecture & Props Interface (`AppSelect.vue`)

The component will be created at `frontend/components/common/AppSelect.vue` with the following contract:

```typescript
export interface SelectOption<T = string | number> {
  value: T;
  label: string;
  icon?: any;
  description?: string;
  disabled?: boolean;
}

const props = defineProps<{
  modelValue: string | number | null | undefined;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
  icon?: any; // Leading icon displayed on trigger button
  ariaLabel?: string;
  name?: string;
  dropdownClass?: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number): void;
  (e: 'change', value: string | number): void;
}>();
```

### 2. Dropdown Positioning & Visual Styling

- **Trigger Button**:
  - Rendered with `.select-input` / `.glass-panel` base tokens:
    ```html
    <button
      type="button"
      role="combobox"
      :aria-expanded="isOpen"
      :aria-controls="listboxId"
      class="w-full flex items-center justify-between gap-2.5 px-3.5 py-2.5 rounded-xl text-left text-xs sm:text-sm font-semibold transition-all border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-elevated text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-white/[0.16] focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 shadow-sm"
    >
      <div class="flex items-center gap-2.5 min-w-0 flex-1">
        <component :is="icon" v-if="icon" class="w-4 h-4 text-slate-400 shrink-0" />
        <span class="truncate">{{ selectedLabel || placeholder }}</span>
      </div>
      <ChevronDown class="w-4 h-4 text-slate-400 transition-transform duration-200 shrink-0" :class="{ 'rotate-180': isOpen }" />
    </button>
    ```

- **Floating Listbox Popover**:
  - Positioned absolutely below the trigger button (`absolute left-0 top-full mt-2 w-full z-50`).
  - Container tokens: `glass-panel dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] shadow-2xl rounded-2xl p-1.5 backdrop-blur-md max-h-60 overflow-y-auto space-y-0.5`.
  - Open/close animation: Vue `<Transition>` with `opacity` and `scale(0.98)` spring curve.

- **Option Items**:
  - Base option tokens: `w-full flex items-center justify-between gap-2.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-medium transition-colors cursor-pointer select-none`.
  - Active/Selected state: `bg-brand-50/80 dark:bg-brand-950/40 text-brand-950 dark:text-brand-300 font-bold border border-brand-200 dark:border-brand-800/80`.
  - Inactive/Hover state: `text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]`.
  - Trailing checkmark: `<Check class="w-4 h-4 text-brand-500 shrink-0" />` shown when item is selected.

### 3. Keyboard Navigation & ARIA Accessibility

- **Keyboard Handling**:
  - `Enter` / `Space`: When trigger is focused, opens dropdown. When dropdown is open, selects currently highlighted option and closes dropdown.
  - `ArrowDown` / `ArrowUp`: Increments/decrements highlighted option index, wrapping around ends. Automatically scrolls highlighted item into view using `scrollIntoView({ block: 'nearest' })`.
  - `Escape`: Closes dropdown immediately and returns focus to the trigger button.
  - `Tab`: Closes dropdown cleanly and allows natural browser focus progression.
- **Click-Outside Detection**:
  - Uses `window.addEventListener('mousedown', handleClickOutside)` when open, automatically removed on close and `onUnmounted`.

### 4. Callsite Cutover Map

| Page | State Field | Options Array | Leading Icon |
|---|---|---|---|
| `profile.vue` | `targetRole` | `roleOptions` | `Briefcase` |
| `settings.vue` | `timeZone` | `timeZoneOptions` | `Globe` |
| `quiz.vue` | `selectedBookId` | `bookOptions` | `BookOpen` |
| `library.vue` (URL Import) | `importCategory` | `categoryOptions` | `Folder` |
| `library.vue` (PDF Upload) | `pdfCategory` | `categoryOptions` | `Folder` |

## Risks / Trade-offs

- **Unit Test Updates**:
  - Existing tests in `profile.spec.ts`, `settings.spec.ts`, and `quiz.spec.ts` that find `select` elements via `wrapper.find('select')` will be updated to interact with `AppSelect` or emit model updates.
  - *Mitigation*: Ensure all 53 frontend test suites pass with 100% success rate.
