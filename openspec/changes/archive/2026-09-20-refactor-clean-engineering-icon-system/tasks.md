# Tasks

## 1. Frontend: Semantic Loading State & Icon Realignment

- [x] 1.1 In `frontend/components/today/AISynthesisCard.vue`, replace spinning `<Sparkles>` animation with clean engineering loader `<Loader2 class="animate-spin" :stroke-width="1.5">`.
- [x] 1.2 In `frontend/components/today/DocReaderPane.vue` and `frontend/pages/read/[bookId].vue`, replace spinning `<Sparkles>` loading indicators with `<Loader2 class="animate-spin" :stroke-width="1.5">`, and replace key takeaway sparkles with semantic icons.
- [x] 1.3 In `frontend/components/layout/AppHeader.vue`, `frontend/components/layout/AppSidebar.vue`, and `frontend/components/app/AppCommandPalette.vue`, update Insights route icon from `Sparkles` to `Compass`.

## 2. Frontend: Stroke Width Standardization & Palette Normalization

- [x] 2.1 In `frontend/components/profile/EngineerMilestonesCard.vue`, eliminate disparate rainbow icon colors and normalize milestone icon containers to studio neutral glass tiles with 1.5px stroke weight.
- [x] 2.2 In `frontend/components/review/FlashcardHeroCard.vue` and `frontend/components/today/InterviewChallengePane.vue`, standardize action icons with 1.5px stroke weight and studio-consistent tones.
- [x] 2.3 Across core navigation and studio card headers (`AppHeader.vue`, `AppSidebar.vue`, `quiz.vue`), standardize 1.5px stroke weight on interactive icons.

## 3. Verification & Automated Test Suite Execution

- [x] 3.1 Validate OpenSpec specifications and schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Update and execute frontend component and navigation tests (`npm test` in `frontend/`) to ensure all assertions pass cleanly.
- [x] 3.3 Execute full Nuxt production build (`npm run build` in `frontend/`) to verify clean SSR compilation.
