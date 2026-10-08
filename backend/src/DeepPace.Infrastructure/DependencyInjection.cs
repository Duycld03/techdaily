using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using DeepPace.Application.Features.Library.ImportRemotePdf;

namespace DeepPace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<DeepPaceDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseVector();
                o.MigrationsAssembly(typeof(DeepPaceDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddHttpClient();
        services.AddHttpClient<GeminiAiService>(client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient<GeminiEmbeddingService>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient<TermExplanationService>(client => client.Timeout = TimeSpan.FromSeconds(30));
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
        services.AddHttpClient<IGoogleCloudTtsService, GoogleCloudTtsService>(client => client.Timeout = TimeSpan.FromSeconds(30));

        // Service Registrations
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<ITechInsightGenerator, GeminiAiService>();
        services.AddScoped<IQuizGeneratorService, GeminiAiService>();
        services.AddScoped<IAiMarkdownFormatter, GeminiAiService>();
        services.AddScoped<IGeminiAiService, GeminiAiService>();
        services.AddScoped<ITermExplanationService, TermExplanationService>();
        services.AddScoped<IPdfExtractor, PdfPigExtractor>();
        services.AddSingleton<IPdfIngestionQueue, PdfIngestionQueue>();
        services.AddScoped<ILookAheadBufferService, LookAheadBufferService>();
        services.AddSingleton<IWebPushService, WebPushService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOtpService, OtpService>();

        // Background Workers
        services.AddHostedService<Workers.PdfIngestionWorker>();
        services.AddHostedService<Workers.DailyPushNotificationWorker>();

        return services;
    }
}
