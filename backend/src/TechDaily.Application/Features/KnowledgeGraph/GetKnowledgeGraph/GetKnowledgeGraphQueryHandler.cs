using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.KnowledgeGraph.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Features.KnowledgeGraph.GetKnowledgeGraph;

public class GetKnowledgeGraphQueryHandler : IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetKnowledgeGraphQueryHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static readonly (string Id, string Label, Category Category, string Subtitle, string Summary)[] CanonicalPillars =
    [
        ("pillar-FrontendWeb", "Frontend & Web", Category.FrontendWeb, "Vue 3, Nuxt 4, Browser Pipeline, Web Vitals", "Modern web architecture, client-side rendering, performance optimization, and browser lifecycle mechanics."),
        ("pillar-BackendRuntime", "Backend & Runtime", Category.BackendRuntime, "Runtimes, Concurrency, Memory & Async I/O", "High-performance runtime internals, concurrency primitives, asynchronous execution, and service architectures."),
        ("pillar-DatabaseStorage", "Database & Storage", Category.DatabaseStorage, "Storage Engines, Indexing & Persistence", "Relational persistence, storage engine mechanics, index strategies, transaction isolation, and caching."),
        ("pillar-SystemDesign", "Distributed Systems", Category.SystemDesign, "Event-Driven, Consistency & Fault Tolerance", "Scalable distributed patterns, transactional outbox, idempotency, event sourcing, and resilience engineering."),
        ("pillar-EngineeringCraft", "Engineering Craft", Category.EngineeringCraft, "Architecture, Clean Code, Testing", "Foundational engineering practices, clean architecture, automated testing, and software design principles."),
        ("pillar-MentalModels", "Mental Models & Decisions", Category.MentalModels, "First Principles, Cognitive Biases, Inversion", "Foundational cognitive frameworks, multi-disciplinary mental models, and structured decision-making mechanisms."),
        ("pillar-HabitsProductivity", "Habits & Deep Work", Category.HabitsProductivity, "Habit Loops, Focus Rituals, Attention Management", "Deliberate practice systems, environmental cue design, ultradian focus blocks, and sustainable daily pace.")
    ];

    public async Task<Result<KnowledgeGraphResponse>> ExecuteAsync(
        GetKnowledgeGraphQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result<KnowledgeGraphResponse>.Failure(Error.Unauthorized);
        }

        // 1. Load user's own books (only published, non-deleted, created by this user) with their Chunks
        var books = await _dbContext.DocumentBooks
            .AsNoTracking()
            .Include(b => b.Chunks.Where(c => !c.IsDeleted))
            .Where(b => b.CreatedByUserId == request.UserId && b.IsPublished && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        // 2. Load user's cards
        var cards = await _dbContext.SpacedRepetitionCards
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        // 3. Load user's highlights
        var highlights = await _dbContext.UserHighlights
            .AsNoTracking()
            .Include(h => h.DocumentChunk)
            .Where(h => h.UserId == request.UserId && !h.IsDeleted)
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var bookMap = books.ToDictionary(b => b.Id);
        var allUserChunks = books.SelectMany(b => b.Chunks).ToDictionary(c => c.Id);
        var highlightMap = highlights.ToDictionary(h => h.Id);

        // 4. A chunk SHALL be emitted if and only if it belongs to a surviving user book
        //    AND is referenced by at least one of the user's own cards or highlights.
        var referencedChunkIds = new HashSet<Guid>();
        foreach (var card in cards)
        {
            if (card.SourceDocumentChunkId.HasValue && allUserChunks.ContainsKey(card.SourceDocumentChunkId.Value))
            {
                referencedChunkIds.Add(card.SourceDocumentChunkId.Value);
            }
        }
        foreach (var highlight in highlights)
        {
            if (allUserChunks.ContainsKey(highlight.DocumentChunkId))
            {
                referencedChunkIds.Add(highlight.DocumentChunkId);
            }
        }

        var emittedChunks = allUserChunks.Values
            .Where(c => referencedChunkIds.Contains(c.Id))
            .ToList();
        var chunkMap = emittedChunks.ToDictionary(c => c.Id);

        var nodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();
        var referencedPillars = new HashSet<Category>();

        // Resolves the effective category of a card from its chunk/highlight/book, else EngineeringCraft
        Category ResolveCardCategory(SpacedRepetitionCard card)
        {
            if (card.SourceDocumentChunkId.HasValue && allUserChunks.TryGetValue(card.SourceDocumentChunkId.Value, out var c))
            {
                if (bookMap.TryGetValue(c.DocumentBookId, out var b))
                    return b.Category;
            }
            if (card.SourceHighlightId.HasValue && highlightMap.TryGetValue(card.SourceHighlightId.Value, out var h))
            {
                if (h.DocumentChunk != null && bookMap.TryGetValue(h.DocumentChunk.DocumentBookId, out var b))
                    return b.Category;
            }
            return Category.EngineeringCraft;
        }

        // 1. Book Nodes
        foreach (var book in books)
        {
            nodes.Add(new GraphNodeDto(
                Id: book.Id.ToString(),
                Label: book.Title,
                Type: GraphNodeType.Book,
                Category: book.Category.ToString(),
                Subtitle: book.AuthorOrSourceUrl,
                DayOrder: null,
                Summary: null,
                Difficulty: null,
                Status: book.Status.ToString(),
                IntervalDays: null,
                EaseFactor: null,
                RepetitionCount: null,
                DocumentChunkId: null,
                BookId: book.Id.ToString(),
                Tags: null,
                CreatedAt: book.CreatedAt.UtcDateTime
            ));
        }

        // 2. Chunk Nodes
        foreach (var chunk in emittedChunks)
        {
            var parentBook = bookMap[chunk.DocumentBookId];
            nodes.Add(new GraphNodeDto(
                Id: chunk.Id.ToString(),
                Label: chunk.ChapterTitle,
                Type: GraphNodeType.Chunk,
                Category: parentBook.Category.ToString(),
                Subtitle: $"Chapter {chunk.ChunkOrder}",
                DayOrder: chunk.ChunkOrder,
                Summary: chunk.SummaryMarkdown,
                Difficulty: null,
                Status: null,
                IntervalDays: null,
                EaseFactor: null,
                RepetitionCount: null,
                DocumentChunkId: chunk.Id.ToString(),
                BookId: chunk.DocumentBookId.ToString(),
                Tags: null,
                CreatedAt: chunk.CreatedAt.UtcDateTime
            ));
        }

        // 3. Card Nodes
        int masteredCardsCount = 0;
        foreach (var card in cards)
        {
            string status;
            if (card.EaseFactor >= 2.2m && card.IntervalDays >= 21)
            {
                status = MasteryStatus.Mastered;
                masteredCardsCount++;
            }
            else if (card.IntervalDays >= 6)
            {
                status = MasteryStatus.Reviewing;
            }
            else
            {
                status = MasteryStatus.Learning;
            }

            var category = ResolveCardCategory(card).ToString();
            string label = !string.IsNullOrWhiteSpace(card.FrontMarkdown)
                ? (card.FrontMarkdown.Length > 80 ? card.FrontMarkdown[..80].Trim() + "..." : card.FrontMarkdown.Trim())
                : "Flashcard";

            nodes.Add(new GraphNodeDto(
                Id: card.Id.ToString(),
                Label: label,
                Type: GraphNodeType.Card,
                Category: category,
                Subtitle: $"Rep: {card.RepetitionCount} | Int: {card.IntervalDays}d",
                DayOrder: null,
                Summary: card.BackMarkdown,
                Difficulty: null,
                Status: status,
                IntervalDays: card.IntervalDays,
                EaseFactor: card.EaseFactor,
                RepetitionCount: card.RepetitionCount,
                DocumentChunkId: card.SourceDocumentChunkId?.ToString() ?? card.SourceHighlightId?.ToString(),
                BookId: null,
                Tags: null,
                CreatedAt: card.CreatedAt.UtcDateTime
            ));
        }

        // 4. Highlight Nodes
        foreach (var highlight in highlights)
        {
            var rawText = highlight.SelectedText?.Trim() ?? string.Empty;
            var label = rawText.Length > 80 ? rawText[..80].Trim() + "..." : rawText;

            string category = highlight.DocumentChunk != null && bookMap.TryGetValue(highlight.DocumentChunk.DocumentBookId, out var linkedBook)
                ? linkedBook.Category.ToString()
                : Category.EngineeringCraft.ToString();

            nodes.Add(new GraphNodeDto(
                Id: highlight.Id.ToString(),
                Label: label,
                Type: GraphNodeType.Highlight,
                Category: category,
                Subtitle: highlight.Note,
                DayOrder: null,
                Summary: highlight.Note,
                Difficulty: null,
                Status: null,
                IntervalDays: null,
                EaseFactor: null,
                RepetitionCount: null,
                DocumentChunkId: highlight.DocumentChunkId.ToString(),
                BookId: highlight.DocumentChunk?.DocumentBookId.ToString(),
                Tags: highlight.Tags,
                CreatedAt: highlight.CreatedAt.UtcDateTime
            ));
        }

        // Edge derivation:
        // 1. BookToPillar: each user book -> the pillar hub for its category
        foreach (var book in books)
        {
            referencedPillars.Add(book.Category);
            edges.Add(new GraphEdgeDto(
                Id: $"edge-book-{book.Id}-pillar-{book.Category}",
                Source: book.Id.ToString(),
                Target: $"pillar-{book.Category}",
                RelationType: GraphRelationType.BookToPillar,
                Label: "Library",
                Weight: 2
            ));
        }

        // 2. ChunkToBook: each emitted chunk -> its parent book
        foreach (var chunk in emittedChunks)
        {
            edges.Add(new GraphEdgeDto(
                Id: $"edge-chunk-{chunk.Id}-book-{chunk.DocumentBookId}",
                Source: chunk.Id.ToString(),
                Target: chunk.DocumentBookId.ToString(),
                RelationType: GraphRelationType.ChunkToBook,
                Label: "Chapter",
                Weight: 2
            ));
        }

        // 3. Card edges:
        foreach (var card in cards)
        {
            if (card.SourceDocumentChunkId.HasValue && chunkMap.ContainsKey(card.SourceDocumentChunkId.Value))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-card-{card.Id}-chunk-{card.SourceDocumentChunkId.Value}",
                    Source: card.Id.ToString(),
                    Target: card.SourceDocumentChunkId.Value.ToString(),
                    RelationType: GraphRelationType.CardToChunk,
                    Label: "Recall",
                    Weight: 1
                ));
            }
            else if (card.SourceHighlightId.HasValue && highlightMap.ContainsKey(card.SourceHighlightId.Value))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-card-{card.Id}-highlight-{card.SourceHighlightId.Value}",
                    Source: card.Id.ToString(),
                    Target: card.SourceHighlightId.Value.ToString(),
                    RelationType: GraphRelationType.CardToHighlight,
                    Label: "Highlight",
                    Weight: 1
                ));
            }
            else
            {
                var targetCategory = ResolveCardCategory(card);
                referencedPillars.Add(targetCategory);
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-card-{card.Id}-pillar-{targetCategory}",
                    Source: card.Id.ToString(),
                    Target: $"pillar-{targetCategory}",
                    RelationType: GraphRelationType.CardToPillar,
                    Label: "Review",
                    Weight: 1
                ));
            }
        }

        // 4. Highlight edges:
        foreach (var highlight in highlights)
        {
            // HighlightToChunk: when chunk is emitted
            if (chunkMap.ContainsKey(highlight.DocumentChunkId))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-highlight-{highlight.Id}-chunk-{highlight.DocumentChunkId}",
                    Source: highlight.Id.ToString(),
                    Target: highlight.DocumentChunkId.ToString(),
                    RelationType: GraphRelationType.HighlightToChunk,
                    Label: "Highlight",
                    Weight: 1
                ));
            }

            // HighlightToBook: when parent book is present
            if (highlight.DocumentChunk != null && bookMap.ContainsKey(highlight.DocumentChunk.DocumentBookId))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-highlight-{highlight.Id}-book-{highlight.DocumentChunk.DocumentBookId}",
                    Source: highlight.Id.ToString(),
                    Target: highlight.DocumentChunk.DocumentBookId.ToString(),
                    RelationType: GraphRelationType.HighlightToBook,
                    Label: "Excerpt",
                    Weight: 1
                ));
            }
        }

        // 5. SharedTag: pairwise between highlights sharing common normalized tags
        for (int i = 0; i < highlights.Count; i++)
        {
            var h1 = highlights[i];
            if (h1.Tags == null || h1.Tags.Count == 0)
                continue;

            var h1Tags = h1.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .ToHashSet();

            for (int j = i + 1; j < highlights.Count; j++)
            {
                var h2 = highlights[j];
                if (h2.Tags == null || h2.Tags.Count == 0)
                    continue;

                var commonTags = h2.Tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => h1Tags.Contains(t))
                    .Distinct()
                    .ToList();

                if (commonTags.Count > 0)
                {
                    var (sourceId, targetId) = h1.Id.CompareTo(h2.Id) < 0
                        ? (h1.Id, h2.Id)
                        : (h2.Id, h1.Id);

                    edges.Add(new GraphEdgeDto(
                        Id: $"edge-sharedtag-{sourceId}-{targetId}",
                        Source: sourceId.ToString(),
                        Target: targetId.ToString(),
                        RelationType: GraphRelationType.SharedTag,
                        Label: string.Join(", ", commonTags),
                        Weight: commonTags.Count
                    ));
                }
            }
        }

        // Pillar Hub Nodes: emit only pillars that at least one edge targets (0..7 hubs)
        var pillarNodes = new List<GraphNodeDto>();
        foreach (var (id, label, cat, subtitle, summary) in CanonicalPillars)
        {
            if (!referencedPillars.Contains(cat))
                continue;

            pillarNodes.Add(new GraphNodeDto(
                Id: id,
                Label: label,
                Type: GraphNodeType.Pillar,
                Category: cat.ToString(),
                Subtitle: subtitle,
                DayOrder: null,
                Summary: summary,
                Difficulty: null,
                Status: null,
                IntervalDays: null,
                EaseFactor: null,
                RepetitionCount: null,
                DocumentChunkId: null,
                BookId: null,
                Tags: null,
                CreatedAt: null
            ));
        }
        nodes.InsertRange(0, pillarNodes);

        // Stats
        var nodeTypeCounts = new Dictionary<string, int>
        {
            [GraphNodeType.Pillar] = pillarNodes.Count,
            [GraphNodeType.Topic] = 0,
            [GraphNodeType.Book] = books.Count,
            [GraphNodeType.Chunk] = emittedChunks.Count,
            [GraphNodeType.Card] = cards.Count,
            [GraphNodeType.Highlight] = highlights.Count
        };

        var pillarCounts = CanonicalPillars
            .ToDictionary(p => p.Category.ToString(), _ => 0);

        foreach (var node in nodes)
        {
            if (!string.IsNullOrEmpty(node.Category) && pillarCounts.ContainsKey(node.Category))
            {
                pillarCounts[node.Category]++;
            }
        }

        var stats = new GraphStatsDto(
            TotalNodes: nodes.Count,
            TotalEdges: edges.Count,
            NodeTypeCounts: nodeTypeCounts,
            PillarCounts: pillarCounts,
            MasteredCardsCount: masteredCardsCount
        );

        return Result<KnowledgeGraphResponse>.Success(new KnowledgeGraphResponse(nodes, edges, stats));
    }
}
