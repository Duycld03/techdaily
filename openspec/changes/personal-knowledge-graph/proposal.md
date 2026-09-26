# Proposal

## Why

The knowledge graph at `/graph` is meant to visualize what a user has actually learned, but today it projects the entire seeded curriculum and every published book from all accounts, so a user with an empty library still sees a dense graph. This duplicates the `/roadmap` timeline and mindmap (which already own the full prescriptive curriculum outline) and makes the graph a poor mirror of personal progress. The graph must instead render only the authenticated user's own studied artifacts.

## What Changes

- **BREAKING** (graph payload shape): `GET /api/v1/graph` stops returning the global master curriculum and other users' books. It now derives nodes strictly from the authenticated user's own learning artifacts.
- **Book nodes** are scoped to books the user imported (`DocumentBooks.CreatedByUserId == currentUser.Id`), matching the Library page (`GET /api/v1/library/books`). Books created by other accounts or seeded globally are excluded.
- **Topic nodes** are reduced from "all seeded curriculum topics" to only the topics the user has actually touched: topics referenced by the user's own flashcards (`SpacedRepetitionCard.TopicId`) or matched by the user's highlight tags. Untouched curriculum topics no longer appear.
- **Pillar hub nodes** are emitted only when they anchor at least one surviving user node (book, touched topic, or card). Pillars with no user activity are omitted instead of always rendering all five.
- **Card and highlight nodes** remain user-scoped (unchanged) and continue to honor the global soft-delete query filter, so deleting a note or flashcard removes it from the graph.
- Remove now-dead global projection logic in the graph handler (all-topics load, all-published-books load, `IsMasterCurriculumBook` / universal multi-pillar book fan-out) that only existed to render the shared curriculum.
- Update the `/graph` empty state and affected graph specs/scenarios so a brand-new user with no books, cards, or highlights sees an empty personal graph with a "start learning" prompt rather than the full curriculum.
- The existing Mastery status filter (Learning / Reviewing / Mastered) is retained; broad topic inclusion plus this filter lets users narrow to mastered knowledge without changing the backend contract.

## Capabilities

### New Capabilities

_None._ This change modifies an existing capability only.

### Modified Capabilities

- `knowledge-graph`: The graph extraction API and its client visualization/empty-state requirements change from a global curriculum projection to a strictly user-owned "learned knowledge" projection (user-imported books, user-touched topics, user cards/highlights, and only the pillar hubs those nodes connect to). Scenarios asserting that new users receive the full curriculum, that all five pillars always render, and that the seeded master-curriculum book fans out to pillars are inverted or removed accordingly.
