using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class StarterHandbookServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly StarterHandbookService _service;

    public StarterHandbookServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _service = new StarterHandbookService(_db, NullLogger<StarterHandbookService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ProvisionForUserAsync_ShouldCreateBookChunksAndPacer_OwnedByTargetUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "learner@techdaily.local", Name = "Senior Learner" };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProvisionForUserAsync(userId);

        // Assert
        var book = await _db.DocumentBooks
            .FirstOrDefaultAsync(b => b.CreatedByUserId == userId && b.Slug.StartsWith("senior-engineering-craft-handbook"));

        book.Should().NotBeNull();
        book!.Title.Should().Be("Senior Engineering Craft Handbook");
        book.TotalChunks.Should().Be(30);
        book.CreatedByUserId.Should().Be(userId);

        var chunks = await _db.DocumentChunks
            .Where(c => c.DocumentBookId == book.Id)
            .ToListAsync();

        chunks.Should().HaveCount(30);

        var pacer = await _db.UserBookPacers
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentBookId == book.Id);

        pacer.Should().NotBeNull();
        pacer!.IsActive.Should().BeTrue();
        pacer.CurrentChunkOrder.Should().Be(1);
    }

    [Fact]
    public async Task ProvisionForUserAsync_WhenAlreadyProvisioned_IsIdempotent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "learner2@techdaily.local", Name = "Senior Learner 2" };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProvisionForUserAsync(userId);
        await _service.ProvisionForUserAsync(userId); // Second call

        // Assert
        var books = await _db.DocumentBooks
            .Where(b => b.CreatedByUserId == userId && b.Slug.StartsWith("senior-engineering-craft-handbook"))
            .ToListAsync();

        books.Should().HaveCount(1);
    }
}
