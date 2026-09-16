# Core Platform Capability Delta Specification

## Purpose
Establishes fail-loud and transparent error propagation across all artificial intelligence and vector embedding subsystems, standardizes text generation 100% on `gemini-3.5-flash-lite`, upgrades the Google Gemini embedding model to `gemini-embedding-001` with explicit 768-dimensional Matryoshka Representation Learning (MRL) truncation, enforces seeder and ingestion data integrity by prohibiting synthetic/mock persistence, prioritizes RFC 7807 problem details in frontend error resolution, and introduces a dedicated System AI Health Check endpoint (`GET /api/v1/system/ai-health`).
---

## MODIFIED Requirements

### Requirement: Cloud Embedding Service Contract & Dimensions
The system SHALL provide an application-layer interface `IEmbeddingService` for vectorizing text with support for single-text (`GenerateEmbeddingAsync`) and batch-text (`GenerateBatchEmbeddingsAsync`) operations returning 768-dimensional `Pgvector.Vector` structures.

The embedding service SHALL utilize Google Gemini model `gemini-embedding-001` and SHALL include `"outputDimensionality": 768` in all single and batch Google Generative Language API requests to guarantee exact alignment with the PostgreSQL `vector(768)` database schema.

The embedding service SHALL NOT silently fall back to mock or pseudo-random vector generation when Google API calls fail, when the API key is unconfigured, or when network timeouts occur, unless an explicit configuration flag `Gemini:UseOfflineMock` is set to `true`. When `Gemini:UseOfflineMock` is `false` (the default), the service SHALL return `Result<Vector>.Failure` or `Result<List<Vector>>.Failure` containing structured error details.

#### Scenario: Generate embedding using gemini-embedding-001 with 768 dimensions
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` with valid non-empty text and a valid Gemini API key
- **THEN** service invokes Google Generative Language API endpoint for `gemini-embedding-001:embedContent` specifying `"outputDimensionality": 768`
- **AND** returns a `Result<Vector>` with `IsSuccess = true` containing exactly 768 float elements matching PostgreSQL `vector(768)`.

#### Scenario: Gemini Embedding API returns error status code
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` and Google API responds with HTTP 404, 429, or 500
- **AND** configuration setting `Gemini:UseOfflineMock` is `false`
- **THEN** service logs an error with `LogLevel.Error`
- **AND** returns `Result<Vector>.Failure(Error.Custom("Embedding.ApiError", ...))`
- **AND** does NOT return a deterministic mock vector.

#### Scenario: Gemini API key is missing and offline mock is disabled
- **WHEN** `IEmbeddingService` executes while `Gemini:ApiKey` is empty or whitespace
- **AND** `Gemini:UseOfflineMock` is `false`
- **THEN** service returns `Result<Vector>.Failure(Error.Custom("Embedding.MissingApiKey", ...))`
- **AND** does NOT return a deterministic mock vector.

---

### Requirement: Curriculum Vector Backfill Pipeline
The system SHALL provide a backfill mechanism (`CurriculumSeeder.BackfillEmbeddingsAsync`) that populates `DocumentChunk.Embedding` for seed curriculum chunks where `Embedding IS NULL`.

The backfill mechanism SHALL verify the success of `IEmbeddingService.GenerateBatchEmbeddingsAsync`. If vector generation fails or is incomplete, the mechanism SHALL log `LogLevel.Error`, SHALL NOT assign synthetic mock vectors, and SHALL leave `Embedding = null` in PostgreSQL so subsequent startup runs or manual executions can retry.

#### Scenario: Backfill encounters embedding service failure
- **WHEN** `CurriculumSeeder.BackfillEmbeddingsAsync` executes during startup and `IEmbeddingService.GenerateBatchEmbeddingsAsync` returns a failure result
- **THEN** the seeder logs an error message detailing the embedding failure
- **AND** does NOT save changes to `DocumentChunks`
- **AND** leaves unvectorized chunks with `Embedding = null` in the database.

---

### Requirement: Document Chunk Vectorization on Ingestion
When new documents are ingested via `PdfIngestionWorker`, the worker SHALL attempt to vectorize initial slices using `IEmbeddingService`. If chunking or embedding fails due to unhandled exceptions or API errors, the worker SHALL mark the book status as `ProcessingStatus.Failed`, record the error detail in `ErrorMessage`, and SHALL NOT mark incomplete books as `ProcessingStatus.Ready`.

#### Scenario: PDF ingestion worker encounters unhandled embedding failure
- **WHEN** `PdfIngestionWorker` processes an uploaded PDF and `IEmbeddingService.GenerateBatchEmbeddingsAsync` fails or throws an exception
- **THEN** worker marks `book.Status = ProcessingStatus.Failed`
- **AND** sets `book.ErrorMessage` to the specific failure reason
- **AND** saves changes to PostgreSQL without marking the book as `Ready`.
---

### Requirement: AI Content Generation Handlers & Persistence
AI content generation services (`ITechInsightGenerator`, `IQuizGeneratorService`, and `IGeminiAiService`) SHALL standardize text generation on Google Gemini model `gemini-3.5-flash-lite` across all environments (`appsettings.json`, `appsettings.Development.json`, `appsettings.Local.json`, and service defaults). These services SHALL return `Result.Failure` on Gemini API errors and SHALL NOT return canned fallback entities wrapped in successful results. 
Application use case handlers (`GenerateInsightHandler`, `GenerateQuizHandler`, `CreateCardFromHighlightHandler`) SHALL NOT insert or persist synthetic mock records into production database tables (`TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`) when AI generation fails.

#### Scenario: AI insight generation fails
- **WHEN** `GenerateInsightHandler` invokes `ITechInsightGenerator.GenerateInsightAsync` and Gemini API fails
- **THEN** the generator returns `Result<TechInsight>.Failure`
- **AND** the handler returns `Result<TechInsightDto>.Failure` without adding any record to the `TechInsights` table.

#### Scenario: AI quiz question generation fails
- **WHEN** `GenerateQuizHandler` requests new questions from `IQuizGeneratorService` and Gemini API fails
- **THEN** the service returns `Result<List<QuizQuestion>>.Failure`
- **AND** the handler does NOT persist canned mock questions to the `QuizQuestions` table.

#### Scenario: Active recall flashcard synthesis fails
- **WHEN** `CreateCardFromHighlightHandler` requests flashcard synthesis from `IGeminiAiService.SynthesizeActiveRecallCardAsync` and Gemini API fails
- **THEN** the service returns `Result.Failure`
- **AND** the handler returns `Result<CreateCardFromHighlightResponse>.Failure` without inserting a fallback card into `SpacedRepetitionCards`.

---

### Requirement: AI Term Explanation & Cache Protocol
`ITermExplanationService.ExplainTermAsync` SHALL check exact database cache and semantic HNSW vector cache before invoking Gemini text generation (`gemini-3.5-flash-lite`). If the term is not cached and the Gemini API call fails, the service SHALL return `Result<TermExplanationResult>.Failure` with code `AiService.Unavailable`. The service SHALL NOT return generic boilerplate sentences wrapped in `HTTP 200 OK`.

The endpoint `POST /api/v1/daily/explain-term` SHALL return an error HTTP status (such as `400 Bad Request` or `503 Service Unavailable`) when term explanation fails, enabling the frontend reader modal to display a clear error state and retry prompt.

#### Scenario: Term explanation fails during cache miss
- **WHEN** client sends `POST /api/v1/daily/explain-term` for an uncached term and Gemini text generation fails
- **THEN** the service returns `Result.Failure(Error.Custom("AiService.Unavailable", ...))`
- **AND** the API responds with a non-200 HTTP status code and error payload
- **AND** does NOT cache or return generic boilerplate text.

---

### Requirement: Frontend Client Error Resolution & Problem Details
The client error resolution composable `frontend/composables/useApiError.ts` SHALL prioritize RFC 7807 problem details (`detail`) and backend custom error messages (`error`) over generic caller-specified fallback keys (`fallbackKey`).

#### Scenario: Backend returns RFC 7807 ProblemDetails with detail
- **WHEN** an API request fails and the backend returns `{ "detail": "Google Gemini rate limit exceeded", "status": 429 }`
- **AND** the caller invokes `formatError(err, "quiz.generate_error")`
- **THEN** `formatError` returns `"Google Gemini rate limit exceeded"` instead of the generic translation for `"quiz.generate_error"`.

#### Scenario: Backend returns custom error payload
- **WHEN** an API request fails and the backend returns `{ "code": "Embedding.ApiError", "error": "Gemini Embedding API returned status 503" }`
- **AND** the caller invokes `formatError(err, "common.error")`
- **THEN** `formatError` returns `"Gemini Embedding API returned status 503"`.

---

## NEW Requirements

### Requirement: System AI Health Check Endpoint
The platform SHALL provide an operational diagnostics endpoint `GET /api/v1/system/ai-health` that concurrently probes Google Gemini text generation (`gemini-3.5-flash-lite`) and Google Gemini embedding generation (`gemini-embedding-001`) via `Task.WhenAll`.

The health check SHALL verify:
1. Text model (`gemini-3.5-flash-lite`) connectivity and latency.
2. Embedding model (`gemini-embedding-001`) connectivity, latency, and vector dimensionality (asserting length == 768).

The endpoint SHALL respond with `HTTP 200 OK` containing `{ textModel: "healthy", embeddingModel: "healthy", dimension: 768 }` when both probes succeed, or `HTTP 503 Service Unavailable` with diagnostic error details when either probe fails.

#### Scenario: AI health check probe succeeds
- **WHEN** a client or monitoring agent invokes `GET /api/v1/system/ai-health`
- **AND** Gemini text generation (`gemini-3.5-flash-lite`) and Gemini embedding generation (`gemini-embedding-001`) both respond successfully with a 768-dimensional vector
- **THEN** the server returns `HTTP 200 OK`
- **AND** the JSON response contains `status = "healthy"`, `textModel = "healthy"`, `embeddingModel = "healthy"`, and `dimension = 768`.

#### Scenario: AI health check probe detects dimension mismatch or model failure
- **WHEN** a client invokes `GET /api/v1/system/ai-health` and either `gemini-3.5-flash-lite` fails or `gemini-embedding-001` returns a vector with dimension != 768 or returns an HTTP error
- **THEN** the server returns `HTTP 503 Service Unavailable`
- **AND** the JSON response contains `status = "unhealthy"` with diagnostic details identifying the failing model and error reason.

### Requirement: Controlled Offline Mock Vector Configuration
The platform SHALL support an explicit configuration flag `Gemini:UseOfflineMock` in `appsettings*.json`. The system SHALL strictly disallow mock vector generation in production and default environments, permitting mock generation only when `Gemini:UseOfflineMock` is explicitly configured as `true`.

#### Scenario: Offline developer enables mock vectors
- **WHEN** `Gemini:UseOfflineMock` is configured to `true` in `appsettings.Development.json`
- **AND** `IEmbeddingService` is invoked without a valid Gemini API key or network connection
- **THEN** the service logs a warning that offline mock mode is active
- **AND** returns a deterministic 768-dimensional unit vector wrapped in `Result.Success`.

#### Scenario: Production environment with missing key and offline mock disabled
- **WHEN** `Gemini:UseOfflineMock` is `false` (default)
- **AND** `IEmbeddingService` is invoked without a valid Gemini API key
- **THEN** the service returns `Result.Failure(Error.Custom("Embedding.MissingApiKey", ...))`
- **AND** refuses to generate or persist mock vectors.
