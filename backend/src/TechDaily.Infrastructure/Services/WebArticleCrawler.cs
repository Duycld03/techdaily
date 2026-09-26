using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown;
using TechDaily.Application.Interfaces;
using TechDaily.Application.Common;
using TechDaily.Domain.Enums;

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

        // SSRF Defense: Validate URL host and IP against loopback, private subnets, and cloud metadata
        var (validatedUri, resolvedIps) = UrlSecurityValidator.ValidateSafeUrl(targetUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, validatedUri);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 TechDailyCrawler/1.0");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.7");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
        {
            throw new InvalidOperationException("HTTP redirects are not permitted for article crawling.");
        }
        response.EnsureSuccessStatusCode();
        // 2. Direct PDF Document Handling
        if (targetUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            response.Content.Headers.ContentType?.MediaType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true ||
            response.Content.Headers.ContentType?.MediaType?.Equals("application/x-pdf", StringComparison.OrdinalIgnoreCase) == true)
        {
            var uri = new Uri(targetUrl);
            var fileName = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
            var title = !string.IsNullOrWhiteSpace(fileName)
                ? WebUtility.UrlDecode(fileName).Replace('-', ' ').Replace('_', ' ')
                : "PDF Document";
            var description = $"# {title}\n\nA direct PDF document was detected at: [{targetUrl}]({targetUrl}).\n\nUse the Remote PDF Ingestion feature to download and slice this document into reading chapters.";
            var directWordCount = description.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return new CrawlArticleResult(
                Title: title,
                SourceUrl: url,
                MarkdownContent: description,
                EstimatedWordCount: directWordCount,
                IsPdfDetected: true,
                DetectedPdfUrl: targetUrl
            );
        }

        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // 3. Embedded PDF Sniffing (PDF.js, iframes, embeds, Google Docs viewer)
        var (isPdfDetected, detectedPdfUrl) = SniffEmbeddedPdf(rawContent, targetUrl);
        if (isPdfDetected && !string.IsNullOrWhiteSpace(detectedPdfUrl))
        {
            UrlSecurityValidator.ValidateSafeUrl(detectedPdfUrl);
        }

        // 4. Direct Markdown / Plaintext File Handling
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
            if (isPdfDetected && !string.IsNullOrWhiteSpace(detectedPdfUrl))
            {
                return CreatePdfFallbackResult(pageTitle, detectedPdfUrl, url);
            }
            throw new InvalidOperationException("Could not extract readable article content from the web page.");
        }

        // Remove junk elements: script, style, nav, footer, aside, noscript, svg, form, buttons, hidden templates, tab switchers, edit buttons, and feedback widgets
        var junkNodes = contentNode.SelectNodes(".//script|.//style|.//nav|.//footer|.//aside|.//header|.//noscript|.//svg|.//form|.//button|.//iframe|.//feedback|.//div[contains(@class, 'feedback')]|.//*[@hidden]|.//*[@aria-hidden='true' and (self::section or self::div or self::p)]|.//ul[@role='tablist']|.//a[@data-contenteditbtn]|.//*[@data-bi-name='edit']|.//*[contains(@data-bi-name, 'feedback')]|.//*[contains(@id, 'feedback')]");
        if (junkNodes != null)
        {
            foreach (var junk in junkNodes)
            {
                junk.Remove();
            }
        }

        // Filter multi-version moniker sections (e.g. Microsoft Learn ASP.NET Core documentation)
        FilterMonikers(contentNode, targetUrl);

        // Resolve relative links and images against source document URL
        ResolveRelativeUrls(contentNode, targetUrl);

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
            SmartHrefHandling = false
        });

        var markdown = converter.Convert(contentNode.InnerHtml);
        markdown = CleanMarkdown(markdown);

        if (string.IsNullOrWhiteSpace(markdown) || markdown.Length < 40)
        {
            if (isPdfDetected && !string.IsNullOrWhiteSpace(detectedPdfUrl))
            {
                return CreatePdfFallbackResult(pageTitle, detectedPdfUrl, url);
            }
            throw new InvalidOperationException("Extracted article content was empty or unreadable.");
        }

        var wordCount = markdown.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return new CrawlArticleResult(
            Title: pageTitle,
            SourceUrl: url,
            MarkdownContent: markdown,
            EstimatedWordCount: wordCount,
            IsPdfDetected: isPdfDetected,
            DetectedPdfUrl: detectedPdfUrl
        );
    }

    private static void FilterMonikers(HtmlNode root, string url)
    {
        var monikerNodes = root.SelectNodes(".//*[@data-moniker]");
        if (monikerNodes == null || monikerNodes.Count == 0) return;

        string? targetView = null;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            targetView = query["view"]?.Trim();
        }

        if (string.IsNullOrWhiteSpace(targetView))
        {
            var allMonikers = monikerNodes
                .Select(n => n.GetAttributeValue("data-moniker", ""))
                .SelectMany(m => m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .OrderByDescending(m => m)
                .ToList();

            targetView = allMonikers.FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(targetView))
        {
            var nodesToRemove = new List<HtmlNode>();
            foreach (var node in monikerNodes)
            {
                var attr = node.GetAttributeValue("data-moniker", "");
                var monikers = attr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                bool matches = monikers.Any(m => m.Equals(targetView, StringComparison.OrdinalIgnoreCase) ||
                                                 m.Contains(targetView, StringComparison.OrdinalIgnoreCase) ||
                                                 targetView.Contains(m, StringComparison.OrdinalIgnoreCase));
                if (!matches)
                {
                    nodesToRemove.Add(node);
                }
            }

            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }
    }

    private static void ResolveRelativeUrls(HtmlNode root, string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var baseUri))
        {
            return;
        }

        // 1. Resolve anchor links (<a href="...">)
        var anchorNodes = root.SelectNodes(".//a[@href]");
        if (anchorNodes != null)
        {
            foreach (var a in anchorNodes)
            {
                var href = a.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrEmpty(href) ||
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Uri.TryCreate(baseUri, href, out var resolvedUri))
                {
                    a.SetAttributeValue("href", resolvedUri.AbsoluteUri);
                }
            }
        }

        // 2. Resolve image sources (<img src="...">)
        var imgNodes = root.SelectNodes(".//img[@src]");
        if (imgNodes != null)
        {
            foreach (var img in imgNodes)
            {
                var src = img.GetAttributeValue("src", "").Trim();
                if (string.IsNullOrEmpty(src) ||
                    src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Uri.TryCreate(baseUri, src, out var resolvedUri))
                {
                    img.SetAttributeValue("src", resolvedUri.AbsoluteUri);
                }
            }
        }
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
            else if (lang == "txt" || lang == "text" || lang == "plaintext" || lang == "output" || lang == "console") lang = "txt";

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

            // Strip redundant duplicate title elements (e.g. <p class="alert-title">Warning</p> or <p>Warning</p>)
            var titleNode = alert.SelectSingleNode(".//p[contains(@class, 'title') or contains(@class, 'alert')]")
                ?? alert.SelectSingleNode(".//span[contains(@class, 'title')]")
                ?? alert.SelectSingleNode(".//div[contains(@class, 'title')]");

            if (titleNode != null && (titleNode.InnerText.Trim().Equals(alertType, StringComparison.OrdinalIgnoreCase) ||
                                      titleNode.InnerText.Trim().Equals("Caution", StringComparison.OrdinalIgnoreCase) ||
                                      titleNode.InnerText.Trim().Equals("Warning", StringComparison.OrdinalIgnoreCase) ||
                                      titleNode.InnerText.Trim().Equals("Note", StringComparison.OrdinalIgnoreCase) ||
                                      titleNode.InnerText.Trim().Equals("Tip", StringComparison.OrdinalIgnoreCase) ||
                                      titleNode.InnerText.Trim().Equals("Important", StringComparison.OrdinalIgnoreCase)))
            {
                titleNode.Remove();
            }
            else
            {
                var firstP = alert.SelectSingleNode(".//p");
                if (firstP != null && (firstP.InnerText.Trim().Equals(alertType, StringComparison.OrdinalIgnoreCase) ||
                                       firstP.InnerText.Trim().Equals("Caution", StringComparison.OrdinalIgnoreCase) ||
                                       firstP.InnerText.Trim().Equals("Warning", StringComparison.OrdinalIgnoreCase) ||
                                       firstP.InnerText.Trim().Equals("Note", StringComparison.OrdinalIgnoreCase) ||
                                       firstP.InnerText.Trim().Equals("Tip", StringComparison.OrdinalIgnoreCase) ||
                                       firstP.InnerText.Trim().Equals("Important", StringComparison.OrdinalIgnoreCase)))
                {
                    firstP.Remove();
                }
            }

            var blockquote = HtmlNode.CreateNode($"<blockquote><p>[!{alertType}]</p>{alert.InnerHtml}</blockquote>");
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

    public static Category InferCategoryFromContext(string title, string url, string? content = null)
    {
        var combined = $"{title} {url} {content}".ToLowerInvariant();

        if (combined.Contains("aspnet") || combined.Contains("aspnetcore") ||
            combined.Contains("dotnet") || combined.Contains(".net") ||
            combined.Contains("csharp") || combined.Contains("c#") ||
            combined.Contains("entityframework") || combined.Contains("efcore") ||
            combined.Contains("golang") || combined.Contains("rust") ||
            combined.Contains("spring") || combined.Contains("jvm") ||
            combined.Contains("concurrency") || combined.Contains("goroutine") ||
            combined.Contains("multithreading") || combined.Contains("garbage collection") ||
            combined.Contains("async io") || combined.Contains("kestrel") ||
            combined.Contains("tokio") || combined.Contains("backend") ||
            combined.Contains("runtime"))
        {
            return Category.BackendRuntime;
        }

        if (combined.Contains("postgres") || combined.Contains("postgresql") ||
            combined.Contains("redis") || combined.Contains("mysql") ||
            combined.Contains("mongodb") || combined.Contains("database") ||
            combined.Contains("sql") || combined.Contains("storage engine"))
        {
            return Category.DatabaseStorage;
        }

        if (combined.Contains("system design") || combined.Contains("distributed") ||
            combined.Contains("microservice") || combined.Contains("kafka") ||
            combined.Contains("kubernetes") || combined.Contains("docker") ||
            combined.Contains("outbox") || combined.Contains("event sourcing"))
        {
            return Category.SystemDesign;
        }

        if (combined.Contains("atomic habits") || combined.Contains("deep work") ||
            combined.Contains("pragmatic") || combined.Contains("mindset") ||
            combined.Contains("productivity") || combined.Contains("leadership") ||
            combined.Contains("soft skills"))
        {
            return Category.EngineeringCraft;
        }

        if (combined.Contains("vue") || combined.Contains("react") ||
            combined.Contains("angular") || combined.Contains("frontend") ||
            combined.Contains("browser") || combined.Contains("css") ||
            combined.Contains("html") || combined.Contains("dom") ||
            combined.Contains("javascript") || combined.Contains("typescript"))
        {
            return Category.FrontendWeb;
        }

        return Category.EngineeringCraft;
    }


    private static CrawlArticleResult CreatePdfFallbackResult(string pageTitle, string detectedPdfUrl, string sourceUrl)
    {
        var title = !string.IsNullOrWhiteSpace(pageTitle) && !pageTitle.Equals("Web Document", StringComparison.OrdinalIgnoreCase)
            ? pageTitle
            : Path.GetFileNameWithoutExtension(new Uri(detectedPdfUrl).AbsolutePath).Replace('-', ' ').Replace('_', ' ');
        if (string.IsNullOrWhiteSpace(title)) title = "Embedded Technical PDF";

        var description = $"# {title}\n\nAn embedded PDF document was detected at: [{detectedPdfUrl}]({detectedPdfUrl}).\n\nClick **Import & Slice PDF Directly** to download and process this document into reading slices.";
        var wordCount = description.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return new CrawlArticleResult(
            Title: title,
            SourceUrl: sourceUrl,
            MarkdownContent: description,
            EstimatedWordCount: wordCount,
            IsPdfDetected: true,
            DetectedPdfUrl: detectedPdfUrl
        );
    }

    private static (bool IsPdf, string? PdfUrl) SniffEmbeddedPdf(string rawHtml, string baseUrl)
    {
        // a) PDF.js: DEFAULT_URL, pdfDoc, file:
        var pdfJsRegex = new Regex(@"DEFAULT_URL\s*=\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']|pdfDoc\s*=\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']|file:\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var match = pdfJsRegex.Match(rawHtml);
        if (match.Success)
        {
            var matched = match.Groups[1].Success ? match.Groups[1].Value
                : (match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value);
            var resolved = ResolveAbsoluteUrl(baseUrl, matched);
            return (true, resolved);
        }

        // b) <iframe[^>]+src=["']([^"']+\.pdf[^"']*)["']
        var iframeRegex = new Regex(@"<iframe[^>]+src=[""']([^""']+\.pdf(?:\?[^""']*)?)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var iframeMatch = iframeRegex.Match(rawHtml);
        if (iframeMatch.Success)
        {
            var resolved = ResolveAbsoluteUrl(baseUrl, iframeMatch.Groups[1].Value);
            return (true, resolved);
        }

        // c) <embed[^>]+src=["']([^"']+\.pdf[^"']*)["'] or <object[^>]+data=["']([^"']+\.pdf[^"']*)["']
        var embedRegex = new Regex(@"<(?:embed|object)[^>]+(?:src|data)=[""']([^""']+\.pdf(?:\?[^""']*)?)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var embedMatch = embedRegex.Match(rawHtml);
        if (embedMatch.Success)
        {
            var resolved = ResolveAbsoluteUrl(baseUrl, embedMatch.Groups[1].Value);
            return (true, resolved);
        }

        // d) Google Docs/Drive viewer: docs.google.com/viewer\?.*url=([^&"']+)
        var googleDocsRegex = new Regex(@"docs\.google\.com/viewer\?[^""']*url=([^&""']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var googleMatch = googleDocsRegex.Match(rawHtml);
        if (!googleMatch.Success)
        {
            googleMatch = googleDocsRegex.Match(baseUrl);
        }
        if (googleMatch.Success)
        {
            var decoded = WebUtility.UrlDecode(googleMatch.Groups[1].Value);
            var resolved = ResolveAbsoluteUrl(baseUrl, decoded);
            return (true, resolved);
        }

        return (false, null);
    }

    private static string ResolveAbsoluteUrl(string baseUrl, string relativeOrAbsoluteUrl)
    {
        if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var absUri) &&
            (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps))
        {
            return absUri.ToString();
        }
        if (Uri.TryCreate(new Uri(baseUrl), relativeOrAbsoluteUrl, out var combinedUri))
        {
            return combinedUri.ToString();
        }

        return relativeOrAbsoluteUrl;
    }
}
