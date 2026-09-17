using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.KnowledgeGraph.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.KnowledgeGraph.GetKnowledgeGraph;

public class GetKnowledgeGraphQueryHandler : IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetKnowledgeGraphQueryHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
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
                CreatedAt: topic.CreatedAt.UtcDateTime,
                EmbleUrl: null
            ));
        }

        // 2. Book Nodes: type "book", category from Book.Category.ToString(), subtitle author/source
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
                CreatedAt: book.CreatedAt.UtcDateTime,
                EmbleUrl: null
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

            string category = card.TopicId.HasValue && topicMap.TryGetValue(card.TopicId.Value, out var linkedTopic)
                ? linkedTopic.Category.ToString()
                : Category.EngineeringCraft.ToString();

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
                CreatedAt: card.CreatedAt.UtcDateTime,
                EmbleUrl: null
            ));
        }

        // 4. Highlight Nodes: type "highlight", label truncated selected text (max 80 chars), note, tags
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
                CreatedAt: highlight.CreatedAt.UtcDateTime,
                EmbleUrl: null
            ));
        }

        // Edge Derivation:
        // 1. CardToTopic: card.TopicId -> topic.Id (relationType = "CardToTopic")
        foreach (var card in cards)
        {
            if (card.TopicId.HasValue && topicMap.ContainsKey(card.TopicId.Value))
            {
                edges.Add(new GraphEdgeDto(
                    Id: $"edge-card-{card.Id}-{card.TopicId.Value}",
                    Source: card.Id.ToString(),
                    Target: card.TopicId.Value.ToString(),
                    RelationType: GraphRelationType.CardToTopic,
                    Label: "Topic",
                    Weight: 1
                ));
            }
        }

        // 2. BookToTopic: book.Category == topic.Category (connect book to related topics in same category)
        foreach (var book in books)
        {
            foreach (var topic in topics)
            {
                if (book.Category == topic.Category)
                {
                    edges.Add(new GraphEdgeDto(
                        Id: $"edge-book-{book.Id}-topic-{topic.Id}",
                        Source: book.Id.ToString(),
                        Target: topic.Id.ToString(),
                        RelationType: GraphRelationType.BookToTopic,
                        Label: book.Category.ToString(),
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
            [GraphNodeType.Topic] = topics.Count,
            [GraphNodeType.Book] = books.Count,
            [GraphNodeType.Card] = cards.Count,
            [GraphNodeType.Highlight] = highlights.Count
        };

        var pillarCounts = Enum.GetValues<Category>()
            .ToDictionary(c => c.ToString(), _ => 0);

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
