# Proposal

## Why

TechDaily carries two overlapping content models: a fixed 30-day senior curriculum (`Topic` with `DayOrder` 1..30, `InterviewQuestion.TopicId`, `SpacedRepetitionCard.SourceType=Topic`, `GET /api/v1/curriculum/roadmap`) and a bring-your-own-docs library (`DocumentBook` → `DocumentChunk` → `UserBookPacer`). The stated platform invariant is "technology-agnostic, zero hardcoded curriculum boundaries", yet the 30-day curriculum contradicts it and duplicates the recall loop. The dual model also hides a real defect: failed daily drills only create SM-2 review cards when `question.TopicId` is set, so drills sourced from a `DocumentChunk` (every real book question) silently create no review card. Production has no users, so this is the moment to collapse the platform onto a single model with no data-migration cost.

## What Changes

- **BREAKING**: Remove the fixed 30-day senior curriculum entirely. Delete the `Topic` entity, `Topics` table, `InterviewQuestion.TopicId`, `SpacedRepetitionCard.TopicId`, and the `CardSourceType.Topic` source. TechDaily becomes pure bring-your-own-docs: recall sources uniformly from `DocumentChunk`.
- **BREAKING**: New accounts start with an empty library. Remove starter-content auto-provisioning on registration (`StarterHandbookService.ProvisionForUserAsync` at `AuthEndpoints.cs:228` Google OAuth and `:392` email OTP verify), retire `StarterHandbookService`/`IStarterHandbookService`, delete `senior-engineering-craft-handbook.json`, and remove the `Topic` upsert in `CurriculumSeeder`. The empty state guiding users to import documents becomes the initial state.
- **BREAKING**: Remove `GET /api/v1/curriculum/roadmap` and the fixed-30-day roadmap. Repoint the roadmap to visualize progress through the user's active/selected `DocumentBook` and its ordered `DocumentChunk` slices (no day-count framing, no demo pack).
- Remove the today-feed legacy path: delete `HandleLegacyTopicModeAsync` from `GetTodayFocusHandler`, return a clean empty-state response when the user has no ready books, and stop shimming `DocumentChunk` fields into a `TopicDto`.
- Fix the drill-mistake recall gap: a failed daily drill on a `DocumentChunk`-sourced question SHALL create/refresh an SM-2 card. Add a `DocumentChunk`-based card source (`SpacedRepetitionCard` links to `DocumentChunkId`) and drop the `Topic` fallback projections in the review deck.
- Unify quiz grounding onto `DocumentBook`/`DocumentChunk` (remove the "curriculum slices" notion).
- Rebuild the knowledge graph without `Topic` nodes and their edges (`TopicToPillar`, `CardToTopic→Topic`, `BookToTopic`, `HighlightToTopic`); cards and highlights link to `DocumentChunk`/`DocumentBook` nodes.
- Source AI-insights suggestion chips from library `DocumentBook` categories/chapters instead of `Topics`.
- Frontend: remove the 30-day/Day-order/curriculum-topic surfaces (roadmap curriculum track, `DocReaderPane` `topic` prop → chunk, dashboard "Day X / 30" card, header "Curriculum Day" fallback, review "Curriculum Topic" source filter, graph day-order bridges) and their i18n strings.

## Capabilities

### New Capabilities
<!-- None. The target is the existing `library` model; no new capability is introduced. -->

### Modified Capabilities
- `core-platform`: Daily scenario challenge sourced from the active `DocumentChunk` (Req: Senior Scenario Interview Challenge); retire curriculum seeder vector backfill (Req: Curriculum Vector Backfill Pipeline); API docs drop `/curriculum/roadmap` (Req: Interactive API Documentation & OpenAPI Explorer); single unified focus card replaces the dual Card A/Card B "Day X / 30" dashboard (Req: Executive Cockpit Bento Dashboard Layout Integration); REMOVE starter-handbook content invariant and registration provisioning (Reqs: Technology-Agnostic Starter Handbook Content Invariant, User-Centric Starter Handbook Provisioning on Registration); reword topic-mastery profile stats and chunk-vector-completeness framing (Reqs: User Profile Management…, Document Chunk Vector Completeness Invariant).
- `auth`: New accounts no longer provision starter content; registration completes with an empty library (Req: Registration via Email Verification Code (OTP), Google OAuth registration).
- `library`: New users start with an empty catalog; the empty state is the initial state rather than a starter-handbook deletion outcome (Req: Starter Handbook Deletion Autonomy and Empty State Transition).
- `roadmap`: Replace the fixed 30-day curriculum track, demo pack, and `/today?day={dayOrder}` navigation with a track view driven by the active `DocumentBook`'s ordered chunks (REMOVED: Curriculum Roadmap Progression & Macro View; ADDED: Library Book Roadmap Progression & Macro View; MODIFIED: Past Day Drill Review, Hierarchical Mindmap Interactive View, Active Track Synchronization & Switcher, Continuous Milestone Timeline Spine & Active Telemetry).
- `drills`: Domain model requires `InterviewQuestion.DocumentChunkId` and drops `TopicId`; a failed drill creates an SM-2 card from the chunk source (Req: Scenario Multiple-Choice Interview Question Domain Model, Multiple-Choice Submission & Instant Evaluation).
- `review`: Remove `SourceType.Topic` deck filter and the `topicTitle`/`topicSummary`/`topicDeepDiveMarkdown` card fallbacks; cards render from stored front/back or chunk-derived markdown (Reqs: Flashcard Deck Library Querying & Metrics, Spaced Repetition Studio Visual Layout & Modal Glass Surfaces).
- `quiz`: Unify grounded generation onto `DocumentBook`/`DocumentChunk` similarity search, removing the "curriculum slices" branch (Req: Book & Curriculum Grounded Quiz Generation).
- `knowledge-graph`: Remove `Topic` nodes, `Day N` badges, the `Topics` toggle/drawer, and topic edges; cards/highlights bridge to `DocumentChunk`/`DocumentBook` (Reqs: Knowledge Graph Relational Extraction API, Client-Side Canvas 2D Force Layout Visualization, Multi-Dimensional Graph Filtering & Live Search, Node Detail Slide-Over Drawer & 1-Click Action Bridges, Client-Side WebGL 3D Force-Directed Galaxy Visualization, Interactive Visual Graph Legend & Entity Guide, Knowledge Graph Synapse Edge Styling).
- `insights`: Suggested inspiration chips derive from the user's library `DocumentBook` categories/chapters instead of `Topics`.

## Impact

- **Backend entities/schema**: `Topic.cs` (delete), `InterviewQuestion.TopicId` (drop), `SpacedRepetitionCard.TopicId`/`CardSourceType.Topic` (drop; add chunk source), `ITechDailyDbContext.Topics` DbSet, `EntityConfigurations.cs` (Topic config, `IX_SpacedRepetitionCards_UserId_TopicId`), plus a destructive EF migration dropping `Topics` and the two FKs.
- **Backend handlers/services**: `GetCurriculumRoadmapHandler` + `CurriculumEndpoints` (`/api/v1/curriculum/roadmap`), `GetTodayFocusHandler` (`HandleLegacyTopicModeAsync`, `TopicDto` shim), `SubmitDailyDrillHandler` (drill-mistake card gap), `GetReviewDeckHandler`/`GetReviewCardsHandler`/`ReviewCardDto` (topic fallbacks), `GetKnowledgeGraphQueryHandler` (topic nodes/edges), `GetInsightsMetaHandler` (topic chips), `StarterHandbookService`/`CurriculumSeeder`, `AuthEndpoints` provisioning callsites, `senior-engineering-craft-handbook.json`.
- **Frontend**: `pages/roadmap.vue`, `utils/roadmapTreeLayout.ts`, `stores/useRoadmapStore.ts`, `stores/useDailyFocusStore.ts` (`Topic` interface), `components/today/DocReaderPane.vue` (`topic` prop), `components/dashboard/HomeBentoDashboard.vue`, `components/layout/AppHeader.vue`, `components/today/InterviewChallengePane.vue`, `stores/useKnowledgeGraphStore.ts` + graph components (`GraphDetailDrawer.vue`, `GraphCanvas3D.vue`), `stores/useReviewStore.ts` + `components/review/FlashcardBentoCard.vue`/`AdvancedFilterModal.vue`, and `i18n/locales/en.json`/`vi.json` curriculum strings.
- **APIs**: Removed `GET /api/v1/curriculum/roadmap`; `GET /api/v1/daily/today` response drops `topic` and the `?day=` param; `GET /api/v1/graph` drops topic nodes; review DTOs drop topic fields; auth registration responses reflect an empty library.
- **Rollback**: Work isolated on branch `feat/curriculum-onto-library`; tag `pre-curriculum-refactor` on `main` is the immutable restore point.
