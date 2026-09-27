using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.SynthesizeAudio;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetOrSynthesizeChunkAudioHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    private class MockGoogleCloudTtsService : IGoogleCloudTtsService
    {
        public int CallCount { get; private set; }
        public string? LastText { get; private set; }
        public string? LastVoiceId { get; private set; }
        public Result<GoogleCloudTtsResult> ResultToReturn { get; set; } =
            Result<GoogleCloudTtsResult>.Success(new GoogleCloudTtsResult([1, 2, 3, 4], 50, 3.5));

        public Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(
            string text,
            string voiceId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastText = text;
            LastVoiceId = voiceId;
            return Task.FromResult(ResultToReturn);
        }
    }

    public GetOrSynthesizeChunkAudioHandlerTests()
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
    public async Task ExecuteAsync_WhenCacheHit_ReturnsCachedAudioWithoutCallingTts()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Test Book",
            Slug = "test-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Cached narration sentence.",
            SummaryMarkdown = "Summary",
            Language = "vi",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);

        const string voiceId = "vi-VN-Neural2-A";
        var contentHash = GetOrSynthesizeChunkAudioHandler.ComputeSha256Hex(
            GetOrSynthesizeChunkAudioHandler.ExtractNarrationScript(chunk.OriginalTextMarkdown));

        var cachedAudio = new DocumentChunkAudio
        {
            DocumentChunkId = chunk.Id,
            ContentHash = contentHash,
            VoiceId = voiceId,
            MimeType = "audio/mpeg",
            AudioData = [10, 20, 30],
            CharacterCount = 25,
            DurationSeconds = 2.0
        };
        await _db.DocumentChunkAudios.AddAsync(cachedAudio);
        await _db.SaveChangesAsync();

        var mockTts = new MockGoogleCloudTtsService();
        var handler = new GetOrSynthesizeChunkAudioHandler(_db, mockTts, NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);

        var request = new SynthesizeChunkAudioRequest(chunk.Id, voiceId, contentHash);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCacheHit.Should().BeTrue();
        result.Value.AudioBytes.Should().Equal([10, 20, 30]);
        mockTts.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheMiss_SynthesizesStoresInDbAndReturns()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Test Book",
            Slug = "test-book-2",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "New narration sentence to synthesize.",
            SummaryMarkdown = "Summary",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        const string voiceId = "en-US-Neural2-F";
        var mockTts = new MockGoogleCloudTtsService
        {
            ResultToReturn = Result<GoogleCloudTtsResult>.Success(
                new GoogleCloudTtsResult([99, 88, 77], 37, 2.5))
        };

        var handler = new GetOrSynthesizeChunkAudioHandler(_db, mockTts, NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);
        var request = new SynthesizeChunkAudioRequest(chunk.Id, voiceId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCacheHit.Should().BeFalse();
        result.Value.AudioBytes.Should().Equal([99, 88, 77]);
        mockTts.CallCount.Should().Be(1);

        var savedInDb = await _db.DocumentChunkAudios
            .FirstOrDefaultAsync(a => a.DocumentChunkId == chunk.Id && a.VoiceId == voiceId);
        savedInDb.Should().NotBeNull();
        savedInDb!.AudioData.Should().Equal([99, 88, 77]);
        savedInDb.CharacterCount.Should().Be(37);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNarrationScriptExceedsFiveThousandBytes_SuccessfullySynthesizesAndPersistsConcatenatedAudio()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Thói Quen Nguyên Tử",
            Slug = "thoi-quen-nguyen-tu",
            Category = Category.EngineeringCraft,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        // Generate markdown with > 5,000 bytes of Vietnamese text
        var longParagraph = "Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt. ";
        var sb = new StringBuilder();
        while (Encoding.UTF8.GetByteCount(sb.ToString()) < 5500)
        {
            sb.AppendLine(longParagraph);
        }
        var longMarkdown = sb.ToString();

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Sức Mạnh Của Những Thay Đổi Nhỏ",
            OriginalTextMarkdown = longMarkdown,
            SummaryMarkdown = "Tóm tắt chương sách",
            Language = "vi",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        const string voiceId = "vi-VN-Neural2-A";
        var expectedBytes = new byte[] { 0xFF, 0xFB, 0x90, 0x64, 0x11, 0x22, 0x33, 0x44 };
        var mockTts = new MockGoogleCloudTtsService
        {
            ResultToReturn = Result<GoogleCloudTtsResult>.Success(
                new GoogleCloudTtsResult(expectedBytes, 2500, 185.4))
        };

        var handler = new GetOrSynthesizeChunkAudioHandler(_db, mockTts, NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);
        var request = new SynthesizeChunkAudioRequest(chunk.Id, voiceId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCacheHit.Should().BeFalse();
        result.Value.CharacterCount.Should().Be(2500);
        result.Value.DurationSeconds.Should().Be(185.4);
        result.Value.AudioBytes.Should().Equal(expectedBytes);
        mockTts.CallCount.Should().Be(1);
        mockTts.LastVoiceId.Should().Be(voiceId);

        var savedInDb = await _db.DocumentChunkAudios
            .FirstOrDefaultAsync(a => a.DocumentChunkId == chunk.Id && a.VoiceId == voiceId);
        savedInDb.Should().NotBeNull();
        savedInDb!.AudioData.Should().Equal(expectedBytes);
        savedInDb.CharacterCount.Should().Be(2500);
        savedInDb.DurationSeconds.Should().Be(185.4);
    }

    [Fact]
    public async Task ExecuteAsync_WhenQuotaExhausted_ReturnsAudioQuotaExhaustedWithoutCallingTts()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Test Book",
            Slug = "test-book-3",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Narration exceeding quota.",
            SummaryMarkdown = "Summary",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);

        // Seed 950,000 characters used this month
        var heavyAudio = new DocumentChunkAudio
        {
            DocumentChunkId = chunk.Id,
            ContentHash = "previous-hash",
            VoiceId = "vi-VN-Neural2-A",
            MimeType = "audio/mpeg",
            AudioData = [1],
            CharacterCount = 950_000,
            DurationSeconds = 1000,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _db.DocumentChunkAudios.AddAsync(heavyAudio);
        await _db.SaveChangesAsync();

        var mockTts = new MockGoogleCloudTtsService();
        var handler = new GetOrSynthesizeChunkAudioHandler(_db, mockTts, NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);

        var request = new SynthesizeChunkAudioRequest(chunk.Id, "en-US-Neural2-D");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(Error.AudioQuotaExhausted.Code);
        mockTts.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAudioQuota_CalculatesAccurateMonthlyUsageAndRemaining()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Quota Test Book",
            Slug = "quota-test-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Markdown",
            SummaryMarkdown = "Summary",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var audio1 = new DocumentChunkAudio
        {
            DocumentChunkId = chunk.Id,
            ContentHash = "h1",
            VoiceId = "v1",
            AudioData = [1],
            CharacterCount = 500_000,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var audio2 = new DocumentChunkAudio
        {
            DocumentChunkId = chunk.Id,
            ContentHash = "h2",
            VoiceId = "v2",
            AudioData = [2],
            CharacterCount = 420_000,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _db.DocumentChunkAudios.AddRangeAsync(audio1, audio2);
        await _db.SaveChangesAsync();

        var quotaHandler = new GetAudioQuotaHandler(_db);

        // Act
        var quotaResult = await quotaHandler.ExecuteAsync(new GetAudioQuotaRequest());

        // Assert
        quotaResult.IsSuccess.Should().BeTrue();
        quotaResult.Value.MonthlyLimit.Should().Be(950_000);
        quotaResult.Value.UsedCharacters.Should().Be(920_000);
        quotaResult.Value.RemainingCharacters.Should().Be(30_000);
        quotaResult.Value.IsNearLimit.Should().BeTrue();
        quotaResult.Value.IsExhausted.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenVoiceLanguageMismatchesChunk_ReturnsVoiceLanguageMismatchError()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "English Book",
            Slug = "english-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var enChunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "This is English documentation.",
            Language = "en",
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(enChunk);
        await _db.SaveChangesAsync();

        var ttsMock = new MockGoogleCloudTtsService();
        var handler = new GetOrSynthesizeChunkAudioHandler(_db, ttsMock, NullLogger<GetOrSynthesizeChunkAudioHandler>.Instance);

        // Act: request Vietnamese voice on English chunk
        var request = new SynthesizeChunkAudioRequest(enChunk.Id, "vi-VN-Neural2-A", null, "This is English documentation.");
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("VOICE_LANGUAGE_MISMATCH");
        ttsMock.CallCount.Should().Be(0);
    }
}
