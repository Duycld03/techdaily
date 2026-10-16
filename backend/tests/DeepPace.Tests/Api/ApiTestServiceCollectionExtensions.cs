using DeepPace.Application;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeepPace.Tests.Api;

internal static class ApiTestServiceCollectionExtensions
{
    public static IServiceCollection AddApiTestApplicationServices(this IServiceCollection services)
    {
        services.AddApplicationServices();

        // Match production dependencies without replacing the fixture database or starting workers.
        // Individual fixtures override only the external services their scenarios exercise.
        services.AddHttpClient();
        services.AddHttpClient<GeminiAiService>(client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient<GeminiEmbeddingService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<TermExplanationService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<IWebArticleCrawler, WebArticleCrawler>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHttpClient<IGoogleCloudTtsService, GoogleCloudTtsService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<ITechInsightGenerator, GeminiAiService>();
        services.AddScoped<IQuizGeneratorService, GeminiAiService>();
        services.AddScoped<IAiMarkdownFormatter, GeminiAiService>();
        services.AddScoped<IGeminiAiService, GeminiAiService>();
        services.AddScoped<ITermExplanationService, TermExplanationService>();
        services.AddSingleton<IPdfIngestionQueue, PdfIngestionQueue>();
        services.AddScoped<ICatalogAdoptionService, CatalogAdoptionService>();
        services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        services.AddScoped<IAuthorizationReadinessService, AuthorizationReadinessService>();
        services.AddScoped<ICategoryAssignmentGuard, CategoryAssignmentGuard>();
        services.AddScoped<IAdministrativeUnitOfWork, AdministrativeUnitOfWork>();
        services.AddScoped<IDocumentMutationGuard, DocumentMutationGuard>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAdminDashboardReader, AdminDashboardReader>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIngestionSourceStorage, LocalFileIngestionSourceStorage>();
        services.AddScoped<IIngestionQuotaService, IngestionQuotaService>();
        services.AddScoped<IIngestionClaimService, IngestionClaimService>();
        return services;
    }
}
