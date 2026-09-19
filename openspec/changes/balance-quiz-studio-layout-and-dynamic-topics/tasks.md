# Tasks

## 1. Frontend: Container Width & 50/50 Bento Grid Layout

- [x] 1.1 In `frontend/pages/quiz.vue`, update the outer page container class from `max-w-5xl` to `max-w-6xl mx-auto px-4 sm:px-6 py-6 space-y-6` to eliminate wide desktop gutters and unify with `/library` and `/profile`.
- [x] 1.2 In `frontend/pages/quiz.vue`, refactor the generator tab grid from `lg:grid-cols-12` (7/5 split) into a balanced 50/50 grid (`grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch`).

## 2. Frontend: 2x2 Typographic Seniority Level Matrix

- [x] 2.1 In `frontend/pages/quiz.vue`, restructure the 4 seniority level buttons (`Fresher`, `Junior`, `Mid-Level`, `Senior`) from a vertical stack into an ergonomic $2 \times 2$ grid (`grid grid-cols-1 sm:grid-cols-2 gap-3`).
- [x] 2.2 In `frontend/pages/quiz.vue`, ensure level card descriptions maintain standard-compliant responsive typography (`text-xs sm:text-sm`) and discrete accent dot indicators while equalizing vertical card height with the left column (~440px).

## 3. Frontend: Context-Aware Dynamic Topic Suggestion Pipeline

- [x] 3.1 In `frontend/pages/quiz.vue`, implement `computedQuickTopics` that dynamically derives suggestions from `libraryStore.books` (active/reading books) and `profileStore.profile.targetRole` (specialized engineering focus).
- [x] 3.2 In `frontend/pages/quiz.vue`, add deduplication and high-yield architectural fallbacks (Runtime Internals, Concurrency, Database MVCC, Distributed Systems) capped at 6–8 compact chips.
- [x] 3.3 In `frontend/pages/quiz.vue`, bind the suggestion chips to `computedQuickTopics` with `max-w-[200px] truncate whitespace-nowrap shrink-0` styling to prevent visual overflow.

## 4. Frontend: 3-Tier Question Count Controller & Bilingual Localization

- [x] 4.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add localized string key `quiz.count_3` ("3 Questions" / "3 Câu").
- [x] 4.2 In `frontend/pages/quiz.vue`, update the question count segmented pill bar to support 3 distinct pacing options: `3` (Quick Drill), `5` (Standard Practice), and `10` (Interview Mock).

## 5. Verification & Automated Test Suite Execution

- [x] 5.1 Validate OpenSpec specifications and change schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 5.2 Update and execute frontend unit tests (`npm test -- tests/pages/quiz.spec.ts` in `frontend/`) to assert `max-w-6xl`, 50/50 grid, $2 \times 2$ seniority layout, `quiz.count_3`, and dynamic topic chips.
- [x] 5.3 Execute full frontend test suite (`npm test` in `frontend/`) and Nuxt production build (`npm run build` in `frontend/`) to ensure clean compilation and SSR hydration.
