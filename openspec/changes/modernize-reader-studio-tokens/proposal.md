# Proposal: Modernize Document Reader UI with Dev-Learning Studio Tokens and Unified Primary Theme

## Why

While other core interfaces across TechDaily (Library, Notes, Insights, Profile, Quiz, Review, Roadmap, and Knowledge Graph) have been modernized to use Dev-Learning Studio design tokens (`canvas`, `canvas-subtle`, `canvas-elevated`, hairline translucent borders, and primary violet brand tokens), the dedicated document reader at `/read/[bookId]` still relies on legacy slate backgrounds (`slate-900`, `slate-950`), uncalibrated border styles, legacy emerald checkmarks/link colors/cards, and non-standard purple utility classes. Updating `/read/[bookId]` eliminates this visual inconsistency, elevates night-time developer reading ergonomics, and achieves platform-wide design coherence.

## What Changes

- **Obsidian Canvas & Glassmorphism Shell**: Modernize the root container, sticky navigation header, desktop collapsible Table of Contents (TOC) sidebar, and mobile off-canvas drawer from legacy `slate-950` / `slate-900` to Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) with hairline translucent borders (`border-slate-200/80 dark:border-white/[0.08]`).
- **Primary Brand Theme Unification**:
  - Replace legacy emerald styling on completed slice TOC checkmark icons (`text-emerald-500` -> `text-brand-600 dark:text-brand-400`).
  - Replace emerald styling on markdown article links (`prose-a:text-emerald-500` -> `prose-a:text-brand-600 dark:prose-a:text-brand-400`).
  - Replace emerald styling on the final slice completion return-to-library card (`emerald-500/30`, `emerald-50`, `emerald-950` -> `brand-500/30`, `brand-50/30 dark:bg-brand-500/10`, `text-brand-600 dark:text-brand-400`).
  - Replace hardcoded `purple-*` classes on the 1-Click Quiz Chapter action button with standard `brand-*` tokens (`bg-brand-50 dark:bg-brand-500/10 border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 hover:bg-brand-100 dark:hover:bg-brand-500/20`).
- **Modernized Interactive Controls & Novel Typography Dropdown**:
  - Refactor the novel-style typography settings popover (`Aa`), export markdown button, and back-to-library navigation button into Dev-Learning Studio glass panels (`bg-white/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-200/80 dark:border-white/[0.08] shadow-2xl`).
  - Standardize typography segmented controls and sliders with `bg-slate-100 dark:bg-canvas-subtle` and active indicator pills using `dark:bg-canvas-elevated dark:text-brand-400 dark:border-white/[0.12]`.
- **Symmetrical Navigation Cards & Key Takeaways Callout**:
  - Refactor bottom Previous/Next/Completed slice navigation cards into `.glass-card` elements with smooth hover transitions, preserving symmetrical spacing and `whitespace-nowrap shrink-0` across English and Vietnamese locales.
  - Refactor the Key Takeaways card with subtle studio glass styling (`bg-amber-50/60 dark:bg-amber-950/20 border border-amber-200/80 dark:border-amber-500/20`).
- **Floating Text Selection Toolbar & Note Reflection Popover**:
  - Modernize floating text selection action toolbar and the attached reflection note popover with elevated obsidian styling (`bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/80 dark:border-white/[0.12] shadow-2xl`).
  - Style popover quote preview with brand border accent and style note textarea and tag inputs with `dark:bg-canvas-subtle dark:border-white/[0.10] focus:ring-brand-500/40 focus:border-brand-500`.
- **Curation and Error State Banners**:
  - Modernize the JIT AI curating indicator card (`bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20`) and error/retry card with Dev-Learning Studio tokens.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `reader`: Update visual presentation, background tokens, border definitions, and brand theme styling requirements for `/read/[bookId]` to use Dev-Learning Studio obsidian canvas tokens and system primary brand tokens.

## Impact

- **Frontend Components & Pages**:
  - `frontend/pages/read/[bookId].vue` (primary reader page)
- **Frontend Tests**:
  - `frontend/tests/pages/read.spec.ts` (verify reader behavior and state handling)
- **Zero Breaking Changes**:
  - No backend code changes, no database schema changes, and no API contract modifications.
