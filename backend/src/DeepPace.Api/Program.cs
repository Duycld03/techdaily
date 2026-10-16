using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using DeepPace.Api.Endpoints;
using DeepPace.Api.Contracts;
using DeepPace.Api.Middleware;
using DeepPace.Application;
using Microsoft.AspNetCore.Http.Features;
using DeepPace.Infrastructure;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Persistence.Seeders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using DeepPace.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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
    var vapidPrivate = builder.Configuration["WebPush:PrivateKey"];
    var vapidPublic = builder.Configuration["WebPush:PublicKey"];
    if (string.IsNullOrWhiteSpace(vapidPrivate) || string.IsNullOrWhiteSpace(vapidPublic))
    {
        throw new InvalidOperationException("WebPush:PrivateKey and WebPush:PublicKey must be configured in non-development environments.");
    }
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DeepPace";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DeepPaceUsers";
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
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(idClaim, out var userId))
                {
                    context.Fail("Missing or invalid user identifier claim.");
                    return;
                }

                var sessionVersionClaim = context.Principal?.FindFirst("session_version")?.Value;
                if (string.IsNullOrWhiteSpace(sessionVersionClaim) || !int.TryParse(sessionVersionClaim, out var sessionVersion) || sessionVersion <= 0)
                {
                    context.Fail("Missing or invalid session_version claim.");
                    return;
                }

                var authority = context.HttpContext.RequestServices.GetRequiredService<IAccountSessionAuthority>();
                var validation = await authority.ValidateSessionAsync(userId, sessionVersion, context.HttpContext.RequestAborted);

                if (validation.Status == AccountAuthorityStatus.Valid)
                {
                    return;
                }

                if (validation.Status == AccountAuthorityStatus.Unavailable)
                {
                    context.Fail("Account authority verification is temporarily unavailable.");
                    context.HttpContext.Items["SessionAuthorityUnavailable"] = true;
                    return;
                }

                context.Fail("Session has been revoked or account is inactive.");
            }
        };
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, DeepPace.Api.Authorization.AdminAuthorizationMiddlewareResultHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.ViewAdminDashboardAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.RoleAdminAuthorizationHandler>();

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.CatalogAdminAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.AccountAdminAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.AuditReadAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.DocumentOperationsAdminAuthorizationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, DeepPace.Api.Authorization.CategoryAdminAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.CatalogAdminRequirement());
    });
    options.AddPolicy("AccountAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.AccountAdminRequirement());
    });
    options.AddPolicy("AuditRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.AuditReadRequirement());
    });
    options.AddPolicy("DocumentOperationsAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.DocumentOperationsAdminRequirement());
    });
    options.AddPolicy("ViewAdminDashboard", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.ViewAdminDashboardRequirement());
    });
    options.AddPolicy("RoleAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.RoleAdminRequirement());
    });
    options.AddPolicy("CategoryAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new DeepPace.Api.Authorization.CategoryAdminRequirement());
    });
});

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
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "You have exceeded the request rate limit. Please wait before retrying."
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

    options.AddPolicy("OtpEndpointsPolicy", httpContext =>
    {
        // OTP endpoints are unauthenticated; partition by client IP to bound abuse.
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddPolicy("AdminRolesReadPolicy", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddPolicy("AdminRolesWritePolicy", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddPolicy("CategoryAdminReads", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddPolicy("CategoryAdminWrites", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

builder.Services.AddAdminDashboardRateLimiting();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "DeepPace API Reference";
        document.Info.Version = "v1";
        document.Info.Description = "DeepPace deliberate daily practice platform API. Interactive reference for all Minimal API endpoints, domain contracts, and RFC 7807 problem details.";

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
        options.WithTitle("DeepPace API Reference")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.MapGet("/swagger", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
    app.MapGet("/swagger/index.html", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/admin") ||
        context.Request.Path.Equals("/api/v1/catalog/capabilities", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next(context);
});

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.Items.ContainsKey("SessionAuthorityUnavailable"))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Service Unavailable",
            Detail = "Account authority verification is temporarily unavailable."
        });
        return;
    }

    await next(context);
});

app.Use(async (context, next) =>
{
    var isAdminRoute = context.Request.Path.StartsWithSegments("/api/v1/admin") ||
                       context.Request.Path.Equals("/api/v1/catalog/capabilities", StringComparison.OrdinalIgnoreCase);

    if (!isAdminRoute)
    {
        await next(context);
        return;
    }

    var readiness = context.RequestServices.GetRequiredService<IAuthorizationReadinessService>();
    var status = await readiness.GetReadinessAsync(context.RequestAborted);
    if (!status.IsReady || !status.HasRequiredSchemaAndRoles)
    {
        app.Logger.LogWarning("Administrative authorization readiness check failed: {Reason}", status.FailureReason);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Service Unavailable",
            status = StatusCodes.Status503ServiceUnavailable,
            detail = "Administrative authorization is temporarily unavailable."
        });
        return;
    }

    if (!status.IsInitialized && context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Service Unavailable",
            status = StatusCodes.Status503ServiceUnavailable,
            detail = "Privileged authorization has not been initialized."
        });
        return;
    }

    await next(context);
});

app.UseAuthorization();
app.UseRateLimiter();

// Auto-migrate and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<DeepPaceDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("PostgreSQL database migrations applied successfully.");
            await TechInsightsSeeder.SeedAsync(context);
            logger.LogInformation("Tech Insights Catalog seeded successfully.");
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
app.MapCatalogEndpoints();
app.MapAdminDashboardEndpoints();
app.MapAdminAccountsAndAuditEndpoints();
app.MapAdminDocumentOperationsEndpoints();
app.MapAdminCategoriesEndpoints();




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
app.MapGet("/health", async (IAuthorizationReadinessService readinessService) =>
{
    var readiness = await readinessService.GetReadinessAsync();
    if (!readiness.IsReady || !readiness.HasRequiredSchemaAndRoles)
    {
        app.Logger.LogWarning("Health readiness check failed: {Reason}", readiness.FailureReason);
        return Results.Json(new HealthStatusResponse(
            Status: "unhealthy",
            Database: readiness.IsDatabaseConnected ? "schema_unready" : "disconnected",
            Error: "Authorization readiness is temporarily unavailable.",
            Timestamp: DateTime.UtcNow), statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new HealthStatusResponse(
        Status: "healthy",
        Database: "connected",
        Error: null,
        Timestamp: DateTime.UtcNow));
})
.WithName("HealthCheck")
.WithTags("System Diagnostics & Health")
.WithSummary("System Health & Database Liveness")
.WithDescription("Checks system health, core API readiness, and live PostgreSQL database connectivity.")
.Produces<HealthStatusResponse>(StatusCodes.Status200OK)
.Produces<HealthStatusResponse>(StatusCodes.Status503ServiceUnavailable);
if (args.Length > 0 && args[0].Equals("authorization", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length >= 2 && args[1].Equals("initialize", StringComparison.OrdinalIgnoreCase))
    {
        var adminIds = new List<Guid>();
        for (int i = 2; i < args.Length; i++)
        {
            if ((args[i] == "--admin-user-id" || args[i] == "-a") && i + 1 < args.Length)
            {
                if (Guid.TryParse(args[i + 1], out var parsedId))
                {
                    adminIds.Add(parsedId);
                }
                else
                {
                    Console.Error.WriteLine($"Malformed administrator UUID: '{args[i + 1]}'");
                    return 1;
                }
                i++;
            }
        }

        using var scope = app.Services.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<IAuthorizationInitializationService>();
        var initResult = await initService.InitializeAsync(adminIds);
        if (!initResult.IsSuccess)
        {
            Console.Error.WriteLine($"Authorization initialization failed: {initResult.Error.Code} - {initResult.Error.Message}");
            return 1;
        }

        Console.WriteLine($"Authorization initialization completed successfully. Scope: '{initResult.Value.Scope}', Imported {initResult.Value.ImportedAdminUserIds.Count} administrator(s).");
        return 0;
    }

    Console.Error.WriteLine($"Unknown authorization command: '{string.Join(" ", args)}'. Supported commands: authorization initialize --admin-user-id <uuid>");
    return 1;
}
using (var scope = app.Services.CreateScope())
{
    var readinessService = scope.ServiceProvider.GetRequiredService<IAuthorizationReadinessService>();
    var readiness = await readinessService.GetReadinessAsync();
    if (!readiness.IsReady)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical("Required authorization readiness check failed on host startup: {Reason}. Halting process.", readiness.FailureReason);
        Console.Error.WriteLine($"Startup failed: Authorization readiness check failed - {readiness.FailureReason}");
        return 1;
    }
}

app.Run();
return 0;

public partial class Program { }
