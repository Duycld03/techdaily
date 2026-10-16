using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.InterviewQuiz.GenerateQuiz;

public class GenerateQuizHandler(
    IDeepPaceDbContext dbContext,
    IQuizGeneratorService quizGenerator,
    IValidator<GenerateQuizRequest> validator,
    ICategoryAssignmentGuard categoryGuard) : IUseCase<GenerateQuizRequest, GenerateQuizResponse>
{
    public async Task<Result<GenerateQuizResponse>> ExecuteAsync(
        GenerateQuizRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return new Error("Error.Validation", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedTopic = NormalizeTopic(request.Topic);
        Guid categoryId;
        if (request.BookId.HasValue)
        {
            var book = await dbContext.DocumentBooks.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted && b.CreatedByUserId == request.UserId, cancellationToken);
            if (book == null) return Error.Custom("LIBRARY_FORBIDDEN", "Access to this book is not permitted.");
            if (request.CategoryId.HasValue && request.CategoryId != book.CategoryId)
                return Error.Custom("Category.ValidationFailed", "The selected category must match the source book.");
            categoryId = book.CategoryId;
            if (request.IsGrounded && !normalizedTopic.Contains(book.Title, StringComparison.OrdinalIgnoreCase))
                normalizedTopic = $"{book.Title}: {normalizedTopic}";
        }
        else
        {
            if (!request.CategoryId.HasValue || request.CategoryId == Guid.Empty)
                return Error.Custom("Category.ValidationFailed", "CategoryId is required for standalone generation.");
            categoryId = request.CategoryId.Value;
            var categoryValidation = await categoryGuard.ValidateAsync(categoryId, cancellationToken);
            if (!categoryValidation.IsSuccess) return categoryValidation.Error;
        }

        var originalTopic = request.Topic.Trim().ToLowerInvariant();
        var matchingTopic = normalizedTopic.ToLowerInvariant();
        var matching = dbContext.QuizQuestions.AsNoTracking()
            .Where(q => !q.IsDeleted && q.CategoryId == categoryId && q.Level == request.Level &&
                (q.CreatedByUserId == null || q.CreatedByUserId == request.UserId) &&
                (q.Topic.ToLower() == matchingTopic || q.Topic.ToLower() == originalTopic));
        var progresses = await dbContext.UserQuizProgresses.AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .ToDictionaryAsync(p => p.QuestionId, cancellationToken);
        var attemptedIds = progresses.Keys.ToList();
        var finalQuestions = await matching.Where(q => !attemptedIds.Contains(q.Id))
            .OrderByDescending(q => q.CreatedAt).Take(request.Count).ToListAsync(cancellationToken);
        var generatedQuestions = new List<QuizQuestion>();
        Error? generationError = null;

        if (finalQuestions.Count < request.Count)
        {
            var existingTitles = await dbContext.QuizQuestions.AsNoTracking()
                .Where(q => !q.IsDeleted && q.CategoryId == categoryId &&
                    (q.CreatedByUserId == null || q.CreatedByUserId == request.UserId))
                .OrderByDescending(q => q.CreatedAt).Select(q => q.QuestionText)
                .Take(50).ToListAsync(cancellationToken);
            var generated = await quizGenerator.GenerateQuestionsAsync(normalizedTopic, categoryId,
                request.Level, request.Count - finalQuestions.Count, existingTitles, request.Locale, cancellationToken);
            if (!generated.IsSuccess)
                generationError = generated.Error;
            else
            {
                var unique = generated.Value
                    .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText))
                    .DistinctBy(q => q.QuestionText.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Take(request.Count - finalQuestions.Count).ToList();
                var texts = unique.Select(q => q.QuestionText.Trim()).ToList();
                var existing = await matching.Where(q => texts.Contains(q.QuestionText.Trim())).ToListAsync(cancellationToken);
                foreach (var question in unique)
                {
                    var duplicate = existing.FirstOrDefault(q => string.Equals(q.QuestionText.Trim(), question.QuestionText.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (duplicate != null)
                    {
                        if (!finalQuestions.Any(q => q.Id == duplicate.Id) &&
                            (!progresses.TryGetValue(duplicate.Id, out var progress) || !progress.IsMastered))
                            finalQuestions.Add(duplicate);
                        continue;
                    }
                    question.CategoryId = categoryId;
                    question.Topic = normalizedTopic;
                    question.Level = request.Level;
                    question.CreatedByUserId = request.UserId;
                    generatedQuestions.Add(question);
                    finalQuestions.Add(question);
                }
            }
        }

        if (finalQuestions.Count < request.Count)
        {
            var currentIds = finalQuestions.Select(q => q.Id).ToList();
            var masteredIds = progresses.Where(p => p.Value.IsMastered).Select(p => p.Key).ToList();
            var recovery = await matching.Where(q => !currentIds.Contains(q.Id) && !masteredIds.Contains(q.Id))
                .OrderByDescending(q => q.CreatedAt).Take(request.Count - finalQuestions.Count).ToListAsync(cancellationToken);
            finalQuestions.AddRange(recovery);
        }
        if (finalQuestions.Count == 0)
            return generationError ?? Error.Custom("AiService.GenerationFailed", "No suitable questions could be generated.");

        async Task<Result<GenerateQuizResponse>> Persist(DocumentCategory category)
        {
            foreach (var question in generatedQuestions)
                question.Category = category;
            if (generatedQuestions.Count > 0)
                await dbContext.QuizQuestions.AddRangeAsync(generatedQuestions, cancellationToken);
            var dtos = finalQuestions.Take(request.Count).Select(q =>
            {
                progresses.TryGetValue(q.Id, out var progress);
                return new QuizQuestionDto
                {
                    Id = q.Id, Topic = q.Topic, CategoryId = category.Id, Category = category.ToDescriptor(),
                    Level = q.Level, QuestionText = q.QuestionText, Options = q.Options,
                    CorrectOptionIndex = q.CorrectOptionIndex, ExplanationMarkdown = q.ExplanationMarkdown,
                    Tags = q.Tags, IsMastered = progress?.IsMastered ?? false,
                    LastSelectedOptionIndex = null, IsLastAnswerCorrect = null,
                    CorrectCount = progress?.CorrectCount ?? 0, IncorrectCount = progress?.IncorrectCount ?? 0
                };
            }).ToList();
            return new GenerateQuizResponse(dtos, normalizedTopic, request.Level, dtos.Count);
        }

        return request.BookId.HasValue
            ? await categoryGuard.ExecuteForBookAsync<GenerateQuizResponse>(request.BookId.Value, request.UserId,
                categoryId, (_, category) => Persist(category), cancellationToken)
            : await categoryGuard.ExecuteAsync<GenerateQuizResponse>(categoryId, Persist, cancellationToken);
    }

    public static string NormalizeTopic(string rawTopic)
    {
        if (string.IsNullOrWhiteSpace(rawTopic)) return string.Empty;
        var trimmed = rawTopic.Trim();
        var cleaned = System.Text.RegularExpressions.Regex.Replace(trimmed,
            @"^(về|ve|about|chủ đề|chu de)\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? trimmed.ToLowerInvariant() : cleaned.ToLowerInvariant();
    }
}
