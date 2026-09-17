# Tasks: Universal Engineering Pillars and Smart Web PDF Crawler

## 1. Framework-Agnostic Core Engineering Pillars on `/profile` & Localization

- [x] 1.1 In `frontend/components/profile/DomainGoalTracker.vue`, update `pillars` configuration array to represent the four universal engineering layers (Backend Runtime & Concurrency, Data Storage & Persistence, Distributed Systems & Architecture, Frontend & Browser Engineering) with updated `key`, `titleKey`, and `defaultTitle`.
- [x] 1.2 In `frontend/components/profile/DomainGoalTracker.vue`, refactor `matchCategory(keyOrTopic: string)` to support multi-stack keywords across all four pillars (.NET, Node, Nest, Express, Go, Golang, Goroutine, Java, Spring, JVM, Python, PostgreSQL, MongoDB, Redis, MySQL, SQLite, Cassandra, ACID, B-Trees, LSM, Kafka, RabbitMQ, Microservices, Outbox, Vue, React, TypeScript, etc.).
- [x] 1.3 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, update `profile.domain_backend_runtime`, `profile.domain_data_storage`, `profile.domain_system_design`, and `profile.domain_frontend` with exact localized strings, while preserving backward-compatible aliases for `domain_dotnet` and `domain_postgres`.
- [x] 1.4 Update existing test assertions in `frontend/tests/components/profile.spec.ts` and `frontend/tests/pages/profile.spec.ts` to assert against the updated universal engineering pillar titles and multi-stack topic breakdowns.

## 2. Smart Embedded Web PDF Sniffer in Backend

- [x] 2.1 Update `CrawlArticleResult` in `backend/src/TechDaily.Application/Interfaces/IWebArticleCrawler.cs` and `CrawlUrlResponse` in `backend/src/TechDaily.Application/Features/Library/CrawlUrl/CrawlUrlHandler.cs` to include `bool IsPdfDetected = false` and `string? DetectedPdfUrl = null`.
- [x] 2.2 In `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`, implement `SniffEmbeddedPdf` detecting PDF.js scripts (`DEFAULT_URL`, `pdfDoc`, `file:`), HTML tags (`<iframe src="*.pdf">`, `<embed src="*.pdf">`, `<object data="*.pdf">`), Google Docs/Drive viewer links (`docs.google.com/viewer?url=...`), and direct `application/pdf` MIME headers.
- [x] 2.3 In `WebArticleCrawler.cs`, implement relative PDF URL resolution against the base URI and enforce SSRF validation on the resolved PDF URL before returning it in `CrawlArticleResult`.
- [x] 2.4 Add unit tests in `backend/tests/TechDaily.Tests/Infrastructure/WebArticleCrawlerTests.cs` verifying successful detection for PDF.js scripts, iframe embeds, embed tags, and Google Docs viewer links.

## 3. Remote PDF Streaming Ingestion Pipeline

- [x] 3.1 Define `ImportRemotePdfRequest` and `ImportRemotePdfValidator` in `backend/src/TechDaily.Application/Features/Library/ImportRemotePdf/ImportRemotePdfHandler.cs`.
- [x] 3.2 Implement `ImportRemotePdfHandler` (`IUseCase<ImportRemotePdfRequest, UploadPdfResponse>`) executing SSRF validation, streaming the remote PDF via `HttpCompletionOption.ResponseHeadersRead` directly to temporary disk spooling (`bufferSize = 80KB`), creating `DocumentBook` with `SourceType = PdfBook`, and enqueuing into `IPdfIngestionQueue`.
- [x] 3.3 Register `ImportRemotePdfHandler` in `backend/src/TechDaily.Application/DependencyInjection.cs` and map `POST /api/v1/library/import-remote-pdf` in `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`.
- [x] 3.4 Create unit tests in `backend/tests/TechDaily.Tests/Application/ImportRemotePdfHandlerTests.cs` covering successful remote streaming ingestion, SSRF rejection on private IPs, and size boundary enforcement (> 350 MB).

## 4. Frontend Remote PDF Ingestion & Modal UX

- [x] 4.1 Update `frontend/stores/useLibraryStore.ts` to add the `importRemotePdf(payload: { pdfUrl: string; title: string; category: number; language?: string })` action.
- [x] 4.2 In `frontend/pages/library.vue`, update `handleCrawlUrl` to detect `result.isPdfDetected` and display a dedicated Embedded PDF Preview Card in the URL Crawler tab showing the document title, resolved PDF URL, and an "Import & Slice PDF Directly" action button.
- [x] 4.3 In `frontend/pages/library.vue`, wire the "Import & Slice PDF Directly" button to call `libraryStore.importRemotePdf`, close the modal, show a localized success toast, and trigger background status polling (`checkBackgroundPolling`).
- [x] 4.4 Add localized keys for the embedded PDF preview banner and direct import action in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 5. Smart Word-Count Slicing for Web Articles

- [x] 5.1 Refactor `SplitIntoChunks` in `backend/src/TechDaily.Application/Features/Library/ImportDocument/ImportDocumentHandler.cs` into a two-pass chunking pipeline: Pass 1 splits by Markdown headings outside code blocks, and Pass 2 applies paragraph-aware soft subdivision on sections exceeding 1,500 words.
- [x] 5.2 Implement `SubdivideLongSection` in `ImportDocumentHandler.cs` to group paragraphs (~800–1,200 words, max 1,500 words) outside code fences and blockquotes, titling subdivided slices sequentially (`{Title} (Part 1)`, `{Title} (Part 2)`).
- [x] 5.3 Add unit tests in `backend/tests/TechDaily.Tests/Application/ImportDocumentHandlerTests.cs` verifying that a monolithic 4,000+ word article without `#` headings splits into 3–4 slices of ~1,000 words while keeping code blocks intact.

## 6. Engineering Mindset & Productivity Category Support

- [x] 6.1 Add `EngineeringCraft = 4` to `Category` enum in `backend/src/TechDaily.Domain/Enums/DomainEnums.cs`.
- [x] 6.2 Update `IAiMarkdownFormatter.cs` and `GeminiAiService.FormatSliceAsync` to accept an optional `Category? category = null` parameter.
- [x] 6.3 In `GeminiAiService.FormatSliceAsync`, adapt system instructions when `category == Category.EngineeringCraft` to evaluate core behavioral principles, mental models, cognitive habits, and engineering leadership trade-offs, making code blocks optional.
- [x] 6.4 Update `PdfIngestionWorker.cs`, `CurateSliceHandler.cs`, and `GetTodayFocusHandler.cs` to pass `book.Category` when calling `FormatSliceAsync`.
- [x] 6.5 In `frontend/pages/library.vue`, add Category 4 (`Engineering Craft & Mindset`) to the category filter chips array and modal category selectors.
- [x] 6.6 Add `library.categories.craft` ("Engineering Craft & Mindset" / "Tư Duy Kỹ Sư & Năng Suất") to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 7. Verbatim Text Preservation for Books (NEVER Overwrite with AI Summaries)

- [x] 7.1 In `backend/src/TechDaily.Infrastructure/Workers/PdfIngestionWorker.cs` (line 138), refactor slice curation to NEVER overwrite `chunk.OriginalTextMarkdown` with `aiResult.Value.FormattedMarkdown` when `book.Category == Category.EngineeringCraft` or for book imports, preserving 100% of the author's verbatim prose extracted by `PdfPigExtractor`.
- [x] 7.2 In `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs` (line 77), ensure `chunk.OriginalTextMarkdown` is never overwritten with AI summaries, restricting AI output to `chunk.SummaryMarkdown`, `chunk.KeyTakeaways`, `chunk.EstimatedReadMinutes`, and `InterviewQuestions`.
- [x] 7.3 In `PdfIngestionWorker.cs` and `CurateSliceHandler.cs`, map AI-generated summary content to `chunk.SummaryMarkdown` and key insights to `chunk.KeyTakeaways`, guaranteeing that the reading interface (`frontend/pages/read/[bookId].vue`) renders the author's authentic, uncompressed text from `OriginalTextMarkdown`.
- [x] 7.4 Add unit tests in `backend/tests/TechDaily.Tests/Infrastructure/PdfIngestionWorkerTests.cs` and `backend/tests/TechDaily.Tests/Application/CurateSliceHandlerTests.cs` verifying that `chunk.OriginalTextMarkdown` remains 100% unchanged before and after AI formatting for `Category.EngineeringCraft` books.

## 8. Local Testing Strategy with Real-World Fixtures

- [x] 8.1 Execute local test using PDF fixture `/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf` (112-page Vietnamese *Atomic Habits* PDF) in local dev environment (`Category.EngineeringCraft`):
  - Verify `PdfPigExtractor` extracts all 112 pages without memory/LOH issues and creates chapter chunks with headings.
  - Verify that slicing keeps 100% verbatim text across all chapters without AI compression, and `chunk.OriginalTextMarkdown` preserves exact author prose (e.g. personal baseball accident story, Dave Brailsford marginal gains).
  - Verify that `chunk.SummaryMarkdown` and `chunk.KeyTakeaways` are populated without mutating `chunk.OriginalTextMarkdown`.
- [x] 8.2 Execute local test using Embedded Web PDF fixture `https://thuviensach.vn/pdf/viewer.php?id=1c342b`:
  - Verify `WebArticleCrawler` sniffs the PDF.js viewer link, resolves the absolute PDF URL, passes SSRF checks, and returns `IsPdfDetected = true` with `DetectedPdfUrl`.
  - Verify `POST /api/v1/library/import-remote-pdf` streams the remote PDF into temporary storage (`techdaily-uploads/{bookId}.pdf`) and enqueues into `IPdfIngestionQueue`.
- [x] 8.3 In local browser (`http://localhost:3000/read/[bookId]`), open the imported *Atomic Habits* book and verify that the reader renders the author's true words, rich storytelling, and full paragraphs rather than condensed AI summaries.

## 9. Final Verification & Quality Assurance

- [x] 9.1 Execute backend unit tests (`dotnet test backend/TechDaily.sln`) covering crawler sniffing, remote PDF streaming, paragraph-aware slicing, and verbatim text preservation.
- [x] 9.2 Execute frontend test suite (`npm run test`) to verify profile domain tracker rendering and multi-stack topic breakdown calculations.
- [x] 9.3 Run `openspec validate --strict universal-pillars-and-smart-web-pdf-crawler` to confirm all planning artifacts adhere to OpenSpec conventions.
