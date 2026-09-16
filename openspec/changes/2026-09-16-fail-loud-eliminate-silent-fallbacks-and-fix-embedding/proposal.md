# Proposal: Fail Loud & Transparent: Eliminate Silent Fallbacks, Fix Gemini Embedding Model, and Add AI Health Check

## Title
Fail Loud & Transparent: Eliminate Silent Fallbacks, Fix Gemini Embedding Model, and Add AI Health Check

## Context & Problem Statement

TechDaily relies on artificial intelligence for daily technical learning, personalized semantic retrieval (RAG), active recall flashcard generation, scenario interview quizzes, and interactive term explanations. These capabilities depend heavily on two Google Gemini APIs:
1. **Gemini Text Generation:** Standardized 100% on `gemini-3.5-flash-lite` across all environments (`appsettings.json`, `appsettings.Development.json`, `appsettings.Local.json`, `GeminiAiService.cs`, `TermExplanationService.cs`) for generating insights, interview quizzes, active recall flashcards, and term explanations (selected for superior instruction following, under-the-hood analysis, and Vietnamese technical phrasing while retaining <200ms latency).
2. **Gemini Embedding Generation:** Vector embeddings standardized on `gemini-embedding-001` with explicit `outputDimensionality: 768` for document chunks (`DocumentChunk.Embedding`) and term explanation semantic caching (`TermExplanationCache.Embedding`), indexed via PostgreSQL `pgvector` with HNSW cosine distance indexes.

Investigation of recent system behavior and backend codebase telemetry reveals severe architectural flaws rooted in **silent mock fallbacks**, **model deprecation**, **database schema mismatch**, and **frontend error opacity**:

### 1. Google Gemini Embedding 404 & Silent Mock Vector Poisoning
- The configured embedding model in `GeminiEmbeddingService.cs` is `text-embedding-004`. Google has deprecated/migrated this endpoint on the Generative Language API v1beta, causing every embedding request to return **HTTP 404 Not Found**.
- Instead of bubbling this failure up to callers, `GeminiEmbeddingService.cs` intercepted every non-200 HTTP response, missing API key, and network exception by silently calling `GenerateDeterministicMockVector(text)`.
- Because this mock generator succeeded without error, background seeders (`CurriculumSeeder.BackfillEmbeddingsAsync`) and ingestion workers (`PdfIngestionWorker`) assumed vectors were successfully generated and persisted deterministic pseudo-random vectors into PostgreSQL.
- As a consequence, semantic search (HNSW cosine similarity queries) against `DocumentChunks` and `TermExplanationCaches` was operating on meaningless pseudo-random noise, completely destroying RAG accuracy while giving operators zero warning that embedding calls were failing.

### 2. Matryoshka Representation Learning (MRL) 768-Dimension Alignment
- Google's recommended embedding model is now `gemini-embedding-001`.
- `gemini-embedding-001` natively supports Matryoshka Representation Learning (MRL) and can output embeddings with dimensions such as 768, 1536, or 3072. By default, without an explicit dimensionality parameter, the API returns higher-dimensional vectors.
- The PostgreSQL schema in `TechDailyDbContext.cs` strictly defines `vector(768)` for both `DocumentChunks.Embedding` and `TermExplanationCaches.Embedding`.
- Without specifying `"outputDimensionality": 768` in single (`embedContent`) and batch (`batchEmbedContents`) request payloads, calls to `gemini-embedding-001` will fail PostgreSQL type constraints or result in dimensional array mismatches.

### 3. Canned Mock Data Insertion in AI Handlers Polluting Production Tables
- `GeminiAiService.cs` contains fallback methods (`GenerateMockInsight` and `GenerateMockQuestions`) that generate hardcoded boilerplate records whenever Gemini is unavailable or returns an error.
- `GenerateInsightHandler.cs` and `GenerateQuizHandler.cs` inspect `result.IsSuccess`. Because `GeminiAiService` returned mock objects wrapped in a successful `Result`, these handlers persisted canned mock insights into the `TechInsights` table and canned mock questions into the `QuizQuestions` table.
- Similarly, `CreateCardFromHighlightHandler` invoked `SynthesizeActiveRecallCardAsync`, which caught Gemini errors and returned a canned generic question ("What is the core architectural principle behind..."), creating and persisting a low-quality flashcard into `SpacedRepetitionCards`.
- In `TermExplanationService.cs`, when Gemini failed or returned no text, `GetFallbackExplanation` returned a generic boilerplate sentence ("The term X in Y represents a core runtime or architectural mechanism...") wrapped in an `HTTP 200 OK` response. Users were deceived into believing the AI had provided an insightful definition, and the system missed opportunities to report an outage.

### 4. Background Seeder & Ingestion Integrity
- `CurriculumSeeder.BackfillEmbeddingsAsync` quietly bypassed errors when batch generation failed, leaving developers in the dark regarding vector initialization.
- In `PdfIngestionWorker`, unhandled chunking and embedding errors were caught with warnings, but the worker proceeded to set `book.Status = ProcessingStatus.Ready` (100% progress), leaving documents in an incomplete, un-vectorized state instead of marking them as `ProcessingStatus.Failed` with clear diagnostic details.

### 5. Frontend Error Opacity and Swallowed Exceptions
- In `frontend/composables/useApiError.ts`, the formatting logic evaluated caller-provided `fallbackKey` translations *before* checking backend RFC 7807 `detail` or `error` properties. Even when the backend returned meaningful error messages (such as `Embedding.ApiError: Google API quota exceeded`), the UI displayed generic localized placeholders (e.g., "Failed to generate quiz").
- Nuxt components and stores (`frontend/pages/quiz.vue` line 117 and `frontend/stores/useLibraryStore.ts` lines 219, 260) contained empty `catch { return null }` or `catch(() => {})` blocks that silently swallowed network and ingestion failures.

### 6. Lack of System AI Health Observability
- The existing `/health` endpoint only checks database connectivity (`db.Database.CanConnectAsync()`).
- There is no automated operational probe verifying that Google Gemini text generation and embedding endpoints are reachable, authenticated, fast, and producing vectors of the exact expected dimensionality (768). Outages could only be discovered after users reported feature failures.

---

## Proposed Solution

We propose a comprehensive **"Fail Loud & Transparent"** architectural overhaul across backend services, persistence pipelines, API diagnostics, and frontend error formatting:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                FAIL LOUD & TRANSPARENT ARCHITECTURE                    │
│                                                                                        │
│   Incoming AI Operation                                                                │
│   (Insight / Quiz / Recall Card / Term / Embeddings / PDF Ingestion)                   │
│                                  │                                                     │
│                                  ▼                                                     │
│                      ┌──────────────────────────────────────────────┐                  │
│                      │  Google Gemini API                           │                  │
│                      │  - Text: gemini-3.5-flash-lite               │                  │
│                      │  - Embedding: gemini-embedding-001 (768-D)   │                  │
│                      └──────────────────────┬───────────────────────┘                  │
│                                             │                                          │
│                        ┌────────────────────┴───────────────────┐                      │
│                        ▼                                        ▼                      │
│                [ 200 OK Valid ]                         [ 4xx / 5xx / Timeout ]        │
│                        │                                        │                      │
│                        ▼                                        ▼                      │
│               Persist Real Data                        DO NOT PERSIST MOCK DATA        │
│               Return Result.Success                    DO NOT RETURN HTTP 200          │
│                                                                 │                      │
│                                                                 ▼                      │
│                                                    Return Result.Failure               │
│                                                    (Embedding.ApiError / AiUnavailable)│
│                                                                 │                      │
│                                                                 ▼                      │
│                                                    Transparent RFC 7807 Error          │
│                                                    Frontend shows real diagnostic UI   │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Key Pillars of the Change:

1. **Standardize Text Generation on `gemini-3.5-flash-lite` Across All Environments:**
   - Standardize text generation model across all environments (`appsettings.json`, `appsettings.Development.json`, `appsettings.Local.json`, `GeminiAiService.cs`, `TermExplanationService.cs`) on `gemini-3.5-flash-lite`.
   - Leverages superior instruction following, under-the-hood analysis, and Vietnamese technical phrasing while retaining <200ms latency.

2. **Migrate to `gemini-embedding-001` with 768-D MRL Enforcement:**
   - Replace deprecated `text-embedding-004` with `gemini-embedding-001` across configuration files (`appsettings.json`, `appsettings.Development.json`, `appsettings.Local.json`) and service defaults in `GeminiEmbeddingService.cs`.
   - Add `"outputDimensionality": 768` to both single (`embedContent`) and batch (`batchEmbedContents`) payloads to ensure Google Gemini truncates vectors to match PostgreSQL `vector(768)`.
3. **Eliminate Silent Mock Vector Fallbacks:**
   - Remove automatic calls to `GenerateDeterministicMockVector` in `GeminiEmbeddingService.cs`.
   - Return explicit `Result<Vector>.Failure(Error.Custom("Embedding.ApiError", ...))` and `Result<List<Vector>>.Failure(...)` on any API error, HTTP status failure, or missing API key.
   - Retain `GenerateDeterministicMockVector` strictly behind an explicit configuration flag: `Gemini:UseOfflineMock == true`, intended solely for local offline development without internet.

4. **Enforce Seeder & Ingestion Integrity:**
   - In `CurriculumSeeder.BackfillEmbeddingsAsync`, log failed vector generations with `LogLevel.Error`, avoid saving any fake vectors, and preserve `Embedding = null` so future runs or manual backfills can retry cleanly.
   - In `PdfIngestionWorker`, unhandled chunking or embedding failures must mark the book with `ProcessingStatus.Failed` and persist the explicit exception message in `book.ErrorMessage`.

5. **Eliminate Canned/Mock Data Insertion Across AI Handlers:**
   - `GeminiAiService.cs`:
     - Discontinue `GenerateMockInsight` fallback in `GenerateInsightAsync`. Return `Result<TechInsight>.Failure` so the handler never saves fake insights to the database.
     - Discontinue `GenerateMockQuestions` fallback in `GenerateQuestionsAsync`. Return `Result<List<QuizQuestion>>.Failure` so the handler never saves fake questions to the database.
     - In `SynthesizeActiveRecallCardAsync`, return `Result<(string Front, string Back)>.Failure` on Gemini errors; update `CreateCardFromHighlightHandler` to fail loudly without inserting boilerplate flashcards into `SpacedRepetitionCards`.
   - `TermExplanationService.cs`:
     - Stop returning generic sentences (`GetFallbackExplanation`) with `HTTP 200 OK`. Return `Result<TermExplanationResult>.Failure(Error.Custom("AiService.Unavailable", ...))` so the frontend displays a clear error state ("AI service temporarily unavailable") with a retry action.

6. **Frontend Error Transparency & Catch Block Cleanup:**
   - Update `frontend/composables/useApiError.ts` to prioritize RFC 7807 `detail` or backend `error` messages over generic fallback keys.
   - Remove empty catch blocks in `frontend/pages/quiz.vue` and `frontend/stores/useLibraryStore.ts`, ensuring background failures are either logged or presented appropriately.

7. **System AI Health Check Endpoint (`GET /api/v1/system/ai-health`):**
   - Provide a dedicated endpoint in `TechDaily.Api` that actively probes:
     - Text generation model (`gemini-3.5-flash-lite`) connectivity and latency.
     - Embedding model (`gemini-embedding-001`) connectivity, latency, and vector dimensionality (asserting length == 768).
   - Return `HTTP 200 OK` with detailed status on health, or `HTTP 503 Service Unavailable` with structured diagnostic error details on failure.

---

## User Value & Operational Impact

1. **Zero Database Corruption:** Database tables (`DocumentChunks`, `TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`) will only contain real, high-quality AI-generated content or valid user data—never synthetic canned mock records.
2. **Deterministic Vector Similarity:** Semantic vector search (HNSW cosine similarity) works with true semantic representations from `gemini-embedding-001`, resolving 404 errors and dimension mismatch bugs.
3. **Transparent User Experience:** When Google AI experiences rate limits or outages, users are informed with clear, actionable UI messages and retry options instead of being presented with meaningless canned text.
4. **Proactive Operational Monitoring:** DevOps and automated uptime monitors can query `GET /api/v1/system/ai-health` to instantly verify API key validity, latency, and vector dimension compliance.

---

## Risks & Mitigations

| Risk | Impact | Mitigation Strategy |
| :--- | :--- | :--- |
| **Transient Gemini API outages block feature requests** | High | Fail loud transparently with retry buttons on UI. Semantic cache in `TermExplanationCache` continues to serve cached terms without hitting Gemini. |
| **Offline developers cannot run the backend without Gemini API keys** | Medium | Provide `Gemini:UseOfflineMock: true` setting in `appsettings.Development.json` for developers working in air-gapped environments. |
| **PostgreSQL vector dimension mismatch** | High | Explicitly specify `"outputDimensionality": 768` in all embed requests and enforce unit/integration tests asserting vector length equals 768. |
| **Existing database records contain legacy mock vectors or canned entries** | Low | Existing clean-up scripts and backfill runners can identify records with `Embedding == null` or purge legacy canned phrases. |
