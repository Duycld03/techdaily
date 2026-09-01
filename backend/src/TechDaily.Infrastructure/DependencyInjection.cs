using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Application.Interfaces;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;

namespace TechDaily.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=techdaily_db;Username=techdaily_user;Password=techdaily_password_secret";

        services.AddDbContext<TechDailyDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseVector();
                o.MigrationsAssembly(typeof(TechDailyDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<ITechDailyDbContext>(sp => sp.GetRequiredService<TechDailyDbContext>());

        // HTTP Clients for External Services
        services.AddHttpClient<GeminiAiService>();
        services.AddHttpClient<TermExplanationService>();
        services.AddHttpClient<TelegramNotifier>();
        services.AddHttpClient<IWebArticleCrawler, WebArticleCrawler>();

        // Service Registrations
        services.AddScoped<IAiReviewService, GeminiAiService>();
        services.AddScoped<ITechInsightGenerator, GeminiAiService>();
        services.AddScoped<ITermExplanationService, TermExplanationService>();
        services.AddScoped<ITelegramNotifier, TelegramNotifier>();
        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();
        services.AddScoped<IPdfExtractor, PdfPigExtractor>();

        return services;
    }
}
