using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Application.Common;
using DeepPace.Application.Features.DailyFocus.SwitchBook;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Features.InterviewQuiz.GenerateQuiz;
using DeepPace.Application.Features.KnowledgeGraph.DTOs;
using DeepPace.Application.Features.KnowledgeGraph.GetKnowledgeGraph;
using DeepPace.Application.Features.Library.CurateSlice;
using DeepPace.Application.Features.Library.ExportBookMarkdown;
using DeepPace.Application.Features.Library.GetBookById;
using DeepPace.Application.Features.Library.GetBookSlice;
using DeepPace.Application.Features.Library.GetBookStatus;
using DeepPace.Application.Features.Library.SynthesizeAudio;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class PersonalBookOwnershipIsolationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public PersonalBookOwnershipIsolationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(User UserA, User UserB, DocumentBook BookA, DocumentChunk ChunkA)> SeedTwoUsersWithBookAAsync()
    {
        var userA = new User { Id = Guid.NewGuid(), Email = "userA@example.com", Name = "User A" };
        var userB = new User { Id = Guid.NewGuid(), Email = "userB@example.com", Name = "User B" };

        var bookA = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "User A's Private Book",
            Slug = "user-a-book",
            Category = Category.BackendRuntime,
            CreatedByUserId = userA.Id,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1,
            IsPublished = true,
            IsDeleted = false
        };

        var chunkA = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookA.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Private content of user A",
            SummaryMarkdown = "Summary A",
            Language = "en",
            IsAiFormatted = false
        };

        bookA.Chunks.Add(chunkA);

        await _db.Users.AddRangeAsync(userA, userB);
        await _db.DocumentBooks.AddAsync(bookA);
        await _db.DocumentChunks.AddAsync(chunkA);
        await _db.SaveChangesAsync();

        return (userA, userB, bookA, chunkA);
    }

    [Fact]
    public async Task GetBookById_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new GetBookByIdHandler(_db);

        // User B requesting User A's book
        var result = await handler.ExecuteAsync(new GetBookByIdRequest(bookA.Id, UserId: userB.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);

        // User A requesting own book
        var successResult = await handler.ExecuteAsync(new GetBookByIdRequest(bookA.Id, UserId: userA.Id));
        successResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookSlice_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new GetBookSliceHandler(_db);

        // User B requesting slice of User A's book
        var result = await handler.ExecuteAsync(new GetBookSliceRequest(bookA.Id, ChunkOrder: 1, UserId: userB.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);

        // User A requesting own slice
        var successResult = await handler.ExecuteAsync(new GetBookSliceRequest(bookA.Id, ChunkOrder: 1, UserId: userA.Id));
        successResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookStatus_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new GetBookStatusHandler(_db, new GetBookStatusValidator());

        var result = await handler.ExecuteAsync(new GetBookStatusRequest(bookA.Id, UserId: userB.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task ExportBookMarkdown_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new ExportBookMarkdownHandler(_db);

        var result = await handler.ExecuteAsync(new ExportBookMarkdownRequest(bookA.Id, UserId: userB.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task CurateSlice_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new CurateSliceHandler(_db, new FakeAiFormatter(), NullLogger<CurateSliceHandler>.Instance);

        var result = await handler.ExecuteAsync(new CurateSliceRequest(bookA.Id, ChunkOrder: 1, UserId: userB.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task SynthesizeAudio_WhenCalledByAnotherUser_ReturnsNotFound()
    {
        var (userA, userB, _, chunkA) = await SeedTwoUsersWithBookAAsync();
        var handler = new GetOrSynthesizeChunkAudioHandler(_db, new FakeTtsService(), NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);

        var request = new SynthesizeChunkAudioRequest(chunkA.Id, VoiceId: "en-US-Neural2-A", UserId: userB.Id);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task SwitchBook_WhenCalledWithAnotherUsersBook_ReturnsNotFound()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new SwitchBookHandler(_db, new SwitchBookValidator());

        var result = await handler.ExecuteAsync(new SwitchBookRequest(UserId: userB.Id, BookId: bookA.Id));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task GenerateQuiz_GroundedOnAnotherUsersBook_ReturnsForbidden()
    {
        var (userA, userB, bookA, _) = await SeedTwoUsersWithBookAAsync();
        var handler = new GenerateQuizHandler(_db, new FakeQuizGenerator(), new GenerateQuizValidator());

        var request = new GenerateQuizRequest(
            UserId: userB.Id,
            Topic: "General",
            Category: Category.BackendRuntime,
            Level: QuizLevel.Senior,
            Count: 5,
            Locale: "en",
            BookId: bookA.Id,
            IsGrounded: true);

        var result = await handler.ExecuteAsync(request);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Quiz.Forbidden");
    }

    [Fact]
    public async Task KnowledgeGraph_ExcludesAnotherUsersBooks()
    {
        var (userA, userB, bookA, chunkA) = await SeedTwoUsersWithBookAAsync();
        var handler = new GetKnowledgeGraphQueryHandler(_db);

        var query = new GetKnowledgeGraphQuery(userB.Id);
        var result = await handler.ExecuteAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().NotContain(n => n.Id == $"book-{bookA.Id}");
        result.Value.Nodes.Should().NotContain(n => n.Id == $"chunk-{chunkA.Id}");
    }

    [Fact]
    public async Task AdoptedBook_RemainsFullyUsable_AfterSourceCatalogDocumentIsHidden()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "adopter@example.com", Name = "Adopter" };
        var catalogDoc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Source Catalog Book",
            Description = "Catalog desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attr",
            Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 1
        };

        var catalogSlice = new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = catalogDoc.Id,
            SliceOrder = 1,
            ChapterTitle = "Adopted Ch 1",
            OriginalTextMarkdown = "Markdown text",
            SummaryMarkdown = "Summary",
            IsAiFormatted = true
        };
        catalogDoc.Slices.Add(catalogSlice);

        // User adopts the book
        var adoptedBook = DocumentBook.CreateFromCatalogSnapshot(catalogDoc, user.Id, "adopted-book-slug");

        await _db.Users.AddAsync(user);
        await _db.CatalogDocuments.AddAsync(catalogDoc);
        await _db.DocumentBooks.AddAsync(adoptedBook);
        await _db.SaveChangesAsync();

        // Administrator hides the source catalog document
        catalogDoc.Hide(Guid.NewGuid());
        await _db.SaveChangesAsync();

        // Learner can still read the owned book
        var getBookHandler = new GetBookByIdHandler(_db);
        var bookResult = await getBookHandler.ExecuteAsync(new GetBookByIdRequest(adoptedBook.Id, UserId: user.Id));
        bookResult.IsSuccess.Should().BeTrue();
        bookResult.Value.Book.Title.Should().Be("Source Catalog Book");

        // Learner can still read the slice
        var getSliceHandler = new GetBookSliceHandler(_db);
        var sliceResult = await getSliceHandler.ExecuteAsync(new GetBookSliceRequest(adoptedBook.Id, 1, UserId: user.Id));
        sliceResult.IsSuccess.Should().BeTrue();
        sliceResult.Value.Slice.ChapterTitle.Should().Be("Adopted Ch 1");

        // Learner can export markdown
        var exportHandler = new ExportBookMarkdownHandler(_db);
        var exportResult = await exportHandler.ExecuteAsync(new ExportBookMarkdownRequest(adoptedBook.Id, UserId: user.Id));
        exportResult.IsSuccess.Should().BeTrue();
    }

    private class FakeAiFormatter : IAiMarkdownFormatter
    {
        public Task<Result<AiFormattedSliceResult>> FormatSliceAsync(string rawText, string chapterTitle, string language = "en", Category? category = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AiFormattedSliceResult>.Success(new AiFormattedSliceResult("formatted", "summary", new(), 3)));
        }
    }

    private class FakeTtsService : IGoogleCloudTtsService
    {
        public Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(string text, string voiceId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<GoogleCloudTtsResult>.Success(new GoogleCloudTtsResult(new byte[] { 1, 2, 3 }, text.Length, 1.5)));
        }
    }

    private class FakeQuizGenerator : IQuizGeneratorService
    {
        public Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(
            string topic,
            Category category,
            QuizLevel level,
            int count = 5,
            List<string>? referenceTextChunks = null,
            string language = "en",
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<List<QuizQuestion>>.Success(new List<QuizQuestion>()));
        }
    }
}
