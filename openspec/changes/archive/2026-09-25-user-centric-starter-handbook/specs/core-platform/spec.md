# Spec Delta

## ADDED Requirements

### Requirement: Technology-Agnostic Starter Handbook Content Invariant
The platform's canonical starter handbook (*Senior Engineering Craft Handbook*) SHALL define foundational chapters organized into 4 core technical pillars rather than an artificial 30-day program:
1. **Frontend Systems**: Reactive state propagation, modern web rendering & hydration models, browser rendering pipeline (reflow, repaint, compositing), web performance & Core Web Vitals, state management & cache invalidation, real-time protocols (WebSockets, SSE, long polling), module bundling & build optimization.
2. **Backend Runtime & Systems**: Generational garbage collection & memory management, contiguous memory buffers & zero-allocation slicing, asynchronous execution & non-blocking event loops, thread synchronization & concurrency primitives, asynchronous channels & producer-consumer pipelines, dependency injection scopes & lifecycle hygiene, high-throughput socket & stream processing pipelines, compile-time metaprogramming & AOT compilation.
3. **Database & Storage Systems**: Multi-Version Concurrency Control (MVCC) & Write-Ahead Logging (WAL), transaction isolation levels & concurrency anomalies, indexing structures (B-Tree, LSM-Tree, Inverted Indexes, BRIN), query optimization & execution plan analysis, connection pooling architectures, horizontal table partitioning & sharding, vector embeddings & approximate nearest neighbor search.
4. **Distributed Systems & Architecture**: Distributed caching patterns & cache stampede mitigation, transactional outbox & dual-write reliability, idempotency keys & deduplication windows, distributed rate limiting & token bucket algorithms, resilience patterns & circuit breakers, distributed tracing & OpenTelemetry W3C context propagation, CQRS & event sourcing architectures, zero-trust security & token-based authorization.

The curriculum titles, chapter slugs, summaries, and domain invariants SHALL NOT be branded around specific application frameworks, runtime frameworks, or proprietary database engines (including Vue, Nuxt, .NET/ASP.NET, or PostgreSQL). All conceptual definitions SHALL remain technology-agnostic (Backend Runtimes, Frontend Systems, Database Storage, Distributed Systems). Code snippets in TypeScript, C#, SQL, Go, or Python MAY be included strictly as concrete illustrative examples of the underlying universal concepts.
#### Scenario: User inspects starter handbook chapters
- **WHEN** a user or client inspects the chapters of the *Senior Engineering Craft Handbook*
- **THEN** all chapter titles and summaries describe universal engineering concepts rather than framework-specific tutorials
- **AND** illustrative code examples demonstrate practical applications without binding the curriculum to specific frontend frameworks.

### Requirement: User-Centric Starter Handbook Provisioning on Registration
When a new user account is created (via email/password registration or OAuth integration), the system SHALL automatically clone and provision a dedicated instance of the *Senior Engineering Craft Handbook* assigned to the new user with `CreatedByUserId = user.Id`.

The provisioned book SHALL include:
1. An active `UserBookPacer` initializing Chapter 1 / Slice 1 as active (`CurrentChunkOrder = 1`).
2. Full ownership permissions allowing the user to read, annotate, track pacing, generate flashcards, or delete the handbook from their library.

#### Scenario: New user registers account
- **WHEN** a new user successfully completes registration
- **THEN** a `DocumentBook` titled "Senior Engineering Craft Handbook" is created with `CreatedByUserId` set to the new user's ID
- **AND** a `UserBookPacer` is created with `CurrentChunkOrder = 1` and `IsActive = true`
- **AND** the user can immediately begin reading and learning without manual document importation.
