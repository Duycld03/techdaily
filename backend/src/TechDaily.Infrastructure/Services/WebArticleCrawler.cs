using System.Text;
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
    }

    public async Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        }

        // 1. Resolve raw GitHub URLs if needed
        var targetUrl = ResolveGitHubRawUrl(url);

        using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 TechDailyCrawler/1.0");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.7");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // 2. Direct Markdown / Plaintext File Handling
        if (targetUrl.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            targetUrl.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            response.Content.Headers.ContentType?.MediaType?.Equals("text/plain", StringComparison.OrdinalIgnoreCase) == true)
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

        // Remove junk elements: script, style, nav, footer, aside, noscript, svg, form, buttons
        var junkNodes = contentNode.SelectNodes(".//script|.//style|.//nav|.//footer|.//aside|.//header|.//noscript|.//svg|.//form|.//button|.//iframe|.//feedback|.//div[contains(@class, 'feedback')]");
        if (junkNodes != null)
        {
            foreach (var junk in junkNodes)
            {
                junk.Remove();
            }
        }

        // Preprocess Code Blocks to ensure syntax highlighting preservation
        PreprocessCodeBlocks(contentNode);

        // Preprocess Alert / Callout Boxes
        PreprocessAlertBoxes(contentNode);

        // Convert cleaned HTML to Markdown
        var converter = new Converter(new Config
        {
            UnknownTags = Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
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

    private static void PreprocessCodeBlocks(HtmlNode root)
    {
        var preNodes = root.SelectNodes(".//pre");
        if (preNodes == null) return;

        foreach (var pre in preNodes)
        {
            var codeNode = pre.SelectSingleNode(".//code") ?? pre;
            var classAttr = codeNode.GetAttributeValue("class", "") + " " + pre.GetAttributeValue("class", "") + " " + pre.GetAttributeValue("data-lang", "");

            var langMatch = Regex.Match(classAttr, @"(?:lang|language|highlight)-([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            var lang = langMatch.Success ? langMatch.Groups[1].Value.ToLowerInvariant() : "";

            // Normalize common aliases
            if (lang == "csharp" || lang == "cs" || lang == "dotnet") lang = "csharp";
            else if (lang == "javascript" || lang == "js") lang = "javascript";
            else if (lang == "typescript" || lang == "ts") lang = "typescript";
            else if (lang == "python" || lang == "py") lang = "python";
            else if (lang == "shell" || lang == "sh" || lang == "terminal") lang = "bash";
            else if (lang == "yml") lang = "yaml";

            if (!string.IsNullOrEmpty(lang))
            {
                codeNode.SetAttributeValue("class", $"language-{lang}");
            }
        }
    }

    private static void PreprocessAlertBoxes(HtmlNode root)
    {
        var alertNodes = root.SelectNodes(".//div[contains(@class, 'NOTE') or contains(@class, 'TIP') or contains(@class, 'WARNING') or contains(@class, 'CAUTION') or contains(@class, 'alert')]");
        if (alertNodes == null) return;

        foreach (var alert in alertNodes)
        {
            var alertClass = alert.GetAttributeValue("class", "").ToUpperInvariant();
            string alertType = "NOTE";
            if (alertClass.Contains("TIP")) alertType = "TIP";
            else if (alertClass.Contains("WARNING")) alertType = "WARNING";
            else if (alertClass.Contains("CAUTION") || alertClass.Contains("DANGER")) alertType = "CAUTION";
            else if (alertClass.Contains("IMPORTANT")) alertType = "IMPORTANT";

            var blockquote = HtmlNode.CreateNode($"<blockquote><p><strong>[{alertType}]</strong> {alert.InnerHtml}</p></blockquote>");
            alert.ParentNode.ReplaceChild(blockquote, alert);
        }
    }

    private static string ResolveGitHubRawUrl(string url)
    {
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
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null);
        if (!string.IsNullOrWhiteSpace(ogTitle))
        {
            return CleanHtmlString(ogTitle);
        }

        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (!string.IsNullOrWhiteSpace(titleNode?.InnerText))
        {
            var cleaned = CleanHtmlString(titleNode.InnerText);
            var parts = cleaned.Split(new[] { " - ", " | ", " — " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0].Trim() : cleaned;
        }

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
        // Fix escaped brackets and excessive newlines
        var cleaned = markdown.Replace(@"\[", "[").Replace(@"\]", "]");
        cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
        return cleaned.Trim();
    }
}
