using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Infrastructure.Persistence.Seeders;

public static class CurriculumSeeder
{
    public static async Task SeedAsync(TechDailyDbContext context)
    {
        var masterBookId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        if (!await context.Topics.AnyAsync())
        {
            var masterBook = new DocumentBook
            {
                Id = masterBookId,
                Title = "30-Day Senior Fullstack Curriculum",
                Slug = "30-day-senior-curriculum",
                SourceType = SourceType.MarkdownSeries,
                Category = Category.BackendDotNet,
                TotalChunks = 30,
                AuthorOrSourceUrl = "https://techdaily.dev/curriculum",
                IsPublished = true
            };

            await context.DocumentBooks.AddAsync(masterBook);

            var curriculumData = GetCurriculumItems(masterBook.Id);

            foreach (var (topic, question, chunk) in curriculumData)
            {
                await context.Topics.AddAsync(topic);
                await context.InterviewQuestions.AddAsync(question);
                await context.DocumentChunks.AddAsync(chunk);
            }

            // Seed default development test user
            var devUser = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "senior.dev@techdaily.local",
                Name = "Senior Engineer (Dev)",
                PreferredLocale = "en"
            };
            await context.Users.AddAsync(devUser);

            var streak = StreakRecord.Create(devUser.Id);
            await context.StreakRecords.AddAsync(streak);

            await context.SaveChangesAsync();
            return;
        }

        // If topics already exist, sync multiple choice options and explanations
        var items = GetCurriculumItems(masterBookId);
        foreach (var (topic, seededQuestion, _) in items)
        {
            var existingQuestion = await context.InterviewQuestions
                .Include(q => q.Topic)
                .FirstOrDefaultAsync(q => q.Topic.Slug == topic.Slug);

            if (existingQuestion != null && (existingQuestion.Options == null || !existingQuestion.Options.Any()))
            {
                existingQuestion.QuestionText = seededQuestion.QuestionText;
                existingQuestion.Options = seededQuestion.Options;
                existingQuestion.CorrectOptionIndex = seededQuestion.CorrectOptionIndex;
                existingQuestion.ExplanationMarkdown = seededQuestion.ExplanationMarkdown;
            }
        }
        await context.SaveChangesAsync();
    }

    private static List<(Topic topic, InterviewQuestion question, DocumentChunk chunk)> GetCurriculumItems(Guid bookId)
    {
        var list = new List<(Topic, InterviewQuestion, DocumentChunk)>();

        void AddDay(
            int day,
            string slug,
            string title,
            Category category,
            Difficulty difficulty,
            string summary,
            string deepDive,
            string questionText,
            List<string> questionOptions,
            int correctOptionIndex,
            string explanationMarkdown,
            List<string> expectedPoints,
            string modelAnswer,
            string originalDoc,
            string docSummary,
            List<string> takeaways,
            MicroQuizVo quiz)
        {
            var topicId = Guid.Parse($"20000000-0000-0000-0000-{day:D12}");
            var questionId = Guid.Parse($"30000000-0000-0000-0000-{day:D12}");
            var chunkId = Guid.Parse($"40000000-0000-0000-0000-{day:D12}");

            var topic = new Topic
            {
                Id = topicId,
                Slug = slug,
                Title = title,
                Category = category,
                Difficulty = difficulty,
                DayOrder = day,
                Summary = summary,
                DeepDiveMarkdown = deepDive
            };

            var question = new InterviewQuestion
            {
                Id = questionId,
                TopicId = topicId,
                QuestionText = questionText,
                Options = questionOptions,
                CorrectOptionIndex = correctOptionIndex,
                ExplanationMarkdown = explanationMarkdown,
                ExpectedKeyPoints = expectedPoints,
                ModelAnswerMarkdown = modelAnswer,
                Difficulty = difficulty
            };

            var chunk = new DocumentChunk
            {
                Id = chunkId,
                DocumentBookId = bookId,
                ChunkOrder = day,
                ChapterTitle = title,
                OriginalTextMarkdown = originalDoc,
                SummaryMarkdown = docSummary,
                KeyTakeaways = takeaways,
                MicroQuiz = quiz,
                Language = "en",
                EstimatedReadMinutes = 3
            };

            list.Add((topic, question, chunk));
        }

        // --- Day 1: Vue 3 Reactivity Engine ---
        AddDay(
            1, "vue3-reactivity-engine", "Vue 3 Reactivity Engine Under The Hood",
            Category.FrontendWeb, Difficulty.Senior,
            "Deep dive into Proxy, Reflect, track(), trigger(), and memory implications of ref() vs reactive() vs shallowRef().",
            "### Reactivity System\nVue 3 replaces Object.defineProperty with ES6 `Proxy` and `Reflect`.",
            "You have a large table rendering 10,000 real-time financial telemetry records updated via WebSocket 60 times/sec. The UI is experiencing severe frame drops and high memory usage. What architectural solution best resolves the performance bottleneck?",
            new()
            {
                "Wrap the array with reactive() and use toRaw() inside computed() getters.",
                "Use shallowRef() to store the dataset and trigger updates by replacing .value or invoking triggerRef(), avoiding deep reactive Proxy traversal.",
                "Convert the dataset into individual ref() properties for each nested item property.",
                "Disable Vue template reactivity completely and mutate DOM directly with document.querySelector."
            },
            1,
            "### Architectural Breakdown\n`shallowRef()` creates a reactive reference where only the `.value` access is tracked, completely bypassing the recursive `Proxy` wrapping overhead for all nested objects and 10,000 array elements. In high-frequency telemetry streaming (60 fps), deep reactive wrapping causes severe GC allocation spikes and Proxy trap overhead. Option B eliminates this overhead completely.\n\n- **Option A Pitfall:** `reactive()` still recursively wraps all elements initially, and `toRaw()` inside computed getters does not prevent the initial allocation penalty.\n- **Option C Pitfall:** Allocating 10,000+ individual `ref()` wrappers produces millions of heap objects and exacerbates GC pause times.\n- **Option D Pitfall:** Mutating DOM directly breaks Vue component lifecycle and state consistency.",
            new() { "Destructuring breaks property accessor interception", "shallowRef avoids recursive deep reactive proxy wrapping", "Triggering updates via triggerRef or replacement" },
            "Destructuring an object wrapped with `reactive()` decouples properties from the Proxy getter...",
            "In Vue 3, `reactive()` returns a Proxy wrapper around the target object. Property accesses trigger `track()` and mutations trigger `trigger()`...",
            "Vue 3 Reactivity uses ES6 Proxy. `shallowRef` is optimal for large data sets because it avoids recursive proxy generation.",
            new() { "Proxy intercepts get/set operations", "shallowRef only tracks .value reference changes", "Destructuring reactive objects loses Proxy bindings" },
            new() { Question = "What happens when you destructure a property from a reactive() object?", Options = new() { "It keeps reactivity", "Reactivity is lost unless toRefs() is used", "Throws a TypeError", "Creates a computed property" }, AnswerIndex = 1, Explanation = "Destructuring extracts primitive values away from the reactive Proxy object." }
        );

        // --- Day 2: Rendering Strategies ---
        AddDay(
            2, "rendering-strategies-ssr-hydration", "Rendering Strategies (SSR vs SSG vs ISR vs Island Architecture)",
            Category.FrontendWeb, Difficulty.Senior,
            "Nuxt 4 rendering lifecycle, server HTML generation, client bundle hydration, and resolving hydration mismatches.",
            "### Hydration & Rendering Lifecycle\nSSR generates static HTML on the server...",
            "During an e-commerce high-traffic flash sale, you observe client-side hydration errors when users land on product detail pages, causing full DOM re-renders and jarring layout shifts. Which architectural fix eliminates hydration mismatches while maintaining SEO?",
            new()
            {
                "Wrap dynamic client-dependent sections (such as real-time local countdown timers, personalized geo-pricing, and browser carts) inside <ClientOnly> with server-rendered skeletons.",
                "Switch the entire application from Nuxt SSR to a purely Client-Side Rendered (SPA) static site.",
                "Use suppressHydrationWarning on the root <html> tag to ignore DOM discrepancies.",
                "Disable JavaScript entirely and deliver pure server-rendered static HTML."
            },
            0,
            "### Architectural Breakdown\nHydration mismatches occur when the server-rendered DOM tree differs from the virtual DOM generated on initial client hydration (e.g. non-deterministic timestamps, browser storage, or window measurements). Isolating client-specific interactive widgets inside `<ClientOnly>` with clean skeleton fallbacks preserves server-rendered SEO markup for the core product while ensuring deterministic client hydration.\n\n- **Option B Pitfall:** Switching to full SPA eliminates initial HTML server rendering, destroying SEO indexing and degrading TTFB.\n- **Option C Pitfall:** Suppressing warnings masks broken event listeners and leads to subtle UI state bugs.\n- **Option D Pitfall:** Disabling JS removes all client interactivity, filtering, and real-time purchasing workflows.",
            new() { "DOM tree differences between server render and client initial render", "Browser-only APIs (window, localStorage) during SSR", "TTFB is faster in SSR, while TTI requires full script download and execution" },
            "A hydration mismatch occurs when the DOM tree generated by the server does not strictly match the VDOM tree generated during client hydration...",
            "Hydration is the process where Vue attaches event listeners to pre-rendered server HTML...",
            "SSR offers instant TTFB but requires careful hydration management to avoid DOM mismatches.",
            new() { "Hydration attaches listeners to server HTML", "Mismatch causes DOM re-render penalty", "Use <ClientOnly> for browser-specific data" },
            new() { Question = "Which condition directly triggers a Vue 3 Hydration Mismatch?", Options = new() { "Using async/await in setup()", "Rendering Date.now() or window.innerWidth directly in template during SSR", "Importing CSS modules", "Using Pinia store" }, AnswerIndex = 1, Explanation = "Non-deterministic values differ between server render time and client execution time." }
        );

        // --- Day 3: ASP.NET Core Kestrel Internals & System.IO.Pipelines ---
        AddDay(
            3, "dotnet-kestrel-pipelines", "ASP.NET Core Kestrel Internals & System.IO.Pipelines",
            Category.BackendDotNet, Difficulty.Senior,
            "Deep dive into Kestrel transport layers, Socket abstractions, System.IO.Pipelines (PipeReader/PipeWriter), and zero-allocation HTTP request parsing.",
            "### Kestrel Transport & Request Pipeline\nKestrel uses `System.IO.Pipelines` to achieve high-throughput zero-copy I/O...",
            "In an ASP.NET Core streaming gateway processing 50,000 concurrent multipart file uploads, the server experiences Gen 2 GC pauses and high memory fragmentation. Which architectural change resolves this bottleneck?",
            new()
            {
                "Increase the CLR Server GC heap limit and invoke GC.Collect(2, GCCollectionMode.Forced) after each request.",
                "Allocate a new byte[65536] buffer in each async iteration of HttpRequest.Body.ReadAsync().",
                "Use PipeReader from System.IO.Pipelines with ReadOnlySequence<byte> and AdvanceTo(consumed, examined) to read directly from pooled memory blocks without buffer allocation.",
                "Convert the incoming HTTP stream to a base64 string before parsing."
            },
            2,
            "### Architectural Breakdown\n`System.IO.Pipelines` provides a high-throughput, zero-allocation abstraction over I/O streams. `PipeReader` manages contiguous memory buffers from a memory pool (`MemoryPool<byte>`), giving access to slices via `ReadOnlySequence<byte>`. Calling `AdvanceTo` informs the pipeline which bytes were consumed and examined, returning buffers to the pool without creating Gen 2 heap allocations.\n\n- **Option A Pitfall:** Forcing full Gen 2 GC sweeps halts all application threads and creates catastrophic request timeouts.\n- **Option B Pitfall:** Continuously allocating byte arrays under high load promotes short-lived buffers to Gen 2 and LOH, triggering severe fragmentation.\n- **Option D Pitfall:** Base64 conversion inflates memory footprint by 33% and creates massive string heap allocations.",
            new() { "PipeReader/PipeWriter manages pooled contiguous memory buffers", "AdvanceTo allows consuming partial bytes without allocating intermediate arrays", "Zero-copy parsing avoids Large Object Heap (LOH) fragmentation under heavy payload traffic" },
            "Traditional stream architectures require continuous allocation of byte arrays (byte[] buffers) which quickly promotes short-lived streams into Gen 2 and LOH fragmentation. System.IO.Pipelines decouples buffer allocation from parsing by managing a shared memory pool via PipeReader and PipeWriter...",
            "In ASP.NET Core, Kestrel leverages `System.IO.Pipelines` to parse incoming HTTP/1.1 and HTTP/2 frames. Unlike classic `Stream` approaches where callers allocate byte arrays, `PipeReader.ReadAsync()` returns a `ReadOnlySequence<byte>` sliced directly from memory pool blocks. Callers inspect data and call `PipeReader.AdvanceTo(consumed, examined)` to release memory back to the pool without GC overhead.",
            "Kestrel uses System.IO.Pipelines to eliminate GC pressure in high-throughput network I/O.",
            new() { "PipeReader uses pooled memory blocks", "AdvanceTo marks consumed vs examined boundaries", "ReadOnlySequence<byte> supports multi-segment zero-copy slicing" },
            new() { Question = "How does System.IO.Pipelines avoid buffer allocations when parsing incoming HTTP requests?", Options = new() { "It creates a new Task<byte[]> for each packet", "It manages a pool of memory buffers and passes ReadOnlySequence<byte> directly to the parser", "It delegates request parsing to the OS kernel driver", "It converts requests to base64 strings" }, AnswerIndex = 1, Explanation = "System.IO.Pipelines uses a pooled buffer architecture that exposes slices via ReadOnlySequence<byte> without allocating new byte arrays." }
        );

        // --- Day 4: C# Async/Await State Machine & SynchronizationContext ---
        AddDay(
            4, "csharp-async-state-machine", "C# Async/Await State Machine & SynchronizationContext",
            Category.BackendDotNet, Difficulty.Senior,
            "IAsyncStateMachine lowering, ValueTask<T> vs Task<T> allocation mechanics, ConfigureAwait(false), and avoiding ThreadPool starvation.",
            "### Async State Machine Lowering\nThe C# compiler transforms async methods into a struct implementing `IAsyncStateMachine`...",
            "In a high-throughput microservice, a Redis cache lookup method returns cached data synchronously 98% of the time, but queries PostgreSQL on cache misses. What return type and configuration should you choose for optimal throughput?",
            new()
            {
                "Return Task<T> and call .Result synchronously on cache hits.",
                "Return ValueTask<T> to avoid heap Task allocations on the 98% synchronous hot path, and use ConfigureAwait(false) on internal asynchronous queries.",
                "Return Thread and start a new background worker per request.",
                "Return ValueTask<T> and await the exact same ValueTask instance multiple times in parallel."
            },
            1,
            "### Architectural Breakdown\n`ValueTask<T>` is a discriminated union struct that requires zero heap allocation when completed synchronously. For methods that hit a fast cache 98% of the time, returning `ValueTask<T>` eliminates millions of transient `Task` allocations, relieving GC Gen 0 pressure. `ConfigureAwait(false)` avoids unnecessary SynchronizationContext capture and thread hopping.\n\n- **Option A Pitfall:** Calling `.Result` blocks the current thread and can trigger ThreadPool starvation or deadlocks.\n- **Option C Pitfall:** Creating dedicated OS threads per request exhausts kernel resources and adds substantial context switching cost.\n- **Option D Pitfall:** Awaiting a `ValueTask` backed by pooled `IValueTaskSource` more than once causes undefined behavior and data corruption.",
            new() { "ValueTask is a discriminated union struct avoiding heap Task allocation for synchronous completions", "Awaiting a ValueTask multiple times causes undefined behavior when backed by pooled IValueTaskSource", "Use AsTask() if multiple awaits or concurrent operations are needed" },
            "When an async method completes synchronously (e.g. cache hit), `ValueTask<T>` creates zero heap allocations because it is a stack-allocated struct. When returning `Task<T>`, the CLR must allocate a `Task` object on the Managed Heap even if the result was immediately available. However, because `ValueTask<T>` may wrap an `IValueTaskSource` that is pooled and reset upon consumption, awaiting it more than once or calling `.Result` concurrently can corrupt the underlying pool state...",
            "In .NET, the compiler lowers async methods into state machine structs. `ValueTask<T>` eliminates allocations on hot synchronous paths by avoiding heap-allocated Task objects. Always use `ConfigureAwait(false)` in non-UI libraries to prevent SynchronizationContext deadlocks and context-switching overhead.",
            "ValueTask<T> eliminates heap allocations for synchronous paths. Never await a ValueTask multiple times.",
            new() { "ValueTask<T> is a struct avoiding heap Task allocation", "Never await a ValueTask<T> multiple times", "ConfigureAwait(false) bypasses SynchronizationContext capture" },
            new() { Question = "Why should you never await a ValueTask<T> more than once?", Options = new() { "It throws an OutOfMemoryException", "The underlying IValueTaskSource may be pooled and reused, leading to race conditions and corrupted results", "It cancels the background thread", "ValueTask only works on the UI thread" }, AnswerIndex = 1, Explanation = "ValueTask instances backed by IValueTaskSource are reset and returned to object pools upon completion; awaiting twice accesses a reused or recycled object." }
        );

        // --- Day 8: Garbage Collection (.NET) ---
        AddDay(
            8, "dotnet-gc-internals", "Garbage Collection (GC) Internals & Allocations",
            Category.BackendDotNet, Difficulty.Senior,
            "Gen 0/1/2, Large Object Heap (LOH > 85KB), Pinned Object Heap (POH), GC Pause, and Background GC.",
            "### .NET GC Architecture\nGenerational garbage collection segregates objects by lifetime...",
            "A payment processing API experiences intermittent 400ms latency spikes during peak load. APM diagnostics reveal severe Large Object Heap (LOH) fragmentation caused by temporary 128KB JSON payload buffers. How should you fix this?",
            new()
            {
                "Use GC.Collect() periodically in a background timer.",
                "Use ArrayPool<byte>.Shared.Rent(131072) with a try...finally block returning the buffer to the pool via Return().",
                "Split the JSON payload into 80KB chunks using multiple nested string concatenations.",
                "Switch the application runtime from 64-bit to 32-bit."
            },
            1,
            "### Architectural Breakdown\nObjects >= 85,000 bytes are allocated directly to the Large Object Heap (LOH), which is not compacted during standard GC sweeps. Frequent allocation and disposal of 128KB buffers quickly fragments the LOH and triggers expensive Gen 2 collections. Renting from `ArrayPool<byte>.Shared` reuses existing pooled buffers across requests with zero new heap allocations.\n\n- **Option A Pitfall:** Calling `GC.Collect()` triggers blocking full collection sweeps across all processors, worsening latency spikes.\n- **Option C Pitfall:** String concatenation creates large amounts of intermediate transient strings in Gen 0.\n- **Option D Pitfall:** 32-bit runtime caps total virtual memory to 2GB-4GB, increasing OutOfMemoryException risk.",
            new() { "LOH is not compacted by default causing OutOfMemoryException despite free RAM", "Objects > 85,000 bytes allocated directly to LOH", "ArrayPool reuses byte arrays avoiding heap allocations and Gen 2 collection triggers" },
            "LOH fragmentation occurs because large object allocations are not compacted during typical GC sweeps due to the cost of copying large memory segments...",
            "The .NET garbage collector is a generational collector with three generations: 0, 1, and 2. Objects larger than 85,000 bytes bypass Gen 0 and land directly on the LOH...",
            ".NET GC utilizes generational heuristics. LOH fragmentation is mitigated by ArrayPool pooling.",
            new() { "LOH threshold is 85,000 bytes", "Gen 2 collections induce expensive GC pauses", "ArrayPool<T>.Shared reduces allocations to near zero" },
            new() { Question = "What is the size threshold for an object to be allocated directly onto the Large Object Heap (LOH)?", Options = new() { "16 KB", "64 KB", "85,000 bytes (~83 KB)", "1 MB" }, AnswerIndex = 2, Explanation = "Objects 85,000 bytes or larger are routed directly to the LOH in standard .NET runtimes." }
        );

        // --- Day 9: High-Performance Memory (Span<T>, Memory<T>) ---
        AddDay(
            9, "dotnet-span-memory", "High-Performance Memory: Span<T>, Memory<T>, ref struct",
            Category.BackendDotNet, Difficulty.Senior,
            "Zero-allocation slicing, contiguous memory representation, safe stack pointers, and compiler safety rules.",
            "### Span<T> and ref struct\n`Span<T>` represents a contiguous region of arbitrary memory...",
            "You need to parse high-frequency network protocol frames across asynchronous network boundary reads. Why must you use Memory<byte> instead of Span<byte> across async methods?",
            new()
            {
                "Span<T> is limited to 1,024 elements only.",
                "Span<T> is a ref struct whose interior pointer resides on the call stack; because async state machines are lifted to the managed heap across await points, storing a Span<T> violates CLR stack safety rules.",
                "Span<T> does not support numeric byte conversions.",
                "Memory<T> uses native C++ pointers whereas Span<T> uses managed references."
            },
            1,
            "### Architectural Breakdown\n`Span<T>` is a `ref struct` that can only live on the execution stack to prevent dangling stack pointers. Because the C# compiler transforms async methods into heap-allocated state machine structs (`IAsyncStateMachine`), fields inside the state machine cannot be `ref struct` types. `Memory<T>` / `ReadOnlyMemory<T>` is a regular struct that can safely be stored on the heap and sliced into a `Span<T>` within synchronous method frames.\n\n- **Option A Pitfall:** `Span<T>` length is an `int` supporting up to 2GB contiguous elements.\n- **Option C Pitfall:** `Span<T>` supports full numeric casting via `MemoryMarshal.Cast`.\n- **Option D Pitfall:** Both `Memory<T>` and `Span<T>` are native managed .NET runtime abstractions.",
            new() { "Span<T> contains a stack ref pointer which could lead to dangling pointers if placed on heap", "Async state machine transforms local variables into heap-allocated fields", "Memory<T> provides heap-safe slicing for async workflows" },
            "`Span<T>` is declared as a `ref struct` because it contains an interior ref pointer that can point directly to stack memory...",
            "By constraining `Span<T>` to stack frames, the C# compiler guarantees memory safety without garbage collector overhead...",
            "`Span<T>` gives zero-overhead slicing across stack, native, and heap memory. Use `Memory<T>` across async boundaries.",
            new() { "Span<T> is a stack-only ref struct", "Cannot be boxed, used in async methods, or stored in generic classes", "Use Memory<T> when async spanning is required" },
            new() { Question = "Why can't Span<T> be used as a parameter or field in an async method?", Options = new() { "It is too slow", "Async methods compile to struct/class state machines on the heap, violating ref struct stack invariants", "It lacks generic support", "EF Core forbids it" }, AnswerIndex = 1, Explanation = "Async state machines are heap-allocated, which would cause stack pointers inside Span<T> to become dangling pointers." }
        );

        // --- Day 16: PostgreSQL MVCC & WAL ---
        AddDay(
            16, "postgres-mvcc-wal", "PostgreSQL MVCC & WAL (Write-Ahead Logging)",
            Category.DatabaseStorage, Difficulty.Senior,
            "xmin, xmax, Dead Tuples, VACUUM / AUTOVACUUM, and WAL durability guarantees.",
            "### PostgreSQL MVCC\nPostgres achieves concurrency control via Multi-Version Concurrency Control...",
            "A high-frequency inventory update service executes 5,000 UPDATE operations per minute. Over time, query latencies degrade significantly and table size balloons from 50MB to 4GB. What is the root cause and optimal solution?",
            new()
            {
                "PostgreSQL MVCC does not support indices on integer columns; rebuild indices with HASH.",
                "Every UPDATE writes a new tuple version and marks the old tuple dead; dead tuples accumulate causing table/index bloat. Tune AUTOVACUUM aggressiveness (e.g. lower autovacuum_vacuum_scale_factor and increase autovacuum_cost_limit) and enable HOT (Heap-Only Tuples) updates.",
                "The database has run out of WAL space; disable WAL logging with UNLOGGED tables.",
                "Execute DROP TABLE products and recreate it nightly via cron."
            },
            1,
            "### Architectural Breakdown\nPostgreSQL uses MVCC, meaning `UPDATE` writes a new row version with current `xmin` and marks the old row's `xmax`. When update frequency exceeds `AUTOVACUUM` throughput, dead tuples accumulate, causing severe table and index bloat. Tuning AUTOVACUUM cost limits and ensuring HOT (Heap-Only Tuples) updates (no indexed columns changed) prevents index pointer explosion and reclaims dead tuple slots inline.\n\n- **Option A Pitfall:** PostgreSQL B-Tree indices handle integers with high efficiency; HASH indices do not support range scans and do not solve tuple bloat.\n- **Option C Pitfall:** `UNLOGGED` tables lose durability upon server crashes and do not prevent MVCC bloat.\n- **Option D Pitfall:** Dropping production tables causes severe data loss and downtime.",
            new() { "Postgres writes a new row version with current xmin and updates old row xmax", "Dead tuples accumulate leading to table bloat and performance degradation", "AUTOVACUUM freezes transaction IDs and reclaims dead tuple storage" },
            "In PostgreSQL, an `UPDATE` does not overwrite existing disk blocks in-place; instead, it inserts a new tuple with the updating transaction ID as `xmin` and marks the previous tuple's `xmax`...",
            "Write-Ahead Logging (WAL) ensures that data modifications are written to sequential transaction logs before data pages are flushed to disk...",
            "PostgreSQL MVCC treats updates as insert+mark dead. AUTOVACUUM is critical to prevent bloat and transaction ID wraparound.",
            new() { "MVCC uses xmin and xmax for visibility", "Dead tuples cause table and index bloat", "WAL guarantees ACID durability with sequential disk I/O" },
            new() { Question = "What does AUTOVACUUM do with dead tuples in PostgreSQL?", Options = new() { "Physically shrinks the database file immediately on disk", "Marks dead tuple space as reusable for future inserts/updates on the same page", "Deletes the indexes", "Converts them to JSONB" }, AnswerIndex = 1, Explanation = "Standard VACUUM marks page space as reusable without returning disk space to the OS." }
        );

        // --- Day 24: Transactional Outbox Pattern ---
        AddDay(
            24, "transactional-outbox-pattern", "Transactional Outbox Pattern & Reliable Messaging",
            Category.SystemDesign, Difficulty.Senior,
            "Solving the dual-write problem between database transactions and message brokers without 2PC.",
            "### Dual-Write Problem\nUpdating a database and publishing a message in separate calls is vulnerable to partial failure...",
            "When an Order is placed, you must deduct inventory in PostgreSQL and publish an OrderCreated event to Apache Kafka. Which design avoids the dual-write inconsistency where the database commits but the Kafka message fails to publish?",
            new()
            {
                "Publish to Kafka first; if Kafka succeeds, call dbContext.SaveChangesAsync().",
                "Implement the Transactional Outbox Pattern: Insert the Order entity and an OutboxMessage record in the same local PostgreSQL ACID transaction, then use an asynchronous relay (or CDC Debezium) to publish to Kafka with at-least-once delivery.",
                "Wrap both PostgreSQL and Kafka in a Distributed Transaction using 2-Phase Commit (2PC) over WS-AtomicTransaction.",
                "Execute the Kafka publish inside a Task.Run background thread without awaiting it."
            },
            1,
            "### Architectural Breakdown\nThe Transactional Outbox Pattern solves the dual-write problem by saving the business entity and event message inside the same local relational database transaction. Because relational databases guarantee atomic commit/rollback (ACID), either both are saved or neither is. An asynchronous worker or CDC log reader (Debezium) polls the outbox table and delivers events reliably to Kafka with at-least-once semantics.\n\n- **Option A Pitfall:** If database commit fails after publishing to Kafka, duplicate external event processing occurs without the order existing in the database.\n- **Option C Pitfall:** Kafka does not natively support XA/2PC distributed transactions across relational databases; 2PC also introduces high latency and lock holding.\n- **Option D Pitfall:** Unawaited fire-and-forget tasks are silently lost on process restarts or unhandled network exceptions.",
            new() { "Dual-write vulnerability: process crash after commit but before publish drops the message", "Outbox table saves event inside same ACID database transaction", "Background worker or CDC (Debezium) polls outbox and delivers with at-least-once guarantee" },
            "Publishing directly after `SaveChangesAsync()` creates a non-atomic boundary. If the application crashes, network fails, or broker is unreachable right after the DB commit, the event is permanently lost...",
            "The Transactional Outbox pattern stores event records in an `OutboxMessages` table within the same ACID transaction as the business entity...",
            "Transactional Outbox guarantees reliable event dispatch without distributed two-phase commits.",
            new() { "Never dual-write across uncoordinated resources", "Outbox table commits with business data", "Relay workers provide at-least-once event delivery" },
            new() { Question = "What guarantees that an Outbox message is recorded alongside the business entity update?", Options = new() { "RabbitMQ acknowledgments", "The same ACID database transaction and connection", "Redis distributed lock", "gRPC retry policy" }, AnswerIndex = 1, Explanation = "Both entity changes and Outbox message records are committed in a single local database transaction." }
        );

        return list;
    }
}
