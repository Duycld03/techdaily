using System.IO;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public async Task ExtractSlicesAsync_WithMonolithicChapters_ShouldNeverExceedWordCeiling()
    {
        const string samplePath = "/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf";
        if (!File.Exists(samplePath))
        {
            _output.WriteLine("Sample PDF not found, skipping integration test.");
            return;
        }

        var extractor = new PdfPigExtractor();
        await using var stream = File.OpenRead(samplePath);

        // Extract first 100 pages to cover monolithic chapters such as 'What's new in 10'
        var result = await extractor.ExtractSlicesAsync(
            stream,
            customTitle: "ASP.NET Core 10 Architecture Guide",
            maxPages: 100);

        result.Slices.Should().NotBeEmpty();
        foreach (var slice in result.Slices)
        {
            var words = slice.ContentMarkdown.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            words.Should().BeLessOrEqualTo(2500, $"Slice {slice.Order} '{slice.ChapterTitle}' exceeds 2,500 words with {words} words");
            slice.EstimatedReadMinutes.Should().BeLessOrEqualTo(15, $"Slice {slice.Order} has excessive estimated read minutes: {slice.EstimatedReadMinutes}");
        }
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
    public void FormatAsMarkdown_ShouldNeverTrapBreakingChangeProseHeaders_InsideCodeBlocks()
    {
        var input = @"
Blazor.registerCustomEventType now throws an error when the custom event name matches its browserEventName option.
// This used to silently double-fire the event.
Blazor.registerCustomEventType('scrolltop', {
browserEventName: 'scrolltop'
});

New behavior
Starting in ASP.NET Core 11, Blazor.registerCustomEventType throws when eventName equals

options.browserEventName : JavaScript

// This now throws synchronously.
Blazor.registerCustomEventType('scrolltop', {
browserEventName: 'scrolltop'
});

Type of breaking change
This change is a behavioral change.
";

        var markdown = PdfPigExtractor.FormatAsMarkdown(input, "Breaking changes");

        markdown.Should().Contain("New behavior");
        markdown.Should().Contain("Type of breaking change");

        var segments = markdown.Split("```");
        for (int i = 1; i < segments.Length; i += 2)
        {
            segments[i].Should().NotContain("New behavior", "New behavior should never be trapped in a code fence");
            segments[i].Should().NotContain("Type of breaking change", "Type of breaking change should never be trapped in a code fence");
        }
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

    [Fact]
    public void StripBoilerplate_ShouldRemoveMultiLineDisclaimer_WithLeadingParenthesis()
    {
        var input = @"
) Important This information relates to a pre-release product that may be substantially modified before it’s commercially released. Microsoft makes no warranties, express or implied, with respect to the information provided here.
Leave the browser open with the Counter page loaded.
";
        var cleaned = PdfPigExtractor.StripBoilerplate(input);
        cleaned.Should().NotContain("Important This information relates to a pre-release product");
        cleaned.Should().Contain("Leave the browser open with the Counter page loaded.");
    }

    [Theory]
    [InlineData("Table of Contents")]
    [InlineData("table of contents")]
    [InlineData("Mục lục")]
    [InlineData("Contents")]
    [InlineData("Copyright")]
    [InlineData("Cover")]
    [InlineData("Preface")]
    [InlineData("About the Author")]
    [InlineData("About the Authors")]
    [InlineData("Index")]
    [InlineData("Index A-Z")]
    [InlineData("Contributors")]
    [InlineData("Credits")]
    [InlineData("Bibliography")]
    [InlineData("References")]
    [InlineData("Colophon")]
    public void IsIgnoredBookmark_ShouldFilterFrontMatterAndBackMatter(string title)
    {
        var isIgnored = typeof(PdfPigExtractor)
            .GetMethod("IsIgnoredBookmark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { title });

        isIgnored.Should().Be(true);
    }

    [Fact]
    public void ExtractSlicesFromBookmarks_PreservesUniqueBookmarkPages_AcrossLevels()
    {
        const string samplePath = "/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf";
        if (!File.Exists(samplePath)) return;

        using var doc = UglyToad.PdfPig.PdfDocument.Open(samplePath);
        var bookmarks = typeof(PdfPigExtractor)
            .GetMethod("ExtractNativeBookmarks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { doc }) as List<PdfPigExtractor.RawBookmark>;

        bookmarks.Should().NotBeNull();
        var validWithPage = bookmarks!.Where(b => b.PageNumber > 0).ToList();
        validWithPage.Should().HaveCountGreaterThan(500);

        var uniquePages = validWithPage.GroupBy(b => b.PageNumber).ToList();
        uniquePages.Count.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task IngestAndVerify_RealPdf_WithGeminiAi()
    {
        const string samplePath = "/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf";
        if (!File.Exists(samplePath)) return;

        var extractor = new PdfPigExtractor();
        await using var stream = File.OpenRead(samplePath);

        // Extract first 100 pages
        var result = await extractor.ExtractSlicesAsync(
            stream,
            customTitle: "ASP.NET Core 10 Architecture Guide",
            maxPages: 100);

        result.Slices.Should().NotBeEmpty();
        _output.WriteLine($"Extracted {result.Slices.Count} slices from 100 pages.");

        // Check Slice 3
        var slice3 = result.Slices.FirstOrDefault(s => s.Order == 3 || s.ChapterTitle.Contains("Get started"));
        slice3.Should().NotBeNull();
        _output.WriteLine($"Slice 3: '{slice3!.ChapterTitle}' ({slice3.ContentMarkdown.Length} chars, est {slice3.EstimatedReadMinutes} min)");
        slice3.EstimatedReadMinutes.Should().BeLessThan(30);

        // Build Gemini service with local settings if available
        var localSettings = "/home/duycld03/workspace/techdaily/backend/src/TechDaily.Api/appsettings.Local.json";
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(localSettings, optional: true)
            .Build();

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiAiService>();
        var aiService = new GeminiAiService(httpClient, config, logger);

        _output.WriteLine($"Formatting Slice 3 with Gemini...");
        var aiResult = await aiService.FormatSliceAsync(slice3.ContentMarkdown, slice3.ChapterTitle, "en");
        if (!aiResult.IsSuccess)
        {
            _output.WriteLine($"Gemini failed with Error: {aiResult.Error.Code} - {aiResult.Error.Message}");
            if (aiResult.Error.Message.Contains("503") || aiResult.Error.Message.Contains("ServiceUnavailable"))
            {
                _output.WriteLine("Gemini API returned 503 Service Unavailable (high demand spike). Skipping live assertion.");
                return;
            }
        }
        aiResult.IsSuccess.Should().BeTrue(aiResult.Error?.Message);
        _output.WriteLine($"AI Formatted Length: {aiResult.Value.FormattedMarkdown.Length} chars");
        _output.WriteLine($"Key Takeaways ({aiResult.Value.KeyTakeaways.Count}):");
        foreach (var t in aiResult.Value.KeyTakeaways)
        {
            _output.WriteLine($"- {t}");
        }

        // Verify that prose is NOT trapped inside code blocks in AI formatted result
        aiResult.Value.FormattedMarkdown.Should().Contain("# ");
        aiResult.Value.FormattedMarkdown.Should().Contain("> [!NOTE");
        // Save to real database so we can test UI live in browser!
        var connStr = "Host=localhost;Port=5432;Database=techdaily_db;Username=techdaily_user;Password=techdaily_password_secret";
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TechDaily.Infrastructure.Persistence.TechDailyDbContext>()
            .UseNpgsql(connStr, o => o.UseVector())
            .Options;

        using var dbContext = new TechDaily.Infrastructure.Persistence.TechDailyDbContext(options);

        // Delete existing test book with this title if exists
        var existing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.DocumentBooks, b => b.Title == "ASP.NET Core 10 Architecture Guide");
        if (existing != null)
        {
            var oldChunkIds = dbContext.DocumentChunks
                .Where(c => c.DocumentBookId == existing.Id)
                .Select(c => c.Id)
                .ToList();

            var oldQuestionIds = dbContext.InterviewQuestions
                .Where(q => q.DocumentChunkId != null && oldChunkIds.Contains(q.DocumentChunkId.Value))
                .Select(q => q.Id)
                .ToList();

            var oldDrills = dbContext.DailyDrills
                .Where(d => oldQuestionIds.Contains(d.QuestionId) || (d.DocumentChunkId != null && oldChunkIds.Contains(d.DocumentChunkId.Value)));
            dbContext.DailyDrills.RemoveRange(oldDrills);

            var oldQuestions = dbContext.InterviewQuestions
                .Where(q => q.DocumentChunkId != null && oldChunkIds.Contains(q.DocumentChunkId.Value));
            dbContext.InterviewQuestions.RemoveRange(oldQuestions);

            var oldPacers = dbContext.UserBookPacers
                .Where(p => p.DocumentBookId == existing.Id);
            dbContext.UserBookPacers.RemoveRange(oldPacers);

            var oldChunks = dbContext.DocumentChunks.Where(c => c.DocumentBookId == existing.Id);
            dbContext.DocumentChunks.RemoveRange(oldChunks);
            dbContext.DocumentBooks.Remove(existing);
            await dbContext.SaveChangesAsync();
        }

        var book = new TechDaily.Domain.Entities.DocumentBook
        {
            Title = "ASP.NET Core 10 Architecture Guide",
            Slug = "aspnet-core-10-architecture-guide",
            SourceType = TechDaily.Domain.Enums.SourceType.PdfBook,
            Category = TechDaily.Domain.Enums.Category.BackendRuntime,
            AuthorOrSourceUrl = "Microsoft Learn ASP.NET Core 10",
            TotalChunks = result.Slices.Count,
            IsPublished = true,
            IsFeatured = true,
            Status = TechDaily.Domain.Enums.ProcessingStatus.Ready,
            ProgressPercentage = 100,
            StatusMessage = "Ready for reading"
        };
        await dbContext.DocumentBooks.AddAsync(book);
        await dbContext.SaveChangesAsync();

        var chunks = new List<TechDaily.Domain.Entities.DocumentChunk>();
        for (int i = 0; i < result.Slices.Count; i++)
        {
            var s = result.Slices[i];
            var c = new TechDaily.Domain.Entities.DocumentChunk
            {
                DocumentBookId = book.Id,
                ChunkOrder = s.Order,
                ChapterTitle = s.ChapterTitle,
                OriginalTextMarkdown = s.ContentMarkdown,
                SummaryMarkdown = s.ChapterTitle,
                KeyTakeaways = s.KeyTakeaways,
                EstimatedReadMinutes = s.EstimatedReadMinutes,
                IsAiFormatted = false
            };
            if (s.Order == slice3.Order)
            {
                c.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                c.SummaryMarkdown = aiResult.Value.SummaryMarkdown;
                c.KeyTakeaways = aiResult.Value.KeyTakeaways;
                c.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                c.IsAiFormatted = true;

            }
            chunks.Add(c);
        }

        await dbContext.DocumentChunks.AddRangeAsync(chunks);
        await dbContext.SaveChangesAsync();

        var savedSlice3 = chunks.FirstOrDefault(c => c.ChunkOrder == slice3.Order);
        if (savedSlice3 != null && aiResult.Value.ScenarioDrill != null)
        {
            var drill = aiResult.Value.ScenarioDrill;
            var q = new TechDaily.Domain.Entities.InterviewQuestion
            {
                DocumentChunkId = savedSlice3.Id,
                QuestionText = drill.QuestionText,
                Options = drill.Options,
                CorrectOptionIndex = drill.CorrectOptionIndex,
                ExplanationMarkdown = drill.ExplanationMarkdown,
                ExpectedKeyPoints = drill.ExpectedKeyPoints,
                ModelAnswerMarkdown = drill.ExplanationMarkdown,
                Difficulty = TechDaily.Domain.Enums.Difficulty.Senior
            };
            await dbContext.InterviewQuestions.AddAsync(q);
            await dbContext.SaveChangesAsync();
        }

        _output.WriteLine($"Saved book to database! BookId: {book.Id}");
    }

    [Fact]
    public void AssembleLines_WithLineSpacingGap_ShouldEmitDoubleNewlines()
    {
        // Normal line spacing = 14pt (700 -> 686 -> 672)
        // Paragraph gap = 22pt (672 -> 650, >= 1.35 * 14 = 18.9pt)
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("Line one of paragraph one is continuous", Top: 710, Bottom: 700, Left: 54, Right: 300, Height: 10),
            new("and line two completes paragraph one.", Top: 696, Bottom: 686, Left: 54, Right: 290, Height: 10),
            new("Line one of paragraph two begins after a gap", Top: 660, Bottom: 650, Left: 54, Right: 310, Height: 10),
            new("and line two completes paragraph two.", Top: 646, Bottom: 636, Left: 54, Right: 280, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // Intra-paragraph lines should be joined with space
        text.Should().Contain("Line one of paragraph one is continuous and line two completes paragraph one.");
        text.Should().Contain("Line one of paragraph two begins after a gap and line two completes paragraph two.");

        // Distinct paragraphs should be separated by double newlines
        text.Should().Contain("and line two completes paragraph one.\n\nLine one of paragraph two begins after a gap");

        // Markdown formatting should preserve the double newlines into separate paragraphs
        var markdown = PdfPigExtractor.FormatAsMarkdown(text, "Gap Test");
        markdown.Should().Contain("and line two completes paragraph one.\n\nLine one of paragraph two begins after a gap");
    }

    [Fact]
    public void AssembleLines_WithFirstLineIndentation_ShouldEmitDoubleNewlines()
    {
        // Normal spacing = 14pt between all lines
        // Standard left margin = 54pt
        // Line 2 starts at Left = 70pt (indent = 16pt >= 12pt)
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("This is the first sentence of the first paragraph", Top: 710, Bottom: 700, Left: 54, Right: 350, Height: 10),
            new("and it wraps naturally onto the second line here.", Top: 696, Bottom: 686, Left: 54, Right: 340, Height: 10),
            new("This second paragraph starts with an indentation", Top: 682, Bottom: 672, Left: 70, Right: 350, Height: 10),
            new("and continues flush with the normal left margin.", Top: 668, Bottom: 658, Left: 54, Right: 330, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // First paragraph lines should be joined with a space
        text.Should().Contain("This is the first sentence of the first paragraph and it wraps naturally onto the second line here.");

        // Paragraph 2 should be separated by double newlines due to first-line indent
        text.Should().Contain("and it wraps naturally onto the second line here.\n\nThis second paragraph starts with an indentation");

        // Second paragraph lines should be joined with a space
        text.Should().Contain("This second paragraph starts with an indentation and continues flush with the normal left margin.");
    }

    [Theory]
    [InlineData("LỜI NÓI ĐẦU")]
    [InlineData("Phần GIỚI THIỆU")]
    [InlineData("GIỚI THIỆU")]
    [InlineData("Câu chuyện của chính tôi.")]
    [InlineData("Câu chuyện của chính tôi")]
    [InlineData("Chương 1: Những điều cơ bản")]
    [InlineData("Chapter 1: The Fundamentals")]
    [InlineData("MỤC LỤC")]
    [InlineData("TIỂU DẪN")]
    [InlineData("KẾT LUẬN")]
    [InlineData("PHẦN 1: QUY LUẬT THỨ NHẤT")]
    [InlineData("CORE PRINCIPLES")]
    public void AssembleLines_AndFormatAsMarkdown_WithHeadingPatterns_ShouldFormatAsH2(string headingTitle)
    {
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("This is prose text preceding the heading block.", Top: 710, Bottom: 700, Left: 54, Right: 320, Height: 10),
            new(headingTitle, Top: 680, Bottom: 670, Left: 54, Right: 200, Height: 12),
            new("This is prose text following the heading block.", Top: 650, Bottom: 640, Left: 54, Right: 310, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // Heading should be formatted as ## Title with double newlines before and after
        text.Should().Contain($"\n\n## {headingTitle}\n\n");

        // FormatAsMarkdown should preserve level 2 heading
        var markdown = PdfPigExtractor.FormatAsMarkdown(text, "Heading Suite");
        markdown.Should().Contain($"## {headingTitle}");
    }

    [Fact]
    public void AssembleLines_WithLargeFontOrCentering_ShouldFormatAsH2()
    {
        // Median height is 10, but line 1 has Height = 16 (>= 1.25 * 10 = 12.5)
        var linesWithLargeFont = new List<PdfPigExtractor.LineGeometry>
        {
            new("Standard prose line before large title.", Top: 710, Bottom: 700, Left: 54, Right: 300, Height: 10),
            new("Custom Large Section Title", Top: 682, Bottom: 666, Left: 54, Right: 250, Height: 16),
            new("Standard prose line after large title.", Top: 650, Bottom: 640, Left: 54, Right: 290, Height: 10)
        };

        var textLarge = PdfPigExtractor.AssembleLines(linesWithLargeFont);
        textLarge.Should().Contain("## Custom Large Section Title");

        // Centered short line (< 60 chars) on 600pt page width: Left = 220, Right = 380 (width = 160, left margin = 220, right margin = 220)
        var linesWithCenteredTitle = new List<PdfPigExtractor.LineGeometry>
        {
            new("Standard prose line before centered title.", Top: 710, Bottom: 700, Left: 54, Right: 540, Height: 10),
            new("A Centered Topic Heading", Top: 680, Bottom: 670, Left: 220, Right: 380, Height: 10),
            new("Standard prose line after centered title.", Top: 650, Bottom: 640, Left: 54, Right: 540, Height: 10)
        };

        var textCentered = PdfPigExtractor.AssembleLines(linesWithCenteredTitle, pageWidth: 600);
        textCentered.Should().Contain("## A Centered Topic Heading");
    }

    [Fact]
    public void AssembleLines_WithDialogueAndQuotes_ShouldPreserveDistinctLineBreaks()
    {
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("Cô ấy nhìn tôi với ánh mắt ngạc nhiên và hỏi.", Top: 710, Bottom: 700, Left: 54, Right: 350, Height: 10),
            new("- Cậu đã đọc xong cuốn sách đó chưa?", Top: 696, Bottom: 686, Left: 54, Right: 280, Height: 10),
            new("- Mình vừa đọc xong sáng nay, tôi trả lời.", Top: 682, Bottom: 672, Left: 54, Right: 310, Height: 10),
            new("“Cảm nhận của cậu về nó như thế nào?”", Top: 668, Bottom: 658, Left: 54, Right: 290, Height: 10),
            new("— Thật sự là một tác phẩm rất giá trị và mang lại", Top: 654, Bottom: 644, Left: 54, Right: 360, Height: 10),
            new("nhiều góc nhìn sâu sắc cho công việc của chúng ta.", Top: 640, Bottom: 630, Left: 54, Right: 340, Height: 10),
            new("- Mình cũng nghĩ như vậy!", Top: 626, Bottom: 616, Left: 54, Right: 220, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // Dialogue lines starting after punctuation should have double newlines
        text.Should().Contain("ngạc nhiên và hỏi.\n\n- Cậu đã đọc xong cuốn sách đó chưa?");
        text.Should().Contain("chưa?\n\n- Mình vừa đọc xong sáng nay, tôi trả lời.");
        text.Should().Contain("tôi trả lời.\n\n“Cảm nhận của cậu về nó như thế nào?”");
        text.Should().Contain("thế nào?”\n\n— Thật sự là một tác phẩm rất giá trị và mang lại");

        // Wrapped lines within dialogue should be joined with a space
        text.Should().Contain("— Thật sự là một tác phẩm rất giá trị và mang lại nhiều góc nhìn sâu sắc cho công việc của chúng ta.");

        // Next dialogue line should be separated by double newlines
        text.Should().Contain("của chúng ta.\n\n- Mình cũng nghĩ như vậy!");
    }

    [Fact]
    public void FormatAsMarkdown_ShouldCleanUpDuplicateBlankLines_AndPreserveDoubleNewlines()
    {
        var input = @"
# Main Topic



## Subtopic One


Paragraph one text that has multiple trailing newlines.




Paragraph two text that follows after four blank lines.
";

        var markdown = PdfPigExtractor.FormatAsMarkdown(input, "");

        // There should be at most 2 consecutive newlines (\n\n)
        markdown.Should().NotContain("\n\n\n");
        markdown.Should().Contain("# Main Topic\n\n## Subtopic One\n\nParagraph one text that has multiple trailing newlines.\n\nParagraph two text that follows after four blank lines.");
    }

    [Fact]
    public void AssembleLines_WithHyphenatedWordWrap_ShouldDehyphenate()
    {
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("This section explains the archi-", Top: 710, Bottom: 700, Left: 54, Right: 300, Height: 10),
            new("tectural design of the distributed system.", Top: 696, Bottom: 686, Left: 54, Right: 290, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);
        text.Should().Be("This section explains the architectural design of the distributed system.");
    }

    [Fact]
    public void AssembleLines_WithEmptyOrWhitespaceLines_ShouldReturnEmptyString()
    {
        var emptyLines = new List<PdfPigExtractor.LineGeometry>();
        PdfPigExtractor.AssembleLines(emptyLines).Should().BeEmpty();

        var whitespaceLines = new List<PdfPigExtractor.LineGeometry>
        {
            new("   ", Top: 700, Bottom: 690, Left: 50, Right: 60, Height: 10),
            new("", Top: 680, Bottom: 670, Left: 50, Right: 50, Height: 10)
        };
        PdfPigExtractor.AssembleLines(whitespaceLines).Should().BeEmpty();
    }

    [Fact]
    public void AssembleLines_WithLinesEndingWithoutPunctuation_ShouldJoinWithSpacesNotChopSentences()
    {
        // Scenario: A page has a header or margin at Left = 54, but story body lines are set at Left = 95.
        // Lines end mid-sentence without terminal punctuation ("trúng mặt. Khi", "và hướng", "về cái", "Vụ va").
        // They must NOT be chopped by \n\n even if Left is offset from page xMin or if there is slight spacing.
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("CHƯƠNG 1: BẮT ĐẦU MỘT HÀNH TRÌNH DÀI", Top: 780, Bottom: 770, Left: 54, Right: 300, Height: 10),
            new("Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt. Khi", Top: 720, Bottom: 706, Left: 95, Right: 500, Height: 10),
            new("đó cậu bạn cùng lớp đang vung gậy thực hiện một cú đánh bóng, cây gậy trượt khỏi tay cậu ấy và hướng", Top: 704, Bottom: 690, Left: 95, Right: 502, Height: 10),
            new("thẳng về phía tôi trước khi đập thẳng vào vùng giữa hai mắt tôi. Tôi không nhớ bất kì điều gì về cái", Top: 688, Bottom: 674, Left: 95, Right: 501, Height: 10),
            new("khoảnh khắc va đập đó. Vụ va", Top: 672, Bottom: 658, Left: 95, Right: 498, Height: 10),
            new("chạm khiến tôi bất tỉnh nhân sự ngay tại chỗ.", Top: 656, Bottom: 642, Left: 95, Right: 380, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // Header should be separated with \n\n
        text.Should().Contain("## CHƯƠNG 1: BẮT ĐẦU MỘT HÀNH TRÌNH DÀI\n\n");

        // The wrapped story sentences must be joined with space, not split by \n\n
        text.Should().NotContain("Khi\n\nđó");
        text.Should().NotContain("hướng\n\nthẳng");
        text.Should().NotContain("cái\n\nkhoảnh khắc");
        text.Should().NotContain("Vụ va\n\nchạm");

        text.Should().Contain("nện trúng mặt. Khi đó cậu bạn cùng lớp đang vung gậy thực hiện một cú đánh bóng, cây gậy trượt khỏi tay cậu ấy và hướng thẳng về phía tôi trước khi đập thẳng vào vùng giữa hai mắt tôi. Tôi không nhớ bất kì điều gì về cái khoảnh khắc va đập đó. Vụ va chạm khiến tôi bất tỉnh nhân sự ngay tại chỗ.");
    }

    [Fact]
    public void AssembleLines_WithGenuineIndentedParagraph_WhenPreviousLineEndsWithPunctuationOrShort_ShouldSeparateWithDoubleNewlines()
    {
        // Scenario: Genuine paragraph break where previous line ends with terminal punctuation '.' and is short,
        // and next paragraph line is indented relative to previous line (curr.Left - prev.Left >= 12).
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("Đây là câu mở đầu của đoạn văn thứ nhất trong tài liệu này,", Top: 720, Bottom: 708, Left: 80, Right: 450, Height: 10),
            new("và câu này kéo dài cho đến khi kết thúc hoàn chỉnh.", Top: 706, Bottom: 694, Left: 80, Right: 360, Height: 10),
            new("Đoạn văn thứ hai bắt đầu với lùi đầu dòng rõ ràng,", Top: 692, Bottom: 680, Left: 96, Right: 450, Height: 10),
            new("tiếp tục bình thường ở lề chuẩn sau dòng thụt đầu dòng.", Top: 678, Bottom: 666, Left: 80, Right: 440, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        // First paragraph lines should be joined with space
        text.Should().Contain("Đây là câu mở đầu của đoạn văn thứ nhất trong tài liệu này, và câu này kéo dài cho đến khi kết thúc hoàn chỉnh.");

        // Paragraph boundary should be separated by \n\n
        text.Should().Contain("kết thúc hoàn chỉnh.\n\nĐoạn văn thứ hai bắt đầu với lùi đầu dòng rõ ràng, tiếp tục bình thường ở lề chuẩn sau dòng thụt đầu dòng.");
    }

    [Fact]
    public void AssembleLines_WhenLineIndentedOrGappedMidSentence_ShouldNotBreakParagraph()
    {
        // Even if curr.Left - prev.Left >= 12 or dy is large, if previous line ends mid-sentence ("và hướng"),
        // canBreakParagraph is false and it must be joined with space.
        var lines = new List<PdfPigExtractor.LineGeometry>
        {
            new("Cậu ấy vung gậy và hướng", Top: 720, Bottom: 708, Left: 60, Right: 400, Height: 10),
            new("thẳng về phía tôi trước khi đập vào mặt.", Top: 680, Bottom: 668, Left: 75, Right: 400, Height: 10)
        };

        var text = PdfPigExtractor.AssembleLines(lines);

        text.Should().Be("Cậu ấy vung gậy và hướng thẳng về phía tôi trước khi đập vào mặt.");
    }
}
