using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.ExplainTerm;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Application.Features.Library.ImportDocument;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Notes.GetHighlights;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;

namespace TechDaily.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Daily Focus Handlers
        services.AddScoped<IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>, GetTodayFocusHandler>();
        services.AddScoped<IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse>, SubmitDailyDrillHandler>();
        services.AddScoped<IUseCase<ExplainTermRequest, ExplainTermResponse>, ExplainTermHandler>();

        // Review Handlers
        services.AddScoped<IUseCase<GetReviewDeckRequest, GetReviewDeckResponse>, GetReviewDeckHandler>();
        services.AddScoped<IUseCase<GradeReviewCardRequest, GradeReviewCardResponse>, GradeReviewCardHandler>();

        // Library Handlers
        services.AddScoped<IUseCase<GetBooksRequest, GetBooksResponse>, GetBooksHandler>();
        services.AddScoped<IUseCase<GetBookByIdRequest, GetBookByIdResponse>, GetBookByIdHandler>();
        services.AddScoped<IUseCase<ImportDocumentRequest, ImportDocumentResponse>, ImportDocumentHandler>();

        // Notes / Highlights Handlers
        services.AddScoped<IUseCase<GetHighlightsRequest, GetHighlightsResponse>, GetHighlightsHandler>();
        services.AddScoped<IUseCase<CreateHighlightRequest, CreateHighlightResponse>, CreateHighlightHandler>();
        services.AddScoped<IUseCase<DeleteHighlightRequest, DeleteHighlightResponse>, DeleteHighlightHandler>();

        return services;
    }
}
