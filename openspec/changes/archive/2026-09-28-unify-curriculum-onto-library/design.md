# Design

## Context

See proposal.md ("Why") for motivation. Current state relevant to the approach:

- Two content models coexist. World B (curriculum): `Topic` (`DayOrder` 1..30) → `InterviewQuestion.TopicId` → `DailyDrill`; `SpacedRepetitionCard.SourceType=Topic` with `TopicId`. World A (library): `DocumentBook` → `DocumentChunk` → `UserBookPacer`, `UserHighlight`, `DocumentChunkAudio`.
- The recall entities already carry World-A seams: `InterviewQuestion.DocumentChunkId?`, `DailyDrill.DocumentChunkId?`, and `CardSourceType` (`Topic|Highlight|QuizMistake`). `GetTodayFocusHandler` already runs World A first (`HandleBookPacerModeAsync`) and only falls back to World B (`HandleLegacyTopicModeAsync`) for guests / zero-book accounts.
- A per-user "Senior Engineering Craft Handbook" `DocumentBook` (30 chunks) is auto-provisioned on registration by `StarterHandbookService.ProvisionForUserAsync` (`AuthEndpoints.cs:228`, `:392`) from `senior-engineering-craft-handbook.json` (loaded via `CurriculumSeeder`), which ALSO upserts 30 `Topic` rows.
- Defect: `SubmitDailyDrillHandler` creates an SM-2 card only when `question.TopicId.HasValue`. Every `DocumentChunk`-sourced question has `TopicId == null`, so failed drills on real book content create no review card. `SpacedRepetitionCard` has no link to `DocumentChunk`; the review deck reads `card.Topic.Title/Summary` for `SourceType=Topic` and hardcodes `FrontendWeb`/`Senior` fallbacks otherwise.
- Production has no users. No production data migration is required; the schema drop can be destructive.

## Goals / Non-Goals

**Goals:**
- Single content model: recall, today feed, roadmap, graph, quiz, and review all source from `DocumentBook`/`DocumentChunk`.
- New accounts start with an empty library; empty states already exist and become the initial state.
- Close the drill-mistake recall gap so chunk-sourced drill failures produce SM-2 cards.
- Keep the change appliable in ordered phases so the additive/defect-fix work lands and is verifiable before the destructive schema drop.

**Non-Goals:**
- No new ingestion sources, RAG, or analytics features (separate future changes).
- No change to the SM-2 algorithm, PDF/URL ingestion, TTS, or reader typography.
- No renaming of `InterviewQuestion`/`DailyDrill`; they remain the scenario-drill entities, now exclusively chunk-sourced.
- No preservation of any senior curriculum content (explicit user decision: pure bring-your-own-docs).

## Decisions

- **Remove `Topic` entirely rather than keep it as a projection.** With zero users and the invariant demanding a technology-agnostic model, a projection would preserve dead coupling (`DayOrder`, pillar modules). Alternative (keep `Topic` as a read-model over chunks) rejected: it retains the concept the change exists to delete.
- **Card recall source = stored markdown + optional `SourceDocumentChunkId`.** Add `CardSourceType.DocumentChunk` and a nullable `SourceDocumentChunkId` on `SpacedRepetitionCard`; persist `FrontMarkdown`/`BackMarkdown` at creation (the existing Highlight/QuizMistake pattern) so the review deck never reads a `Topic`. Alternative (require a live join to `DocumentChunk` for face text) rejected: chunk text is re-curated/edited over time and cards must stay stable; `SourceDocumentChunkId` is for navigation only.
- **Fix the drill-mistake gap as part of the recall unification, not a follow-up.** `SubmitDailyDrillHandler` creates/refreshes a card from the question's `DocumentChunkId` on failure. This is the observable behavior the unified model requires and the natural regression test for the change.
- **Roadmap is repointed, not removed.** The `DocumentBook` milestone-timeline view already exists in `roadmap.vue`; the roadmap becomes progress through the active/selected book's ordered chunks (no day-count, no demo pack, no `/today?day=`). Alternative (delete the roadmap page) rejected: the per-book progress visualization is still useful under pure BYO and reuses existing code. `GET /api/v1/curriculum/roadmap` is removed; roadmap data derives from library book + chunk + pacer state.
- **Roadmap page adopts a mandatory layout archetype.** `roadmap.vue:514` currently wraps content in an unconstrained `max-w-6xl mx-auto` container, violating the layout-archetype invariant (empty desktop void on 1080p). The repointed roadmap SHALL be hosted in `StudioLayout` (the archetype that solves the "single card in a massive black void" problem and matches the existing full-bleed studio canvas language); the `max-w-*` wrapper is removed. Alternative (keep the wrapper) rejected: it reproduces the prohibited void.
- **Today feed drops the legacy path and the `TopicDto` shim.** Remove `HandleLegacyTopicModeAsync`; when the user has no ready books, return the empty-state response (`HasActiveBook = false`). `GET /api/v1/daily/today` returns the `DocumentChunk` directly (no `topic` field, no `?day=` param).
- **Registration provisions nothing.** Remove both `ProvisionForUserAsync` callsites; retire `StarterHandbookService`/`IStarterHandbookService`; delete `senior-engineering-craft-handbook.json` and the `Topic` upsert in `CurriculumSeeder`.
- **Destructive EF migration.** One migration drops the `Topics` table, `InterviewQuestions.TopicId`, `SpacedRepetitionCards.TopicId`, and `IX_SpacedRepetitionCards_UserId_TopicId`. Safe because there are no users; no backfill.
- **Single change, four ordered phases.** The proposal is captured once (full end-state contract in the spec deltas) and applied in phases; apply reads the frozen artifacts from disk so multi-session work cannot drift. Phase ordering removes all `Topic` reads before the schema drop.

## Risks / Trade-offs

- **Large blast radius across 9 capabilities.** → Spec deltas capture the full end-state up front; tasks are phased; a git checkpoint tag is created before the destructive Phase 4.
- **Partially-applied mega-change leaves a limbo state (no independent archive of the additive part).** → Accepted per the user's "one change, apply in phases" choice; mitigated by branch `feat/curriculum-onto-library` and tag `pre-curriculum-refactor`, plus a mid-change checkpoint tag before Phase 4.
- **Dropping `Topic` before all reads are gone would break the build/migration.** → Phase order: recall/today (P2) and UI/graph/roadmap (P3) remove every `Topic` read; the schema drop is Phase 4 only.
- **Frontend/i18n drift (stale `curriculum_day_*` keys, `topic` props).** → Tasks enumerate the exact files and i18n keys from the frontend audit; Gate 2 visual verification on `/`, `/today`, `/roadmap`, `/graph`, `/review` in both locales confirms no broken bindings.
- **Removing starter content means a brand-new account sees an empty app.** → Intended (pure BYO); empty-state guidance to import documents already specified in the `library` capability and is retained as the initial state.

## Migration Plan

Work occurs on branch `feat/curriculum-onto-library`; tag `pre-curriculum-refactor` on `main` is the immutable restore point.

- **Phase 1 — Additive recall foundation (reversible):** add `CardSourceType.DocumentChunk` + `SpacedRepetitionCard.SourceDocumentChunkId` (additive migration); persist card front/back at creation; fix `SubmitDailyDrillHandler` to create a card from `question.DocumentChunkId`; make review deck/list read stored markdown with no `Topic` dependency for non-topic cards. Verify: fail a chunk drill → card appears in `/review`.
- **Phase 2 — Recall & today cutover:** remove `HandleLegacyTopicModeAsync` and the `TopicDto` shim; `/today` returns the chunk + empty state; unify quiz grounding onto `DocumentBook`/`DocumentChunk`; source insights chips from library categories.
- **Phase 3 — UI, roadmap, graph, registration cutover:** repoint roadmap to active-book chunks and remove the curriculum track UI + `useRoadmapStore`/`/curriculum/roadmap`; rebuild the knowledge graph without `Topic` nodes/edges; convert `DocReaderPane` to a chunk prop; update dashboard/header/review UI and i18n; remove `StarterHandbookService` provisioning callsites in `AuthEndpoints`.
- **Checkpoint:** create git tag `pre-curriculum-schema-drop` before Phase 4.
- **Phase 4 — Destructive removal:** delete `Topic.cs`, `CurriculumEndpoints`/`GetCurriculumRoadmapHandler`, `StarterHandbookService`, `senior-engineering-craft-handbook.json`, the `CurriculumSeeder` topic upsert, `InterviewQuestion.TopicId`, `SpacedRepetitionCard.TopicId`/`CardSourceType.Topic`; add the destructive EF migration dropping `Topics` and the two FKs; remove `Topics` from `ITechDailyDbContext`.
- **Rollback:** `git reset --hard pre-curriculum-schema-drop` (undo destructive phase only) or `git reset --hard pre-curriculum-refactor` / re-checkout `main` (undo the whole epic). Both tags are immutable.

## Open Questions

None that affect the specs, approach, or task breakdown. The `InterviewQuestions` table retains its name (rename deferred; not observable behavior).
