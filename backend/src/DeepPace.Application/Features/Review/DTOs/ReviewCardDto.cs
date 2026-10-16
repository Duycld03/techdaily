using DeepPace.Application.Features.Admin.Categories.DTOs;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;

using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Review.DTOs;

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
    public CategoryDescriptorDto? Category { get; set; }
    public Guid? CategoryId { get; set; }
    public Difficulty? Difficulty { get; set; }
    public string TopicSummary { get; set; } = string.Empty;
    public string TopicDeepDiveMarkdown { get; set; } = string.Empty;
    public int RepetitionCount { get; set; }
    public decimal EaseFactor { get; set; }
    public int IntervalDays { get; set; }
    public DateOnly NextReviewDate { get; set; }
    public CardStatus Status { get; set; }

    public static ReviewCardDto FromEntity(Domain.Entities.SpacedRepetitionCard card)
    {
        var question = card.SourceQuizQuestion is { IsDeleted: false } sourceQuestion
            && (!sourceQuestion.CreatedByUserId.HasValue || sourceQuestion.CreatedByUserId == card.UserId)
                ? sourceQuestion : null;
        var chunk = IsOwnedSurvivingChunk(card.SourceDocumentChunk, card.UserId)
            ? card.SourceDocumentChunk
            : card.SourceHighlight is { IsDeleted: false } highlight && highlight.UserId == card.UserId
                && IsOwnedSurvivingChunk(highlight.DocumentChunk, card.UserId)
                    ? highlight.DocumentChunk : null;
        var categoryId = question?.CategoryId ?? chunk?.DocumentBook?.CategoryId;
        var category = question != null ? question.Category : chunk?.DocumentBook?.Category;
        return new ReviewCardDto
        {
            Id = card.Id,
            SourceType = card.SourceType,
            SourceHighlightId = card.SourceHighlightId,
            SourceQuizQuestionId = card.SourceQuizQuestionId,
            SourceDocumentChunkId = card.SourceDocumentChunkId,
            FrontMarkdown = card.FrontMarkdown ?? string.Empty,
            BackMarkdown = card.BackMarkdown ?? string.Empty,
            TopicTitle = question?.Topic ?? chunk?.ChapterTitle ?? card.FrontMarkdown ?? string.Empty,
            Category = category?.ToDescriptor(),
            CategoryId = categoryId,
            Difficulty = question == null ? null : question.Level switch
            {
                QuizLevel.Mastery => Domain.Enums.Difficulty.Lead,
                QuizLevel.Advanced => Domain.Enums.Difficulty.Senior,
                _ => Domain.Enums.Difficulty.Intermediate
            },
            TopicSummary = card.BackMarkdown ?? string.Empty,
            TopicDeepDiveMarkdown = card.BackMarkdown ?? string.Empty,
            RepetitionCount = card.RepetitionCount,
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            NextReviewDate = card.NextReviewDate,
            Status = card.Status
        };
    }

    private static bool IsOwnedSurvivingChunk(DocumentChunk? chunk, Guid userId)
    {
        return chunk is { IsDeleted: false, DocumentBook: { IsDeleted: false } book }
            && book.CreatedByUserId == userId;
    }
}

internal static class ReviewCardSourceQuery
{
    internal static IQueryable<SpacedRepetitionCard> WithReviewSources(this IQueryable<SpacedRepetitionCard> query)
    {
        return query
            .Include(c => c.SourceQuizQuestion).ThenInclude(q => q!.Category)
            .Include(c => c.SourceDocumentChunk).ThenInclude(c => c!.DocumentBook).ThenInclude(b => b.Category)
            .Include(c => c.SourceHighlight).ThenInclude(h => h!.DocumentChunk)
                .ThenInclude(c => c!.DocumentBook).ThenInclude(b => b.Category);
    }
}
