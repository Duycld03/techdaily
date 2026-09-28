using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Security;

namespace TechDaily.Infrastructure.Persistence.Seeders;

public static class E2EAccountSeeder
{
    public static async Task SeedAsync(TechDailyDbContext context, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["E2E_PROD_EMAIL"] ?? configuration["E2e:Email"] ?? "e2e_prod_agent@techdaily.local";
        var password = configuration["E2E_PROD_PASSWORD"] ?? configuration["E2e:Password"] ?? "E2eLiveTestPass2026!";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    Name = "E2E Test Agent",
                    PasswordHash = PasswordHasher.HashPassword(password),
                    PreferredLocale = "vi",
                    TargetRole = "Senior System Architect",
                    DailyGoalMinutes = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(newUser);
                await context.SaveChangesAsync();
                logger.LogInformation("E2E Test Account successfully provisioned: {Email}", normalizedEmail);
            }
            else
            {
                // Verify if password needs updating to match current configuration
                if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    user.PasswordHash = PasswordHasher.HashPassword(password);
                    user.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    logger.LogInformation("E2E Test Account password hash updated for: {Email}", normalizedEmail);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not seed E2E Test Account: {Message}", ex.Message);
        }
    }
}
