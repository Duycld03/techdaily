# Design

## Context

The frontend runs on Nuxt 4, Vue 3.5 Composition API, TypeScript 5.7+ with strict typing and `noUncheckedIndexedAccess`, and `@nuxt/eslint`. Currently, `npm run typecheck` (`vue-tsc`) fails with 107 diagnostics across 33 files, while `npm run lint` completes with 190 warnings.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Eliminate all 107 typecheck errors across all 33 files so `npm run typecheck` exits cleanly with code 0.
- Eliminate all 190 ESLint warnings so `npm run lint` exits with 0 errors and 0 warnings.
- Keep 100% of the 104 Vitest test suites green (`npm test`).
- Ensure no behavioral or visual regression across all features (Reader, Audio player, Mindmap canvas, 3D Graph, Quiz, Review, Admin).

**Non-Goals:**
- No broad architectural rewrites or replacing functional state stores.
- No blanket suppression comments (`@ts-ignore`, `@ts-nocheck`, or global lint disabling in config).
- No changes to backend APIs or database schemas.

## Decisions

### 1. Phased Sequential Strategy (Approach A)
- **Phase 1: Zero TypeCheck Errors (`vue-tsc`)**:
  - Focus strictly on type contracts, null narrowing, and drifted properties.
  - Fixes compile-time soundness before touching style or unused symbols.
- **Phase 2: Zero ESLint Warnings (`eslint`)**:
  - Remove dead imports, unused parameters, explicit `any`, code smells, and duplicate imports.
  - Annotate audited `v-html` render points with explicit scoped directives and sanitization explanations.
- **Phase 3: Dual Quality Gate Validation**:
  - Run `typecheck`, `lint`, and `test` sequentially to ensure complete regression protection.

### 2. Model & API Drift Solutions
- **Audio & Ingestion**:
  - In `app/components/reader/ReaderAudioPlayer.vue`: Replace references to non-existent `title` and `chunkIndex` on `ChunkSummary` with authoritative properties `chapterTitle` and `chunkOrder`.
  - In `app/composables/useSliceAudio.ts`: Extend `AudioErrorType` to include `"SYSTEM_TTS_FAILED"`, aligning with speech synthesis error handling.
- **Interactive Quizzes & Dashboard**:
  - In `app/stores/useInterviewQuizStore.ts`: Expose `isLastQuestion` computed getter (`currentIndex.value >= questions.value.length - 1`) for the Next/Finish label. Answered-question keyboard shortcuts use the existing `nextQuestion()` transition even on the last question, which opens the summary.
  - In `app/components/dashboard/HomeBentoDashboard.vue`: Read challenge content from `question` and reading estimates from `documentChunk.estimatedReadMinutes`. Do not declare nonexistent response fields to suppress diagnostics. Show the daily-goal target only, since the response supplies no measured elapsed study time.
  - In `app/components/showcase/Sm2AssessmentShowcase.vue`: Align showcase objects with `ReviewCard`, including `frontMarkdown`, `backMarkdown`, category descriptors, and required SM-2 scheduling fields.
- **Cytoscape & 3D Graph**:
  - In `app/components/graph/GraphCanvas.vue`: Replace deprecated `cytoscape.Stylesheet` with `cytoscape.StylesheetCSS`.
  - In `app/components/graph/GraphCanvas3D.vue`: Use the installed constructor API, `new ForceGraph3D(container)`. Narrowly type application-owned node/link data and nullable level-of-detail labels omitted by the packaged overloads; retain the default TrackballControls.
  - In `app/components/graph/GraphMinimap.vue`: Animate `pan`, not `center: { x, y }`. Convert the clicked model point to pan with `viewportSize / 2 - modelCoordinate * zoom`; Cytoscape's `center` option selects elements rather than accepting model coordinates.
- **Admin Stores & Nuxt Config**:
  - In `app/stores/useAdminAuditStore.ts` and `useAdminUsersStore.ts`: Pass query parameters via `useApiClient` query options instead of embedding invalid `{ query }` inside `RequestInit`.
  - In `nuxt.config.ts`: Remove obsolete `@nuxtjs/i18n` property `optimizeTranslationDirective`, clean link tags in head config.

- **Authenticated Browser Smoke**:
  - The graph/roadmap E2E helper requires externally supplied `AUTH_TOKEN` and `AUTH_USER` before launching Chromium. Never commit JWTs or fallback identities; retain a one-line warning beside the environment reads.
  - `NO_AUTH=true` skips credential injection and verifies both protected-route redirects and absence of authentication cookies/local storage instead of running authenticated graph assertions.

### 3. Safe Null & Undefined Narrowing under `noUncheckedIndexedAccess`
- For array lookups (`arr[0]`, `chapters[0]`, `questions[currentIndex]`): Use early guard clauses (`if (!firstChapter) return ...`) or fallback values, rather than forced non-null assertions `!`.
- For optional refs (`node.value`, `synth`, `UtteranceCtor`): Guard against `null`/`undefined` before member invocation.

### 4. Elimination of Explicit `any`
- Replace `(err: any)` in catch blocks with `(err: unknown)` and format via `useApiError.formatError(err)` or `err instanceof Error ? err.message : String(err)`.
- Replace loose `any` signatures in stores and callbacks with typed DTOs from `types/api.generated.ts`.

### 5. `vue/no-v-html` Policy
- Audit the original 15 `v-html` locations, including the reader route; remove obsolete unreachable render points rather than suppressing them.
- Document the actual client-side rendering boundary: Markdown-it disables raw HTML, escapes generated attributes, and validates URLs; fenced code uses Shiki or HTML escaping. Shiki serializes code tokens as escaped text. Do not claim server sanitization, especially for unsaved editor previews.
- Attach a localized `eslint-disable-next-line vue/no-v-html` comment immediately before each remaining render element, with the applicable Markdown-it or Shiki rationale. Keep the lint rule enabled everywhere else.

## Risks / Trade-offs

- **Risk**: Updating a property name (e.g. `title` -> `chapterTitle` in `ReaderAudioPlayer.vue`) could reveal a runtime bug if tests weren't exercising that specific property.
  - *Mitigation*: Run the specific component unit tests (`npm test ReaderAudioPlayer`) immediately after editing.
- **Risk**: Removing "unused" variables might accidentally drop side effects in reactive getters or watchers.
  - *Mitigation*: Audit whether the unused variable was meant to trigger reactivity before deleting.
