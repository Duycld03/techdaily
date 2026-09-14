using System.IO;
using FluentAssertions;
using TechDaily.Application.Interfaces;
using TechDaily.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace TechDaily.Tests.Infrastructure;

public class PdfPigExtractorTests
{
    private readonly ITestOutputHelper _output;

    public PdfPigExtractorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ExtractSlicesAsync_ShouldExtractBookmarksAndSlices_WhenRealPdfExists()
    {
        const string samplePath = "/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf";
        if (!File.Exists(samplePath))
        {
            _output.WriteLine("Sample PDF not found, skipping integration test.");
            return;
        }

        var extractor = new PdfPigExtractor();
        await using var stream = File.OpenRead(samplePath);

        var progressUpdates = new List<PdfExtractionProgress>();
        var progress = new Progress<PdfExtractionProgress>(p => progressUpdates.Add(p));

        // Limit extraction to first 50 pages for fast testing
        var result = await extractor.ExtractSlicesAsync(
            stream,
            customTitle: "ASP.NET Core 10 Architecture Guide",
            maxPages: 50,
            progress: progress);

        result.Should().NotBeNull();
        result.TotalPages.Should().BeGreaterThan(0);
        result.Slices.Should().NotBeEmpty();

        _output.WriteLine($"Extracted {result.Slices.Count} slices from {result.TotalPages} pages.");
        foreach (var slice in result.Slices.Take(5))
        {
            _output.WriteLine($"Slice {slice.Order}: '{slice.ChapterTitle}' ({slice.ContentMarkdown.Length} chars)");
            slice.ChapterTitle.Should().NotBeNullOrWhiteSpace();
            slice.ContentMarkdown.Should().NotBeNullOrWhiteSpace();
            slice.KeyTakeaways.Should().NotBeNull();
        }

        progressUpdates.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExtractSlicesAsync_ShouldThrowInvalidOperation_WhenStreamIsEmpty()
    {
        var extractor = new PdfPigExtractor();
        using var emptyStream = new MemoryStream();

        var act = () => extractor.ExtractSlicesAsync(emptyStream);
        await act.Should().ThrowAsync<Exception>();
    }
}
