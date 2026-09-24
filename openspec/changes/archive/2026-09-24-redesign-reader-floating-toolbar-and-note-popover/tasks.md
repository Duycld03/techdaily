# Tasks: Redesign Reader Floating Toolbar & Note Popover

## 1. Frontend - Floating Toolbar & Button Hierarchy

- [x] 1.1 In `frontend/pages/read/[bookId].vue`, refactor the floating toolbar action buttons (`Explain with Gemini`, `Highlight/Note`, `Copy`) using Dev-Learning Studio semantic tokens
- [x] 1.2 In `frontend/pages/read/[bookId].vue`, replace the amber button styling (`bg-amber-500`) on `Highlight/Note` with Iris Violet brand-tinted glass (`bg-brand-500/10 text-brand-300 border border-brand-500/20 hover:bg-brand-500/20 hover:text-white`), transitioning to solid brand violet (`bg-brand-600 text-white`) when the note popover is open
- [x] 1.3 In `frontend/pages/read/[bookId].vue`, verify that all toolbar action buttons include `whitespace-nowrap shrink-0` and responsive padding for bilingual layout robustness in both English and Vietnamese

## 2. Frontend - Note Reflection Popover & Input Styling

- [x] 2.1 In `frontend/pages/read/[bookId].vue`, bind localized placeholders `:placeholder="$t('reader.note_placeholder')"` to the reflection `<textarea>` and `:placeholder="$t('reader.tags_placeholder')"` to the tag `<input>`
- [x] 2.2 In `frontend/pages/read/[bookId].vue`, replace the thick neon focus outline on the reflection textarea and tag input with refined hairline borders (`border-slate-700/80 dark:border-white/[0.10]`) and soft brand focus rings (`focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60`)
- [x] 2.3 In `frontend/pages/read/[bookId].vue`, style the popover container with glassmorphic obsidian styling (`bg-slate-950/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/60 dark:border-white/[0.12] rounded-2xl shadow-2xl`) and responsive width (`w-72 sm:w-80 max-w-[calc(100vw-2rem)]`)
- [x] 2.4 In `frontend/pages/read/[bookId].vue`, refine the quote preview container with an Iris Violet accent bar (`border-l-2 border-brand-500/60`) and high-contrast text

## 3. Testing & Verification

- [x] 3.1 In `frontend/tests/pages/read.spec.ts`, add unit tests asserting that the reflection textarea and tag input have localized placeholder bindings and that the `Highlight/Note` button does not render amber styling classes
- [x] 3.2 Execute full frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.3 Capture visual test screenshots via headless browser at 1920x1080 (Desktop) and 390x844 (Mobile) in Dark and Light modes verifying the floating toolbar and note popover design system alignment
