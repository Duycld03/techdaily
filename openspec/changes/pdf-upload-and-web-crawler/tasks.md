# Tasks: PDF File Upload & Web URL Document Crawler

- [ ] 1. Add NuGet packages (`PdfPig` / `UglyToad.PdfPig`, `HtmlAgilityPack`, `ReverseMarkdown`) to `TechDaily.Infrastructure.csproj` <!-- id: task-nuget-packages -->
- [ ] 2. Define `IPdfExtractor` and `IWebArticleCrawler` interfaces in `TechDaily.Application/Interfaces/` <!-- id: task-crawler-interfaces -->
- [ ] 3. Implement `PdfPigExtractor` in `TechDaily.Infrastructure/Services/PdfPigExtractor.cs` with text normalization and slice segmenter <!-- id: task-pdf-service -->
- [ ] 4. Implement `WebArticleCrawler` in `TechDaily.Infrastructure/Services/WebArticleCrawler.cs` with GitHub Raw resolver and HTML-to-Markdown cleaner <!-- id: task-crawler-service -->
- [ ] 5. Implement `UploadPdfHandler` use-case handler with validation and book/chunk creation <!-- id: task-upload-pdf-handler -->
- [ ] 6. Implement `CrawlUrlHandler` use-case handler with URL validation and content preview response <!-- id: task-crawl-url-handler -->
- [ ] 7. Register services in `DependencyInjection.cs` across Application and Infrastructure <!-- id: task-register-di -->
- [ ] 8. Map `POST /api/v1/library/upload-pdf` and `POST /api/v1/library/crawl-url` in `LibraryEndpoints.cs` <!-- id: task-api-endpoints -->
- [ ] 9. Add `uploadPdf` and `crawlUrl` methods in `frontend/stores/useLibraryStore.ts` <!-- id: task-store-methods -->
- [ ] 10. Update Import Document Modal in `frontend/pages/library.vue` with 3-tab layout (Markdown, PDF Drag & Drop, URL Crawler & Preview) <!-- id: task-frontend-modal-tabs -->
- [ ] 11. Add complete i18n translation keys in `frontend/i18n/locales/en.json` and `vi.json` <!-- id: task-i18n-keys -->
- [ ] 12. Write unit tests for PDF extraction, URL crawler, and handlers in `TechDaily.Tests` <!-- id: task-unit-tests -->
- [ ] 13. Verify full end-to-end flow with Vitest, Dotnet test, and Playwright verification <!-- id: task-verify-all -->
