using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Library.GetBookStatus;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetBookStatusHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public GetBookStatusHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetBookStatus_ShouldReturnStatus_WhenBookExists()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "High-Performance PostgreSQL 17",
            Slug = "high-performance-postgresql-17",
            Category = Category.DatabaseStorage,
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 45,
            StatusMessage = "Processing page 45 of 100",
            TotalChunks = 5
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var validator = new GetBookStatusValidator();
        var handler = new GetBookStatusHandler(_db, validator);

        // Act
        var result = await handler.ExecuteAsync(new GetBookStatusRequest(book.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(book.Id);
        result.Value.Status.Should().Be(ProcessingStatus.Processing);
        result.Value.ProgressPercentage.Should().Be(45);
        result.Value.StatusMessage.Should().Be("Processing page 45 of 100");
        result.Value.TotalChunks.Should().Be(5);
    }

    [Fact]
    public async Task GetBookStatus_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var validator = new GetBookStatusValidator();
        var handler = new GetBookStatusHandler(_db, validator);

        // Act
        var result = await handler.ExecuteAsync(new GetBookStatusRequest(Guid.NewGuid()));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESOURCE_NOT_FOUND");
    }
}
