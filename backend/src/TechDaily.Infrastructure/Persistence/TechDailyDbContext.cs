using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Common;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Persistence;

public class TechDailyDbContext : DbContext, ITechDailyDbContext
{
    public TechDailyDbContext(DbContextOptions<TechDailyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<DocumentBook> DocumentBooks => Set<DocumentBook>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DailyDrill> DailyDrills => Set<DailyDrill>();
    public DbSet<AiReview> AiReviews => Set<AiReview>();
    public DbSet<SpacedRepetitionCard> SpacedRepetitionCards => Set<SpacedRepetitionCard>();
    public DbSet<StreakRecord> StreakRecords => Set<StreakRecord>();
    public DbSet<UserHighlight> UserHighlights => Set<UserHighlight>();
    public DbSet<TermExplanationCache> TermExplanationCaches => Set<TermExplanationCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension in PostgreSQL
        modelBuilder.HasPostgresExtension("vector");

        // Apply all entity configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(condition, parameter);
    }
}
