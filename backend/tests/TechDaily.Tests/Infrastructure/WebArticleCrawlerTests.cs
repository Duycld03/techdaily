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

    [Fact]
    public async Task CrawlUrlAsync_ShouldResolveRelativeUrls_AgainstDocumentSourceUrl()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head><title>ASP.NET Core Routing</title></head>
<body>
<article>
    <h1>Routing</h1>
    <p>See <a href=""dependency-injection?view=aspnetcore-10.0"">DI</a> for services.</p>
    <p>Read the <a href=""/en-us/dotnet/api/endpoint"">Endpoint API</a> reference.</p>
    <p>Check out <a href=""../mvc/controllers/routing"">MVC Controllers</a> guide.</p>
    <p>Jump to <a href=""#routing-basics"">Routing Basics Section</a>.</p>
    <img src=""../images/arch.png"" alt=""Architecture Diagram"" />
</article>
</body>
</html>";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(html));
        var crawler = new WebArticleCrawler(httpClient);

        // Act
        var result = await crawler.CrawlUrlAsync("https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0");

        // Assert
        // Relative peer doc
        result.MarkdownContent.Should().Contain("[DI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)");
        // Root-relative link
        result.MarkdownContent.Should().Contain("[Endpoint API](https://learn.microsoft.com/en-us/dotnet/api/endpoint)");
        // Parent-directory relative link
        result.MarkdownContent.Should().Contain("[MVC Controllers](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing)");
        // In-page section fragment bookmark resolved with canonical source URL
        result.MarkdownContent.Should().Contain("[Routing Basics Section](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#routing-basics)");
        // Image source resolved
        result.MarkdownContent.Should().Contain("![Architecture Diagram](https://learn.microsoft.com/en-us/aspnet/core/images/arch.png)");
    }

    [Fact]
    public async Task CrawlUrlAsync_ShouldFilterHiddenTabsAndUnauthorizedTemplates()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head><title>Minimal API</title></head>
<body>
<article>
    <div unauthorized-private-section hidden>
        <p>Access to this page requires authorization.</p>
    </div>
    <a href=""#"" hidden>Read in English</a>
    <h1>Tutorial: Create a Minimal API with ASP.NET Core</h1>
    <div class=""tabGroup"" id=""tabgroup_1"">
        <ul role=""tablist"">
            <li role=""presentation""><a href=""#tabpanel_1_visual-studio"" role=""tab"">Visual Studio</a></li>
            <li role=""presentation""><a href=""#tabpanel_1_visual-studio-code"" role=""tab"">Visual Studio Code</a></li>
        </ul>
        <section id=""tabpanel_1_visual-studio"" role=""tabpanel"" data-tab=""visual-studio"">
            <p>Visual Studio instructions: Open Visual Studio and create project.</p>
        </section>
        <section id=""tabpanel_1_visual-studio-code"" role=""tabpanel"" data-tab=""visual-studio-code"" aria-hidden=""true"" hidden=""hidden"">
            <p>Visual Studio Code instructions: Run dotnet new webapi.</p>
        </section>
    </div>
</article>
</body>
</html>";

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(html));
        var crawler = new WebArticleCrawler(httpClient);

        // Act
        var result = await crawler.CrawlUrlAsync("https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio");

        // Assert
        result.MarkdownContent.Should().Contain("Visual Studio instructions");
        result.MarkdownContent.Should().NotContain("Visual Studio Code instructions");
        result.MarkdownContent.Should().NotContain("Access to this page requires authorization");
        result.MarkdownContent.Should().NotContain("Read in English");
        result.MarkdownContent.Should().NotContain("[Visual Studio]");
        result.MarkdownContent.Should().NotContain("[Visual Studio Code]");
    }
}
