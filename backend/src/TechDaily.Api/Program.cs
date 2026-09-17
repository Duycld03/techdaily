using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TechDaily.Api.Endpoints;
using TechDaily.Api.Middleware;
using TechDaily.Application;
using Microsoft.AspNetCore.Http.Features;
using TechDaily.Infrastructure;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TechDaily.Application.Interfaces;
using TechDaily.Infrastructure.Maintenance;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure 300MB Upload Body Limit (Zero-LOH Disk Spooling)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 367_001_600; // 350 MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 367_001_600; // 350 MB
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add Services
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "TechDaily_Senior_Super_Secret_Key_2026_Min_32_Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechDaily";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TechDailyUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://techdaily.duckdns.org", "http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:3000" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure Multi-Layer Anti-Spam Rate Limiting (Sliding Window: 10 req/min)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "You have exceeded the rate limit of 10 AI requests per minute. Please wait before retrying."
        }, cancellationToken: token);
    };

    options.AddPolicy("AiEndpointsPolicy", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
if (args.Contains("--cleanup-data"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<TechDailyDbContext>();

    if (context.Database.IsRelational())
    {
        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations verified and applied.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not apply database migrations automatically.");
        }
    }

    var runner = services.GetRequiredService<DatabaseMaintenanceRunner>();

    var isExecute = args.Contains("--execute");
    var isDryRun = args.Contains("--dry-run") || !isExecute;
    var isBackfill = args.Contains("--backfill-embeddings");
    var isReseed = args.Contains("--reseed-catalog");

    var batchSize = 25;
    var batchArg = args.FirstOrDefault(a => a.StartsWith("--batch-size=", StringComparison.OrdinalIgnoreCase));
    if (batchArg != null && int.TryParse(batchArg["--batch-size=".Length..], out var parsedBatch) && parsedBatch > 0)
    {
        batchSize = Math.Clamp(parsedBatch, 5, 50);
    }

    // Baseline diagnostic analysis
    var baseline = await runner.AnalyzeTaintedDataAsync();

    if (isExecute)
    {
        var purge = await runner.PurgeTaintedDataAsync();
        int reseededInsights = 0;
        if (isReseed)
        {
            reseededInsights = await runner.ReseedCatalogAsync();
        }

        BackfillReport? backfill = null;
        if (isBackfill)
        {
            backfill = await runner.BackfillEmbeddingsAsync(batchSize);
        }

        var post = await runner.AnalyzeTaintedDataAsync();

        Console.WriteLine();
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("                    TECHDAILY DATABASE MAINTENANCE EXECUTION REPORT                     ");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine($" {"Target Table / Resource",-30} | {"Pre-Purge",-10} | {"Purged",-10} | {"Remaining Tainted",-18} ");
        Console.WriteLine("-------------------------------+------------+------------+--------------------");
        Console.WriteLine($" {"TermExplanationCaches",-30} | {baseline.TermExplanationCachesTainted,-10} | {purge.TermExplanationCachesPurged,-10} | {post.TermExplanationCachesTainted,-18} ");
        Console.WriteLine($" {"TechInsights",-30} | {baseline.TechInsightsTainted,-10} | {purge.TechInsightsPurged,-10} | {post.TechInsightsTainted,-18} ");
        Console.WriteLine($" {"QuizQuestions",-30} | {baseline.QuizQuestionsTainted,-10} | {purge.QuizQuestionsPurged,-10} | {post.QuizQuestionsTainted,-18} ");
        Console.WriteLine($" {"SpacedRepetitionCards",-30} | {baseline.SpacedRepetitionCardsTainted,-10} | {purge.SpacedRepetitionCardsPurged,-10} | {post.SpacedRepetitionCardsTainted,-18} ");
        Console.WriteLine($" {"DocumentChunks (Unvectorized)",-30} | {baseline.UnvectorizedDocumentChunks,-10} | {(backfill != null ? backfill.TotalVectorized.ToString() : "N/A"),-10} | {post.UnvectorizedDocumentChunks,-18} ");
        Console.WriteLine("=========================================================================================");
        if (isReseed)
        {
            Console.WriteLine($" Catalog reseeded: {reseededInsights} curated TechInsights active.");
        }
        if (isBackfill)
        {
            Console.WriteLine($" Vector backfill: {backfill?.TotalVectorized ?? 0} chunks vectorized across {backfill?.TotalBatches ?? 0} batches ({backfill?.FailedBatches ?? 0} failed).");
        }
        Console.WriteLine(" Database maintenance operations completed successfully.");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine();
    }
    else if (isReseed || isBackfill)
    {
        int reseededInsights = 0;
        if (isReseed)
        {
            reseededInsights = await runner.ReseedCatalogAsync();
        }

        BackfillReport? backfill = null;
        if (isBackfill)
        {
            backfill = await runner.BackfillEmbeddingsAsync(batchSize);
        }

        var post = await runner.AnalyzeTaintedDataAsync();

        Console.WriteLine();
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("                    TECHDAILY DATABASE MAINTENANCE OPERATION REPORT                     ");
        Console.WriteLine("=========================================================================================");
        if (isReseed)
        {
            Console.WriteLine($" Catalog reseeded: {reseededInsights} curated TechInsights active.");
        }
        if (isBackfill)
        {
            Console.WriteLine($" Vector backfill: {backfill?.TotalVectorized ?? 0} chunks vectorized across {backfill?.TotalBatches ?? 0} batches ({backfill?.FailedBatches ?? 0} failed).");
            Console.WriteLine($" Remaining unvectorized chunks: {post.UnvectorizedDocumentChunks}.");
        }
        Console.WriteLine("=========================================================================================");
        Console.WriteLine();
    }
    else
    {
        // Dry-Run diagnostic analysis
        Console.WriteLine();
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("                    TECHDAILY DATABASE MAINTENANCE: DRY-RUN REPORT                      ");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine($" {"Target Table / Resource",-32} | {"Tainted Rows",-14} | {"Planned Action",-25} ");
        Console.WriteLine("----------------------------------+----------------+---------------------------");
        Console.WriteLine($" {"TermExplanationCaches",-32} | {baseline.TermExplanationCachesTainted,-14} | {"Purge fallback entries",-25} ");
        Console.WriteLine($" {"TechInsights",-32} | {baseline.TechInsightsTainted,-14} | {"Purge mock insights",-25} ");
        Console.WriteLine($" {"QuizQuestions",-32} | {baseline.QuizQuestionsTainted,-14} | {"Purge mock questions",-25} ");
        Console.WriteLine($" {"SpacedRepetitionCards",-32} | {baseline.SpacedRepetitionCardsTainted,-14} | {"Purge boilerplate cards",-25} ");
        Console.WriteLine($" {"DocumentChunks (Unvectorized)",-32} | {baseline.UnvectorizedDocumentChunks,-14} | {"Backfill 768-D vectors",-25} ");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine(" Zero mutations performed. Transaction rolled back (Dry-Run mode).");
        Console.WriteLine(" Run with --execute to commit purge, --reseed-catalog to seed, --backfill-embeddings to embed.");
        Console.WriteLine("=========================================================================================");
        Console.WriteLine();
    }

    return;
}

// Configure Middleware Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechDaily API v1"));
}

app.UseCors("AllowFrontend");


app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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
            await TechInsightsSeeder.SeedAsync(context);
            logger.LogInformation("Tech Insights Catalog seeded successfully.");

            var embeddingService = services.GetService<IEmbeddingService>();
            if (embeddingService != null)
            {
                await CurriculumSeeder.BackfillEmbeddingsAsync(context, embeddingService, logger);
                logger.LogInformation("Curriculum vector embeddings verified and backfilled.");
            }

            try
            {
                var poisonedEntries = await context.TermExplanationCaches
                    .Where(t => t.ExplanationText.Contains("Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại")
                             || t.ExplanationText.Contains("represents a core runtime or architectural mechanism"))
                    .ExecuteDeleteAsync();
                if (poisonedEntries > 0)
                {
                    logger.LogInformation("Purged {Count} poisoned legacy fallback entries from TermExplanationCaches.", poisonedEntries);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not purge poisoned cache entries.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply migrations automatically. Please ensure PostgreSQL is running.");
    }
}

// Map API Endpoints
app.MapGroup("/api/v1/curriculum")
    .WithTags("Curriculum Roadmap")
    .MapCurriculumEndpoints();

app.MapGroup("/api/v1/insights")
    .WithTags("Tech Insights Feed")
    .RequireAuthorization()
    .MapInsightsEndpoints();

app.MapGroup("/api/v1/quiz")
    .WithTags("Interview Quiz & Mastery Arena")
    .RequireAuthorization()
    .MapQuizEndpoints();

app.MapGroup("/api/v1/daily")
    .WithTags("Daily Focus Hub")
    .MapDailyFocusEndpoints();

app.MapGroup("/api/v1/review")
    .WithTags("Spaced Repetition Review")
    .MapReviewEndpoints();

app.MapLibraryEndpoints();

app.MapNotesEndpoints();

app.MapGroup("/api/v1/graph")
    .WithTags("Knowledge Graph")
    .RequireAuthorization()
    .MapKnowledgeGraphEndpoints();

app.MapGroup("/api/v1/auth")
    .WithTags("Authentication")
    .MapAuthEndpoints(builder.Configuration);

app.MapGroup("/api/v1/user")
    .WithTags("User Profile & Settings")
    .RequireAuthorization()
    .MapUserEndpoints();

app.MapGroup("/api/v1/notifications")
    .WithTags("Web Push & Notifications")
    .MapNotificationEndpoints();

app.MapGroup("/api/v1/system")
    .WithTags("System Diagnostics & Health")
    .MapSystemEndpoints();
// Health Check Endpoint
app.MapGet("/health", async (TechDailyDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = canConnect ? "healthy" : "degraded",
            database = canConnect ? "connected" : "unavailable",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            database = "error",
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: 503);
    }
});

app.Run();
