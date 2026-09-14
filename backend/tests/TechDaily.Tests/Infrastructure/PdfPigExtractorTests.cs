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
    public void FormatAsMarkdown_ShouldIsolateCodeBlocks_AndNeverTrapProseExplanations()
    {
        var input = @"
Change the app
Leave the browser open with the Counter page loaded. By using the dotnet watch command to run the app, you can make changes to the app's markup.
Components/Pages/Counter.razor :
@page ""/counter""
<PageTitle>Counter</PageTitle>
<h1>Counter</h1>
<p role=""status"">Current count: @currentCount</p>
<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>
@code {
    private int currentCount = 0;
    private void IncrementCount()
    {
        currentCount++;
    }
}
The Counter component renders the Counter web page. An H1 heading is displayed.
";

        var markdown = PdfPigExtractor.FormatAsMarkdown(input, "Get started");

        // The heading should be rendered cleanly
        markdown.Should().Contain("# Get started");

        // Instructions and filenames should be prose, NOT trapped inside code blocks
        markdown.Should().Contain("Leave the browser open with the Counter page loaded.");
        markdown.Should().Contain("Components/Pages/Counter.razor :");
        markdown.Should().Contain("The Counter component renders the Counter web page.");

        // The code fence should cleanly contain the razor code
        markdown.Should().Contain("```csharp");
        markdown.Should().Contain("@page \"/counter\"");
        markdown.Should().Contain("currentCount++;");

        // The trailing explanation should be outside code block
        var parts = markdown.Split("```");
        // parts[0] is prose before code, parts[1] is code, parts[2] is prose after code
        parts.Length.Should().BeGreaterOrEqualTo(3);
        parts[2].Should().Contain("The Counter component renders the Counter web page.");
    }

    [Fact]
    public void StripBoilerplate_ShouldRemovePreReleaseDisclaimer_AndDates()
    {
        var input = @"
### 07/30/2025
Important This information relates to a pre-release product that may be substantially modified before it's commercially released. Microsoft makes no warranties, express or implied, with respect to the information provided here. For the current release, see the .NET 9 version of this article.
This tutorial shows how to create, run, and modify an ASP.NET Core Blazor Web App.
";

        var cleaned = PdfPigExtractor.StripBoilerplate(input);

        cleaned.Should().NotContain("Important This information relates to a pre-release product");
        cleaned.Should().NotContain("07/30/2025");
        cleaned.Should().Contain("This tutorial shows how to create, run, and modify an ASP.NET Core Blazor Web App.");
    }

    [Fact]
    public void FormatCuratedTitle_ShouldPrefixParentModule_WhenAppropriate()
    {
        // Level 2 topic under Fundamentals inherits parent module
        var bm1 = new PdfPigExtractor.RawBookmark("Dependency injection", 42, 2, "Fundamentals");
        PdfPigExtractor.FormatCuratedTitle(bm1, 1).Should().Be("Fundamentals: Dependency injection");

        // Generic title inherits parent
        var bm2 = new PdfPigExtractor.RawBookmark("Overview", 10, 2, "Fundamentals");
        PdfPigExtractor.FormatCuratedTitle(bm2, 2).Should().Be("Fundamentals: Overview");

        // Root book title should not be prefixed
        var bm3 = new PdfPigExtractor.RawBookmark("Fundamentals", 5, 1, "ASP.NET Core documentation");
        PdfPigExtractor.FormatCuratedTitle(bm3, 3).Should().Be("Fundamentals");

        // Already prefixed title should not be duplicated
        var bm4 = new PdfPigExtractor.RawBookmark("Blazor: Components", 99, 2, "Blazor");
        PdfPigExtractor.FormatCuratedTitle(bm4, 4).Should().Be("Blazor: Components");
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
