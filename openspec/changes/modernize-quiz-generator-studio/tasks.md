# Tasks

## 1. Frontend: Studio Header & Page Layout Architecture

- [x] 1.1 In `frontend/pages/quiz.vue`, modernize the page header section with a clean studio icon tile (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400`), high-contrast title, and localized studio subtitle, strictly eliminating glowing badge halos or animated pulse rings.
- [x] 1.2 In `frontend/pages/quiz.vue`, preserve the interactive tab switcher bar while ensuring seamless integration with the new studio header styling.

## 2. Frontend: 2-Column Bento Generation Grid

- [x] 2.1 In `frontend/pages/quiz.vue`, refactor Tab 1 (`quizStore.activeTab === 'generate'`) from a single vertical stacked card into an ergonomic 2-column Bento Grid on desktop (`grid grid-cols-1 lg:grid-cols-12 gap-6`).
- [x] 2.2 In `frontend/pages/quiz.vue`, construct the Left Bento Column (`lg:col-span-7`) containing the custom topic input, quick topic chips, and grounded-in-book toggle card with `AppSelect` book dropdown.
- [x] 2.3 In `frontend/pages/quiz.vue`, construct the Right Bento Column (`lg:col-span-5`) organizing the seniority level selection, question count controls, and primary generation CTA.

## 3. Frontend: Minimalist Typographic Seniority Cards & Segmented Count Controls

- [x] 3.1 In `frontend/pages/quiz.vue`, refactor the 4 seniority level cards (`Fresher`, `Junior`, `Mid-Level`, `Senior`) into clean, distraction-free typographic cards without emojis or icon badges, featuring responsive description copy (`text-xs sm:text-sm font-normal text-slate-500 dark:text-slate-400`) and a discrete active dot indicator (`w-2 h-2 rounded-full bg-brand-500`).
- [x] 3.2 In `frontend/pages/quiz.vue`, replace isolated question count buttons with an integrated segmented pill bar (`bg-slate-100 dark:bg-canvas-subtle p-1 rounded-xl border border-slate-200/80 dark:border-white/[0.08]`) for selecting 5 vs 10 questions.
- [x] 3.3 In `frontend/pages/quiz.vue`, modernize the primary generation button (`data-testid="generate-quiz-btn"`) with calm inline loader feedback during `quizStore.isGenerating` and full-width mobile/desktop responsiveness.

## 4. Frontend: Bilingual Localization & Responsive Typography Compliance

- [x] 4.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, verify and add concise studio subtitles and labels for the quiz generation hub.
- [x] 4.2 In `frontend/pages/quiz.vue`, enforce `whitespace-nowrap shrink-0` and responsive gap layout across all action buttons, chips, and level cards to prevent text wrapping collisions between English and Vietnamese locales.

## 5. Verification & Automated Test Suite Execution

- [x] 5.1 Validate OpenSpec specifications and change schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 5.2 Execute frontend unit and component tests (`npm test` in `frontend/`) to ensure all quiz page interactions and store bindings remain green.
- [x] 5.3 Execute Nuxt production build check (`npm run build` in `frontend/`) to verify clean SSR hydration and zero compilation errors.
