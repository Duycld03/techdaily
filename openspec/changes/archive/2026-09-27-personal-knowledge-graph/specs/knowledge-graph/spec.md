## MODIFIED Requirements

### Requirement: Knowledge Graph Relational Extraction API
The system SHALL expose a protected HTTP GET endpoint `GET /api/v1/graph` that extracts and returns an associative **personal** knowledge graph for the authenticated user, derived strictly from that user's own learning artifacts in relational PostgreSQL 17 without introducing auxiliary graph databases or continuous server-side vector calculations. The graph SHALL represent only knowledge the user has actually engaged with — books the user imported, curriculum topics the user has touched, the user's personal highlights, and the user's spaced-repetition cards — and SHALL NOT project the global seeded curriculum or content owned by other users. Rendering the full prescriptive curriculum outline remains the responsibility of the `/roadmap` timeline and mindmap views, not the knowledge graph.

The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and SHALL return `HTTP 401 Unauthorized` with RFC 7807 problem details when invoked without a valid token.

All node source queries SHALL honor the global soft-delete query filter (`IsDeleted == false`), so that any book, highlight, or flashcard the user has deleted is absent from the graph without special handling.

The returned response payload (`KnowledgeGraphResponse`) SHALL consist of:
1. `nodes`: An array of `GraphNodeDto` items representing:
   - **Card Nodes:** Spaced repetition flashcards from `SpacedRepetitionCards` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label`, `topicId`, `status` (`Learning`, `Reviewing`, `Mastered`), `intervalDays`, `easeFactor`, and `repetitionCount`.
   - **Highlight Nodes:** Personal reading highlights from `UserHighlights` owned by the authenticated user (`UserId == currentUser.Id`), containing `id`, `label` (truncated quote), `documentChunkId`, `bookId`, `note`, `tags`, and `createdAt`.
   - **Book Nodes:** Only books the authenticated user imported — from `DocumentBooks` where `CreatedByUserId == currentUser.Id` and the book is published and not deleted — containing `id`, `label`, `category`, `totalChunks`, and `authorOrSourceUrl`. Books created by other accounts or seeded globally SHALL NOT appear (parity with the Library page `GET /api/v1/library/books`).
   - **Topic Nodes:** Only curriculum topics the user has actually touched. A topic SHALL be emitted if and only if it is referenced by at least one of the user's own flashcards (`SpacedRepetitionCard.TopicId`) or matched by at least one of the user's highlight tags (a normalized tag equal to the topic slug or title). Untouched curriculum topics SHALL NOT appear. Each emitted topic node contains `id`, `label`, `category`, `dayOrder`, `summary`, and `difficulty`.
   - **Pillar Hub Nodes:** Canonical architectural pillar anchors (id pattern `pillar-{Category}` for the five pillars Frontend & Web, Backend & Runtime, Database & Storage, Distributed Systems, Engineering Craft), containing `id`, `label`, `category`, and `type: "pillar"`. A pillar hub SHALL be emitted only when it anchors at least one surviving user node — a user book, a touched topic, or a user card of that category. Pillars with no user activity SHALL be omitted; the graph therefore contains between zero and five pillar hubs depending on the breadth of the user's learning.

2. `edges`: An array of `GraphEdgeDto` items connecting only nodes present in `nodes`:
   - `TopicToPillar`: Connecting each emitted topic to its parent pillar hub (`Topic.Id -> pillar-{Category}`).
   - `BookToPillar`: Connecting each user book to the pillar hub for the book's own category (`Book.Id -> pillar-{Category}`).
   - `CardToTopic`: Connecting a user flashcard to its target topic (`Card.TopicId -> Topic.Id`) when that topic is present.
   - `CardToHighlight`: Connecting a highlight-sourced flashcard (`SourceType = Highlight`, `SourceHighlightId != null`) to its source highlight.
   - `CardToPillar`: Connecting a flashcard with `TopicId == null` and `SourceHighlightId == null` to `pillar-{card.Category}`.
   - `BookToTopic`: Connecting a user book to specific emitted topics when explicitly referenced in book chapter titles, chunk summaries, or matching topic slugs/titles, rather than through a blind Cartesian product of all topics within the category.
   - `HighlightToBook`: Connecting a user highlight to its source book via `DocumentChunk.DocumentBookId` when that book is present.
   - `HighlightToTopic`: Connecting a user highlight to an emitted topic when highlight tags match the topic slug or title.
   - `SharedTag`: Connecting user highlights that share one or more normalized tag keywords.

3. `stats`: Metadata containing `totalNodes`, `totalEdges`, `nodeTypeCounts` (including counts for `pillar`, `topic`, `book`, `card`, `highlight`), and `pillarCounts`.

The query execution SHALL execute in a single consolidated read pass using EF Core `AsNoTracking()`, utilizing indexes on foreign keys (`UserId`, `TopicId`, `DocumentChunkId`, `CreatedByUserId`) to keep database query time low on production PostgreSQL 17. Because the projection is scoped to a single user's artifacts, the uncompressed JSON payload SHALL remain well under 100 KB.

#### Scenario: Unauthenticated request to knowledge graph endpoint
- **WHEN** an unauthenticated client sends `GET /api/v1/graph` without a JWT Bearer token
- **THEN** the system returns `HTTP 401 Unauthorized` with RFC 7807 problem details.

#### Scenario: Authenticated user requests knowledge graph
- **WHEN** an authenticated user who has imported books and created highlights and flashcards sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the `nodes` array contains the user's own book nodes, the topics touched by the user's cards or highlight tags, and the user's card and highlight nodes
- **AND** the `edges` array links cards to topics, highlights to books, and highlights sharing common tags
- **AND** the payload excludes books, cards, and highlights belonging to other users, and excludes curriculum topics the user has not touched.

#### Scenario: New user with zero flashcards or highlights requests graph
- **WHEN** an authenticated user who has not imported any book and has no highlights or flashcards sends `GET /api/v1/graph`
- **THEN** the system returns `HTTP 200 OK`
- **AND** the `nodes` array contains no book, topic, card, or highlight nodes and no pillar hub nodes (all node collections are empty arrays without causing null reference errors)
- **AND** the client renders the graph empty state rather than a populated canvas.

#### Scenario: Flashcard created from highlight links to highlight node
- **WHEN** an authenticated user has a flashcard created from a reading highlight (`SourceType = Highlight` and `SourceHighlightId != null`)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToHighlight"` connecting `card.Id` to `card.SourceHighlightId`
- **AND** the card node is positioned relative to its source highlight cluster.

#### Scenario: Flashcard with no linked topic or highlight links to pillar hub
- **WHEN** an authenticated user has a flashcard with `TopicId == null` and `SourceHighlightId == null` (e.g. quiz mistake card)
- **THEN** the backend graph projection derives an edge with `relationType: "CardToPillar"` connecting `card.Id` to `pillar-{card.Category}`
- **AND** the pillar hub for that category is emitted so the card node does not become an isolated degree-0 node.

#### Scenario: Shared tag associative edge generation
- **WHEN** an authenticated user has two highlights that both contain the tag `"mvcc"`
- **THEN** the backend graph projection derives a bidirectional or directed edge between the two highlight nodes with `relationType: "SharedTag"` and `label: "mvcc"`.

#### Scenario: Authenticated user receives pillar hub nodes and guaranteed connected topics
- **WHEN** an authenticated user whose artifacts span only some technical domains sends `GET /api/v1/graph` with a valid JWT Bearer token
- **THEN** the system returns `HTTP 200 OK` with `KnowledgeGraphResponse`
- **AND** the `nodes` array contains a `type: "pillar"` hub only for each category that has at least one user book, touched topic, or card (between 0 and 5 hubs), and omits pillars with no user activity
- **AND** the `edges` array contains a `TopicToPillar` edge for every emitted topic, connecting it to its respective pillar hub
- **AND** no emitted topic node has an edge degree of 0.

#### Scenario: Universal multi-disciplinary book connections
- **WHEN** a user-imported book spans multiple technical domains
- **THEN** the backend graph projection generates a `BookToPillar` edge only to the pillar hub of the book's own category, plus `BookToTopic` edges to matching touched topics
- **AND** no automatic fan-out to all four core technical pillars is generated for that book.

#### Scenario: Specific book-to-topic linking without Cartesian blowout
- **WHEN** a user-imported book belongs to `Category.BackendRuntime` and covers topics on GC and memory allocation
- **THEN** direct `BookToTopic` edges are generated only for emitted topics whose titles or slugs match the book's contents
- **AND** no automatic Cartesian product edges are created to unrelated topics solely because they share `Category.BackendRuntime`.

#### Scenario: Only user-imported books appear as book nodes
- **WHEN** an authenticated user has imported exactly one book while a different account has published a separate book
- **THEN** the graph's book nodes contain only the user's imported book
- **AND** the other account's published book is absent from the response.

#### Scenario: Only touched curriculum topics appear
- **WHEN** an authenticated user has a flashcard linked to Topic A but has no card or matching highlight tag for Topic B
- **THEN** Topic A is emitted as a topic node with a `TopicToPillar` edge to its pillar hub
- **AND** Topic B is absent from the graph.

#### Scenario: Deleted highlight or flashcard is excluded from the graph
- **WHEN** an authenticated user soft-deletes a highlight or flashcard and then requests `GET /api/v1/graph`
- **THEN** the deleted node and all of its edges are absent from the response
- **AND** any topic or pillar hub that no longer anchors a surviving user node is also omitted.
