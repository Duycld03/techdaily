using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using TechDaily.Api.Endpoints;
using TechDaily.Api.Middleware;
using TechDaily.Application;
using TechDaily.Infrastructure;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "TechDaily API",
        Version = "v1",
        Description = "Daily Senior Engineering & Interview Drill Platform API"
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechDaily API v1"));
}

app.UseCors("AllowFrontend");

// Serve Static Audio Files (/uploads/audios/...)
var audioStoragePath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "storage", "audios");
if (!Directory.Exists(audioStoragePath))
{
    Directory.CreateDirectory(audioStoragePath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(audioStoragePath),
    RequestPath = "/uploads/audios"
});

// Auto-migrate and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<TechDailyDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("PostgreSQL database migrations applied successfully.");
            await CurriculumSeeder.SeedAsync(context);
            logger.LogInformation("Master 30-Day Curriculum seeded successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply migrations automatically. Please ensure PostgreSQL is running.");
    }
}

// Map API Endpoints
app.MapGroup("/api/v1/daily")
    .WithTags("Daily Focus Hub")
    .MapDailyFocusEndpoints();

app.MapGroup("/api/v1/review")
    .WithTags("Spaced Repetition Review")
    .MapReviewEndpoints();

app.MapGroup("/api/v1/auth")
    .WithTags("Authentication")
    .MapAuthEndpoints(builder.Configuration);

// Health Check Endpoint
app.MapGet("/health", async (TechDailyDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTimeOffset.UtcNow,
        DatabaseConnected = canConnect,
        Environment = app.Environment.EnvironmentName
    });
})
.WithName("HealthCheck")
.WithTags("System");

app.Run();

public partial class Program { }
