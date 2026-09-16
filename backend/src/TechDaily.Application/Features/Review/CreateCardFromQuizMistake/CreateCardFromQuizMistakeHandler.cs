using System.Text;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Features.Review.CreateCardFromQuizMistake;

public record CreateCardFromQuizMistakeRequest(Guid QuestionId, Guid UserId);

public class CreateCardFromQuizMistakeResponse
{
    public Guid CardId { get; set; }

    public CreateCardFromQuizMistakeResponse() { }

    public CreateCardFromQuizMistakeResponse(Guid cardId)
    {
        CardId = cardId;
    }
}

public class CreateCardFromQuizMistakeHandler : IUseCase<CreateCardFromQuizMistakeRequest, CreateCardFromQuizMistakeResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public CreateCardFromQuizMistakeHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateCardFromQuizMistakeResponse>> ExecuteAsync(
        CreateCardFromQuizMistakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var question = await _dbContext.QuizQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (question == null)
        {
            return Error.NotFound;
        }

        // Idempotency: return existing card if already created
        var existingCard = await _dbContext.SpacedRepetitionCards
            .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceQuizQuestionId == request.QuestionId, cancellationToken);

        if (existingCard != null)
        {
            return new CreateCardFromQuizMistakeResponse(existingCard.Id);
        }

        var sbFront = new StringBuilder();
        sbFront.AppendLine(question.QuestionText);
        if (question.Options != null && question.Options.Count > 0)
        {
            sbFront.AppendLine();
            for (int i = 0; i < question.Options.Count; i++)
            {
                var label = (char)('A' + i);
                sbFront.AppendLine($"- **{label}.** {question.Options[i]}");
            }
        }
        var front = sbFront.ToString().Trim();

        var sbBack = new StringBuilder();
        if (question.Options != null && question.CorrectOptionIndex >= 0 && question.CorrectOptionIndex < question.Options.Count)
        {
            var label = (char)('A' + question.CorrectOptionIndex);
            sbBack.AppendLine($"**Correct Option: {label}. {question.Options[question.CorrectOptionIndex]}**");
            sbBack.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(question.ExplanationMarkdown))
        {
            sbBack.AppendLine(question.ExplanationMarkdown);
        }
        var back = sbBack.ToString().Trim();

        var card = SpacedRepetitionCard.CreateFromQuizMistake(
            request.UserId,
            question.Id,
            front,
            back);

        await _dbContext.SpacedRepetitionCards.AddAsync(card, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateCardFromQuizMistakeResponse(card.Id);
    }
}
