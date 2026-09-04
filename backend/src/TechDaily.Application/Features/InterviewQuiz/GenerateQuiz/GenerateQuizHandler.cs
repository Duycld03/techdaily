using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.InterviewQuiz.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.InterviewQuiz.GenerateQuiz;

public class GenerateQuizHandler : IUseCase<GenerateQuizRequest, GenerateQuizResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IQuizGeneratorService _quizGenerator;
    private readonly IValidator<GenerateQuizRequest> _validator;

    public GenerateQuizHandler(
        ITechDailyDbContext dbContext,
        IQuizGeneratorService quizGenerator,
        IValidator<GenerateQuizRequest> validator)
    {
        _dbContext = dbContext;
        _quizGenerator = quizGenerator;
        _validator = validator;
    }

    public async Task<Result<GenerateQuizResponse>> ExecuteAsync(
        GenerateQuizRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new Error("Error.Validation", errors);
        }

        var normalizedTopic = NormalizeTopic(request.Topic);
        var category = request.Category ?? InferCategoryFromTopic(normalizedTopic);

        // 1. Fetch IDs of questions already attempted by this user (to prevent repeating completed/attempted questions)
        var attemptedQuestionIds = await _dbContext.UserQuizProgresses
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Select(p => p.QuestionId)
            .ToListAsync(cancellationToken);

        // 2. Fetch existing unattempted questions in DB matching topic and level
        var candidates = await _dbContext.QuizQuestions
            .AsNoTracking()
            .Where(q => !q.IsDeleted &&
                        q.Level == request.Level &&
                        (q.Topic.ToLower() == normalizedTopic || q.Topic.ToLower() == request.Topic.Trim().ToLower()) &&
                        !attemptedQuestionIds.Contains(q.Id))
            .Take(request.Count * 2)
            .ToListAsync(cancellationToken);

        var existingUnattempted = candidates;
        if (candidates.Count > request.Count)
        {
            var rng = new Random();
            existingUnattempted = candidates.OrderBy(_ => rng.Next()).Take(request.Count).ToList();
        }

        var finalQuestions = new List<QuizQuestion>(existingUnattempted);

        // 3. If we don't have enough unattempted questions, generate the rest with Gemini
        if (finalQuestions.Count < request.Count)
        {
            var needed = request.Count - finalQuestions.Count;

            var existingTitles = await _dbContext.QuizQuestions
                .AsNoTracking()
                .Where(q => !q.IsDeleted && (q.Topic.ToLower() == normalizedTopic || q.Topic.ToLower() == request.Topic.Trim().ToLower() || q.Category == category))
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => q.QuestionText)
                .Take(50)
                .ToListAsync(cancellationToken);

            var genResult = await _quizGenerator.GenerateQuestionsAsync(
                normalizedTopic,
                category,
                request.Level,
                needed,
                existingTitles,
                request.Locale,
                cancellationToken);

            if (genResult.IsSuccess && genResult.Value != null && genResult.Value.Count > 0)
            {
                var candidateTexts = genResult.Value.Select(q => q.QuestionText.Trim()).ToList();
                var existingTexts = await _dbContext.QuizQuestions
                    .AsNoTracking()
                    .Where(q => candidateTexts.Contains(q.QuestionText.Trim()))
                    .Select(q => q.QuestionText.Trim())
                    .ToListAsync(cancellationToken);

                var toPersist = genResult.Value
                    .Where(q => !existingTexts.Contains(q.QuestionText.Trim()))
                    .ToList();

                foreach (var q in toPersist)
                {
                    q.CreatedByUserId = request.UserId;
                    q.Topic = normalizedTopic;
                }

                if (toPersist.Count > 0)
                {
                    await _dbContext.QuizQuestions.AddRangeAsync(toPersist, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                finalQuestions.AddRange(genResult.Value);
            }
        }

        // Fallback: If still under requested count (e.g. generator returned fewer), fill from other topic questions
        if (finalQuestions.Count < request.Count)
        {
            var currentIds = finalQuestions.Select(q => q.Id).ToList();
            var fallbackFill = await _dbContext.QuizQuestions
                .AsNoTracking()
                .Where(q => !q.IsDeleted &&
                            q.Level == request.Level &&
                            (q.Topic.ToLower() == normalizedTopic || q.Topic.ToLower() == request.Topic.Trim().ToLower()) &&
                            !currentIds.Contains(q.Id))
                .Take(request.Count - finalQuestions.Count)
                .ToListAsync(cancellationToken);

            finalQuestions.AddRange(fallbackFill);
        }

        // 4. Load user progress for these questions
        var questionIds = finalQuestions.Select(q => q.Id).ToList();
        var progresses = await _dbContext.UserQuizProgresses
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId && questionIds.Contains(p.QuestionId))
            .ToDictionaryAsync(p => p.QuestionId, cancellationToken);

        // 5. Map to DTOs
        var dtos = finalQuestions.Select(q =>
        {
            progresses.TryGetValue(q.Id, out var prog);
            return new QuizQuestionDto
            {
                Id = q.Id,
                Topic = q.Topic,
                Category = q.Category,
                Level = q.Level,
                QuestionText = q.QuestionText,
                Options = q.Options,
                CorrectOptionIndex = q.CorrectOptionIndex,
                ExplanationMarkdown = q.ExplanationMarkdown,
                Tags = q.Tags,
                IsMastered = prog?.IsMastered ?? false,
                LastSelectedOptionIndex = null,
                IsLastAnswerCorrect = null,
                CorrectCount = prog?.CorrectCount ?? 0,
                IncorrectCount = prog?.IncorrectCount ?? 0
            };
        }).ToList();

        return new GenerateQuizResponse(
            dtos,
            normalizedTopic,
            request.Level,
            dtos.Count
        );
    }

    public static string NormalizeTopic(string rawTopic)
    {
        if (string.IsNullOrWhiteSpace(rawTopic)) return string.Empty;
        var trimmed = rawTopic.Trim();
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            trimmed,
            @"^(về|ve|about|chủ đề|chu de)\s+",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? trimmed.ToLowerInvariant() : cleaned.ToLowerInvariant();
    }

    private static Category InferCategoryFromTopic(string topic)
    {
        var lower = topic.ToLowerInvariant();
        if (lower.Contains("postgres") || lower.Contains("sql") || lower.Contains("database") || lower.Contains("redis") || lower.Contains("mongo"))
            return Category.DatabaseStorage;
        if (lower.Contains("vue") || lower.Contains("react") || lower.Contains("frontend") || lower.Contains("css") || lower.Contains("browser") || lower.Contains("javascript") || lower.Contains("typescript"))
            return Category.FrontendWeb;
        if (lower.Contains("system") || lower.Contains("distributed") || lower.Contains("microservice") || lower.Contains("kafka") || lower.Contains("docker") || lower.Contains("kubernetes"))
            return Category.SystemDesign;
        return Category.BackendDotNet;
    }
}
