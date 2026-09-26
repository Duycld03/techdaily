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
        ("pillar-EngineeringCraft", "Engineering Craft", Category.EngineeringCraft, "Architecture, Clean Code, Testing", "Foundational engineering practices, clean architecture, automated testing, and software design principles.")
    ];

    private static Category GetEffectiveBookCategory(DocumentBook book)
    {
        if (book.Category == Category.FrontendWeb)
        {
            var titleOrSlug = $"{book.Title} {book.Slug}".ToLowerInvariant();
            if (titleOrSlug.Contains("aspnet") ||
                titleOrSlug.Contains("dotnet") ||
                titleOrSlug.Contains("csharp") ||
                titleOrSlug.Contains("c#") ||
                titleOrSlug.Contains("golang") ||
                titleOrSlug.Contains("rust") ||
                titleOrSlug.Contains("java") ||
                titleOrSlug.Contains("python"))
            {
                return Category.BackendRuntime;
            }
        }

        return book.Category;
    }

    private static bool IsMasterCurriculumBook(DocumentBook book)
    {
        return string.Equals(book.Slug, "30-day-senior-curriculum", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(book.Title) && (
                book.Title.Contains("30-Day", StringComparison.OrdinalIgnoreCase) ||
                book.Title.Contains("Curriculum", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesTopic(string? text, string topicTitle, string topicSlug)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var slugSpaced = topicSlug.Replace("-", " ");
        return text.Contains(topicTitle, StringComparison.OrdinalIgnoreCase)
            || text.Contains(topicSlug, StringComparison.OrdinalIgnoreCase)
            || text.Contains(slugSpaced, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Result<KnowledgeGraphResponse>> ExecuteAsync(
        GetKnowledgeGraphQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result<KnowledgeGraphResponse>.Failure(Error.Unauthorized);
        }

        var topics = await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(t => t.DayOrder)
            .ToListAsync(cancellationToken);

        var books = await _dbContext.DocumentBooks
            .AsNoTracking()
            .Include(b => b.Chunks)
            .Where(b => b.IsPublished && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var cards = await _dbContext.SpacedRepetitionCards
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var highlights = await _dbContext.UserHighlights
            .AsNoTracking()
            .Include(h => h.DocumentChunk)
            .Where(h => h.UserId == request.UserId)
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var nodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        var topicMap = topics.ToDictionary(t => t.Id);
        var bookMap = books.ToDictionary(b => b.Id);
        var highlightMap = highlights.ToDictionary(h => h.Id);
        // 0. Canonical Pillar Hub Nodes: type "pillar"
        foreach (var (id, label, cat, subtitle, summary) in CanonicalPillars)
        {
            nodes.Add(new GraphNodeDto(
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

        // 1. Topic Nodes: type "topic", category from Topic.Category.ToString(), dayOrder, summary
        foreach (var topic in topics)
        {
            nodes.Add(new GraphNodeDto(
                Id: topic.Id.ToString(),
                Label: topic.Title,
                Type: GraphNodeType.Topic,
                Category: topic.Category.ToString(),
                Subtitle: $"Day {topic.DayOrder}",
                DayOrder: topic.DayOrder,
                Summary: topic.Summary,
                Difficulty: topic.Difficulty.ToString(),
                Status: null,
                IntervalDays: null,
                EaseFactor: null,
                RepetitionCount: null,
                DocumentChunkId: null,
                BookId: null,
                Tags: null,
                CreatedAt: topic.CreatedAt.UtcDateTime
            ));
        }

        // 2. Book Nodes: type "book", category from effective book category, subtitle author/source
        foreach (var book in books)
        {
            var effectiveCategory = GetEffectiveBookCategory(book);
            nodes.Add(new GraphNodeDto(
                Id: book.Id.ToString(),
                Label: book.Title,
                Type: GraphNodeType.Book,
                Category: effectiveCategory.ToString(),
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

        // 3. Card Nodes: type "card", status derived from SM-2 (EaseFactor >= 2.2m && IntervalDays >= 21 -> "Mastered", IntervalDays >= 6 -> "Reviewing", else "Learning"), metrics (interval, ease, repetitions)
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

            string category;
            if (card.TopicId.HasValue && topicMap.TryGetValue(card.TopicId.Value, out var linkedTopic))
            {
                category = linkedTopic.Category.ToString();
            }
            else if (card.SourceHighlightId.HasValue && highlightMap.TryGetValue(card.SourceHighlightId.Value, out var linkedHl))
            {
                category = linkedHl.DocumentChunk != null && bookMap.TryGetValue(linkedHl.DocumentChunk.DocumentBookId, out var linkedBook)
                    ? GetEffectiveBookCategory(linkedBook).ToString()
                    : Category.EngineeringCraft.ToString();
            }
            else
            {
                category = Category.EngineeringCraft.ToString();
            }

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
                DocumentChunkId: card.SourceHighlightId?.ToString(),
                BookId: card.TopicId?.ToString(),
                Tags: null,
                CreatedAt: card.CreatedAt.UtcDateTime
            ));
        }

        // 4. Highlight Nodes: type "highlight", label truncated selected text (max 80 chars), note, tags
        foreach (var highlight in highlights)
        {
            var rawText = highlight.SelectedText?.Trim() ?? string.Empty;
            var label = rawText.Length > 80 ? rawText[..80].Trim() + "..." : rawText;

            string category = highlight.DocumentChunk != null && bookMap.TryGetValue(highlight.DocumentChunk.DocumentBookId, out var linkedBook)
                ? GetEffectiveBookCategory(linkedBook).ToString()
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

        // Edge Derivation:
        // 0. TopicToPillar: topic.Id -> pillar-{topic.Category} (guarantees degree >= 1 for 100% of topics)
        foreach (var topic in topics)
        {
            edges.Add(new GraphEdgeDto(
                Id: $"edge-topic-{topic.Id}-pillar-{topic.Category}",
                Source: topic.Id.ToString(),
                Target: $"pillar-{topic.Category}",
                RelationType: GraphRelationType.TopicToPillar,
                Label: "Pillar",
                Weight: 2
            ));
        }

        // 1. BookToPillar: book.Id -> pillar-{category}
        foreach (var book in books)
        {
            var effectiveCategory = GetEffectiveBookCategory(book);
            if (IsMasterCurriculumBook(book))
            {
                foreach (var cat in new[] { Category.FrontendWeb, Category.BackendRuntime, Category.DatabaseStorage, Category.SystemDesign })
                {
                    edges.Add(new GraphEdgeDto(
                        Id: $"edge-book-{book.Id}-pillar-{cat}",
                        Source: book.Id.ToString(),
                        Target: $"pillar-{cat}",
                        RelationType: GraphRelationType.BookToPillar,
                        Label: "Curriculum",
                        Weight: 2
                    ));
                }
            }
            else
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-book-{book.Id}-pillar-{effectiveCategory}",
                    Source: book.Id.ToString(),
                    Target: $"pillar-{effectiveCategory}",
                    RelationType: GraphRelationType.BookToPillar,
                    Label: "Library",
                    Weight: 2
                ));
            }
        }

        // 2. Card Edge Derivation: CardToTopic, CardToHighlight, or CardToPillar (Guarantees degree >= 1 for 100% of cards)
        foreach (var card in cards)
        {
            if (card.TopicId.HasValue && topicMap.ContainsKey(card.TopicId.Value))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-card-{card.Id}-topic-{card.TopicId.Value}",
                    Source: card.Id.ToString(),
                    Target: card.TopicId.Value.ToString(),
                    RelationType: GraphRelationType.CardToTopic,
                    Label: "Topic",
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
                // Fallback to pillar hub (e.g. quiz mistake cards or standalone cards)
                string targetCategory = Category.EngineeringCraft.ToString();
                if (card.TopicId.HasValue && topicMap.TryGetValue(card.TopicId.Value, out var t))
                {
                    targetCategory = t.Category.ToString();
                }
                else if (card.SourceHighlightId.HasValue && highlightMap.TryGetValue(card.SourceHighlightId.Value, out var h)
                    && h.DocumentChunk != null && bookMap.TryGetValue(h.DocumentChunk.DocumentBookId, out var b))
                {
                    targetCategory = GetEffectiveBookCategory(b).ToString();
                }

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

        // 3. BookToTopic: refined matching (NO naive Cartesian product)
        foreach (var book in books)
        {
            var isMaster = IsMasterCurriculumBook(book);
            foreach (var topic in topics)
            {
                bool isMatch = isMaster
                    || MatchesTopic(book.Title, topic.Title, topic.Slug)
                    || MatchesTopic(book.Slug, topic.Title, topic.Slug);

                if (!isMatch && book.Chunks != null && book.Chunks.Count > 0)
                {
                    foreach (var chunk in book.Chunks)
                    {
                        if (MatchesTopic(chunk.ChapterTitle, topic.Title, topic.Slug)
                            || MatchesTopic(chunk.OriginalTextMarkdown, topic.Title, topic.Slug)
                            || (!string.IsNullOrWhiteSpace(chunk.ChapterTitle) && chunk.ChapterTitle.Length >= 4 && topic.Title.Contains(chunk.ChapterTitle, StringComparison.OrdinalIgnoreCase)))
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    edges.Add(new GraphEdgeDto(
                        Id: $"edge-book-{book.Id}-topic-{topic.Id}",
                        Source: book.Id.ToString(),
                        Target: topic.Id.ToString(),
                        RelationType: GraphRelationType.BookToTopic,
                        Label: topic.Category.ToString(),
                        Weight: 1
                    ));
                }
            }
        }

        // 3. HighlightToBook: highlight -> highlight.DocumentChunk.DocumentBookId (relationType = "HighlightToBook")
        foreach (var highlight in highlights)
        {
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

        // 4. HighlightToTopic: if highlight tags match topic slug or title (case-insensitive) (relationType = "HighlightToTopic")
        var addedHighlightTopicEdges = new HashSet<string>();
        foreach (var highlight in highlights)
        {
            if (highlight.Tags == null || highlight.Tags.Count == 0)
                continue;

            foreach (var tag in highlight.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                var trimmedTag = tag.Trim();
                foreach (var topic in topics)
                {
                    bool isMatch = string.Equals(trimmedTag, topic.Slug, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(trimmedTag, topic.Title, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(trimmedTag.Replace("-", " "), topic.Title, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(trimmedTag.Replace(" ", "-"), topic.Slug, StringComparison.OrdinalIgnoreCase);

                    if (isMatch)
                    {
                        var edgeKey = $"{highlight.Id}-{topic.Id}";
                        if (addedHighlightTopicEdges.Add(edgeKey))
                        {
                            edges.Add(new GraphEdgeDto(
                                Id: $"edge-highlight-{highlight.Id}-topic-{topic.Id}",
                                Source: highlight.Id.ToString(),
                                Target: topic.Id.ToString(),
                                RelationType: GraphRelationType.HighlightToTopic,
                                Label: trimmedTag,
                                Weight: 1
                            ));
                        }
                    }
                }
            }
        }

        // 5. SharedTag: pairwise between highlights sharing common normalized tags (relationType = "SharedTag")
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

        // Compute stats
        var nodeTypeCounts = new Dictionary<string, int>
        {
            [GraphNodeType.Pillar] = CanonicalPillars.Length,
            [GraphNodeType.Topic] = topics.Count,
            [GraphNodeType.Book] = books.Count,
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

        var response = new KnowledgeGraphResponse(nodes, edges, stats);
        return Result<KnowledgeGraphResponse>.Success(response);
    }
}
