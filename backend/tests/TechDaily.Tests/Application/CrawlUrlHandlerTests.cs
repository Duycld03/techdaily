using FluentAssertions;
using TechDaily.Application.Features.Library.CrawlUrl;
using TechDaily.Application.Interfaces;
using Xunit;

namespace TechDaily.Tests.Application;

public class CrawlUrlHandlerTests
{
    [Fact]
    public async Task CrawlUrl_ShouldReturnExtractedMarkdown_WhenValidUrlProvided()
    {
        // Arrange
        var mockCrawler = new MockCrawler(new CrawlArticleResult(
            Title: "Designing Data-Intensive Applications Summary",
            SourceUrl: "https://example.com/ddia",
            MarkdownContent: "# DDIA\nReplication and Partitioning notes.",
            EstimatedWordCount: 50
        ));

        var validator = new CrawlUrlValidator();
        var handler = new CrawlUrlHandler(mockCrawler, validator);

        var request = new CrawlUrlRequest("https://example.com/ddia");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Designing Data-Intensive Applications Summary");
        result.Value.MarkdownContent.Should().Contain("Replication");
    }

    [Fact]
    public async Task CrawlUrl_ShouldFailValidation_WhenUrlIsInvalid()
    {
        // Arrange
        var mockCrawler = new MockCrawler(new CrawlArticleResult("", "", "", 0));
        var validator = new CrawlUrlValidator();
        var handler = new CrawlUrlHandler(mockCrawler, validator);

        var request = new CrawlUrlRequest("invalid-not-a-url");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    private class MockCrawler : IWebArticleCrawler
    {
        private readonly CrawlArticleResult _result;

        public MockCrawler(CrawlArticleResult result)
        {
            _result = result;
        }

        public Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}
