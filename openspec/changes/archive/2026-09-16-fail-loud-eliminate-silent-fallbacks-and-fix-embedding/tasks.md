# Tasks: Fail Loud & Transparent: Eliminate Silent Fallbacks, Fix Gemini Embedding Model, and Add AI Health Check

## Phase 1: Model Standardization (`gemini-3.5-flash-lite`), Embedding Migration & Fail-Loud Service Alignment

- [x] 1.1 Update application configuration files and project documentation:
  - In `backend/src/TechDaily.Api/appsettings.json`:
    - Ensure `"Model": "gemini-3.5-flash-lite"`.
    - Add/update `"EmbeddingModel": "gemini-embedding-001"`.
    - Add `"UseOfflineMock": false`.
  - In `backend/src/TechDaily.Api/appsettings.Development.json` (or create if absent):
    - Ensure `"Model": "gemini-3.5-flash-lite"`.
    - Ensure `"EmbeddingModel": "gemini-embedding-001"`.
    - Set `"UseOfflineMock": false` by default (document setting to `true` for offline development).
  - In `backend/src/TechDaily.Api/appsettings.Local.json` (if present or when created for local overrides):
    - Ensure `"Model": "gemini-3.5-flash-lite"`.
    - Ensure `"EmbeddingModel": "gemini-embedding-001"`.
    - Ensure `"UseOfflineMock": false`.
  - In `AGENTS.md`:
    - Update Rule 12 to standardize on `gemini-3.5-flash-lite` (<5s latency) for production AI generation in interactive loops.
- [x] 1.2 Refactor `GeminiEmbeddingService.cs`:
  - Update default model fallback in constructor from `text-embedding-004` to `gemini-embedding-001`.
  - Inject `IConfiguration` to read `Gemini:UseOfflineMock` (boolean, default `false`).
  - In `GenerateEmbeddingAsync`:
    - Standardize embedding model on `gemini-embedding-001` with explicit `"outputDimensionality": 768` in the JSON request payload.
    - Validate that returned vector dimensions equal 768 before returning `new Vector(floats)`.
    - Stop silently calling `GenerateDeterministicMockVector` on missing API key or HTTP error.
    - Return `Result<Vector>.Failure(Error.Custom("Embedding.MissingApiKey", ...))` when API key is missing and `UseOfflineMock == false`.
    - Return `Result<Vector>.Failure(Error.Custom("Embedding.ApiError", ...))` when Google API returns non-200 or throws exceptions and `UseOfflineMock == false`.
    - Allow `GenerateDeterministicMockVector` strictly when `UseOfflineMock == true` with an explicit `LogWarning`.
  - In `GenerateBatchEmbeddingsAsync`:
    - Standardize embedding model on `gemini-embedding-001` with explicit `"outputDimensionality": 768` added to each item in the `requests` array.
    - Validate dimension of each returned embedding (assert == 768).
    - Stop falling back to mock vectors in sequential retry; return `Result<List<Vector>>.Failure` on failure.

---

## Phase 2: Background Seeders & Ingestion Integrity

- [x] 2.1 Refactor `CurriculumSeeder.BackfillEmbeddingsAsync`:
  - Call `embeddingService.GenerateBatchEmbeddingsAsync(texts, cancellationToken)`.
  - If `!embResult.IsSuccess` or returned count does not match `unvectorizedChunks.Count`:
    - Log error via `_logger.LogError("Curriculum vector embeddings backfill failed: {Error}", embResult.Error.Message)`.
    - Do NOT assign mock vectors.
    - Preserve `chunk.Embedding = null` in the database.
    - Do not call `context.SaveChangesAsync()` on failure, leaving chunks eligible for future retry.
- [x] 2.2 Refactor `PdfIngestionWorker.cs`:
  - In initial slice embedding block, check `embResult.IsSuccess`.
  - If embedding generation fails or throws an unhandled exception:
    - Log with `_logger.LogError(ex, "Failed to vectorize slices for book {BookId}", book.Id)`.
    - Mark `book.Status = ProcessingStatus.Failed`.
    - Set `book.StatusMessage = "Failed during vector embedding generation"`.
    - Set `book.ErrorMessage = ex.Message` or `embResult.Error.Message`.
    - Save changes to database and abort marking the book as `Ready`.
  - Ensure unhandled chunking or slice generation exceptions consistently mark `book.Status = ProcessingStatus.Failed`.

---

## Phase 3: AI Handlers & Elimination of Canned Fallback Entities

- [x] 3.1 Refactor `GeminiAiService.cs`:
  - Update default model fallback in constructor to `gemini-3.5-flash-lite`: `configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite"`.
  - In `GenerateInsightAsync`:
    - Remove calls to `GenerateMockInsight` on missing API key, HTTP failure, or empty candidates.
    - Return `Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", ...))`.
    - Ensure `GenerateInsightHandler.cs` receives the failure and does not persist canned insights to `TechInsights`.
  - In `GenerateQuestionsAsync`:
    - Remove calls to `GenerateMockQuestions` on missing API key, HTTP failure, or empty candidates.
    - Return `Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", ...))`.
    - Ensure `GenerateQuizHandler.cs` receives the failure and does not persist canned questions to `QuizQuestions`.
  - In `SynthesizeActiveRecallCardAsync`:
    - Change return signature from `Task<(string Front, string Back)>` to `Task<Result<(string Front, string Back)>>`.
    - Remove fallback question/answer generation (`Fallback()`).
    - Return `Result.Failure(Error.Custom("AiService.RecallSynthesisFailed", ...))` on API failure.
- [x] 3.2 Update `CreateCardFromHighlightHandler.cs`:
  - Inspect result of `_geminiAiService.SynthesizeActiveRecallCardAsync`.
  - If `!result.IsSuccess`, return `Result<CreateCardFromHighlightResponse>.Failure(result.Error)`.
  - Verify no `SpacedRepetitionCard` entity is added or persisted to the database on failure.
- [x] 3.3 Refactor `TermExplanationService.cs`:
  - Update default model fallback in constructor to `gemini-3.5-flash-lite`: `configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite"`.
  - Remove `GetFallbackExplanation` return with `HTTP 200`.
  - If Gemini API call fails, times out, or returns empty response:
    - Log `LogLevel.Error`.
    - Return `Result<TermExplanationResult>.Failure(Error.Custom("AiService.Unavailable", "AI term explanation service is temporarily unavailable."))`.
  - Ensure `ExplainTermHandler.cs` propagates this failure so the API returns `HTTP 503` or `HTTP 400` with problem details.

---

## Phase 4: Frontend Error Transparency & Catch Block Cleanup

- [x] 4.1 Update `frontend/composables/useApiError.ts`:
  - Extract `responseData = errorObj?.data || errorObj?.response?._data`.
  - Prioritize RFC 7807 `responseData.detail` or `errorObj.detail`.
  - Prioritize backend custom `responseData.error` or `errorObj.error`.
  - Prioritize machine-readable `responseData.code` or `errorObj.code` translation keys (`api_errors.${code}`).
  - Prioritize network error detection (`NETWORK_ERROR`).
  - Prioritize non-numeric, non-HTTP `message`.
  - Fall back to `fallbackKey` ONLY when the backend provided no `detail`, `error`, or valid `code`.
- [x] 4.2 Clean up empty catch blocks in Vue components and Pinia stores:
  - In `frontend/pages/quiz.vue` (line 117):
    - Replace `libraryStore.fetchBooks().catch(() => {})` with error logging or graceful toast notification.
  - In `frontend/stores/useLibraryStore.ts`:
    - In `fetchSlice` (line 219): replace empty catch with error recording (`error.value = formatError(err)`) and diagnostic logging.
    - In `curateSlice` (line 260): replace empty catch with error recording (`error.value = formatError(err)`) and diagnostic logging.
- [x] 4.3 Update `frontend/components/today/TermExplainerModal.vue`:
  - Ensure the modal displays an explicit error alert when `explainTerm` fails.
  - Include a "Try Again" button that re-invokes `loadExplanation()`.

---

## Phase 5: System AI Health Check Endpoint

- [x] 5.1 Create `SystemEndpoints.cs` in `backend/src/TechDaily.Api/Endpoints/`:
  - Define `MapSystemEndpoints(this RouteGroupBuilder group)`.
  - Add `GET /api/v1/system/ai-health` with summary and documentation tags.
  - Implement health check probe executed concurrently via `Task.WhenAll`:
    - Text model probe: Ping Gemini text model (`gemini-3.5-flash-lite`) and measure latency in milliseconds, returning status and latency.
    - Embedding model probe: Ping `IEmbeddingService.GenerateEmbeddingAsync("health-check")` (`gemini-embedding-001`), measure latency, and assert `dimension == 768`, returning status, latency, and dimension.
  - Return `HTTP 200 OK` when both probes succeed:
    ```json
    {
      "status": "healthy",
      "textModel": "healthy",
      "embeddingModel": "healthy",
      "dimension": 768,
      "details": {
        "text": {
          "model": "gemini-3.5-flash-lite",
          "status": "healthy",
          "latencyMs": 245
        },
        "embedding": {
          "model": "gemini-embedding-001",
          "status": "healthy",
          "dimension": 768,
          "latencyMs": 182
        }
      },
      "timestamp": "2026-09-16T12:00:00Z"
    }
    ```
  - Return `HTTP 503 Service Unavailable` when either probe fails with diagnostic details.
- [x] 5.2 Map system endpoint group in `backend/src/TechDaily.Api/Program.cs`:
  - Map `app.MapGroup("/api/v1/system").WithTags("System Diagnostics & Health").MapSystemEndpoints();`.

---

## Phase 6: Automated Verification & Unit/Integration Tests

- [x] 6.1 Unit Tests for `GeminiEmbeddingService`:
  - Assert that embedding request payload specifies `"outputDimensionality": 768`.
  - Assert that `GeminiEmbeddingService` returns `Result.Failure` when API returns 404/500 and `UseOfflineMock == false`.
  - Assert that `UseOfflineMock == true` generates a 768-D unit vector.
- [x] 6.2 Unit Tests for AI Handlers Fail-Loud Behavior:
  - Assert `GenerateInsightHandler` does not insert into `TechInsights` on Gemini error.
  - Assert `GenerateQuizHandler` does not insert into `QuizQuestions` on Gemini error.
  - Assert `CreateCardFromHighlightHandler` returns failure and does not persist cards on Gemini error.
  - Assert `TermExplanationService` returns failure on Gemini error.
- [x] 6.3 Integration Test for `GET /api/v1/system/ai-health`:
  - Assert 200 OK with `{ textModel: "healthy", embeddingModel: "healthy", dimension: 768 }` verifying `gemini-3.5-flash-lite` and `gemini-embedding-001` when healthy.
  - Assert 503 Service Unavailable with diagnostic payload when Gemini API fails.
- [x] 6.4 Frontend Unit Tests for `useApiError`:
  - Assert that `{ detail: "Rate limit exceeded" }` overrides a generic `fallbackKey`.
  - Assert that `{ error: "Model unavailable" }` overrides a generic `fallbackKey`.
