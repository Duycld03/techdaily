using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.DTOs;

public class ReviewCardDto
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
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
}
