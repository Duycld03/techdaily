using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Infrastructure.Persistence.Seeders;

public static class CurriculumSeeder
{
    public static async Task SeedAsync(TechDailyDbContext context)
    {
        if (await context.Topics.AnyAsync())
        {
            return; // Already seeded
        }

        var masterBook = new DocumentBook
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
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
            "When does using reactive() lead to lost reactivity upon destructuring? Why does shallowRef() yield massive performance benefits over ref() for large lists (e.g. 5,000 items)?",
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
            "What causes a Hydration Mismatch? Compare TTFB vs TTI between traditional SSR and Client-Side Rendering.",
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
            "How does System.IO.Pipelines solve the buffer management problem and eliminate Gen 2 GC pressure during high-concurrency HTTP streaming in ASP.NET Core?",
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
            "Why does ValueTask<T> provide significant performance gains for synchronously completing hot paths, and why must you never await a ValueTask<T> multiple times?",
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
            "Why is LOH fragmentation dangerous? How do you leverage ArrayPool<T>.Shared to avoid Gen 2 collections in high-throughput APIs?",
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
            "Why is Span<T> declared as a ref struct and prohibited from being stored on the Managed Heap (or inside async methods)?",
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
            "Why does an UPDATE in PostgreSQL actually execute an INSERT of a new row and flag the old row as dead? What happens if AUTOVACUUM cannot keep up?",
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
            "Why is publishing a message to RabbitMQ immediately after SaveChangesAsync() an unsafe anti-pattern in distributed financial systems?",
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
