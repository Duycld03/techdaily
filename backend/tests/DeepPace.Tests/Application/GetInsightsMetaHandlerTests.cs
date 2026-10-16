using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Features.Insights.GetInsightsMeta;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class GetInsightsMetaHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly GetInsightsMetaHandler _handler;
    private readonly User _user = new() { Email = "meta@example.test", Name = "Reader" };
    private readonly DocumentCategory _category = DocumentCategory.Create("novel-topic", "Novel subject", "Chủ đề mới", 2, false);

    public GetInsightsMetaHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Users.Add(_user);
        _db.DocumentCategories.Add(_category);
        _db.SaveChanges();
        _handler = new GetInsightsMetaHandler(_db);
    }

    [Fact]
    public async Task Metadata_UsesCatalogIncludingInactiveNovelCategories_WithoutInventedTopics()
    {
        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest(_user.Id));

        var item = result.Value.Categories.Single(c => c.CategoryId == _category.Id);
        item.CategoryId.Should().Be(_category.Id);
        item.Category.NameVi.Should().Be("Chủ đề mới");
        item.Category.IsActive.Should().BeFalse();
        item.Count.Should().Be(0);
        result.Value.SuggestedTopics[_category.Id].Should().BeEmpty();
    }

    [Fact]
    public async Task Metadata_CountsPublishedInsights_AndOnlySuggestsAuthenticatedOwnedReadySources()
    {
        _db.TechInsights.AddRange(
            new TechInsight { CategoryId = _category.Id, Slug = "published", Title = "Published", IsPublished = true },
            new TechInsight { CategoryId = _category.Id, Slug = "draft", Title = "Draft", IsPublished = false });
        var ownerBook = new DocumentBook { Title = "Owned subject", Slug = "owned", CategoryId = _category.Id, CreatedByUserId = _user.Id, Status = ProcessingStatus.Ready };
        var otherBook = new DocumentBook { Title = "Private other reader title", Slug = "other", CategoryId = _category.Id, Status = ProcessingStatus.Ready };
        var pendingBook = new DocumentBook { Title = "Pending source", Slug = "pending", CategoryId = _category.Id, CreatedByUserId = _user.Id, Status = ProcessingStatus.Pending };
        _db.DocumentBooks.AddRange(ownerBook, otherBook, pendingBook);
        _db.DocumentChunks.AddRange(
            new DocumentChunk { DocumentBookId = ownerBook.Id, ChapterTitle = "Chapter 1: Source mechanics", ChunkOrder = 1 },
            new DocumentChunk { DocumentBookId = ownerBook.Id, ChapterTitle = "Source mechanics", ChunkOrder = 2 },
            new DocumentChunk { DocumentBookId = ownerBook.Id, ChapterTitle = "Deleted chapter", ChunkOrder = 3, IsDeleted = true },
            new DocumentChunk { DocumentBookId = otherBook.Id, ChapterTitle = "Private chapter", ChunkOrder = 1 },
            new DocumentChunk { DocumentBookId = pendingBook.Id, ChapterTitle = "Pending chapter", ChunkOrder = 1 });
        await _db.SaveChangesAsync();

        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest(_user.Id));

        result.Value.Categories.Single(c => c.CategoryId == _category.Id).Count.Should().Be(1);
        result.Value.SuggestedTopics[_category.Id].Should().Equal("Owned subject", "Source mechanics");
    }

    [Fact]
    public async Task Metadata_CapsDistinctCleanedTopicsAtEight()
    {
        var book = new DocumentBook { Title = "Source title", Slug = "large", CategoryId = _category.Id, CreatedByUserId = _user.Id, Status = ProcessingStatus.Ready };
        _db.DocumentBooks.Add(book);
        _db.DocumentChunks.AddRange(Enumerable.Range(1, 15).Select(i => new DocumentChunk { DocumentBookId = book.Id, ChunkOrder = i, ChapterTitle = $"Chapter {i}: Topic number {i}" }));
        await _db.SaveChangesAsync();

        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest(_user.Id));

        result.Value.SuggestedTopics[_category.Id].Should().HaveCount(8).And.OnlyHaveUniqueItems();
        result.Value.SuggestedTopics[_category.Id].Should().Contain("Topic number 1");
    }

    [Theory]
    [InlineData("CHƯƠNG 4: QUY LUẬT SỐ 1 - KHIẾN VIỆC ĐÓ TRỞ NÊN HIỂN NHIÊN (Section 1)", "QUY LUẬT SỐ 1 - KHIẾN VIỆC ĐÓ TRỞ NÊN HIỂN NHIÊN")]
    [InlineData("Chapter 19: Query Optimization & Cost-Based Execution Planning", "Query Optimization & Cost-Based Execution Planning")]
    [InlineData("Tóm tắt chương (Section 3)", "")]
    [InlineData("Raft Consensus Protocol", "Raft Consensus Protocol")]
    [InlineData("QUI LUẬT SỐ 12: NỖ LỰC TỐI THIỂU (Section 1)", "NỖ LỰC TỐI THIỂU")]
    public void CleanTopicTitle_StripsNoiseAndChapterPrefixes(string raw, string expected) => GetInsightsMetaHandler.CleanTopicTitle(raw).Should().Be(expected);

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
