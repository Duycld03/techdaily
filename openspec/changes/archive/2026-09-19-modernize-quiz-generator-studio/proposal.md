# Proposal: Modernize Quiz Generator Studio with Clean Minimalist Bento Layout

## Why

The current AI Quiz Generator interface on `/quiz` (Tab 1: "Generate / Tạo Đề Mới") employs an outdated, monolithic single-column vertical form with plain rectangular buttons, micro-text descriptions below the typography standard, and generic styling that lags behind the polished Dev-Learning Studio standard established in `/profile`, `/library`, and `/notes`. 

Modernizing this tab into a structured, balanced 2-Column Bento Studio elevates the user onboarding experience, maximizes vertical screen real estate, and improves clarity when configuring custom interview drills. Crucially, this upgrade adheres to a strict design restraint: eliminating decorative noise, omitting glowing badge halos, and rejecting icon spam or emoji clutter on seniority level selectors in favor of sleek, minimalist typography, refined glassmorphic surfaces, and subtle primary brand accents.

## What Changes

- **Minimalist Studio Page Header**: Replaces the plain icon header with a sleek studio header banner featuring a structured icon tile (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20`), high-contrast title, and concise, localized subtitle, completely free from animated pulse halos, distracting glowing badges, or decorative gradients.
- **Structured 2-Column Bento Generation Layout**: Restructures the long vertical form into an ergonomic 2-column grid on desktop viewports (`lg:grid-cols-12 gap-6`):
  - **Left Column (Topic & Source Context - 7 cols)**: Dedicated `.glass-card` housing the custom topic text input, quick topic chips, and the in-book grounding toggle card with integrated `AppSelect` book picker.
  - **Right Column (Seniority & Generation Controls - 5 cols)**: Dedicated `.glass-card` organizing the 4-tier seniority level selection, question count segmented pills, and the full-width primary generation trigger.
- **Clean Minimalist Seniority Level Cards**: Refactors the seniority level picker (Fresher, Junior, Mid-Level, Senior) into clean, high-density typographic cards. Strictly eliminates decorative emoji prefixes, icons, and visual clutter, utilizing refined typography (`text-sm sm:text-base font-bold` for titles, `text-xs sm:text-sm` for role scope descriptions) and a discrete, elegant active indicator dot (`w-2 h-2 rounded-full bg-brand-500`).
- **Segmented Question Count Control**: Modernizes question count selection (5 / 10 questions) into an integrated segmented pill bar (`bg-slate-100 dark:bg-canvas-subtle p-1 rounded-xl border border-slate-200/80 dark:border-white/[0.08]`) matching the interactive studio controls in `/profile`.
- **Calm Generation Loading Feedback**: Enhances the generate action button with a smooth inline spinner, clear progress copy, and disabled state protection while avoiding disruptive screen-wide overlays or chaotic animations.
- **Responsive Typography & Bilingual Layout Compliance**: Upgrades role description copy to meet the responsive typography standard (≥14px on desktop/tablet, avoiding illegible subtext) and reinforces `whitespace-nowrap shrink-0` across all chips, badges, and tab switchers in both English and Vietnamese.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `quiz`: Updates the visual layout and interaction requirements of the Quiz Generation Studio interface, mandating the 2-column Bento structure, minimalist typographic level cards without icon spam, and segmented control tokens.

## Impact

- **Frontend**:
  - `frontend/pages/quiz.vue`: Refactor the Tab 1 (`quizStore.activeTab === 'generate'`) template and styling into the 2-column Bento architecture.
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: Ensure concise, matching subtitle and header copy if needed.
- **Tests**:
  - `frontend/tests/pages/quiz.spec.ts`: Verify test selectors and interaction assertions remain green.
- **Backend & Database**: Zero changes. Purely frontend UI/UX presentation upgrade.
