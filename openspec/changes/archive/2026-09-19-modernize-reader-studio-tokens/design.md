# Design: Modernize Document Reader UI with Dev-Learning Studio Tokens

## Context

See `proposal.md` for background and motivation.

TechDaily uses a standardized Dev-Learning Studio design system defined in `tailwind.config.js` and `frontend/assets/css/main.css`. Core tokens include:
- `canvas`: `#09090b` (obsidian black canvas background)
- `canvas-subtle`: `#121215` (secondary background for sidebars, cards, and input surfaces)
- `canvas-elevated`: `#18181b` (tertiary surface for modals, popovers, and elevated panels)
- Hairline borders: `border-slate-200/80` (light) and `dark:border-white/[0.08]` (dark)
- Brand palette: `brand-500` (`#7c3aed`), `brand-600`, `brand-400`, `brand-50` (light fill), and `brand-500/10` (dark subtle fill)
- Glassmorphism utilities: `.glass-card` (`bg-white/80 dark:bg-canvas-subtle/80 backdrop-blur-md border border-slate-200/80 dark:border-white/[0.06]`), `.glass-panel` (`bg-white/90 dark:bg-canvas-elevated/90 backdrop-blur-lg border border-slate-200 dark:border-white/[0.08]`)

While Library, Notes, Insights, Profile, Quiz, Review, Roadmap, and Knowledge Graph have been modernized with these tokens, `frontend/pages/read/[bookId].vue` still relies on legacy `slate-950`, `slate-900`, `slate-800`, uncalibrated borders, emerald accents, and hardcoded `purple-*` classes.

## Goals / Non-Goals

**Goals:**
- Modernize the reader shell (`frontend/pages/read/[bookId].vue`), sticky header, desktop TOC sidebar, and mobile slide-over TOC drawer to use Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) and hairline borders (`border-slate-200/80 dark:border-white/[0.08]`).
- Unify all completed slice checkmarks, article markdown links, and final slice completion cards to system primary brand tokens (`brand-500`, `brand-600`, `brand-400`), completely eliminating legacy emerald green (`emerald-500`) in the reader.
- Replace non-standard `purple-*` classes on the 1-Click Quiz Chapter button with primary brand tokens (`bg-brand-50 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400`).
- Modernize the novel-style typography settings popover (`Aa`), export button, back button, and bottom navigation cards using glass panel / glass card patterns.
- Enhance the scoped text selection floating action toolbar and reflection note popover with elevated obsidian styling (`dark:bg-canvas-elevated/95`, `dark:border-white/[0.12]`).
- Maintain 100% adherence to AGENTS.md invariants: 100% English codebase, bilingual layout resilience with `whitespace-nowrap shrink-0`, responsive typography standard (desktop `text-base`/`text-lg`, mobile `text-sm`), and VueUse event hygiene.

**Non-Goals:**
- No changes to backend APIs, C# handlers, database schema, or migrations.
- No modifications to reader state composables (`useReaderTypography.ts`, `useMarkdownRenderer.ts`) or store business logic (`useLibraryStore.ts`, `useNotesStore.ts`).
- No modifications to other routes or shared components outside the reader scope.

## Decisions

### Decision 1: Reader Shell & Layout Structure
- **Root Page Container**: Change from `bg-white dark:bg-slate-950` to `bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 transition-colors duration-200`.
- **Top Sticky Navigation Header**: Change from `bg-white/95 dark:bg-slate-900/90 border-b border-slate-200 dark:border-slate-800` to `bg-white/90 dark:bg-canvas/90 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.08]`.
- **Header Buttons**:
  - "Library" back button: `border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated`.
  - TOC toggle button: Active state uses `border-brand-300 dark:border-brand-500/30 bg-brand-50 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400`; inactive state uses `border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated`.
  - 1-Click Quiz button: Replace `bg-purple-50 dark:bg-purple-950/50 border-purple-200 dark:border-purple-800 text-purple-700 dark:text-purple-300` with `bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 hover:bg-brand-100 dark:hover:bg-brand-500/20`.
  - Export Markdown button: `border-slate-200/80 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-subtle hover:bg-slate-200 dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-200`.

### Decision 2: Table of Contents (TOC) Sidebar & Drawer Modernization
- **Desktop Sidebar**:
  - Container: `border-r border-slate-200/80 dark:border-white/[0.08] bg-slate-50/70 dark:bg-canvas-subtle/70`.
  - Header: `border-b border-slate-200/80 dark:border-white/[0.06]`.
  - Completed slice indicator: Replace `text-emerald-500` with `text-brand-600 dark:text-brand-400`.
  - Active slice item: `bg-brand-500/10 dark:bg-brand-500/15 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm`.
  - Inactive slice item: `hover:bg-slate-100 dark:hover:bg-canvas-elevated/60 text-slate-700 dark:text-slate-300`.
- **Mobile Off-Canvas Drawer**:
  - Drawer panel: `bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white border-r border-slate-200/80 dark:border-white/[0.08] shadow-2xl`.
  - Completed checkmark: `text-brand-600 dark:text-brand-400`.

### Decision 3: Typography Settings Dropdown (`Aa`)
- **Panel Container**:
  - Use glass-panel styling: `bg-white/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-200/80 dark:border-white/[0.08] shadow-2xl rounded-2xl`.
- **Segmented Control Buttons**:
  - Inactive options: `bg-slate-100 dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-slate-200/80 dark:border-white/[0.06]`.
  - Active options: `bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-300 dark:border-white/[0.12]`.

### Decision 4: Reading Pane, Navigation Cards, and Key Takeaways
- **Reading Pane Background**: Clean selection styling with `selection:bg-brand-500/30 selection:text-brand-600 dark:selection:text-brand-300`.
- **Prose Hyperlinks**:
  - In `article.markdown-body`: Replace `prose-a:text-emerald-500 hover:prose-a:underline` with `prose-a:text-brand-600 dark:prose-a:text-brand-400 hover:prose-a:underline`.
- **Key Takeaways Callout**:
  - Card: `bg-amber-50/60 dark:bg-amber-950/20 border border-amber-200/80 dark:border-amber-500/20 rounded-3xl`.
  - Title and bullet dots: `text-amber-900 dark:text-amber-300` with `bg-amber-500`.
- **Symmetrical Bottom Navigation Cards**:
  - Previous Slice Card: `glass-card border-slate-200/80 dark:border-white/[0.08] hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50 dark:hover:bg-canvas-elevated`.
  - Next Slice Card: `border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 hover:border-brand-500/60 dark:hover:border-brand-500/40 text-right`.
  - Return to Library (Final Slice): Replace emerald classes with primary brand classes (`border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 hover:border-brand-500/60 dark:hover:border-brand-500/40 text-brand-600 dark:text-brand-400`).

### Decision 5: Floating Selection Toolbar & Note Reflection Popover
- **Floating Toolbar**:
  - Change from `bg-slate-900 dark:bg-slate-800 border border-slate-700` to `bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/80 dark:border-white/[0.12] shadow-2xl rounded-2xl`.
- **Note Popover**:
  - Container: `bg-slate-950/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/80 dark:border-white/[0.12] shadow-2xl rounded-2xl`.
  - Quote preview: `border-l-2 border-brand-500 pl-2 text-slate-300`.
  - Textarea and inputs: `bg-slate-800/80 dark:bg-canvas-subtle border-slate-700 dark:border-white/[0.10] text-slate-100 placeholder-slate-500 focus:ring-1 focus:ring-brand-500/50 focus:border-brand-500`.

## Risks / Trade-offs

- **Risk: Teleported Modal Backdrop & Dark Mode Inheritance**:
  - Teleported elements (`<Teleport to="body">`) such as the mobile TOC drawer, floating action toolbar, and reflection note popover must properly inherit dark mode classes.
  - Nuxt's `@nuxtjs/color-mode` applies `.dark` to the `<html>` root element. Since `document.body` is a descendant of `<html>`, all `dark:` variant classes function consistently without needing manual parent class binding.
- **Risk: Responsive Text Overflow in Action Buttons Across Locales**:
  - In bilingual UI (English/Vietnamese), labels like *"Return to Library"* vs *"Quay lại thư viện"* or *"Key Takeaways"* vs *"Điểm then chốt"* have differing character counts.
  - Every action button and card header uses `whitespace-nowrap shrink-0` and responsive `gap` to prevent visual collision and awkward wrapping.
