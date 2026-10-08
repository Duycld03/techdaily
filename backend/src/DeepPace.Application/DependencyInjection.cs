using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using DeepPace.Application.Common;
using DeepPace.Application.Features.DailyFocus.ExplainTerm;
using DeepPace.Application.Features.DailyFocus.GetTodayFocus;
using DeepPace.Application.Features.DailyFocus.SubmitDailyDrill;
using DeepPace.Application.Features.Library.CrawlUrl;
using DeepPace.Application.Features.Library.DeleteBook;
using DeepPace.Application.Features.Library.GetBookById;
using DeepPace.Application.Features.Library.GetBooks;
using DeepPace.Application.Features.Library.ImportDocument;
using DeepPace.Application.Features.Library.UploadPdf;
using DeepPace.Application.Features.Library.ExportBookMarkdown;
using DeepPace.Application.Features.Library.ImportRemotePdf;
using DeepPace.Application.Features.Library.UpdateBook;
using DeepPace.Application.Features.Notes.CreateHighlight;
using DeepPace.Application.Features.Notes.DeleteHighlight;
using DeepPace.Application.Features.Notes.GetHighlights;
using DeepPace.Application.Features.Notes.UpdateHighlight;
using DeepPace.Application.Features.Review.GetReviewDeck;
using DeepPace.Application.Features.Review.GradeReviewCard;
using DeepPace.Application.Features.Review.CreateCardFromHighlight;
using DeepPace.Application.Features.Review.CreateCardFromQuizMistake;
using DeepPace.Application.Features.Review.GetReviewCards;
using DeepPace.Application.Features.Review.UpdateReviewCard;
using DeepPace.Application.Features.Review.DeleteReviewCard;
using DeepPace.Application.Features.Review.ResetReviewCardProgress;
using DeepPace.Application.Features.Review.GetReviewAnalytics;
using DeepPace.Application.Features.KnowledgeGraph.DTOs;
using DeepPace.Application.Features.KnowledgeGraph.GetKnowledgeGraph;
using DeepPace.Application.Features.Library.SynthesizeAudio;

namespace DeepPace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);


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
        services.AddScoped<IUseCase<GetReviewAnalyticsRequest, GetReviewAnalyticsResponse>, GetReviewAnalyticsHandler>();

        // Library Handlers
        services.AddScoped<IUseCase<GetBooksRequest, GetBooksResponse>, GetBooksHandler>();
        services.AddScoped<IUseCase<GetBookByIdRequest, GetBookByIdResponse>, GetBookByIdHandler>();
        services.AddScoped<IUseCase<Features.Library.GetBookStatus.GetBookStatusRequest, Features.Library.DTOs.BookIngestionStatusDto>, Features.Library.GetBookStatus.GetBookStatusHandler>();
        services.AddScoped<IUseCase<ImportDocumentRequest, ImportDocumentResponse>, ImportDocumentHandler>();
        services.AddScoped<IUseCase<DeleteBookRequest, DeleteBookResponse>, DeleteBookHandler>();
        services.AddScoped<IUseCase<UpdateBookRequest, UpdateBookResponse>, UpdateBookHandler>();
        services.AddScoped<IUseCase<UploadPdfRequest, UploadPdfResponse>, UploadPdfHandler>();
        services.AddScoped<IUseCase<CrawlUrlRequest, CrawlUrlResponse>, CrawlUrlHandler>();
        services.AddScoped<IUseCase<Features.Library.CurateSlice.CurateSliceRequest, Features.Library.CurateSlice.CurateSliceResponse>, Features.Library.CurateSlice.CurateSliceHandler>();
        services.AddScoped<IUseCase<Features.Library.GetBookSlice.GetBookSliceRequest, Features.Library.GetBookSlice.GetBookSliceResponse>, Features.Library.GetBookSlice.GetBookSliceHandler>();
        services.AddScoped<IUseCase<ExportBookMarkdownRequest, ExportBookMarkdownResponse>, ExportBookMarkdownHandler>();
        services.AddScoped<IUseCase<ImportRemotePdfRequest, UploadPdfResponse>, ImportRemotePdfHandler>();
        services.AddScoped<IUseCase<SynthesizeChunkAudioRequest, SynthesizeChunkAudioResponse>, GetOrSynthesizeChunkAudioHandler>();
        services.AddScoped<IUseCase<GetAudioQuotaRequest, AudioQuotaResponse>, GetAudioQuotaHandler>();

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

        // Knowledge Graph Handlers
        services.AddScoped<IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse>, GetKnowledgeGraphQueryHandler>();

        return services;
    }
}
