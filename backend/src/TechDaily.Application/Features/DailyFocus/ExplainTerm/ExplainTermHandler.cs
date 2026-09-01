using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.DailyFocus.ExplainTerm;

public record ExplainTermRequest(
    string Term,
    string Category,
    string Context,
    string Locale = "en");

public class ExplainTermResponse
{
    public string Term { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
}

public class ExplainTermValidator : AbstractValidator<ExplainTermRequest>
{
    public ExplainTermValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
    }
}

public class ExplainTermHandler : IUseCase<ExplainTermRequest, ExplainTermResponse>
{
    private readonly ITermExplanationService _termExplanationService;
    private readonly IValidator<ExplainTermRequest> _validator;

    public ExplainTermHandler(
        ITermExplanationService termExplanationService,
        IValidator<ExplainTermRequest> validator)
    {
        _termExplanationService = termExplanationService;
        _validator = validator;
    }

    public async Task<Result<ExplainTermResponse>> ExecuteAsync(
        ExplainTermRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var safeCategory = string.IsNullOrWhiteSpace(request.Category) ? "Software Architecture" : request.Category;
        var safeContext = request.Context ?? string.Empty;

        var explanationResult = await _termExplanationService.ExplainTermAsync(
            request.Term,
            safeCategory,
            safeContext,
            request.Locale,
            cancellationToken);

        if (explanationResult.IsFailure)
        {
            return explanationResult.Error;
        }

        return new ExplainTermResponse
        {
            Term = request.Term,
            Explanation = explanationResult.Value,
            Locale = request.Locale
        };
    }
}
