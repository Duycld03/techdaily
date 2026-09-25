# Design

## Context

TechDaily is designed as a technology-agnostic active recall engine. Previously, a static database seeder created a single shared `DocumentBook` (`30-Day Senior Fullstack Curriculum`) with `CreatedByUserId = null`. This introduced two architectural issues:
1. **Ownership Violation**: Users could not delete this book from their library because `DeleteBookHandler` enforces `CreatedByUserId == currentUserId`.
2. **Framework & Engine Lock-in**: The curriculum's chapters were tied to specific tools and products (Vue 3, Nuxt 4, .NET 10 internals, PostgreSQL-specific tooling) rather than teaching universal engineering principles, conflicting with the technology-agnostic platform standard defined in `AGENTS.md`.
This design outlines the decoupling of the static curriculum into a user-owned **Senior Engineering Craft Handbook** provisioned on registration, alongside technology-agnostic content revisions and clean empty-state transitions.

## Goals / Non-Goals

**Goals:**
- Rewrite all 30 handbook modules into framework-agnostic, conceptual engineering topics across Frontend Systems, Backend Runtimes, Database Storage, and Distributed Systems.
- Provision a dedicated `DocumentBook` copy of the handbook to each newly registered user (`CreatedByUserId = user.Id`), complete with 30 `DocumentChunks` and an active `UserBookPacer`.
- Update `GetBooksHandler` so authenticated users see only their own documents in `/library`.
- Allow users to delete their starter handbook and transition seamlessly to an Empty State across `/library`, `/today`, and `/roadmap`.
- Ensure illustrative code examples (TypeScript, C#, SQL, Go) serve as conceptual references without making the curriculum framework-specific.

**Non-Goals:**
- Multi-tenant enterprise organization sharing (TechDaily is an individual developer mastery platform).
- Public third-party document marketplace or purchasing flows.

## Decisions

### 1. Template-Based User Provisioning Service (`IStarterHandbookService`)
Instead of a shared global book in `DocumentBooks`, the canonical starter curriculum will be maintained as a seedable template.
- An application service `IStarterHandbookService.ProvisionForUserAsync(Guid userId, CancellationToken ct)` will be registered in DI.
- During user registration (`AuthEndpoints.cs` for email/password and Google OAuth), this service is called to instantiate:
  - 1 `DocumentBook` (Title: "Senior Engineering Craft Handbook", `CreatedByUserId = userId`).
  - 30 `DocumentChunks` with technology-agnostic content and interview scenario questions.
  - 1 `UserBookPacer` with `CurrentChunkOrder = 1` and `IsActive = true`.
- **Alternative Considered**: Sharing a single global read-only book across all users. *Rejected* because it prevents users from deleting it, hides the empty state, and mixes system documents into user libraries.

### 2. Technology-Agnostic Curriculum Syllabus
The handbook in `curriculum-30-days.json` (rebranded to `senior-engineering-craft-handbook.json`) is organized into chapters across fundamental engineering pillars:

```
+-----------------------------------------------------------------------------------+
|                     Senior Engineering Craft Handbook                             |
+-----------------------------------------------------------------------------------+
|  Pillar 1: Frontend Systems (Chapters 1–7)                                        |
|  - Chapter 1: Reactive State Propagation & Dependency Tracking                    |
|  - Chapter 2: Modern Web Rendering & Hydration Architectures (SSR/SSG/ISR/Islands)|
|  - Chapter 3: Browser Engine Pipeline (DOM/CSSOM, Reflow, Repaint, GPU Compositing)|
|  - Chapter 4: Web Performance & Core Web Vitals (LCP, INP, CLS, Resource Timing)  |
|  - Chapter 5: State Management, Cache Invalidation & Optimistic UI                |
|  - Chapter 6: Real-Time Protocols (WebSockets, SSE, Long Polling, Backpressure)   |
|  - Chapter 7: Module Systems, Bundling Mechanics & Code Splitting (ESM)          |
+-----------------------------------------------------------------------------------+
|  Pillar 2: Backend Runtime & Systems (Chapters 8–15)                              |
|  - Chapter 8: Generational Garbage Collection & Memory Management                 |
|  - Chapter 9: Contiguous Memory Buffers & Zero-Allocation Slicing                 |
|  - Chapter 10: Asynchronous Execution Models & Non-Blocking State Machines        |
|  - Chapter 11: Thread Synchronization & Concurrency Primitives                    |
|  - Chapter 12: High-Performance Producer-Consumer Pipelines & Asynchronous Queues|
|  - Chapter 13: Dependency Injection Mechanics & Lifecycle Scoping                 |
|  - Chapter 14: High-Throughput HTTP & Socket I/O Pipelines                        |
|  - Chapter 15: Metaprogramming, AST Transformers & Ahead-Of-Time (AOT) Compilation|
+-----------------------------------------------------------------------------------+
|  Pillar 3: Database & Storage Systems (Chapters 16–22)                            |
|  - Chapter 16: Multi-Version Concurrency Control (MVCC) & Write-Ahead Logging(WAL)|
|  - Chapter 17: Transaction Isolation Levels & Concurrency Anomalies               |
|  - Chapter 18: Indexing Data Structures (B-Trees, LSM-Trees, Inverted Indexes)    |
|  - Chapter 19: Query Optimization & Cost-Based Execution Planning                 |
|  - Chapter 20: Connection Management & High-Scale Pooling                         |
|  - Chapter 21: Horizontal Data Partitioning & Sharding Strategies                 |
|  - Chapter 22: Vector Databases, High-Dimensional Embeddings & Similarity Search  |
+-----------------------------------------------------------------------------------+
|  Pillar 4: Distributed Systems & Architecture (Chapters 23–30)                    |
|  - Chapter 23: Distributed Caching Strategies & Cache Stampede Defense            |
|  - Chapter 24: Transactional Outbox Pattern & Reliable Dual-Write Delivery        |
|  - Chapter 25: Idempotent Processing & Deduplication Architectures                |
|  - Chapter 26: Distributed Rate Limiting & Throttling Algorithms                  |
|  - Chapter 27: System Resilience: Circuit Breakers, Bulkheads & Degradation       |
|  - Chapter 28: Distributed Tracing, Metrics & Context Propagation (OpenTelemetry) |
|  - Chapter 29: Command Query Responsibility Segregation (CQRS) & Event Sourcing   |
|  - Chapter 30: Zero-Trust Security & Token-Based Authorization Systems            |
+-----------------------------------------------------------------------------------+
```

### 3. User-Scoped Library Querying & Deletion Flow
- In `GetBooksHandler.cs`, change query to:
  ```csharp
  var query = _dbContext.DocumentBooks
      .AsNoTracking()
      .Where(b => b.CreatedByUserId == request.UserId && !b.IsDeleted);
  ```
- In `DeleteBookHandler.cs`, verification `book.CreatedByUserId == request.UserId` naturally succeeds for the user's starter handbook.
- When deleted:
  - `book.IsDeleted = true;`
  - All chunks set `chunk.IsDeleted = true;`
  - Associated `UserBookPacer` entries for this book are removed or marked inactive.

### 4. Zero-Book Empty State Coordination
When a user deletes all books:
- `GetTodayFocusHandler`: When `readyBooks.Count == 0`, returns a dedicated empty-state DTO or `HasActiveBook: false`.
- Frontend `/today.vue`, `/roadmap.vue`, and `/library.vue` display consistent empty states with a direct CTA: "Import a PDF, web document, or markdown file to start your personalized learning journey".

## Risks / Trade-offs

- **Storage Overhead per User**: Each user receives 30 chunk rows (~100 KB in PostgreSQL).
  - *Mitigation*: 100 KB per user is trivial for PostgreSQL storage while granting complete data isolation, user annotations, and deletion autonomy without cross-user interference.
- **Migration for Existing / Dev Users**:
  - *Mitigation*: Seeder will ensure the dev user (`00000000-0000-0000-0000-000000000001`) owns a copy of the new handbook, and any unowned legacy books are cleaned up.
