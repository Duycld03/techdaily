# Tasks

> **Mandatory Skills Reading Protocol (AGENTS.md Pillar 5)**:
> Before modifying or implementing any frontend task below, developers and agents MUST explicitly read the corresponding canonical skill guides via `read`:
> - `read .agents/skills/vue/SKILL.md` (or `skill://vue`)
> - `read .agents/skills/vue-best-practices/SKILL.md` (or `skill://vue-best-practices`)
> - `read .agents/skills/nuxt/SKILL.md` (or `skill://nuxt`)
> - `read .agents/skills/pinia/SKILL.md` (or `skill://pinia`)
> - `read .agents/skills/vueuse-functions/SKILL.md` (or `skill://vueuse-functions`)
> - `read .agents/skills/vitest/SKILL.md` (or `skill://vitest`)

## 1. Phase 1 — Config, Composables & Stores TypeCheck Resolution

- [x] 1.1 Read `skill://nuxt` and `skill://vue-best-practices`; resolve `nuxt.config.ts` type errors by removing deprecated `@nuxtjs/i18n` property `optimizeTranslationDirective` and correcting head link types.
- [x] 1.2 Read `skill://pinia`; update `frontend/app/plugins/00.client-storage.ts` and `frontend/app/stores/useAuthStore.ts` to align store method signatures and eliminate references to unexported `init` and `invalidateStoredSession`.
- [x] 1.3 Read `skill://pinia`; update `frontend/app/stores/useInterviewQuizStore.ts` to expose `isLastQuestion` computed getter, resolving `app/pages/quiz.vue` missing property diagnostic.
- [x] 1.4 Read `skill://pinia` and `skill://vue-best-practices`; fix `useAdminAuditStore.ts` and `useAdminUsersStore.ts` to pass query parameters through `useApiClient` query options instead of embedding `{ query }` inside `RequestInit`.
- [x] 1.5 Read `skill://vueuse-functions`; update `frontend/app/composables/useSliceAudio.ts` to add `"SYSTEM_TTS_FAILED"` to `AudioErrorType`, and add proper null/undefined narrowing for `speechSynthesis`, `UtteranceCtor`, and `voices.find(...)`.
- [x] 1.6 Read `skill://vue-best-practices`; resolve promise return type mismatch diagnostics in `app/utils/legacyAudioMigration.ts`, `app/utils/legacyStorageMigration.ts`, and `app/utils/sliceAudioCache.ts`.

## 2. Phase 1 — Component & Page Model Drift and Null Narrowing

- [x] 2.1 Read `skill://vue` and `skill://vue-best-practices`; fix `frontend/app/components/reader/ReaderAudioPlayer.vue` to reference `chapterTitle` and `chunkOrder` on `ChunkSummary` instead of obsolete `title` and `chunkIndex`, and align audio error enum comparison.
- [x] 2.2 Read `skill://vue`; add safe null/undefined narrowing in `frontend/app/components/roadmap/RoadmapMindmapCanvas.vue` for `chapters[0]` and `firstChapter` properties under `noUncheckedIndexedAccess`.
- [x] 2.3 Read `skill://vue`; add safe null guards in `frontend/app/components/graph/GraphDetailDrawer.vue` for `node.value` accesses.
- [x] 2.4 Read `skill://vue`; fix `frontend/app/components/review/FlashcardDeck.vue` type disjointness comparisons between card rating and numeric SM-2 quality grades.
- [x] 2.5 Read `skill://vue`; fix `frontend/app/components/dashboard/HomeBentoDashboard.vue` and `app/pages/today.vue` daily scenario question property references to match current contract.
- [x] 2.6 Read `skill://vue`; align `frontend/app/components/showcase/Sm2AssessmentShowcase.vue` and `RetentionAnalyticsShowcase.vue` dummy data with authoritative `ReviewCard` and `SourceChannelRetention` types.
- [x] 2.7 Read `skill://vue`; fix route parameter handling and null safety in `frontend/app/pages/read/[bookId].vue`, `frontend/app/pages/roadmap.vue`, `frontend/app/pages/admin/users.vue`, `frontend/app/pages/admin/login.vue`, and `frontend/app/components/common/AppTimePicker.vue`.
- [x] 2.8 Read `skill://vue`; fix `frontend/app/components/today/InterviewChallengePane.vue` status comparison and null narrowing for `newDrill`.

## 3. Phase 1 — Graph Canvas & TypeCheck Validation

- [x] 3.1 Read `skill://vue`; update `frontend/app/components/graph/GraphCanvas.vue` to use `cytoscape.StylesheetCSS` instead of deprecated `Stylesheet`, and supply required animation fit parameters.
- [x] 3.2 Read `skill://vue`; fix `frontend/app/components/graph/GraphCanvas3D.vue` invocation syntax and `GraphMinimap.vue` center options.
- [x] 3.3 Execute `npm --prefix frontend run typecheck` and verify that all 107 diagnostics are eliminated and the command exits cleanly with 0 errors.

## 4. Phase 2 — Dead Imports & Unused Variable Elimination

- [x] 4.1 Read `skill://vue-best-practices`; remove unused Lucide icons in `AppHeader.vue`, `AdminCatalogDocumentDetail.vue`, `GraphCanvas3D.vue`, `GraphControlBar.vue`, `GraphDetailDrawer.vue`, `RoadmapMindmapCanvas.vue`, `LayoutArchetypesShowcase.vue`, `DocReaderPane.vue`, and `ReaderHeaderBar.vue`.
- [x] 4.2 Read `skill://vue-best-practices`; remove unused Lucide icons and unused component imports in `pages/admin/audit.vue`, `pages/admin/catalog.vue`, `pages/admin/categories.vue`, `pages/admin/users.vue`, `pages/insights.vue`, `pages/login.vue`, `pages/notes.vue`, `pages/quiz.vue`, `pages/read/[bookId].vue`, `pages/review.vue`, `pages/settings.vue`, and `pages/today.vue`.
- [x] 4.3 Read `skill://vue-best-practices`; clean up unused `props`, `emit`, `route`, and callback parameters (`err`, `e`, `event`) across all flagged components or prefix intentional unused placeholders with `_`.

## 5. Phase 2 — Loose `any` Elimination & Code Smells Cleanup

- [x] 5.1 Read `skill://vue-best-practices`; remove duplicate `useSliceAudio` import in `ReaderAudioPlayer.vue`.
- [x] 5.2 Read `skill://vue-best-practices`; fix `no-useless-assignment` warnings in `useMarkdownRenderer.ts` (`closeIdx`), `useSliceAudio.ts` (`baseUrl`), `ReviewForecastChart.vue` (`count`), and `useDailyFocusStore.ts` (`locale`).
- [x] 5.3 Read `skill://vue-best-practices`; fix `prefer-const` in `frontend/app/workers/ttsSynth.worker.ts` and hyphenate `@update:modelValue` to `@update:model-value` in `frontend/app/pages/settings.vue`.
- [x] 5.4 Read `skill://vue-best-practices`; replace explicit `any` in `useApiClient.ts`, stores (`useAuthStore`, `useDailyFocusStore`, `useInsightsStore`, `useInterviewQuizStore`, `useLibraryStore`, `useProfileStore`, `useReviewStore`), and `vitest.config.ts` with `unknown` or concrete DTOs.
- [x] 5.5 Read `skill://vue-best-practices`; replace explicit `any` in pages and components (`pages/login.vue`, `pages/library.vue`, `GraphCanvas3D.vue`, `FlashcardDeck.vue`, `BasePagination.vue`, `LocaleSelector.vue`, `AppCommandPalette.vue`).

## 6. Phase 2 — Scoped `v-html` Audit & Lint Validation

- [x] 6.1 Audit the original 15 `v-html` render points in `ShikiCodeBlock.vue`, `FlashcardBentoCard.vue`, `FlashcardDeck.vue`, `DocReaderPane.vue`, `InterviewChallengePane.vue`, `TermExplainerModal.vue`, `pages/insights.vue`, `pages/notes.vue`, `pages/quiz.vue`, `pages/review.vue`, and `pages/read/[bookId].vue`. Remove obsolete unreachable sinks and attach scoped `eslint-disable-next-line vue/no-v-html` comments to every remaining point, explaining the actual client Markdown-it raw-HTML/URL protections or Shiki code escaping without claiming server sanitization.
- [x] 6.2 Execute `npm --prefix frontend run lint` and verify that all 190 warnings and 0 errors are achieved (0 errors, 0 warnings).

## 7. Phase 3 — Dual Gate & Quality Verification

- [x] 7.1 Run `npm --prefix frontend run typecheck` and confirm 0 errors.
- [x] 7.2 Run `npm --prefix frontend run lint` and confirm 0 errors and 0 warnings.
- [x] 7.3 Run `npm --prefix frontend test` and verify that 100% of the 104 Vitest suites (1,028+ tests) pass without regression.
- [x] 7.4 Run `npm --prefix frontend run build` and confirm production SSR client and server builds succeed cleanly.

## 8. Review Follow-up Fixes — 2026-10-19

- [x] 8.1 Route Enter, Space, and ArrowRight through the existing next/finish transition, including the answered final question; retain `isLastQuestion` for the navigation label.
- [x] 8.2 Replace the minimap's invalid coordinate-based `center` option and type cast with a zoom-aware `pan` target.
- [x] 8.3 Remove fictional daily-focus response fields, migrate dashboard fixtures to complete typed responses, use `question` and `documentChunk`, and show the goal target without inventing elapsed study time.
- [x] 8.4 Remove the graph E2E helper's hardcoded JWT/user fallback; require environment credentials before browser launch, add the requested one-line warning, and verify genuine guest redirects without storing credentials.

### Review Follow-up Verification (Before Remaining Apply Tasks)

- Regression RED: seven intended failures reproduced the final-question keyboard, minimap coordinate, and fabricated study-time defects before the production fixes.
- Targeted GREEN: all 35 tests passed across the quiz page, minimap, and dashboard suites, including nonfinal single-step keyboard navigation.
- Full behavioral gate: `npm --prefix frontend test` passed all 104 suites and 1,033 tests. Existing unrelated Happy-DOM abort and HTTP error output remains visible.
- `npm --prefix frontend run typecheck` completed with no TypeScript diagnostics; duplicate `RoleDescriptor` import and Vue Router/Volar plugin-resolution warnings remain visible.
- `npm --prefix frontend run lint` reported 0 errors and 189 warnings. The broader zero-warning tasks remain incomplete.
- `NUXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5000 npm --prefix frontend run build` completed the production client/server build successfully.
- Missing E2E credentials failed before browser launch. `NO_AUTH=true` passed all 12 guest-route/storage assertions across desktop, tablet, and mobile.
- Direct Chromium verification used an authentic local QA session at desktop 1440x900 and mobile 390x844 in English and Vietnamese. All three final-question shortcuts reached the summary in each configuration; quiz state was seeded locally without quiz API writes. Nonfinal Enter advanced exactly once.
- Four real-data minimap checks centered the clicked model point at zoom 0.7 and 1.8 after graph layout settled. The live daily-focus response contained only authoritative fields; dashboard goal-only footers had no overlap or horizontal overflow in either locale/viewport.
- Desktop/mobile screenshots for the quiz, graph, and dashboard were captured, visually inspected, and presented in the task response. These checks cover the reviewed fixes, not every feature in this change.

## Final Apply Verification

- `npm --prefix frontend run typecheck` exited successfully with no TypeScript diagnostics. The duplicate `RoleDescriptor` warning is resolved. The existing Vue Router/Volar `vue-router/volar/sfc-route-blocks` package-export resolution message remains visible; dependency versions and plugin loading were not suppressed or changed.
- `npm --prefix frontend run lint` completed with 0 errors and 0 warnings. The original 15 HTML render points were audited: one unreachable constant-empty flashcard sink was removed, and 14 remaining sinks retain narrowly scoped, truthful client Markdown-it or Shiki escaping rationales.
- `npm --prefix frontend test` passed all 104 suites and 1,031 tests. The 3D graph mock now matches the installed constructible API. Two incidental CSS/initialization-only tests were removed rather than re-pinned; behavioral graph tests remain. Existing Happy-DOM abort and HTTP refresh-error output remains visible.
- `NUXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5000 npm --prefix frontend run build` successfully built the production SSR client and Nitro server; Nuxt reported 21.1 seconds.
- Direct Chromium verification captured 27 complete state matrices in English and Vietnamese at desktop 1440x900 and mobile 390x844. Screenshots were visually inspected and presented. All captured matrices had no document-level horizontal overflow; canvas graphs remain intentionally pannable.
- Verified surfaces include dashboard, today, library, notes, quiz setup/answer/summary, review empty/answer states, insights, settings/notification times, reader, roadmap timeline/mindmap states, 2D graph/detail drawer, 3D graph, showcase assessment/retention/layouts, learner/admin sign-in, admin users/catalog/categories/audit, and read-only catalog slice inspection.
- Authentic learner and administrator sessions came from the real login flow. Browser-local quiz/review DTO fixtures exercised UI states without submitting quiz or card changes. Notification-time edits were not saved; catalog inspection was read-only.
- All 24 quiz keyboard scenarios passed: Enter, Space, and ArrowRight each finished an answered final question or advanced a nonfinal question exactly once in all four locale/viewport combinations.
- Four settled-layout minimap checks at zoom 0.7 and 1.8 in both locales centered the clicked model point with zero x/y error. The real 3D renderer displayed 10 nodes and 7 edges with default TrackballControls; label toggling, camera rotation, 2D cleanup, and the actual 2D fit animation were exercised.
- Notification-time ArrowUp changed minutes from 00 to 01 in all four configurations. Command-palette focus reached an input. A browser-local null admin selection hid account actions and displayed the selection prompt without an uncaught application exception.
- General, Vue, TypeScript, security, and regression reviews reported no blocking findings. Google OAuth, paid AI regeneration, and audio playback quality were not exercised; no claims are made about those external flows.
- Unchanged observations outside this type/lint cleanup: the English admin-audit apply button displays the `admin.audit.apply` key, and leaving immediately after sign-out produced one navigation-aborted revoke request. No uncaught application exception was observed during the browser checks.
