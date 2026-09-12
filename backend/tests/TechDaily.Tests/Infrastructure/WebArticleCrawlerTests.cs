using FluentAssertions;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class WebArticleCrawlerTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseHtml;

        public FakeHttpMessageHandler(string responseHtml)
        {
            _responseHtml = responseHtml;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_responseHtml, System.Text.Encoding.UTF8, "text/html")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task CrawlUrlAsync_ShouldFilterOutNonMatchingMonikers_WhenViewQueryParameterProvided()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head><title>Routing in ASP.NET Core</title></head>
<body>
<article>
    <h1>Routing in ASP.NET Core</h1>
    <div data-moniker=""aspnetcore-10.0 aspnetcore-9.0"">
        <h2>Routing basics (.NET 10)</h2>
        <p>This is the modern routing documentation for .NET 10.</p>
        <pre><code class=""lang-csharp"">var app = builder.Build();</code></pre>
    </div>
    <div data-moniker=""aspnetcore-6.0"">
        <h2>Routing basics (.NET 6)</h2>
        <p>This is legacy routing documentation for .NET 6.</p>
        <pre><code class=""lang-csharp"">app.UseRouting();</code></pre>
    </div>
    <div data-moniker=""aspnetcore-3.1"">
        <h2>Routing basics (.NET Core 3.1)</h2>
        <p>This is obsolete routing documentation for .NET Core 3.1.</p>
    </div>
</article>
</body>
</html>";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(html));
        var crawler = new WebArticleCrawler(httpClient);

        // Act
        var result = await crawler.CrawlUrlAsync("https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0");

        // Assert
        result.MarkdownContent.Should().Contain("Routing basics (.NET 10)");
        result.MarkdownContent.Should().Contain("modern routing documentation for .NET 10");
        result.MarkdownContent.Should().NotContain("Routing basics (.NET 6)");
        result.MarkdownContent.Should().NotContain("legacy routing documentation for .NET 6");
        result.MarkdownContent.Should().NotContain("Routing basics (.NET Core 3.1)");
    }

    [Fact]
    public async Task CrawlUrlAsync_ShouldNormalizeTxtCodeBlocks_WithLanguageTxt()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head><title>CLI Output Sample</title></head>
<body>
<article>
    <h1>CLI Output Sample</h1>
    <p>Output from running the command:</p>
    <pre><code class=""lang-txt"">1. Endpoint: (null)
2. Endpoint: Hello</code></pre>
</article>
</body>
</html>";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(html));
        var crawler = new WebArticleCrawler(httpClient);

        // Act
        var result = await crawler.CrawlUrlAsync("https://example.com/docs/cli-output");

        // Assert
        result.MarkdownContent.Should().Contain("```txt");
        result.MarkdownContent.Should().Contain("1. Endpoint: (null)");
        result.MarkdownContent.Should().Contain("2. Endpoint: Hello");
    }
}
