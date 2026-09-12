# Proposal: Document Relative Link Resolution & External Link Tab Isolation

## Problem
When crawling technical documentation (such as Microsoft Learn, official framework docs, or GitHub readmes), source HTML pages frequently use relative URLs for navigation:
- Peer documents: `<a href="dependency-injection?view=aspnetcore-10.0">DI</a>`
- Root-relative API references: `<a href="/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointdatasource">EndpointDataSource</a>`
- Parent-directory guides: `<a href="../mvc/controllers/routing">Routing</a>`

When `WebArticleCrawler` converts this HTML into Markdown using `ReverseMarkdown`, it preserves relative URLs verbatim:
`[DI](dependency-injection?view=aspnetcore-10.0)`

When rendered in TechDaily reader at `/read/[bookId]?slice=7`, clicking any relative link causes the browser to resolve it relative to the current route (`/read/`):
- Clicking `DI` navigates internally to `/read/dependency-injection?view=aspnetcore-10.0`.
- The reader interprets `dependency-injection` as a `bookId`, which does not exist in the database, resulting in a blank screen (`Technical Document`, 0/0 slices).
- Clicking root-relative links like `/en-us/dotnet/api/...` hits TechDaily's host resulting in a 404 error.
- Furthermore, markdown links currently navigate within the same tab, which unexpectedly kicks the user out of their reading session.

## Proposed Solution
Implement a robust two-layer resolution and presentation strategy:

1. **Crawler Layer (`WebArticleCrawler.cs`):**
   - When crawling a web article with a known `targetUrl`, preprocess all `<a>` and `<img>` nodes before converting to Markdown.
   - Resolve relative `href` and `src` attributes against the document's absolute base URI (`new Uri(baseUri, relativePath).AbsoluteUri`), except for in-page section bookmarks (`#anchor`).
   - All generated Markdown links will point directly to the authoritative original documentation.

2. **Renderer Layer (`useMarkdownRenderer.ts`):**
   - Add a custom `link_open` rule in MarkdownIt.
   - Automatically inject `target="_blank" rel="noopener noreferrer"` into all external links so clicking documentation references opens them in a new browser tab without abandoning the user's reading session on TechDaily.
   - For in-page anchor links (`href="#..."`), keep internal smooth scrolling behavior.

## User Impact
- Clicking hyperlinks inside technical articles opens the official reference docs in a new tab.
- TechDaily reader never breaks into blank screen errors or 404 pages caused by relative links.
- Reading flow, bookmarks, and active recall slice state remain completely uninterrupted.
