using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.DTOs;

public class ReviewCardDto
{
    public Guid Id { get; set; }
    public Guid? TopicId { get; set; }
    public CardSourceType SourceType { get; set; } = CardSourceType.Topic;
    public string? FrontMarkdown { get; set; }
    public string? BackMarkdown { get; set; }
    public Guid? SourceHighlightId { get; set; }
    public Guid? SourceQuizQuestionId { get; set; }
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
            TopicId = card.TopicId,
            SourceType = card.SourceType,
            SourceHighlightId = card.SourceHighlightId,
            SourceQuizQuestionId = card.SourceQuizQuestionId,
            FrontMarkdown = !string.IsNullOrWhiteSpace(card.FrontMarkdown) ? card.FrontMarkdown : (card.Topic != null ? card.Topic.Title : string.Empty),
            BackMarkdown = !string.IsNullOrWhiteSpace(card.BackMarkdown) ? card.BackMarkdown : (card.Topic != null ? card.Topic.Summary : string.Empty),
            TopicTitle = card.Topic != null ? card.Topic.Title : (card.FrontMarkdown ?? string.Empty),
            Category = card.Topic != null ? card.Topic.Category : Category.FrontendWeb,
            Difficulty = card.Topic != null ? card.Topic.Difficulty : Difficulty.Senior,
            TopicSummary = card.Topic != null ? card.Topic.Summary : (card.BackMarkdown ?? string.Empty),
            TopicDeepDiveMarkdown = card.Topic != null ? card.Topic.DeepDiveMarkdown : (card.BackMarkdown ?? string.Empty),
            RepetitionCount = card.RepetitionCount,
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            NextReviewDate = card.NextReviewDate,
            Status = card.Status
        };
    }
}
