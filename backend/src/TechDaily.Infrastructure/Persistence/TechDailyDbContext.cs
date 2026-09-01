using System.Linq.Expressions;
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
    public DbSet<TechInsight> TechInsights => Set<TechInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var isNpgsql = Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (isNpgsql)
        {
            // Enable pgvector extension in PostgreSQL
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<DocumentChunk>().Property(c => c.Embedding).HasColumnType("vector(768)");
            modelBuilder.Entity<TermExplanationCache>().Property(t => t.Embedding).HasColumnType("vector(768)");
        }
        else
        {
            // For SQLite / InMemory test providers, convert Vector to string directly to avoid EF 9 primitive collection conflicts
            modelBuilder.Entity<DocumentChunk>()
                .Property(c => c.Embedding)
                .HasConversion(
                    v => v == null ? null : string.Join(";", v.ToArray()),
                    s => s == null ? null : new Pgvector.Vector(s.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray()));

            modelBuilder.Entity<TermExplanationCache>()
                .Property(t => t.Embedding)
                .HasConversion(
                    v => v == null ? null : string.Join(";", v.ToArray()),
                    s => s == null ? null : new Pgvector.Vector(s.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray()));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
                }
            }
        }

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

    private static LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}
