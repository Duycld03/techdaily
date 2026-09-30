# Tasks

## 1. Standardize Header & Layout on Notes Page

- [x] 1.1 Restructure `#header` slot in `frontend/pages/notes.vue`: implement standardized top bar layout with left-aligned `w-10 h-10 rounded-2xl` icon badge container, `<Highlighter>` glyph (`w-5 h-5 sm:w-6 sm:h-6`), `<h1>` title (`text-xl sm:text-2xl font-black tracking-tight`), and subtitle (`text-xs sm:text-sm font-medium`).
- [x] 1.2 Relocate the full-text search input from `#filters` into the right side of `#header` slot (`relative w-full sm:w-72 h-10 rounded-xl`), preserving shortcut indicator and search clear trigger.
- [x] 1.3 Dedicate the `#filters` slot in `frontend/pages/notes.vue` exclusively to horizontal tag filter chips spanning the full container width (`w-full overflow-x-auto no-scrollbar py-0.5`).

## 2. Standardize Header & Layout on Library Page

- [x] 2.1 Update top header icon container in `frontend/pages/library.vue` to `w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20` and `<BookOpen>` glyph to `w-5 h-5 sm:w-6 sm:h-6 :stroke-width="1.5"`.
- [x] 2.2 Harmonize title typography in `frontend/pages/library.vue` to `text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white` and subtitle to `text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`.
- [x] 2.3 Align search input height and styling to `h-10 rounded-xl` matching the "+ Thêm Tài Liệu" primary action button.

## 3. Verification & Dual-Gate Validation

- [x] 3.1 Execute Vitest test suites (`npx vitest run tests/pages/notes.spec.ts tests/pages/library.spec.ts`) to ensure 100% data contract pass rate (Gate 1).
- [x] 3.2 Perform automated headless browser visual inspection via `browser` in `eval` across Desktop (1440x900) and Mobile (390x844) viewports for both `/library` and `/notes` (Gate 2).
