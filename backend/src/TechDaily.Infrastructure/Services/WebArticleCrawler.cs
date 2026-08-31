using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class WebArticleCrawler : IWebArticleCrawler
{
    private readonly HttpClient _httpClient;

    public WebArticleCrawler(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TechDaily-Crawler/1.0 (Senior Engineering Micro-Learning; +https://techdaily.dev)");
        }
    }

    public async Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format.", nameof(url));
        }

        // 1. Auto-resolve GitHub blob URLs to raw user content
        var targetUrl = ResolveGitHubRawUrl(url);

        using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // 2. Direct Markdown / Plain Text Response
        if (contentType.Contains("markdown") || contentType.Contains("text/plain") || targetUrl.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            var title = ExtractMarkdownTitle(rawContent, targetUrl);
            var words = rawContent.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return new CrawlArticleResult(
                Title: title,
                SourceUrl: url,
                MarkdownContent: rawContent.Trim(),
                EstimatedWordCount: words
            );
        }

        // 3. HTML Page Processing with HtmlAgilityPack & ReverseMarkdown
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(rawContent);

        var pageTitle = ExtractHtmlTitle(htmlDoc, targetUrl);

        // Find best content node
        var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//article")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//main")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'markdown-body')]")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'post-content')]")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'article-content')]")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'content')]")
            ?? htmlDoc.DocumentNode.SelectSingleNode("//body");

        if (contentNode == null)
        {
            throw new InvalidOperationException("Could not extract readable article content from the web page.");
        }

        // Remove junk elements: script, style, nav, footer, aside, noscript, svg, form
        var junkNodes = contentNode.SelectNodes(".//script|.//style|.//nav|.//footer|.//aside|.//header|.//noscript|.//svg|.//form|.//button|.//iframe");
        if (junkNodes != null)
        {
            foreach (var junk in junkNodes)
            {
                junk.Remove();
            }
        }

        // Convert cleaned HTML to Markdown
        var converter = new Converter(new Config
        {
            UnknownTags = Config.UnknownTagsOption.Drop,
            GithubFlavored = true,
            RemoveComments = true
        });

        var markdown = converter.Convert(contentNode.InnerHtml);
        markdown = CleanMarkdown(markdown);

        if (string.IsNullOrWhiteSpace(markdown) || markdown.Length < 40)
        {
            throw new InvalidOperationException("Extracted article content was empty or unreadable.");
        }

        var wordCount = markdown.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return new CrawlArticleResult(
            Title: pageTitle,
            SourceUrl: url,
            MarkdownContent: markdown,
            EstimatedWordCount: wordCount
        );
    }

    private static string ResolveGitHubRawUrl(string url)
    {
        // Convert https://github.com/{user}/{repo}/blob/{branch}/{path} -> https://raw.githubusercontent.com/{user}/{repo}/{branch}/{path}
        var match = Regex.Match(url, @"^https?://github\.com/([^/]+)/([^/]+)/blob/([^/]+)/(.*)$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var user = match.Groups[1].Value;
            var repo = match.Groups[2].Value;
            var branch = match.Groups[3].Value;
            var path = match.Groups[4].Value;
            return $"https://raw.githubusercontent.com/{user}/{repo}/{branch}/{path}";
        }
        return url;
    }

    private static string ExtractMarkdownTitle(string markdown, string url)
    {
        var firstLine = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(l => l.StartsWith('#'));

        if (!string.IsNullOrWhiteSpace(firstLine))
        {
            return firstLine.TrimStart('#').Trim();
        }

        var uri = new Uri(url);
        var filename = Path.GetFileNameWithoutExtension(uri.LocalPath);
        return !string.IsNullOrWhiteSpace(filename) ? filename.Replace('-', ' ').Replace('_', ' ') : "Imported Technical Document";
    }

    private static string ExtractHtmlTitle(HtmlDocument doc, string url)
    {
        // 1. Try OpenGraph Title
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null);
        if (!string.IsNullOrWhiteSpace(ogTitle))
        {
            return CleanHtmlString(ogTitle);
        }

        // 2. Try <title>
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (!string.IsNullOrWhiteSpace(titleNode?.InnerText))
        {
            var cleaned = CleanHtmlString(titleNode.InnerText);
            // Split common suffix " - Microsoft Learn" or " | Medium"
            var parts = cleaned.Split(new[] { " - ", " | ", " — " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0].Trim() : cleaned;
        }

        // 3. Fallback to <h1>
        var h1 = doc.DocumentNode.SelectSingleNode("//h1");
        if (!string.IsNullOrWhiteSpace(h1?.InnerText))
        {
            return CleanHtmlString(h1.InnerText);
        }

        var uri = new Uri(url);
        return uri.Host + uri.AbsolutePath;
    }

    private static string CleanHtmlString(string text)
    {
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static string CleanMarkdown(string markdown)
    {
        // Collapse excessive newlines (more than 2)
        var normalized = Regex.Replace(markdown, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }
}
