# Proposal: Custom AppSelect Dropdown Component for Dev-Learning Studio UI

## Why

The previous attempt to style dropdowns via CSS on native HTML `<select>` and `<option>` elements (`unify-form-select-styling`) revealed an insurmountable browser limitation:
- On Chromium, Firefox, and WebKit (particularly on Linux and Windows), clicking a native `<select>` control delegates the rendering of `<option>` elements to the host operating system's native window manager.
- As observed in user bug reports, this native OS popup renders with stark rectangular geometry, unstyled system fonts, OS-default blue selection highlights (`#2563eb`), zero support for custom icons or descriptions, and complete disregard for Tailwind dark mode classes, border-radius, or custom scrollbars.
- This creates a jarring visual inconsistency in key user workflows (`/profile`, `/settings`, `/quiz`, `/library`) where high-fidelity Obsidian `#09090b` canvas and Deep Iris Violet surfaces are abruptly interrupted by unstyled OS menus.

Building a dedicated, reusable, fully accessible custom Vue dropdown component (`frontend/components/common/AppSelect.vue`) solves this fundamentally by rendering all option elements within the Vue DOM tree, guaranteeing 100% Dev-Learning Studio aesthetic fidelity across all operating systems and browsers.

## What Changes

- **Create `frontend/components/common/AppSelect.vue`**:
  - Reusable, accessible dropdown component replacing native `<select>`.
  - Supports `v-model` (string, number, or null), `options: Array<{ value: string | number; label: string; icon?: Component; description?: string }>`, `placeholder`, `disabled`, and optional `leadingIcon`.
  - **Trigger Button**: Styled as a `.glass-panel` control with `dark:bg-canvas-elevated`, hairline borders `dark:border-white/[0.08]`, focus ring with brand violet accent, truncated label display, and an animated `ChevronDown` indicator.
  - **Floating Popover**: Rendered with `.glass-panel` elevation, `dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `shadow-2xl`, rounded-2xl geometry (`rounded-2xl`), smooth open/close fade and scale animation, and custom slim scrollbar.
  - **Option Items**: Rounded-xl geometry, translucent hover states (`dark:hover:bg-white/[0.06]`), active/selected highlight with Deep Iris Violet styling (`bg-brand-50/80 dark:bg-brand-950/40 text-brand-950 dark:text-brand-300 font-bold border border-brand-200 dark:border-brand-800/80`), and trailing checkmark (`Check` icon).
  - **Full Accessibility**: ARIA roles (`role="combobox"`, `role="listbox"`, `role="option"`, `aria-expanded`, `aria-activedescendant`), complete keyboard navigation (`ArrowDown`, `ArrowUp`, `Enter`, `Space`, `Escape`, `Tab`), and click-outside dismissal.
- **Migrate All Native `<select>` Callsites**:
  - `frontend/pages/profile.vue`: Engineering target role selector (`targetRole` with `Briefcase` leading icon).
  - `frontend/pages/settings.vue`: IANA timezone preference selector (`timeZone` with `Globe` leading icon).
  - `frontend/pages/quiz.vue`: Grounded in Book selector (`selectedBookId` with `BookOpen` leading icon).
  - `frontend/pages/library.vue`: Web URL import category selector and PDF upload category selector (`importCategory`, `pdfCategory`).
- **Automated Verification**:
  - Comprehensive unit test suite in `frontend/tests/components/AppSelect.spec.ts` testing open/close toggle, option selection, `v-model` emission, keyboard navigation, and click-outside handling.
  - Regression verification across all modified page test files.

## Capabilities

### Modified Capabilities

- `core-platform`: Update requirement `Dev-Learning Studio Design System & Navigation Shell` to mandate custom `AppSelect` dropdown architecture for form selection controls, superseding native OS `<select>` elements.

## Impact

- **UI & UX Quality**: Completely eliminates ugly native OS dropdown popups; delivers smooth, brand-coherent Dev-Learning Studio menus on all browsers (Chromium, Firefox, Safari) across Linux, macOS, and Windows.
- **API & Domain Contracts**: Zero breaking changes. Form data structures and Pinia stores remain identical.
- **Accessibility**: Full WCAG 2.1 AA keyboard and screen reader compliance.
