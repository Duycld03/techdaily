# Design: Universal Engineering Pillars and Smart Web PDF Crawler

## Context

TechDaily is built to give engineers a disciplined daily reading habit through 3–5 minute reading slices, spaced repetition flashcards, and senior scenario drills. The platform supports multiple document ingestion modalities: Markdown series, PDF document streaming, and web documentation URL crawling.

However, four architectural gaps degrade usability and limit platform adoption:

1. **Framework-Coupled Profile Metrics:**
   The Domain Mastery Goal Tracker (`DomainGoalTracker.vue`) on `/profile` visualizes learning progress across four pinned frameworks (`.NET 10 & C# 13 Runtime`, `PostgreSQL 17 Storage Engine`, `System Design & Distributed`, `Frontend & Browser Performance`). The hardcoded keyword matcher (`matchCategory`) only recognizes `.net`, `c#`, `postgres`, and `sql`. Engineers studying Node.js/NestJS, Go, Java/Spring, Python, MongoDB, or Redis see their completed books and quiz scores omitted from their profile radar.

2. **Web Viewer Embedded PDF Failures:**
   When engineers crawl online technical documentation or open-access books hosted on web reader shells (e.g. `thuviensach.vn/pdf/viewer.php?id=...`, Mozilla PDF.js viewer scripts, or pages embedding an `<iframe>` or `<embed>` pointing to a PDF), `WebArticleCrawler` strips scripts and iframes. It then fails with an empty document error because no `<article>` or `<main>` text content exists in the viewer shell. Furthermore, users cannot easily ingest these remote PDFs without manually finding direct links, downloading files locally, and uploading them through the file picker.

3. **Monolithic Slicing on Web Articles Lacking Heading Hierarchy:**
   `ImportDocumentHandler.cs` relies strictly on Markdown heading regexes (`^#{1,3}\s+`) to slice imported text. When users crawl unstructured or long-form documentation that lacks `#` or `##` headings, the entire 5,000–10,000 word article becomes a single giant chunk, violating the 3–5 minute daily reading slice constraint.

4. **Missing Support for Engineering Craft Literature:**
   The current `Category` enum (`FrontendWeb = 0`, `BackendDotNet = 1`, `DatabaseStorage = 2`, `SystemDesign = 3`) cannot classify essential engineering productivity and leadership literature (e.g., *Atomic Habits*, *Deep Work*, *The Staff Engineer's Path*, *The Pragmatic Programmer*). Moreover, `GeminiAiService.FormatSliceAsync` mandates triple-backtick language code blocks and syntax-focused quiz challenges, causing AI formatting to hallucinate synthetic code for conceptual and behavioral chapters.

5. **Destructive Overwriting of Author's Prose with AI Summaries in Book Ingestion:**
   In `PdfIngestionWorker.cs` (line 138) and `CurateSliceHandler.cs` (line 77), slice curation currently executes:
   ```csharp
   chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
   ```
   This overwrites the author's rich, authentic prose with an AI-condensed summary. When engineers open the reader interface (`/read/[bookId]`) to study seminal works in `Category.EngineeringCraft` (such as *Thói quen nguyên tử* / *Atomic Habits*, *Deep Work*, or *The Pragmatic Programmer*), they are deprived of the author's actual words, personal stories, and nuanced explanations. The reader renders an AI bullet-point summary instead of the original book. To protect reading fidelity, the system must enforce strict verbatim text preservation: `chunk.OriginalTextMarkdown` must remain 100% verbatim as extracted from `PdfPigExtractor` or the crawler, with AI exclusively populating `SummaryMarkdown`, `KeyTakeaways`, and the scenario drill (`InterviewQuestions`).

This design document specifies the architecture, data structures, algorithms, and security boundaries to solve all five challenges cleanly.

---

## Goals / Non-Goals

### Goals
- **Universal Engineering Pillars:** Replace framework-specific labels on `/profile` with four universal, stack-agnostic engineering layers. Expand `matchCategory` to categorize modern languages, runtimes, and databases into their corresponding pillars.
- **Embedded PDF Detection:** Equip `WebArticleCrawler` with pattern-matching heuristics to detect PDF.js viewer configurations, embedded `<iframe src="*.pdf">`, `<embed src="*.pdf">`, Google Docs/Drive viewer parameters, and direct PDF MIME types.
- **Zero-LOH Remote PDF Ingestion Pipeline:** Expose `POST /api/v1/library/import-remote-pdf` to stream remote PDFs directly into temporary disk spooling (`bufferSize = 80KB`) and enqueue background processing via `PdfIngestionWorker` without requiring local user downloads.
- **1-Click Modal UI:** Provide an embedded PDF preview card with an "Import & Slice PDF Directly" action in the URL crawler tab of `/library`.
- **Paragraph-Aware Soft Slicing:** Implement a two-pass chunker in `ImportDocumentHandler` that splits text on headings first, then subdivides oversized sections (> 1,500 words) at natural paragraph boundaries into digestible ~800–1,200 word slices while preserving code blocks intact.
- **Engineering Craft Category & Prompt Specialization:** Add `Category.EngineeringCraft` (value 4) and specialize `GeminiAiService.FormatSliceAsync` to evaluate cognitive habits, focus frameworks, and engineering leadership principles without forcing artificial code blocks.
- **Verbatim Text Preservation for Books:** Guarantee that `chunk.OriginalTextMarkdown` is NEVER overwritten by AI summaries (`FormattedMarkdown`) for `Category.EngineeringCraft` and book imports. `chunk.OriginalTextMarkdown` remains 100% verbatim as extracted by `PdfPigExtractor` or the crawler so the reader (`/read/[bookId]`) renders the author's true words. Restrict AI to populating `SummaryMarkdown`, `KeyTakeaways`, and scenario drill questions (`InterviewQuestions`).
- **Local Testing Strategy with Real-World Fixtures:** Provide explicit local testing workflows using the local dev environment and local database (avoiding remote token wastage):
  * PDF Fixture: `/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf` (112-page Vietnamese *Atomic Habits* PDF).
  * Embedded Web PDF Fixture: `https://thuviensach.vn/pdf/viewer.php?id=1c342b` (PDF.js web viewer sniffing).
  * Verification that slicing keeps 100% verbatim text across all chapters without AI compression.
### Non-Goals
- Building an in-browser headless browser/Puppeteer scraper for pages that require complex CAPTCHA bypass or client-side JavaScript execution.
- Rewriting the underlying `PdfPig` PDF extraction engine or the `System.Threading.Channels` ingestion queue.
- Modifying the SM-2 algorithm or flashcard scheduling formulas.
- Changing database schemas or executing relational migrations (as `Category` is stored as an integer enum).

---

## Architecture & Data Flow

```mermaid
flowchart TD
    subgraph Client ["Client Browser (Vue 3 / Nuxt 3)"]
        UIProfile["/profile: DomainGoalTracker.vue\nUniversal Engineering Pillars"]
        UILibrary["/library: Import Modal"]
        CrawlerTab["URL Crawler Tab"]
        DetectedPdfCard["Embedded PDF Preview Card\n'Import & Slice PDF Directly'"]
        UIReader["/read/[bookId]\nRenders Author's 100% Verbatim Prose"]
    end

    subgraph API ["Backend Application & API Layer"]
        CrawlEndpoint["POST /api/v1/library/crawl-url"]
        CrawlHandler["CrawlUrlHandler"]
        RemotePdfEndpoint["POST /api/v1/library/import-remote-pdf"]
        RemotePdfHandler["ImportRemotePdfHandler"]
        ImportDocHandler["ImportDocumentHandler\nTwo-Pass Smart Slicer"]
        CurateSlice["CurateSliceHandler\n(Preserves Verbatim Text)"]
    end

    subgraph Infra ["Infrastructure Services"]
        Crawler["WebArticleCrawler\nEmbedded PDF Sniffer"]
        DiskSpooler["Zero-LOH Disk Spooler\n80KB Stream Buffer"]
        IngestionQueue["IPdfIngestionQueue\n(Channel<PdfIngestJob>)"]
        Worker["PdfIngestionWorker\n(Preserves Verbatim Text)"]
        PdfPig["PdfPigExtractor\nVerbatim Text Extractor"]
        Gemini["GeminiAiService\nAdaptive Mindset Prompt"]
    end

    subgraph DB ["PostgreSQL"]
        BooksTable[(DocumentBooks)]
        ChunksTable[(DocumentChunks\nOriginalTextMarkdown = 100% Verbatim\nSummaryMarkdown = AI Summary)]
    end

    CrawlerTab -->|Submit Web URL| CrawlEndpoint
    CrawlEndpoint --> CrawlHandler
    CrawlHandler --> Crawler
    Crawler -->|Detects PDF.js / iframe / Google Docs| CrawlHandler
    CrawlHandler -->|CrawlUrlResponse: IsPdfDetected = true| CrawlerTab
    CrawlerTab --> DetectedPdfCard

    DetectedPdfCard -->|Click 1-Click Import| RemotePdfEndpoint
    RemotePdfEndpoint --> RemotePdfHandler
    RemotePdfHandler -->|Stream HTTP GET| DiskSpooler
    DiskSpooler -->|Save to temp disk| IngestionQueue
    RemotePdfHandler -->|Create Book (Processing)| BooksTable
    RemotePdfHandler -->|Return 202 Accepted| DetectedPdfCard

    IngestionQueue --> Worker
    Worker --> PdfPig
    PdfPig -->|Extracts 100% Verbatim Prose| ChunksTable
    Worker --> Gemini
    Gemini -->|Summary, KeyTakeaways, Scenario Drill| ChunksTable
    Worker -.->|NEVER overwrites OriginalTextMarkdown| ChunksTable

    CurateSlice --> Gemini
    CurateSlice -.->|NEVER overwrites OriginalTextMarkdown| ChunksTable

    ChunksTable -->|Streams 100% Verbatim Text| UIReader

    UIProfile -->|matchCategory() multi-stack keywords| UIProfile
```

---

## Detailed Decisions & Implementation

### 1. Framework-Agnostic Core Engineering Pillars on `/profile`

#### Pillar Structure in `DomainGoalTracker.vue`
Instead of tying pillars to specific versions of .NET or PostgreSQL, the four curriculum categories map to universal engineering domains:

```typescript
const pillars: PillarConfig[] = [
  {
    category: 1, // Category.BackendDotNet (Universal Backend Layer)
    key: 'backend_runtime',
    titleKey: 'profile.domain_backend_runtime',
    defaultTitle: 'Backend Runtime & Concurrency',
    defaultTarget: 8,
    icon: Cpu,
    barColor: 'bg-violet-500 dark:bg-violet-400',
    trackColor: 'bg-violet-100 dark:bg-violet-950/50',
    textColor: 'text-violet-600 dark:text-violet-400',
    badgeColor: 'bg-violet-50 dark:bg-violet-950/60 border-violet-200 dark:border-violet-800 text-violet-700 dark:text-violet-300'
  },
  {
    category: 2, // Category.DatabaseStorage
    key: 'data_storage',
    titleKey: 'profile.domain_data_storage',
    defaultTitle: 'Data Storage & Persistence',
    defaultTarget: 7,
    icon: Database,
    barColor: 'bg-sky-500 dark:bg-sky-400',
    trackColor: 'bg-sky-100 dark:bg-sky-950/50',
    textColor: 'text-sky-600 dark:text-sky-400',
    badgeColor: 'bg-sky-50 dark:bg-sky-950/60 border-sky-200 dark:border-sky-800 text-sky-700 dark:text-sky-300'
  },
  {
    category: 3, // Category.SystemDesign
    key: 'system_design',
    titleKey: 'profile.domain_system_design',
    defaultTitle: 'Distributed Systems & Architecture',
    defaultTarget: 8,
    icon: Network,
    barColor: 'bg-emerald-500 dark:bg-emerald-400',
    trackColor: 'bg-emerald-100 dark:bg-emerald-950/50',
    textColor: 'text-emerald-600 dark:text-emerald-400',
    badgeColor: 'bg-emerald-50 dark:bg-emerald-950/60 border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300'
  },
  {
    category: 0, // Category.FrontendWeb
    key: 'frontend',
    titleKey: 'profile.domain_frontend',
    defaultTitle: 'Frontend & Browser Engineering',
    defaultTarget: 7,
    icon: Layers,
    barColor: 'bg-amber-500 dark:bg-amber-400',
    trackColor: 'bg-amber-100 dark:bg-amber-950/50',
    textColor: 'text-amber-600 dark:text-amber-400',
    badgeColor: 'bg-amber-50 dark:bg-amber-950/60 border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-300'
  }
]
```

#### Multi-Stack Keyword Matching in `matchCategory`
To support senior engineers regardless of primary technology stack, `matchCategory` evaluates keywords comprehensively:

```typescript
function matchCategory(keyOrTopic: string): number | null {
  const k = keyOrTopic.toLowerCase()

  // Pillar 1: Backend Runtime & Concurrency (.NET, Node/Nest, Go, Java/Spring, Python, Concurrency)
  if (
    k === '1' || k === 'category.backenddotnet' ||
    k.includes('backend') || k.includes('runtime') || k.includes('concurrency') ||
    k.includes('dotnet') || k.includes('.net') || k.includes('c#') || k.includes('csharp') || k.includes('clr') ||
    k.includes('node') || k.includes('nodejs') || k.includes('nest') || k.includes('nestjs') || k.includes('express') ||
    k.includes('go') || k.includes('golang') || k.includes('goroutine') ||
    k.includes('java') || k.includes('spring') || k.includes('jvm') ||
    k.includes('python') || k.includes('async') || k.includes('thread') || k.includes('memory')
  ) {
    return 1
  }

  // Pillar 2: Data Storage & Persistence (PostgreSQL, MongoDB, Redis, MySQL, ACID, B-Trees, LSM)
  if (
    k === '2' || k === 'category.databasestorage' ||
    k.includes('database') || k.includes('storage') || k.includes('persistence') ||
    k.includes('postgres') || k.includes('postgresql') || k.includes('sql') || k.includes('mysql') ||
    k.includes('mongo') || k.includes('mongodb') || k.includes('redis') || k.includes('sqlite') || k.includes('cassandra') ||
    k.includes('b-tree') || k.includes('lsm') || k.includes('acid') || k.includes('index') || k.includes('replication') ||
    k.includes('mvcc') || k.includes('wal') || k.includes('transaction')
  ) {
    return 2
  }

  // Pillar 3: Distributed Systems & Architecture (Microservices, Kafka, RabbitMQ, Outbox, Consensus)
  if (
    k === '3' || k === 'category.systemdesign' ||
    k.includes('systemdesign') || k.includes('system_design') || k.includes('system design') ||
    k.includes('distributed') || k.includes('architecture') || k.includes('microservice') ||
    k.includes('outbox') || k.includes('kafka') || k.includes('rabbitmq') || k.includes('event-driven') ||
    k.includes('cap theorem') || k.includes('consensus') || k.includes('idempotency') || k.includes('load balancer')
  ) {
    return 3
  }

  // Pillar 4: Frontend & Browser Engineering (DOM, Rendering, Vue, React, TypeScript, Web Vitals)
  if (
    k === '0' || k === 'category.frontendweb' ||
    k.includes('frontend') || k.includes('browser') || k.includes('web') ||
    k.includes('vue') || k.includes('react') || k.includes('angular') || k.includes('svelte') ||
    k.includes('javascript') || k.includes('typescript') || k.includes('dom') || k.includes('css') ||
    k.includes('rendering') || k.includes('web vitals') || k.includes('ssr') || k.includes('hydration')
  ) {
    return 0
  }

  return null
}
```

#### Localization Keys
In `frontend/i18n/locales/vi.json` and `en.json`:
- `profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency"
- `profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence"
- `profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture"
- `profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering"
*(Note: maintain aliases for legacy `profile.domain_dotnet` and `profile.domain_postgres` to avoid breaking any downstream tests).*

---

### 2. Smart Embedded Web PDF Sniffer & Direct Ingestion Pipeline

#### Heuristic Sniffing in `WebArticleCrawler`
During `CrawlUrlAsync`, after receiving the HTTP response headers and body content, the crawler checks if the target URL represents an embedded PDF document rather than standard article HTML:

1. **Direct MIME Type Check:**
   If `response.Content.Headers.ContentType?.MediaType` is `application/pdf` or `application/x-pdf`, the URL is immediately identified as a PDF.

2. **PDF.js Script Inspection:**
   Sites using Mozilla PDF.js (e.g. `viewer.php?id=...`, `web/viewer.html?file=...`) embed script variables configuring the source file:
   - `DEFAULT_URL\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`
   - `pdfDoc\s*=\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`
   - `file:\s*["']([^"']+\.pdf(?:\?[^"']*)?)["']`

3. **HTML Embed / Iframe Nodes:**
   - `<iframe[^>]+src=["']([^"']+\.pdf[^"']*)["']`
   - `<embed[^>]+src=["']([^"']+\.pdf[^"']*)["']`
   - `<object[^>]+data=["']([^"']+\.pdf[^"']*)["']`

4. **Google Docs / Drive Viewer Links:**
   - Queries matching `docs.google.com/viewer\?.*url=([^&"']+)`
   - Query string value is URL-decoded and checked.

```csharp
private (bool isPdf, string? pdfUrl) SniffEmbeddedPdf(string rawHtml, string baseUrl)
{
    // 1. PDF.js script inspection
    var pdfJsRegex = new Regex(@"DEFAULT_URL\s*=\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']|pdfDoc\s*=\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']|file:\s*[""']([^""']+\.pdf(?:\?[^""']*)?)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    var match = pdfJsRegex.Match(rawHtml);
    if (match.Success)
    {
        var relativeUrl = match.Groups[1].Success ? match.Groups[1].Value : (match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value);
        return (true, ResolveAbsoluteUrl(baseUrl, relativeUrl));
    }

    // 2. Iframe / Embed / Object tag inspection
    var tagRegex = new Regex(@"<(?:iframe|embed|object)[^>]+(?:src|data)=[""']([^""']+\.pdf(?:\?[^""']*)?)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    var tagMatch = tagRegex.Match(rawHtml);
    if (tagMatch.Success)
    {
        return (true, ResolveAbsoluteUrl(baseUrl, tagMatch.Groups[1].Value));
    }

    // 3. Google Docs / Drive Viewer URL
    var googleDocsRegex = new Regex(@"docs\.google\.com/viewer\?.*url=([^&""']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    var googleMatch = googleDocsRegex.Match(baseUrl);
    if (googleMatch.Success)
    {
        var decoded = WebUtility.UrlDecode(googleMatch.Groups[1].Value);
        return (true, decoded);
    }

    return (false, null);
}
```

#### DTO Updates
```csharp
// Application/Interfaces/IWebArticleCrawler.cs
public record CrawlArticleResult(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount,
    bool IsPdfDetected = false,
    string? DetectedPdfUrl = null
);

// Application/Features/Library/CrawlUrl/CrawlUrlHandler.cs
public record CrawlUrlResponse(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount,
    bool IsPdfDetected = false,
    string? DetectedPdfUrl = null
);
```

#### Streaming Remote PDF Ingestion (`ImportRemotePdfHandler`)
When an embedded PDF is detected, the user can trigger direct ingestion via `POST /api/v1/library/import-remote-pdf`.

```csharp
public record ImportRemotePdfRequest(
    string PdfUrl,
    string Title,
    Category Category,
    string Language = "en"
);

public class ImportRemotePdfHandler : IUseCase<ImportRemotePdfRequest, UploadPdfResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ITechDailyDbContext _dbContext;
    private readonly IPdfIngestionQueue _ingestionQueue;

    public async Task<Result<UploadPdfResponse>> ExecuteAsync(
        ImportRemotePdfRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. SSRF Safety Check
        WebArticleCrawler.ValidateSafeUrl(request.PdfUrl);

        var bookId = Guid.NewGuid();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "techdaily-uploads");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"{bookId}.pdf");

        // 2. Stream directly to disk using 80KB buffer (Zero-LOH)
        using var response = await _httpClient.GetAsync(
            request.PdfUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using (var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        {
            await remoteStream.CopyToAsync(fileStream, bufferSize: 81920, cancellationToken);
        }

        // 3. Register Book record
        var book = new DocumentBook
        {
            Id = bookId,
            Title = SanitizeText(request.Title),
            Slug = GenerateSlug(request.Title),
            Category = request.Category,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = request.PdfUrl,
            IsPublished = true,
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            StatusMessage = "Remote PDF downloaded, queued for processing...",
            TotalChunks = 0
        };

        await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Enqueue into background ingestion channel
        await _ingestionQueue.EnqueueAsync(new PdfIngestJob(
            book.Id,
            tempFilePath,
            book.Title,
            request.Category,
            request.Language), cancellationToken);

        return new UploadPdfResponse { Book = MapToDto(book) };
    }
}
```

#### Frontend URL Crawler Tab UI (`frontend/pages/library.vue`)
When `crawlUrl(url)` returns `result.isPdfDetected === true`:
- The UI renders an emerald/sky styled highlight card:
  - Header: `"Embedded Technical PDF Detected!"`
  - Document Title & Resolved PDF URL
  - Button: `"Import & Slice PDF Directly ➔"`
- Clicking the button calls `libraryStore.importRemotePdf({ pdfUrl, title, category, language })`.
- The modal immediately closes, a success toast appears (`"Remote PDF queued for ingestion"`), and the library view initiates background polling (`checkBackgroundPolling()`) to display live progress bars.

---

### 3. Smart Paragraph-Aware Word-Count Slicing for Web Articles

#### Slicing Algorithm in `ImportDocumentHandler.cs`
The existing `SplitIntoChunks` method is replaced with a two-pass chunking pipeline:

```csharp
private static List<string> SplitIntoChunks(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return new List<string>();

    // Pass 1: Split on top-level and second-level headings outside code blocks
    var headingChunks = SplitOnHeadings(text);

    // Pass 2: Soft paragraph subdivision for chunks exceeding 1,500 words
    var finalChunks = new List<string>();
    foreach (var chunk in headingChunks)
    {
        var wordCount = CountWords(chunk);
        if (wordCount <= 1500)
        {
            finalChunks.Add(chunk);
        }
        else
        {
            var subSlices = SubdivideLongSection(chunk, targetWords: 1000, maxWords: 1500);
            finalChunks.AddRange(subSlices);
        }
    }

    return finalChunks.Any() ? finalChunks : new List<string> { text.Trim() };
}
```

#### Paragraph Subdivider Details (`SubdivideLongSection`)
1. Parses lines tracking code block fences (`inCodeBlock`).
2. Accumulates paragraphs (delimited by blank lines `\n\n`) outside of code blocks.
3. When the accumulated paragraph buffer exceeds the soft target (~1,000 words), it flushes the current buffer into a sub-slice.
4. Extracts or preserves the base heading title, formatting subsequent parts as:
   - Part 1: `# Section Title (Part 1)`
   - Part 2: `# Section Title (Part 2)`
5. Guaranteed invariants:
   - Fenced code blocks (```...```) are never broken mid-block.
   - Tables and blockquotes remain intact.

---

### 4. Engineering Mindset & Productivity Category Support

#### Domain Enum Update
```csharp
// TechDaily.Domain/Enums/DomainEnums.cs
public enum Category
{
    FrontendWeb = 0,
    BackendDotNet = 1,
    DatabaseStorage = 2,
    SystemDesign = 3,
    EngineeringCraft = 4 // "Tư Duy Kỹ Sư & Năng Suất" / "Engineering Craft & Mindset"
}
```

#### Prompt Specialization in `GeminiAiService.FormatSliceAsync`
`IAiMarkdownFormatter` is updated to receive `Category? category = null`:

```csharp
public async Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
    string rawText,
    string chapterTitle,
    string language = "en",
    Category? category = null,
    CancellationToken cancellationToken = default)
```

When `category == Category.EngineeringCraft`:
- **System Instruction:**
  ```text
  You are a Principal Software Engineer and Leadership Coach specializing in Engineering Craft, Cognitive Habits, and Productivity Systems.
  Your task is to convert raw extracted text from an engineering leadership, productivity, or mindset book into a structured TechInsight-style reading slice.

  MANDATORY RULES:
  1. Document Heading: Start immediately with '# {chapterTitle}' as the top-level H1 header.
  2. Context Note: Follow directly with an executive context callout:
     > [!NOTE]
     > 2-3 sentences explaining the core behavioral framework, cognitive principle, or engineering habit.
  3. Actionable Narrative Prose: Merge fragmented text into natural paragraphs explaining key principles, real-world workplace scenarios, and actionable techniques.
  4. Code Blocks (Optional): Include code or pseudocode ONLY if present in the source text. Do NOT force synthetic code blocks into behavioral literature.
  5. Practical Callouts: Highlight critical mindset shifts or antipatterns with GitHub alerts (`> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`).
  6. Key Takeaways: Conclude with '### Key Takeaways' containing exactly 3 bullet points of high-impact engineering habits or principles.
  7. Senior Leadership Drill: Create exactly 1 high-impact Scenario Challenge evaluating trade-offs in engineering focus, habit formation, time allocation, or staff-level influence without authority.
  ```


---

### 5. Verbatim Text Preservation for Books (NEVER Overwrite with AI Summaries)

#### The Problem: Destructive Overwrites in Existing Curation Pipeline
In the initial implementation, `PdfIngestionWorker.cs` (line 138) and `CurateSliceHandler.cs` (line 77) contained the following line:

```csharp
// CRITICAL FLAW: Overwrites author's prose with AI summary
chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
chunk.SummaryMarkdown = aiResult.Value.SummaryMarkdown;
chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
chunk.IsAiFormatted = true;
```

This caused serious data loss:
- The reader at `/read/[bookId]` displays `currentChunk.value.originalTextMarkdown`.
- Overwriting `OriginalTextMarkdown` with `aiResult.Value.FormattedMarkdown` destroyed the author's rich, narrative prose (such as James Clear's recovery story from his baseball accident or Dave Brailsford's cycling marginal gains in *Atomic Habits*) and replaced it with a condensed 300-word bullet summary.
- The user was unable to read the actual book as written by the author.

#### The Invariant & Solution
For books in `Category.EngineeringCraft` (such as *Thói quen nguyên tử* / *Atomic Habits*, *Deep Work*, *The Pragmatic Programmer*) and book imports:
1. `chunk.OriginalTextMarkdown` **MUST NEVER** be overwritten by `aiResult.Value.FormattedMarkdown`.
2. `chunk.OriginalTextMarkdown` remains 100% verbatim as extracted from `PdfPigExtractor` or the crawler.
3. AI formatting results are strictly mapped to auxiliary fields:
   - `chunk.SummaryMarkdown = aiResult.Value.SummaryMarkdown;` (or formatted markdown summary)
   - `chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;`
   - `chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;`
   - `chunk.IsAiFormatted = true;`
   - `InterviewQuestions` receives the Senior Scenario Drill.
4. The reader view `/read/[bookId]` continues rendering `chunk.OriginalTextMarkdown`, presenting the author's true words, authentic tone, and complete chapters.

#### Implementation in `PdfIngestionWorker.cs` and `CurateSliceHandler.cs`
```csharp
// In PdfIngestionWorker.cs and CurateSliceHandler.cs
if (aiResult.IsSuccess)
{
    // Invariant: NEVER overwrite OriginalTextMarkdown for EngineeringCraft or book imports!
    // chunk.OriginalTextMarkdown remains 100% verbatim as extracted from PdfPigExtractor.
    if (book.Category != Category.EngineeringCraft && book.SourceType != SourceType.PdfBook)
    {
        // Only allow formatted replacement for non-book ephemeral web summaries if applicable
        if (!string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
        {
            chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
        }
    }

    // AI populates summary, key takeaways, and scenario drills without modifying OriginalTextMarkdown
    chunk.SummaryMarkdown = !string.IsNullOrWhiteSpace(aiResult.Value.SummaryMarkdown)
        ? aiResult.Value.SummaryMarkdown
        : aiResult.Value.FormattedMarkdown;
    chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
    chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
    chunk.IsAiFormatted = true;

    if (aiResult.Value.ScenarioDrill != null)
    {
        var drill = aiResult.Value.ScenarioDrill;
        var question = new Domain.Entities.InterviewQuestion
        {
            DocumentChunkId = chunk.Id,
            QuestionText = drill.QuestionText,
            Options = drill.Options,
            CorrectOptionIndex = drill.CorrectOptionIndex,
            ExplanationMarkdown = drill.ExplanationMarkdown,
            ExpectedKeyPoints = drill.ExpectedKeyPoints,
            ModelAnswerMarkdown = drill.ExplanationMarkdown,
            Difficulty = Domain.Enums.Difficulty.Senior
        };
        await dbContext.InterviewQuestions.AddAsync(question, stoppingToken);
    }
}
```

---

### 6. Local Testing Strategy with Real-World Fixtures

To ensure comprehensive quality assurance without incurring external LLM token costs or relying on external test environments, the implementation includes a dedicated local testing strategy using local dev tooling and real-world fixtures:

#### Local Dev Environment Configuration
- **Database:** Local PostgreSQL instance running with pgvector.
- **Backend:** `dotnet run --project backend/src/TechDaily.Api` (listening on `http://localhost:5000`).
- **Frontend:** `npm run dev` in `frontend/` (listening on `http://localhost:3000`).
- **AI Formatter Mode:** For local testing of extraction and slicing, AI calls can be stubbed or mock-configured, or run against local dev secrets without burning production token quotas.

#### Real-World Fixtures & Verification Procedures

1. **PDF Fixture: Vietnamese Atomic Habits (*Thói quen nguyên tử*)**
   - **Path:** `/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf`
   - **Characteristics:** 112-page Vietnamese translation of James Clear's *Atomic Habits*, published via `thuviensach.vn`. Contains front matter ("LỜI NÓI ĐẦU"), personal narrative introduction ("PHẦN GIỚI THIỆU - Câu chuyện của chính tôi"), foundational chapters ("CHƯƠNG 1 - NHỮNG NGUYÊN TẮC CƠ BẢN"), and four laws of behavior change.
   - **Verification Steps:**
     a. Trigger ingestion of the fixture via local API or file upload under `Category.EngineeringCraft` (value 4).
     b. Verify that `PdfPigExtractor` processes all 112 pages without memory exhaustion or LOH spikes.
     c. Verify that chapter heading detection correctly splits the document into chapter slices.
     d. Inspect the created `DocumentChunk` records in PostgreSQL:
        - Confirm `OriginalTextMarkdown` contains 100% verbatim author prose (e.g., verifying exact phrases: *"Hệ thống mang tính cách mạng giúp bạn tiến bộ 1% mỗi ngày"*, *"Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt"*, *"Dave Brailsford"*).
        - Confirm that after slice curation runs, `OriginalTextMarkdown` is **NOT** overwritten, shortened, or replaced by AI summary bullet points.
     e. Open `/read/[bookId]` in browser at `http://localhost:3000`:
        - Confirm that the reader displays the author's full narrative paragraphs, verbatim formatting, and headings.

2. **Embedded Web PDF Fixture: PDF.js Viewer on `thuviensach.vn`**
   - **URL:** `https://thuviensach.vn/pdf/viewer.php?id=1c342b`
   - **Characteristics:** A live web reader shell running Mozilla PDF.js that serves an embedded technical book. The HTML container contains scripts setting `DEFAULT_URL` / `pdfDoc` or viewer parameters without article body text.
   - **Verification Steps:**
     a. Submit `POST /api/v1/library/crawl-url` with `url: "https://thuviensach.vn/pdf/viewer.php?id=1c342b"`.
     b. Verify that `WebArticleCrawler` does not fail with an empty article error.
     c. Verify `SniffEmbeddedPdf` detects the PDF.js configuration, resolves the absolute PDF URL on `thuviensach.vn`, passes SSRF validation, and returns:
        ```json
        {
          "isPdfDetected": true,
          "detectedPdfUrl": "https://thuviensach.vn/...",
          "title": "..."
        }
        ```
     d. Trigger `POST /api/v1/library/import-remote-pdf` with the detected URL.
     e. Verify that `ImportRemotePdfHandler` streams the remote PDF using the 80KB buffer into temporary disk spooling, creates the `DocumentBook`, and queues it to `IPdfIngestionQueue`.
     f. Verify that slicing keeps 100% verbatim text across all chapters without AI compression.
---

## Security & Performance Considerations

1. **SSRF Protection on Remote PDF Ingestion:**
   `ImportRemotePdfHandler` enforces the same security validation as `WebArticleCrawler`:
   - Enforces `http` or `https` schemes.
   - Resolves host DNS and evaluates IP addresses against private subnets (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`), loopback (`127.0.0.0/8`), and AWS/GCP cloud metadata (`169.254.169.254`).
   - Rejects non-public IPs with `HTTP 400 Bad Request` (`SSRF_DETECTED`).

2. **Memory Efficiency (Zero-LOH Streaming):**
   - Remote PDFs are downloaded using `HttpCompletionOption.ResponseHeadersRead` and spooled directly to temp disk using `Stream.CopyToAsync(bufferSize: 81920)`.
   - Files are never read into contiguous byte arrays in memory, completely avoiding allocations in .NET's Large Object Heap (LOH).
   - Temporary files are stored in `Path.GetTempPath()/techdaily-uploads` and cleaned up safely upon worker completion or failure.

3. **Crawl Timeout & Size Limits:**
   - Enforces a 30-second HTTP timeout on remote PDF sniffing and streaming.
   - Enforces a 350 MB maximum stream limit, terminating connections immediately if `Content-Length` or streamed bytes exceed 367,001,600 bytes.

---

## Risks / Trade-offs

1. **Risk: False Positives in Embedded PDF Sniffing:**
   - *Risk:* An article mentioning a `.pdf` file in a hyperlink might be mistaken for an embedded PDF.
   - *Mitigation:* The sniffer requires strict structural patterns: PDF.js `DEFAULT_URL` assignment, `<iframe src="*.pdf">`, `<embed src="*.pdf">`, or Google Docs viewer query parameters. Regular hyperlinks (`<a href="...pdf">`) in standard article prose continue to be converted into clean Markdown links.

2. **Risk: Slicing Mid-Thought on Monolithic Articles:**
   - *Risk:* Splitting an article solely by paragraph count might divide an explanation into two slices.
   - *Mitigation:* Paragraphs are evaluated atomically—individual paragraphs are never split. A slice is finalized only on paragraph boundaries (`\n\n`), targeting a comfortable window of 800–1,200 words, well within the 3–5 minute reading threshold.

3. **Trade-off: In-Memory Category Mapping vs Database Role Tables:**
   - *Decision:* `Category` remains an integer enum (`0..4`) across backend entities, avoiding relational database schema migrations. The frontend `DomainGoalTracker.vue` maps dynamic topics to the four pillars client-side, enabling immediate support for any tech stack without database changes.
