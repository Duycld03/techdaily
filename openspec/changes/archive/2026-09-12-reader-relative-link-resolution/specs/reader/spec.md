## ADDED Requirements

### Requirement: Document Relative Link Resolution
During technical article crawling and import, the crawler SHALL resolve all relative anchor hyperlinks (`<a href="...">`) and images (`<img src="...">`) against the document's canonical source URL into absolute URLs, with the exception of same-page fragment bookmarks (`#...`).

#### Scenario: Crawler processes relative document link
- **WHEN** the crawler encounters an anchor tag with a relative path such as `href="dependency-injection?view=aspnetcore-10.0"` while crawling `https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0`
- **THEN** the crawler resolves the link to `https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0` before generating Markdown.

#### Scenario: Crawler processes root-relative API reference link
- **WHEN** the crawler encounters `href="/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointdatasource"`
- **THEN** the crawler resolves the link to `https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.endpointdatasource`.

#### Scenario: Crawler resolves in-page section bookmarks
- **WHEN** the crawler encounters an in-page fragment bookmark such as `href="#routing-basics"`
- **THEN** the crawler resolves the bookmark against the canonical source URL so the markdown anchor preserves the target destination on the original documentation.

---

### Requirement: Isolated External Link Navigation
The reader markdown renderer SHALL configure anchor rendering such that all external documentation links automatically include `target="_blank"` and `rel="noopener noreferrer"`. Clicking any external link inside an article slice SHALL open the external reference in a new browser tab without replacing or disrupting the active TechDaily reading session.

#### Scenario: User clicks external reference link in reader pane
- **WHEN** user clicks on an external documentation link rendered in markdown (e.g. `[DI](https://learn.microsoft.com/...)`)
- **THEN** the link opens in a separate browser tab with secure `noopener noreferrer` attributes, and the TechDaily reading route `/read/[bookId]` remains open at the current slice.

#### Scenario: In-page anchor navigation
- **WHEN** user clicks a table-of-contents or anchor link targeting a heading on the same page (e.g. `[Basics](#routing-basics)`)
- **THEN** the link does not open a new tab and instead scrolls smoothly to the target element within the active reading pane.
