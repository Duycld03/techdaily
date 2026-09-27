# Design

## Context

See `proposal.md` — Why. The behavior contract is in `specs/knowledge-graph/spec.md` (delta) and the current main spec `openspec/specs/knowledge-graph/spec.md`.

Current extraction lives in `backend/src/TechDaily.Application/Features/KnowledgeGraph/GetKnowledgeGraph/GetKnowledgeGraphQueryHandler.cs`. Today its `ExecuteAsync` loads four sources with mismatched scoping:

- `Topics` — **all** rows, no user filter (global seeded curriculum, via `CurriculumSeeder`). `Topic` has no `UserId`.
- `DocumentBooks` — **all** `IsPublished && !IsDeleted`, no user filter.
- `SpacedRepetitionCards` — user-scoped (`UserId == request.UserId`).
- `UserHighlights` — user-scoped.

Every topic and every published book is emitted as a node, plus five hardcoded `CanonicalPillars`, so an empty library still yields a dense graph that mirrors `/roadmap` (timeline + mindmap). The Library page (`GetBooksHandler`) already scopes books to `CreatedByUserId == userId`, so the graph and library disagree about "the user's books".

Soft delete is already handled globally: `BaseEntity.IsDeleted` + a model-wide `HasQueryFilter(e => !e.IsDeleted)` in `TechDailyDbContext.OnModelCreating`. `DeleteHighlightHandler` calls `highlight.SoftDelete()`. The graph query uses plain EF (`AsNoTracking()`, no `IgnoreQueryFilters`), so soft-deleted rows are already excluded — the deletion requirement is satisfied once the graph is fully user-scoped.

## Goals / Non-Goals

Goals:
- Project `GET /api/v1/graph` strictly from the authenticated user's own artifacts (imported books, touched topics, personal highlights, cards) and the pillar hubs those nodes connect to.
- Keep the response DTO shape (`KnowledgeGraphResponse`, `GraphNodeDto`, `GraphEdgeDto`, `stats`) unchanged so the client rendering/filter/legend/drawer requirements need no contract change.
- Add a graph empty state on `/graph` for users with no learned artifacts.

Non-Goals:
- Changing the roadmap/mindmap curriculum views, the seeder, or the `Topics`/`DocumentBooks` schema.
- Changing 2D/3D rendering, `GraphControlBar` filters, legend, drawer, or HUD requirements beyond consuming the smaller node set.
- Adding a flashcard-deletion feature (none exists today); only ensuring the graph honors existing soft deletes.

## Decisions

- **Scope books to the owner, matching the library.** Add `&& b.CreatedByUserId == request.UserId` to the `DocumentBooks` query. Chosen over a new "shared catalog" concept because the user's intent is a personal graph and library parity is the least-surprising rule. Verify a usable index on `DocumentBooks.CreatedByUserId`; add one if absent (query is user-scoped and hot).
- **Emit only touched topics; still read the `Topics` table.** The seeded `Topics` table is small (~30 rows, 1..30 `DayOrder`) and is still needed to resolve `CardToTopic` targets, highlight-tag→topic matches, and topic labels/metadata. So we keep reading it `AsNoTracking()`, but the *node emission* changes: build the touched-topic id set = `{ card.TopicId | card in userCards, TopicId != null }` ∪ `{ topic | topic.slug/title matches any user highlight tag }`, and emit topic nodes only for that set. Reuse the existing `MatchesTopic(...)` helper already in the handler for tag matching. Removed: the unconditional per-topic node loop, the all-published-books load, and the `IsMasterCurriculumBook` / universal multi-pillar `BookToPillar` fan-out (`GetEffectiveBookCategory`'s cross-pillar behavior for the master book) — that logic only served the global curriculum book.
- **Emit a pillar hub only when it anchors a surviving user node.** After the user nodes and touched topics are assembled, compute the set of categories present among {user books, touched topics, user cards}. Emit `pillar-{Category}` hubs only for those categories (0–5 hubs). This keeps the graph personal and removes the "always 5 pillars" guarantee. `CardToPillar` for orphan cards still forces its category's hub to be emitted, preserving the no-degree-0 invariant.
- **Two-phase assembly.** Load user cards + highlights + books first; derive touched-topic ids and active categories; then read topics and materialize nodes/edges. This avoids emitting anything the user did not touch and keeps a single logical read pass per source.
- **Pillar identifiers follow the code, not the stale spec literal.** The pre-existing main spec text listed `pillar-BackendDotNet`, but `CanonicalPillars` emits `pillar-BackendRuntime` (`Category.BackendRuntime`). The delta uses the label form + `pillar-{Category}` pattern and implementers keep the code's actual ids. Surfacing this rather than silently changing an unrelated identifier.
- **Client empty state — differentiate "no artifacts" from "filtered out".** `pages/graph.vue` already renders an overlay when `store.filteredNodes.length === 0`, but its copy and CTA assume filters hid everything (`graph.empty.*` + a `resetFilters()` button). With a personal graph, a brand-new user legitimately has zero *raw* nodes, where "Reset Filters" is useless. Add a store computed (e.g. `hasAnyNodes` from `rawData.nodes.length`) and branch the overlay: when there are no raw nodes, show a "no learned knowledge yet" message with a CTA to `/library` (import a doc) or `/today`; when raw nodes exist but filters hid them, keep the existing reset-filters copy. Add the new copy to `i18n/locales/en.json` and `vi.json` (both `en` and `vi`).

## Risks / Trade-offs

- [Users who used the graph to browse the seeded curriculum lose that view] → The full curriculum stays available in `/roadmap` timeline and mindmap; the graph is intentionally repositioned as "what I've learned". This is the requested behavior.
- [Brand-new users see an empty graph] → Add an explicit empty state with a CTA so the page reads as "nothing learned yet" rather than broken.
- [Tag→topic matching may over/under-connect] → Reuse the existing `MatchesTopic` logic already shipping for `HighlightToTopic`, so matching behavior is unchanged; only the node-emission gate is new.
- [Missing index on `CreatedByUserId` could slow the query] → Verify and add an EF Core index/migration if absent; result set is strictly smaller than before, so overall latency drops.
- [Rendering specs mention "5 pillar hubs" in overview LOD scenarios] → Those are label-culling examples ("labels restricted to pillar hubs"); they remain valid with ≤5 hubs and need no delta.

## Migration Plan

- No database data migration. If a `CreatedByUserId` index is missing, add an EF Core migration for it (index only, no data change).
- Deploy is backend handler + frontend empty state; payload stays schema-compatible (fewer nodes). Rollback = revert the handler and the `graph.vue` empty-state branch; no persisted state changes.
