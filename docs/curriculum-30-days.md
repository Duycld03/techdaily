# 📚 TechDaily — 30-Day Senior Fullstack Curriculum

Full curriculum covering Frontend (Vue 3/Nuxt 4), .NET 10/C# 13 Internals, PostgreSQL 17/Storage Engine, and System Design & Distributed Patterns.

---

## 🌐 Group 1: Modern Frontend & Browser Internals (Day 1 – 7)

### Day 1: Vue 3 Reactivity Engine Under The Hood
- **Core Concept:** `Proxy`, `Reflect`, `track()`, and `trigger()` effect tracking mechanism. Memory & performance comparison between `ref()`, `reactive()`, and `shallowRef()`.
- **Interview Question:** *"When does using `reactive()` lead to lost reactivity upon destructuring? Why does `shallowRef()` yield massive performance benefits over `ref()` for large lists (e.g. 5,000 property items)?"*
- **Doc Excerpt Source:** *Vue.js 3 Official Guide — Reactivity in Depth*

### Day 2: Rendering Strategies (SSR vs SSG vs ISR vs Island Architecture)
- **Core Concept:** Nuxt 4 rendering lifecycle: Server HTML generation $\rightarrow$ Client bundle download $\rightarrow$ Hydration. Hydration mismatch causes and solutions.
- **Interview Question:** *"What causes a Hydration Mismatch? Compare TTFB (Time to First Byte) vs TTI (Time to Interactive) between traditional SSR and Client-Side Rendering."*
- **Doc Excerpt Source:** *Nuxt 4 Docs — Rendering Modes & Hydration*

### Day 3: Browser Rendering Pipeline (Reflow vs Repaint vs Composite)
- **Core Concept:** DOM + CSSOM $\rightarrow$ Render Tree $\rightarrow$ Layout (Reflow) $\rightarrow$ Paint $\rightarrow$ Compositing. GPU hardware acceleration (`transform`, `opacity`, `will-change`).
- **Interview Question:** *"Why is modifying `top/left` significantly more expensive than `transform: translate()`? How do you prevent Layout Thrashing in complex web apps?"*
- **Doc Excerpt Source:** *web.dev — Inside Look at Modern Web Browsers*

### Day 4: Core Web Vitals & Web Performance Optimization
- **Core Concept:** The 3 Core Vitals: LCP (Largest Contentful Paint), INP (Interaction to Next Paint), CLS (Cumulative Layout Shift). Critical CSS, font preloading, image srcset, and virtual scrolling.
- **Interview Question:** *"What does INP measure and how does it differ from FID? List 3 concrete techniques you used to reduce INP on high-interaction dashboards."*
- **Doc Excerpt Source:** *web.dev — Core Web Vitals & INP Optimization*

### Day 5: State Management & Server State Caching
- **Core Concept:** Client State (Pinia) vs Server State (TanStack Query / Pinia Colada). `stale-while-revalidate`, Optimistic Updates, and error rollback.
- **Interview Question:** *"Design an Optimistic Update workflow for a 'Save to Favorites' button that updates the UI instantly while safely rolling back if the server returns HTTP 500."*
- **Doc Excerpt Source:** *TanStack Query / Pinia Colada Design Guides*

### Day 6: Real-time Frontend (WebSockets, SSE, Long Polling)
- **Core Concept:** WebSockets vs Server-Sent Events (SSE) vs Polling. Heartbeat ping/pong, exponential backoff reconnects, socket lifecycle in SPA/SSR.
- **Interview Question:** *"When should you prefer SSE over WebSockets? How do you prevent multi-tab users from opening 5 redundant WebSocket connections to your backend?"*
- **Doc Excerpt Source:** *MDN Web Docs & SignalR JavaScript Client Specifications*

### Day 7: Modern Build Tools & Micro-Frontends
- **Core Concept:** Vite (Rollup + ESBuild) vs Webpack. Tree-shaking mechanisms (ESM vs CJS), Dynamic Imports (`import()`), Code splitting and chunk analysis.
- **Interview Question:** *"Can tree-shaking eliminate unused code inside a CommonJS (`require`) module? Why or why not?"*
- **Doc Excerpt Source:** *Vite Official Guide — Rollup Bundling Internals*

---

## ⚙️ Group 2: .NET 10 & C# 13 Internals (Day 8 – 15)

### Day 8: Garbage Collection (GC) Internals & Allocations
- **Core Concept:** Gen 0/1/2, Large Object Heap (LOH > 85KB), Pinned Object Heap (POH), GC Pause, Background GC.
- **Interview Question:** *"Why is LOH fragmentation dangerous? How do you leverage `ArrayPool<T>.Shared` to avoid Gen 2 collections in high-throughput APIs?"*
- **Doc Excerpt Source:** *Microsoft Learn — Garbage Collection Fundamentals*

### Day 9: High-Performance Memory: `Span<T>`, `Memory<T>`, `ref struct`
- **Core Concept:** Safe stack pointers, zero-allocation slicing, contiguous memory representation across arrays, strings, and unmanaged memory.
- **Interview Question:** *"Why is `Span<T>` declared as a `ref struct` and prohibited from being stored on the Managed Heap (or inside async methods)?"*
- **Doc Excerpt Source:** *Pro .NET Memory Management / Microsoft Learn*

### Day 10: Async/Await State Machine & `ValueTask` vs `Task`
- **Core Concept:** Compiler lowering to struct state machine, `SynchronizationContext`, `ConfigureAwait(false)`, ThreadPool Starvation.
- **Interview Question:** *"Why must you NEVER await a `ValueTask` twice? In what scenario does `ValueTask` perform worse than `Task`?"*
- **Doc Excerpt Source:** *Stephen Toub — Understanding the Whys, Whats, and Whens of ValueTask*

### Day 11: Lock-Free Programming & Concurrency in .NET
- **Core Concept:** `Monitor`/`lock` vs `Interlocked` vs `ReaderWriterLockSlim` vs `SemaphoreSlim`. CPU Cache Lines and False Sharing.
- **Interview Question:** *"Compare `lock` vs `SemaphoreSlim.WaitAsync()`. Why is using `lock` inside an async method forbidden by the C# compiler?"*
- **Doc Excerpt Source:** *CLR via C# (Jeffrey Richter) — Concurrency Chapter*

### Day 12: Channels & High-Performance Producer-Consumer
- **Core Concept:** `System.Threading.Channels` — In-memory thread-safe zero-allocation queue between background producers and consumers.
- **Interview Question:** *"Compare `System.Threading.Channels` with `BlockingCollection<T>`. When should you configure a Bounded vs Unbounded channel?"*
- **Doc Excerpt Source:** *Stephen Toub — An Introduction to System.Threading.Channels*

### Day 13: Dependency Injection Lifetimes & Captive Dependencies
- **Core Concept:** Transient, Scoped, Singleton lifetimes. Captive Dependencies (Singleton holding Scoped). `IDisposable` vs `IAsyncDisposable` in DI scopes.
- **Interview Question:** *"If a Singleton service injects a Scoped `DbContext`, what memory leaks and concurrency bugs occur? What is the correct resolution pattern?"*
- **Doc Excerpt Source:** *Microsoft Learn — Dependency Injection Guidelines in .NET*

### Day 14: Kestrel Architecture & Middleware Pipeline
- **Core Concept:** Request lifecycle from Socket $\rightarrow$ Kestrel $\rightarrow$ HttpContext $\rightarrow$ Middleware Pipeline. Connection pooling and Socket reuse.
- **Interview Question:** *"What is a short-circuit middleware? Write a custom middleware that measures and logs execution time without causing buffer stream disposal issues."*
- **Doc Excerpt Source:** *ASP.NET Core Middleware Architecture Guide*

### Day 15: C# Source Generators & Native AOT
- **Core Concept:** Compile-time code generation vs Runtime Reflection. Native AOT trimming and reflection warnings in .NET 10.
- **Interview Question:** *"Why does Native AOT restrict traditional Reflection? How do C# Source Generators eliminate runtime reflection overhead for JSON serialization and DI?"*
- **Doc Excerpt Source:** *Microsoft Learn — Source Generators & Native AOT Overview*

---

## 🗄️ Group 3: Database & Storage Engine (Day 16 – 22)

### Day 16: PostgreSQL MVCC & WAL (Write-Ahead Logging)
- **Core Concept:** `xmin`, `xmax`, Dead Tuples, `VACUUM` / `AUTOVACUUM`, WAL durability guarantee.
- **Interview Question:** *"Why does an `UPDATE` in PostgreSQL actually execute an `INSERT` of a new row and flag the old row as dead? What happens if AUTOVACUUM cannot keep up?"*
- **Doc Excerpt Source:** *PostgreSQL 17 Official Manual — Chapter 65: Concurrency Control*

### Day 17: Transaction Isolation Levels & Concurrency Anomalies
- **Core Concept:** Read Committed, Repeatable Read, Serializable. Dirty Read, Non-repeatable Read, Phantom Read, Serialization Anomaly.
- **Interview Question:** *"Explain the difference between a Non-repeatable Read and a Phantom Read with concrete transaction examples. What is PostgreSQL's default level?"*
- **Doc Excerpt Source:** *PostgreSQL 17 Official Manual — Chapter 13: Isolation Levels*

### Day 18: Indexing Deep Dive: B-Tree, BRIN, Partial Indexes
- **Core Concept:** B-Tree structure, Index Scan vs Index-Only Scan vs Bitmap Index Scan, Partial Indexes (`WHERE "IsDeleted" = false`).
- **Interview Question:** *"Why might the Query Planner choose a Sequential Scan over an Index Scan even when an index exists on the filtered column?"*
- **Doc Excerpt Source:** *PostgreSQL 17 Official Manual — Chapter 11: Indexes*

### Day 19: Spatial Indexing with PostGIS (GiST & R-Tree)
- **Core Concept:** GiST (Generalized Search Tree) bounding-box indexing, `ST_MakePoint(lon, lat)`, `ST_DWithin` vs `&&` operator.
- **Interview Question:** *"How does a GiST index organize 2D spatial points? Why does passing coordinates in reverse order break spatial index lookup?"*
- **Doc Excerpt Source:** *PostGIS 3.5 Manual — Spatial Indexing & Performance*

### Day 20: Execution Plan Optimization (`EXPLAIN (ANALYZE, BUFFERS)`)
- **Core Concept:** Execution plan cost estimation, Actual Time, Shared Hit/Read buffers, Nested Loop vs Hash Join vs Merge Join.
- **Interview Question:** *"In `EXPLAIN (ANALYZE, BUFFERS)`, what do 'shared hit' and 'shared read' tell you about buffer cache utilization and disk I/O?"*
- **Doc Excerpt Source:** *Use The Index, Luke! / PostgreSQL Manual*

### Day 21: Connection Pooling & PgBouncer
- **Core Concept:** Process-per-connection cost in PostgreSQL. Session Pooling vs Transaction Pooling vs Statement Pooling.
- **Interview Question:** *"Why does Transaction Pooling in PgBouncer disable prepared statements and temporary tables? How does EF Core 10 handle this?"*
- **Doc Excerpt Source:** *PgBouncer Documentation & Architecture Guide*

### Day 22: Redis Internals & Memory Optimization
- **Core Concept:** Strings, Hashes (ziplist vs hashtable), Sorted Sets (skiplist), HyperLogLog, Redis Streams.
- **Interview Question:** *"How do you store 1,000,000 key-value pairs (`user:id -> metadata`) in Redis with minimum RAM consumption?"*
- **Doc Excerpt Source:** *Redis University — Redis Memory Optimization*

---

## 🏛️ Group 4: System Design & Distributed Patterns (Day 23 – 30)

### Day 23: Idempotency Key & Transactional Ledgers
- **Core Concept:** Idempotency Key header, database unique constraint, idempotent payment processing ledgers.
- **Interview Question:** *"If two duplicate payment requests arrive within the same microsecond, how does the architecture guarantee exactly-one debit while returning the valid result to both?"*
- **Doc Excerpt Source:** *Stripe Engineering Blog — Designing Robust and Idempotent APIs*

### Day 24: Transactional Outbox Pattern
- **Core Concept:** Solving the dual-write problem (Database write + Message Publish) without 2PC. Outbox table + Polling Publisher / Debezium CDC.
- **Interview Question:** *"Why is publishing a message to RabbitMQ immediately after `SaveChangesAsync()` an unsafe anti-pattern in distributed financial systems?"*
- **Doc Excerpt Source:** *Microservices Patterns (Chris Richardson) — Outbox Pattern*

### Day 25: Distributed Locking (Redlock & Fencing Tokens)
- **Core Concept:** Single-instance Redis lock vs Redlock. GC pause / network partition TTL expiration bug and Fencing Tokens.
- **Interview Question:** *"What is a Fencing Token and why is it mandatory to prevent split-brain writes when using distributed locks?"*
- **Doc Excerpt Source:** *Martin Kleppmann — How to do Distributed Locking*

### Day 26: Caching Strategies & Cache Stampede Defense
- **Core Concept:** Cache-Aside, Write-Through, Cache Stampede (dogpiling). Mutex locking, Probabilistic Early Expiration (XFetch algorithm).
- **Interview Question:** *"Explain the XFetch algorithm or SingleFlight mutex to defend against Cache Stampede when a 50,000 QPS key expires."*
- **Doc Excerpt Source:** *Designing Data-Intensive Applications (DDIA)*

### Day 27: Message Queues vs Event Streams (RabbitMQ vs Kafka)
- **Core Concept:** Smart broker / dumb consumer (RabbitMQ) vs Dumb broker / smart consumer (Kafka). Consumer groups, partitions, at-least-once delivery.
- **Interview Question:** *"When must you use Apache Kafka over RabbitMQ? How does Kafka guarantee message ordering within a partition?"*
- **Doc Excerpt Source:** *Apache Kafka Documentation & Architectural Whitepaper*

### Day 28: Saga Pattern (Orchestration vs Choreography)
- **Core Concept:** Distributed transaction management, Compensating Transactions (rollback logic) in multi-service bookings.
- **Interview Question:** *"In a multi-step booking flow, if the final invoicing step fails, how does the Saga Orchestrator execute compensating refunds?"*
- **Doc Excerpt Source:** *Azure Architecture Center — Saga Distributed Transactions*

### Day 29: Resilience Engineering (Circuit Breaker & Rate Limiting)
- **Core Concept:** Polly in .NET, Closed/Open/Half-Open Circuit Breaker states, Token Bucket vs Leaky Bucket algorithms, Jitter in retries.
- **Interview Question:** *"Why must you always add random Jitter to Exponential Backoff when retrying an overloaded dependency?"*
- **Doc Excerpt Source:** *AWS Architecture Blog — Exponential Backoff And Jitter*

### Day 30: System Design Mock: Real-time Event & Notification Broadcast
- **Core Concept:** Scaling SignalR to millions of active sockets using Redis Backplane or Azure SignalR Service.
- **Interview Question:** *"Design a scalable notification architecture for 5 million active users supporting Web push, mobile push, and live in-app SignalR updates."*
- **Doc Excerpt Source:** *Microsoft Docs — Real-time ASP.NET Core with SignalR scale-out*
