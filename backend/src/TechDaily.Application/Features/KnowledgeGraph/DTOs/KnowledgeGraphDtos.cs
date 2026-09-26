namespace TechDaily.Application.Features.KnowledgeGraph.DTOs;

public sealed record GraphNodeDto(
    string Id,
    string Label,
    string Type,
    string Category,
    string? Subtitle = null,
    int? DayOrder = null,
    string? Summary = null,
    string? Difficulty = null,
    string? Status = null,
    int? IntervalDays = null,
    decimal? EaseFactor = null,
    int? RepetitionCount = null,
    string? DocumentChunkId = null,
    string? BookId = null,
    List<string>? Tags = null,
    DateTime? CreatedAt = null
);

public sealed record GraphEdgeDto(
    string Id,
    string Source,
    string Target,
    string RelationType,
    string? Label = null,
    int? Weight = null
);

public sealed record GraphStatsDto(
    int TotalNodes,
    int TotalEdges,
    Dictionary<string, int> NodeTypeCounts,
    Dictionary<string, int> PillarCounts,
    int MasteredCardsCount
);

public sealed record KnowledgeGraphResponse(
    List<GraphNodeDto> Nodes,
    List<GraphEdgeDto> Edges,
    GraphStatsDto Stats
);

public static class GraphNodeType
{
    public const string Pillar = "pillar";
    public const string Topic = "topic";
    public const string Book = "book";
    public const string Card = "card";
    public const string Highlight = "highlight";
}

public static class MasteryStatus
{
    public const string Learning = "Learning";
    public const string Reviewing = "Reviewing";
    public const string Mastered = "Mastered";
}

public static class GraphRelationType
{
    public const string TopicToPillar = "TopicToPillar";
    public const string BookToPillar = "BookToPillar";
    public const string CardToTopic = "CardToTopic";
    public const string CardToHighlight = "CardToHighlight";
    public const string CardToPillar = "CardToPillar";
    public const string BookToTopic = "BookToTopic";
    public const string HighlightToBook = "HighlightToBook";
    public const string HighlightToTopic = "HighlightToTopic";
    public const string SharedTag = "SharedTag";
}
