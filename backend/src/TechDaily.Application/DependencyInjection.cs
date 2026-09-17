using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Curriculum.DTOs;
using TechDaily.Application.Features.Curriculum.GetCurriculumRoadmap;
using TechDaily.Application.Features.DailyFocus.ExplainTerm;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Features.Library.CrawlUrl;
using TechDaily.Application.Features.Library.DeleteBook;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Application.Features.Library.ImportDocument;
using TechDaily.Application.Features.Library.UploadPdf;
using TechDaily.Application.Features.Library.ExportBookMarkdown;
using TechDaily.Application.Features.Library.ImportRemotePdf;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Notes.GetHighlights;
using TechDaily.Application.Features.Notes.UpdateHighlight;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;
using TechDaily.Application.Features.Review.CreateCardFromHighlight;
using TechDaily.Application.Features.Review.CreateCardFromQuizMistake;
using TechDaily.Application.Features.Review.GetReviewCards;
using TechDaily.Application.Features.Review.UpdateReviewCard;
using TechDaily.Application.Features.Review.DeleteReviewCard;
using TechDaily.Application.Features.Review.ResetReviewCardProgress;

namespace TechDaily.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Curriculum / Roadmap Handlers
        services.AddScoped<IUseCase<GetCurriculumRoadmapRequest, CurriculumRoadmapResponse>, GetCurriculumRoadmapHandler>();

        // Daily Focus Handlers
        services.AddScoped<IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>, GetTodayFocusHandler>();
        services.AddScoped<IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse>, SubmitDailyDrillHandler>();
        services.AddScoped<IUseCase<ExplainTermRequest, ExplainTermResponse>, ExplainTermHandler>();
        services.AddScoped<IUseCase<Features.DailyFocus.SwitchBook.SwitchBookRequest, Features.DailyFocus.DTOs.PacerDto>, Features.DailyFocus.SwitchBook.SwitchBookHandler>();

        // Review Handlers
        services.AddScoped<IUseCase<GetReviewDeckRequest, GetReviewDeckResponse>, GetReviewDeckHandler>();
        services.AddScoped<IUseCase<GradeReviewCardRequest, GradeReviewCardResponse>, GradeReviewCardHandler>();
        services.AddScoped<IUseCase<CreateCardFromHighlightRequest, CreateCardFromHighlightResponse>, CreateCardFromHighlightHandler>();
        services.AddScoped<IUseCase<CreateCardFromQuizMistakeRequest, CreateCardFromQuizMistakeResponse>, CreateCardFromQuizMistakeHandler>();
        services.AddScoped<IUseCase<GetReviewCardsRequest, GetReviewCardsResponse>, GetReviewCardsHandler>();
        services.AddScoped<IUseCase<UpdateReviewCardRequest, UpdateReviewCardResponse>, UpdateReviewCardHandler>();
        services.AddScoped<IUseCase<DeleteReviewCardRequest, DeleteReviewCardResponse>, DeleteReviewCardHandler>();
        services.AddScoped<IUseCase<ResetReviewCardProgressRequest, ResetReviewCardProgressResponse>, ResetReviewCardProgressHandler>();

        // Library Handlers
        services.AddScoped<IUseCase<GetBooksRequest, GetBooksResponse>, GetBooksHandler>();
        services.AddScoped<IUseCase<GetBookByIdRequest, GetBookByIdResponse>, GetBookByIdHandler>();
        services.AddScoped<IUseCase<Features.Library.GetBookStatus.GetBookStatusRequest, Features.Library.DTOs.BookIngestionStatusDto>, Features.Library.GetBookStatus.GetBookStatusHandler>();
        services.AddScoped<IUseCase<ImportDocumentRequest, ImportDocumentResponse>, ImportDocumentHandler>();
        services.AddScoped<IUseCase<DeleteBookRequest, DeleteBookResponse>, DeleteBookHandler>();
        services.AddScoped<IUseCase<UploadPdfRequest, UploadPdfResponse>, UploadPdfHandler>();
        services.AddScoped<IUseCase<CrawlUrlRequest, CrawlUrlResponse>, CrawlUrlHandler>();
        services.AddScoped<IUseCase<Features.Library.CurateSlice.CurateSliceRequest, Features.Library.CurateSlice.CurateSliceResponse>, Features.Library.CurateSlice.CurateSliceHandler>();
        services.AddScoped<IUseCase<Features.Library.GetBookSlice.GetBookSliceRequest, Features.Library.GetBookSlice.GetBookSliceResponse>, Features.Library.GetBookSlice.GetBookSliceHandler>();
        services.AddScoped<IUseCase<ExportBookMarkdownRequest, ExportBookMarkdownResponse>, ExportBookMarkdownHandler>();
        services.AddScoped<IUseCase<ImportRemotePdfRequest, UploadPdfResponse>, ImportRemotePdfHandler>();

        // Notes / Highlights Handlers
        services.AddScoped<IUseCase<GetHighlightsRequest, GetHighlightsResponse>, GetHighlightsHandler>();
        services.AddScoped<IUseCase<CreateHighlightRequest, CreateHighlightResponse>, CreateHighlightHandler>();
        services.AddScoped<IUseCase<DeleteHighlightRequest, DeleteHighlightResponse>, DeleteHighlightHandler>();
        services.AddScoped<IUseCase<UpdateHighlightRequest, UpdateHighlightResponse>, UpdateHighlightHandler>();

        // Tech Insights Feed Handlers
        services.AddScoped<IUseCase<Features.Insights.DTOs.GetInsightsFeedRequest, Features.Insights.DTOs.GetInsightsFeedResponse>, Features.Insights.GetInsightsFeed.GetInsightsFeedHandler>();
        services.AddScoped<IUseCase<Features.Insights.DTOs.GenerateInsightRequest, Features.Insights.DTOs.TechInsightDto>, Features.Insights.GenerateInsight.GenerateInsightHandler>();
        services.AddScoped<IUseCase<Features.Insights.DTOs.BookmarkInsightRequest, Features.Insights.DTOs.BookmarkInsightResponse>, Features.Insights.BookmarkInsight.BookmarkInsightHandler>();
        services.AddScoped<IUseCase<Features.Insights.DTOs.GetInsightsMetaRequest, Features.Insights.DTOs.GetInsightsMetaResponse>, Features.Insights.GetInsightsMeta.GetInsightsMetaHandler>();

        // Interview Quiz & Mastery Arena Handlers
        services.AddScoped<IUseCase<Features.InterviewQuiz.DTOs.GenerateQuizRequest, Features.InterviewQuiz.DTOs.GenerateQuizResponse>, Features.InterviewQuiz.GenerateQuiz.GenerateQuizHandler>();
        services.AddScoped<IUseCase<Features.InterviewQuiz.DTOs.SubmitQuizAnswerRequest, Features.InterviewQuiz.DTOs.SubmitQuizAnswerResponse>, Features.InterviewQuiz.SubmitQuizAnswer.SubmitQuizAnswerHandler>();
        services.AddScoped<IUseCase<Features.InterviewQuiz.DTOs.GetQuizReviewQueueRequest, Features.InterviewQuiz.DTOs.GetQuizReviewQueueResponse>, Features.InterviewQuiz.GetQuizReviewQueue.GetQuizReviewQueueHandler>();
        services.AddScoped<IUseCase<Features.InterviewQuiz.DTOs.GetQuizStatsRequest, Features.InterviewQuiz.DTOs.GetQuizStatsResponse>, Features.InterviewQuiz.GetQuizStats.GetQuizStatsHandler>();

        return services;
    }
}
