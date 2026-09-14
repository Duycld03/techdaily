# Tasks: All-in-One Quiz Generation, Lazy Curation & Lookahead Prefetch

## 1. All-in-One AI Prompt & Persistence
- [x] 1.1 Update `IAiMarkdownFormatter.cs` to include `AiScenarioDrillVo` and update `AiFormattedSliceResult`.
- [x] 1.2 Update `GeminiAiService.cs` prompt to generate TechInsight Markdown and Senior Scenario Drill in 1-shot JSON response with depth-balanced parsing.
- [x] 1.3 Update `CurateSliceHandler.cs` to persist both `DocumentChunk` and `InterviewQuestion` (linked to `chunk.Id`), updating `chunk.MicroQuiz`.

## 2. Lean Ingestion & Worker Refactoring
- [x] 2.1 Refactor `PdfIngestionWorker.cs` to format only Slices 1..3 during initial ingestion using the all-in-one prompt.
- [x] 2.2 Remove eager background loop for Slices 4..N in `PdfIngestionWorker.cs`, marking the book `Status = Ready`, `ProgressPercentage = 100`, and `StatusMessage = "Ready for reading"`.
- [x] 2.3 Ensure `GetTodayFocusHandler.cs` utilizes the all-in-one formatter when encountering an uncurated chunk.

## 3. Frontend Lookahead Prefetching & Resilient UI
- [x] 3.1 Update `frontend/pages/library.vue` to simplify book cards, removing the background AI curation progress bar in favor of a `Ready` badge and personal reading milestone.
- [x] 3.2 Update `frontend/pages/read/[bookId].vue` to implement 1-step lookahead prefetching for Slice $N+1$ when viewing Slice $N$.
- [x] 3.3 Implement JIT curation loading spinner, retry card on error, and `isViewingRawTemporarily` memory flag with amber reminder banner in `frontend/pages/read/[bookId].vue`.
- [x] 3.4 Update `frontend/pages/today.vue` to prefetch tomorrow's slice and synchronize reading slice with scenario challenge.

## 4. Local-First Automated Verification
- [x] 4.1 Execute backend test suite (`dotnet test backend/tests/TechDaily.Tests`).
- [x] 4.2 Execute frontend test suite (`npm test`).
- [x] 4.3 Run Playwright local test script to verify:
  - Book upload/ready state in library without progress bar noise.
  - Slice 3 reader view with all-in-one formatted content and instant quiz.
  - Lookahead prefetching triggering for Slice 4.
  - Ephemeral raw text view and re-attempt on refresh.
