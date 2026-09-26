# Tasks

## 1. Application — scope graph projection to the user

- [x] 1.1 In `GetKnowledgeGraphQueryHandler.ExecuteAsync`, scope the `DocumentBooks` query to the owner (`b.CreatedByUserId == request.UserId && b.IsPublished && !b.IsDeleted`); verify via a handler unit test asserting a book owned by another user is excluded while the caller's book is present.
- [x] 1.2 Load user cards and highlights first, then derive the touched-topic id set = distinct non-null `card.TopicId` ∪ topics matched to any user highlight tag (reuse `MatchesTopic`); emit a topic node only when its id is in that set. Verify with a unit test where a card links Topic A and no artifact links Topic B → nodes contain Topic A, exclude Topic B.
- [x] 1.3 Emit a `pillar-{Category}` hub only for categories present among {user books, touched topics, user cards}; drop the unconditional five-pillar loop. Verify with a unit test where the user's only artifacts are Backend & Runtime → exactly one pillar hub is returned and other pillars are absent.
- [x] 1.4 Remove the dead global logic: `IsMasterCurriculumBook`, the universal multi-pillar `BookToPillar` fan-out, and `GetEffectiveBookCategory`'s cross-pillar branch (keep plain category mapping if still needed). Verify the project builds (`dotnet build`) and no reference to the removed members remains (`grep`).
- [x] 1.5 Assemble edges only between nodes present in the payload (`TopicToPillar`, `BookToPillar`, `CardToTopic`, `CardToHighlight`, `CardToPillar`, `BookToTopic`, `HighlightToBook`, `HighlightToTopic`, `SharedTag`); an orphan card (`TopicId == null`, `SourceHighlightId == null`) forces its pillar hub to be emitted. Verify with a unit test asserting no edge references a missing node id and the orphan card has a `CardToPillar` edge (no degree-0 node).
- [x] 1.6 Add a handler unit test for the empty case: a user with no books/cards/highlights yields empty `nodes`/`edges` arrays (no pillar hubs, no null refs) and `stats` counts all zero. Verify the test passes via `dotnet test`.
- [x] 1.7 Add a handler unit test proving soft-deleted artifacts are excluded: soft-delete a highlight, re-run projection → the highlight node and its edges are absent, and any topic/pillar left unanchored is dropped. Verify via `dotnet test`.

## 2. Infrastructure — index for the owner-scoped query

- [x] 2.1 Confirm whether an index covering `DocumentBooks.CreatedByUserId` exists (check EF configuration/migrations); if absent, add an EF Core migration creating it. Verify with `dotnet ef migrations list` showing the new migration and `dotnet ef database update` applying cleanly on a scratch DB.

## 3. Api — endpoint description

- [x] 3.1 Update the `GET /api/v1/graph` `.WithDescription(...)` in `KnowledgeGraphEndpoints.cs` to state the graph returns only the authenticated user's own learned artifacts (imported books, touched topics, personal cards and highlights). Verify the OpenAPI text renders in `/scalar/v1` (or `/openapi/v1.json`) with the updated wording.

## 4. Frontend — differentiate empty states

- [x] 4.1 In `useKnowledgeGraphStore.ts`, add a `hasAnyNodes` computed derived from `rawData.value?.nodes.length`. Verify with a Vitest store test: zero raw nodes → `hasAnyNodes === false`; non-zero → `true`.
- [x] 4.2 In `pages/graph.vue`, branch the zero-visible-nodes overlay: when `!store.hasAnyNodes` show a "no learned knowledge yet" message with a CTA navigating to `/library` (and/or `/today`); when raw nodes exist but filters hid them, keep the existing `graph.empty.*` copy with the `resetFilters()` CTA. Verify via a Vitest component test that mounts with empty `rawData` and asserts the CTA routes to `/library` (data/behavior, not CSS classes).
- [x] 4.3 Add the new empty-state copy keys under `graph.empty` (e.g. `no_knowledge_title`, `no_knowledge_description`, `start_learning_cta`) to `i18n/locales/en.json` and `i18n/locales/vi.json`. Verify `npm test` passes and no missing-key fallback warning appears for the new keys.

## 5. Verification (integration + visual)

- [x] 5.1 Run the full backend suite (`dotnet test`) and the full frontend suite (`npm test`); verify 100% pass (Gate 1).
- [x] 5.2 Smoke the endpoint against the running app for two seeded states — a user with imported book + cards + highlights, and a brand-new user with none — asserting the first returns scoped nodes and the second returns empty arrays with HTTP 200.
- [x] 5.3 Gate 2 visual verification: drive headless Chromium to `/graph` for the empty-personal-graph user and a populated user, capture Desktop (1920x1080) and Mobile (390x844) screenshots in both `en` and `vi`, confirm the empty state shows the "start learning" CTA and the populated graph shows only the user's own nodes; present the screenshots.
