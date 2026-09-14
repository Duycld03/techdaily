# Tasks: Doc Pacer Engine & Asynchronous Large PDF Ingestion

## 1. Domain Model & Database Migrations
- [x] Add `ProcessingStatus`, `ProgressPercentage`, `StatusMessage`, `ErrorMessage`, and `IsFeatured` to `DocumentBook.cs` <!-- id: 1.1 -->
- [x] Create `UserBookPacer.cs` domain entity with tracking fields (`CurrentChunkOrder`, `DailyPaceChunks`, `IsActive`, `LastReadDate`) <!-- id: 1.2 -->
- [x] Configure EF Core entity mapping and indexes in `EntityConfigurations.cs` <!-- id: 1.3 -->
- [x] Generate EF Core migration `AddDocPacerAndAsyncPdfIngestion` and verify against test database <!-- id: 1.4 -->

## 2. Asynchronous Large PDF Ingestion & Bookmark Extraction
- [x] Upgrade upload size limit to 300 MB and implement zero-LOH disk spooling (`bufferSize = 80KB`) in `UploadPdfHandler.cs` <!-- id: 2.1 -->
- [x] Implement `PdfBookmarkParser` in `PdfPigExtractor.cs` to read native PDF Outline/Bookmarks tree and prune front/back matter <!-- id: 2.2 -->
- [x] Scaffold `System.Threading.Channels` ingestion queue and `PdfIngestionWorker : BackgroundService` with incremental progress updates <!-- id: 2.3 -->
- [x] Add status polling endpoint `GET /api/v1/library/books/{id}/status` <!-- id: 2.4 -->

## 3. Look-Ahead Buffer & AI Trade-off Synthesis
- [x] Implement `LookAheadBufferService` to pre-generate drills for the first 3 chunks and maintain a 3-chunk sliding buffer <!-- id: 3.1 -->
- [x] Implement Priority Promotion Channel for JIT generation when users rapidly navigate forward <!-- id: 3.2 -->
- [x] Configure Gemini 3.5 Flash-Lite (`gemini-3.5-flash-lite`) Trade-off Scenario generator with balanced bracket depth parsing <!-- id: 3.3 -->
- [x] Add fallback scenario generation on timeout (>6s) or rate limit <!-- id: 3.4 -->

## 4. Frontend: Doc Pacer & Roadmap Synchronization
- [x] Build `frontend/components/today/AISynthesisCard.vue` skeleton component with pulsating animation <!-- id: 4.1 -->
- [x] Refactor `/today` top navigation bar into the dynamic Pacer Bar with 1-click Book Switcher dropdown <!-- id: 4.2 -->
- [x] Update `useDailyFocusStore.ts` to manage active book pacer state and forward navigation <!-- id: 4.3 -->
- [x] Refactor `frontend/pages/roadmap.vue` to display active book chapter milestones instead of fixed 30-day timeline <!-- id: 4.4 -->
- [x] Update `frontend/pages/library.vue` upload modal to display asynchronous progress bar and polling <!-- id: 4.5 -->
- [x] Add bilingual translation keys in `frontend/i18n/locales/en.json` and `vi.json` <!-- id: 4.6 -->

## 5. Verification & Stress Testing
- [x] Run automated tests for PDF bookmark extraction and background ingestion worker <!-- id: 5.1 -->
- [x] Execute stress test upload with `/home/duycld03/Downloads/aspnet-core-aspnetcore-10.0.pdf` (240MB, 8,351 pages) <!-- id: 5.2 -->
- [x] Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass rate <!-- id: 5.3 -->
- [x] Run backend test suite (`dotnet test backend/tests/TechDaily.Tests`) <!-- id: 5.4 -->
- [x] Verify production build (`npm --prefix frontend run build`) <!-- id: 5.5 -->
