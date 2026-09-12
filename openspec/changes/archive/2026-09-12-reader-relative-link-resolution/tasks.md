# Tasks: Document Relative Link Resolution & External Link Tab Isolation

- [x] 1. Backend Web Crawler Relative URL Resolution
  - [x] 1.1 Implement `ResolveRelativeUrls(HtmlNode root, string sourceUrl)` in `WebArticleCrawler.cs` resolving relative `href` and `src` attributes against the source document URI.
  - [x] 1.2 Preserve in-page section bookmark fragments (`#...`) and data URIs without alteration.
  - [x] 1.3 Add unit test in `WebArticleCrawlerTests.cs` verifying relative and root-relative URLs are resolved into absolute URLs.

- [x] 2. Frontend MarkdownIt External Link Isolation
  - [x] 2.1 Implement custom `link_open` rule in `useMarkdownRenderer.ts` to attach `target="_blank"` and `rel="noopener noreferrer"` to external `http://` / `https://` links.
  - [x] 2.2 Preserve in-page anchor links (`#...`) without `target="_blank"`.
  - [x] 2.3 Add unit tests in `markdownRenderer.spec.ts` verifying external links include security attributes and new tab behavior.

- [x] 3. Verification & Deployment Readiness
  - [x] 3.1 Run `dotnet test backend/tests/TechDaily.Tests` to ensure 100% backend test pass.
  - [x] 3.2 Run `npm --prefix frontend test` to ensure 100% frontend test pass.
  - [x] 3.3 Run `npm --prefix frontend run build` to verify clean production compilation.
