# Tasks: Modernize Document Reader UI with Dev-Learning Studio Tokens

## 1. Frontend - Reader Shell and Header Modernization

- [x] 1.1 Update root reader container in `frontend/pages/read/[bookId].vue` from `bg-white dark:bg-slate-950` to `bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100` and verify dark canvas class in template
- [x] 1.2 Refactor sticky header in `frontend/pages/read/[bookId].vue` to `bg-white/90 dark:bg-canvas/90 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.08]` and verify translucent backdrop blur
- [x] 1.3 Modernize header action buttons (Library back button, desktop/mobile TOC toggle buttons, Export Markdown button) with hairline borders `border-slate-200/80 dark:border-white/[0.08]` and hover states `dark:hover:bg-canvas-elevated`
- [x] 1.4 Replace hardcoded `purple-*` classes on 1-Click Quiz Chapter action button with standard primary brand tokens `bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 hover:bg-brand-100 dark:hover:bg-brand-500/20`

## 2. Frontend - Table of Contents Sidebar and Drawer

- [x] 2.1 Refactor desktop TOC sidebar container in `frontend/pages/read/[bookId].vue` to `border-r border-slate-200/80 dark:border-white/[0.08] bg-slate-50/70 dark:bg-canvas-subtle/70` and verify border styling
- [x] 2.2 Replace completed slice checkmark icons (`CheckCircle2`) in desktop TOC and mobile TOC drawer from `text-emerald-500` to primary brand tokens `text-brand-600 dark:text-brand-400`
- [x] 2.3 Modernize active slice indicator in desktop and mobile TOC to `bg-brand-500/10 dark:bg-brand-500/15 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm` and inactive items to `hover:bg-slate-100 dark:hover:bg-canvas-elevated/60 text-slate-700 dark:text-slate-300`
- [x] 2.4 Refactor mobile TOC slide-over drawer panel to `bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white border-r border-slate-200/80 dark:border-white/[0.08]` with export button matching desktop styling

## 3. Frontend - Reader Content, Markdown Links, and Bottom Navigation Cards

- [x] 3.1 Update reader content selection styling and prose links in `frontend/pages/read/[bookId].vue` from `prose-a:text-emerald-500` to `prose-a:text-brand-600 dark:prose-a:text-brand-400 hover:prose-a:underline`
- [x] 3.2 Modernize Key Takeaways callout container to `bg-amber-50/60 dark:bg-amber-950/20 border border-amber-200/80 dark:border-amber-500/20 rounded-3xl` with standard bullet indicators
- [x] 3.3 Refactor Previous Slice card to `.glass-card border-slate-200/80 dark:border-white/[0.08] hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50 dark:hover:bg-canvas-elevated`
- [x] 3.4 Refactor Next Slice card to `border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 hover:border-brand-500/60 dark:hover:border-brand-500/40`
- [x] 3.5 Refactor final slice "Return to Library" completion card to replace legacy emerald tokens with primary brand tokens (`border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 text-brand-600 dark:text-brand-400`) and ensure `whitespace-nowrap shrink-0` across English and Vietnamese

## 4. Frontend - Typography Dropdown, Floating Mini-Toolbar, and Note Popover

- [x] 4.1 Refactor novel typography settings dropdown panel (`Aa`) to `bg-white/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-200/80 dark:border-white/[0.08] shadow-2xl rounded-2xl`
- [x] 4.2 Standardize typography segmented controls and options with `bg-slate-100 dark:bg-canvas-subtle` and active indicator pills with `dark:bg-canvas-elevated dark:text-brand-400 dark:border-white/[0.12]`
- [x] 4.3 Modernize floating text selection action toolbar container to `bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/80 dark:border-white/[0.12] shadow-2xl rounded-2xl`
- [x] 4.4 Modernize expandable note reflection popover container and form controls with elevated obsidian styling (`dark:bg-canvas-elevated/95`, `dark:border-white/[0.12]`, inputs `dark:bg-canvas-subtle dark:border-white/[0.10]`)
- [x] 4.5 Modernize JIT AI curating card and AI curation error retry banner with Dev-Learning Studio tokens (`bg-brand-50 dark:bg-brand-500/10`, `bg-amber-50/60 dark:bg-amber-950/20`)

## 5. Frontend - Automated Verification and Visual Quality Checks

- [x] 5.1 Run frontend unit tests (`npm test -- tests/pages/read.spec.ts`) and verify all test assertions pass
- [x] 5.2 Verify Nuxt build and type safety (`npm run build`) to ensure zero compilation or styling syntax errors
