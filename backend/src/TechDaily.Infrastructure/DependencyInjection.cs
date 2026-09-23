using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Application.Interfaces;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using TechDaily.Infrastructure.Maintenance;
using TechDaily.Application.Features.Library.ImportRemotePdf;

namespace TechDaily.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<TechDailyDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseVector();
                o.MigrationsAssembly(typeof(TechDailyDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<ITechDailyDbContext>(sp => sp.GetRequiredService<TechDailyDbContext>());

        services.AddHttpClient<GeminiAiService>(client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient<GeminiEmbeddingService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<TermExplanationService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<TelegramNotifier>();
        services.AddHttpClient<IWebArticleCrawler, WebArticleCrawler>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false
            });
        services.AddHttpClient<LookAheadBufferService>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<ImportRemotePdfHandler>(client => client.Timeout = TimeSpan.FromSeconds(180))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false
            });

        // Service Registrations
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<ITechInsightGenerator, GeminiAiService>();
        services.AddScoped<IQuizGeneratorService, GeminiAiService>();
        services.AddScoped<IAiMarkdownFormatter, GeminiAiService>();
        services.AddScoped<IGeminiAiService, GeminiAiService>();
        services.AddScoped<ITermExplanationService, TermExplanationService>();
        services.AddScoped<ITelegramNotifier, TelegramNotifier>();
        services.AddScoped<IPdfExtractor, PdfPigExtractor>();
        services.AddSingleton<IPdfIngestionQueue, PdfIngestionQueue>();
        services.AddScoped<ILookAheadBufferService, LookAheadBufferService>();
        services.AddSingleton<IWebPushService, WebPushService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<DatabaseMaintenanceRunner>();
        // Background Workers
        services.AddHostedService<Workers.PdfIngestionWorker>();
        services.AddHostedService<Workers.DailyPushNotificationWorker>();

        return services;
    }
}
