using System.Net;
using FluentAssertions;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class WebArticleCrawlerSecurityTests
{
    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://localhost:5000/api/v1/secrets")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://172.20.0.2/db")]
    [InlineData("http://192.168.1.1/gateway")]
    [InlineData("http://backend:5000/metrics")]
    [InlineData("http://db:5432")]
    public async Task CrawlUrlAsync_ShouldBlockInternalAndPrivateAddresses(string internalUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();
        var crawler = new WebArticleCrawler(httpClient);

        // Act & Assert
        var act = async () => await crawler.CrawlUrlAsync(internalUrl);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
