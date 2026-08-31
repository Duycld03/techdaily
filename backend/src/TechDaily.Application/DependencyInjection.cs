using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.ExplainTerm;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;

namespace TechDaily.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register Plain Use-Case Handlers (Pure DI)
        services.AddScoped<IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>, GetTodayFocusHandler>();
        services.AddScoped<IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse>, SubmitDailyDrillHandler>();
        services.AddScoped<IUseCase<ExplainTermRequest, ExplainTermResponse>, ExplainTermHandler>();
        services.AddScoped<IUseCase<GetReviewDeckRequest, GetReviewDeckResponse>, GetReviewDeckHandler>();
        services.AddScoped<IUseCase<GradeReviewCardRequest, GradeReviewCardResponse>, GradeReviewCardHandler>();

        return services;
    }
}
