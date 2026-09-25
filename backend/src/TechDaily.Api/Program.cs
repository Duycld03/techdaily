using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
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
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 characters (256-bit entropy).");
}
if (!builder.Environment.IsDevelopment())
{
    var vapidPrivate = builder.Configuration["WebPush:PrivateKey"] ?? builder.Configuration["VAPID_PRIVATE_KEY"];
    var vapidPublic = builder.Configuration["WebPush:PublicKey"] ?? builder.Configuration["VAPID_PUBLIC_KEY"];
    if (string.IsNullOrWhiteSpace(vapidPrivate) || string.IsNullOrWhiteSpace(vapidPublic))
    {
        throw new InvalidOperationException("WebPush:PrivateKey and WebPush:PublicKey must be configured in non-development environments.");
    }
}
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
            ?? new[]
            {
                "https://techdaily.duckdns.org",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://localhost:5000",
                "http://127.0.0.1:5000"
            };

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

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "TechDaily API Reference";
        document.Info.Version = "v1";
        document.Info.Description = "Daily Senior Engineering & Interview Drill Platform API. Interactive developer reference for all Minimal API endpoints, domain contracts, and RFC 7807 problem details.";

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Enter your token below:"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("Bearer", scheme);

        // Global security requirement enabling Scalar's interactive Bearer authorization
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });

        // Clear 0.0.0.0 bind address and set valid servers for browser interactive testing
        document.Servers ??= new List<OpenApiServer>();
        document.Servers.Clear();
        document.Servers.Add(new OpenApiServer { Url = "/", Description = "Current Origin (Auto)" });
        document.Servers.Add(new OpenApiServer { Url = "http://localhost:5000", Description = "Localhost (http://localhost:5000)" });
        document.Servers.Add(new OpenApiServer { Url = "http://127.0.0.1:5000", Description = "Loopback (http://127.0.0.1:5000)" });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseExceptionHandler();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TechDaily API Reference")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.MapGet("/swagger", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
    app.MapGet("/swagger/index.html", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

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
})
.WithName("HealthCheck")
.WithTags("System Diagnostics & Health")
.WithSummary("System Health & Database Liveness")
.WithDescription("Checks system health, core API readiness, and live PostgreSQL database connectivity.");

app.Run();
