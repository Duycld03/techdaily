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

        var category = request.Category ?? InferCategoryFromTopic(request.Topic);

        // 1. Fetch IDs of questions already mastered by this user
        var masteredQuestionIds = await _dbContext.UserQuizProgresses
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId && p.IsMastered)
            .Select(p => p.QuestionId)
            .ToListAsync(cancellationToken);

        // 2. Fetch unmastered existing questions in DB matching topic and level
        var topicTrimmed = request.Topic.Trim().ToLower();
        var candidates = await _dbContext.QuizQuestions
            .AsNoTracking()
            .Where(q => !q.IsDeleted &&
                        q.Level == request.Level &&
                        q.Topic.ToLower() == topicTrimmed &&
                        !masteredQuestionIds.Contains(q.Id))
            .Take(request.Count * 2)
            .ToListAsync(cancellationToken);

        var existingUnmastered = candidates;
        if (candidates.Count > request.Count)
        {
            var rng = new Random();
            existingUnmastered = candidates.OrderBy(_ => rng.Next()).Take(request.Count).ToList();
        }

        var finalQuestions = new List<QuizQuestion>(existingUnmastered);

        // 3. If we don't have enough unmastered questions, generate the rest with Gemini
        if (finalQuestions.Count < request.Count)
        {
            var needed = request.Count - finalQuestions.Count;

            var existingTitles = await _dbContext.QuizQuestions
                .AsNoTracking()
                .Where(q => q.Topic.ToLower() == topicTrimmed)
                .Select(q => q.QuestionText)
                .Take(40)
                .ToListAsync(cancellationToken);

            var genResult = await _quizGenerator.GenerateQuestionsAsync(
                request.Topic.Trim(),
                category,
                request.Level,
                needed,
                existingTitles,
                request.Locale,
                cancellationToken);

            if (genResult.IsSuccess && genResult.Value != null && genResult.Value.Count > 0)
            {
                var newQuestions = genResult.Value;
                foreach (var q in newQuestions)
                {
                    q.CreatedByUserId = request.UserId;
                }

                await _dbContext.QuizQuestions.AddRangeAsync(newQuestions, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                finalQuestions.AddRange(newQuestions);
            }
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
                LastSelectedOptionIndex = prog?.LastSelectedOptionIndex,
                IsLastAnswerCorrect = prog?.IsLastAnswerCorrect,
                CorrectCount = prog?.CorrectCount ?? 0,
                IncorrectCount = prog?.IncorrectCount ?? 0
            };
        }).ToList();

        return new GenerateQuizResponse(
            dtos,
            request.Topic.Trim(),
            request.Level,
            dtos.Count
        );
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
