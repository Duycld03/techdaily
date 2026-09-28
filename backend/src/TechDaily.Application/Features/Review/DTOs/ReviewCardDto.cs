using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.DTOs;

public class ReviewCardDto
{
    public Guid Id { get; set; }
    public CardSourceType SourceType { get; set; } = CardSourceType.DocumentChunk;
    public string? FrontMarkdown { get; set; }
    public string? BackMarkdown { get; set; }
    public Guid? SourceHighlightId { get; set; }
    public Guid? SourceQuizQuestionId { get; set; }
    public Guid? SourceDocumentChunkId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public Category Category { get; set; }
    public Difficulty Difficulty { get; set; }
    public string TopicSummary { get; set; } = string.Empty;
    public string TopicDeepDiveMarkdown { get; set; } = string.Empty;
    public int RepetitionCount { get; set; }
    public decimal EaseFactor { get; set; }
    public int IntervalDays { get; set; }
    public DateOnly NextReviewDate { get; set; }
    public CardStatus Status { get; set; }

    public static ReviewCardDto FromEntity(Domain.Entities.SpacedRepetitionCard card)
    {
        return new ReviewCardDto
        {
            Id = card.Id,
            SourceType = card.SourceType,
            SourceHighlightId = card.SourceHighlightId,
            SourceQuizQuestionId = card.SourceQuizQuestionId,
            SourceDocumentChunkId = card.SourceDocumentChunkId,
            FrontMarkdown = card.FrontMarkdown ?? string.Empty,
            BackMarkdown = card.BackMarkdown ?? string.Empty,
            TopicTitle = card.FrontMarkdown ?? string.Empty,
            Category = Category.FrontendWeb,
            Difficulty = Difficulty.Senior,
            TopicSummary = card.BackMarkdown ?? string.Empty,
            TopicDeepDiveMarkdown = card.BackMarkdown ?? string.Empty,
            RepetitionCount = card.RepetitionCount,
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            NextReviewDate = card.NextReviewDate,
            Status = card.Status
        };
    }
}
