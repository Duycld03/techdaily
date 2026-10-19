# Proposal

## Why

The frontend codebase currently exhibits 107 typecheck diagnostics across 33 files during `npm run typecheck` (`vue-tsc`) and 190 warnings during `npm run lint` (`eslint`). These issues conceal actual component and API contract drifts (such as `ReaderAudioPlayer` referencing non-existent `chunkIndex`/`title` instead of `chunkOrder`/`chapterTitle`, or stores passing fetch query objects improperly), introduce potential runtime `undefined` dereferences, and prevent `typecheck` from being enforced as a blocking CI quality gate. Resolving these issues in a disciplined two-phase approach (Typecheck correctness first, followed by ESLint cleanup) restores total static type safety, eliminates dead weight, and hardens developer feedback loops.

## What Changes

- **Phase 1: Zero TypeCheck Errors (`npm run typecheck`)**:
  - Realign drifted model and store properties to match current contracts (`ChunkSummary.chapterTitle` and `chunkOrder`, `useInterviewQuizStore`, `HomeBentoDashboard`, `Sm2AssessmentShowcase`, `useAdminAuditStore`, `useAdminUsersStore`).
  - Correct enum and error union definitions (e.g. adding `"SYSTEM_TTS_FAILED"` to `AudioErrorType` in `useSliceAudio.ts`, aligning `FlashcardDeck` rating comparisons with SM-2 numeric quality grades).
  - Add safe null and undefined guards for array index lookups and optional properties under `noUncheckedIndexedAccess` (`RoadmapMindmapCanvas.vue`, `GraphDetailDrawer.vue`, `pages/read/[bookId].vue`, `pages/quiz.vue`).
  - Clean up obsolete configuration and plugin types in `nuxt.config.ts` and `00.client-storage.ts`.
- **Phase 2: Zero ESLint Warnings (`npm run lint`)**:
  - Remove all unused Lucide icon imports and dead variable/prop/emit declarations across components, pages, and stores.
  - Eliminate duplicate imports (e.g. `useSliceAudio` in `ReaderAudioPlayer.vue`).
  - Replace explicit `any` signatures with concrete API DTOs, generics, or `unknown` with type guards.
  - Fix variable mutability and assignment issues (`prefer-const` in workers, `no-useless-assignment` in composables).
  - Standardize template event hyphenation (e.g. `@update:model-value` in `settings.vue`).
  - Explicitly mark and justify legitimate Markdown-it/KaTeX `v-html` rendering locations with scoped eslint-disable directives.
- **Phase 3: Quality Gate Verification**:
  - Ensure `npm run typecheck` produces 0 diagnostics.
  - Ensure `npm run lint` produces 0 errors and 0 warnings.
  - Ensure all 104+ Vitest unit/contract test suites pass 100%.
  - Ensure `npm run build` succeeds cleanly.

## Capabilities

### Modified Capabilities

- `core-platform`: Update the `Frontend Static Type Checking` and `Frontend Automated Static Linting & Quality Gates` requirements to require zero typecheck errors and zero lint warnings across all authored frontend source files.
