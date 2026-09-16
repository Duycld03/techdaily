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
