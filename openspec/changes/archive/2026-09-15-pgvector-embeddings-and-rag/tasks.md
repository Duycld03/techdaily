# Tasks: pgvector Embeddings, Semantic Caching, In-Book RAG, Grounded Quizzes, and Anti-Spam

## 1. Application Layer (Contracts, Use Cases & Validation)
- [x] 1.1 Define `IEmbeddingService` interface in `TechDaily.Application/Interfaces` with `GenerateEmbeddingAsync` and `GenerateBatchEmbeddingsAsync`.
- [x] 1.2 Create DTOs and Request/Response models for `AskBookRequest` and `AskBookResponse` with citation metadata.
- [x] 1.3 Implement `AskBookValidator` enforcing question length between 3 and 300 characters to prevent prompt stuffing.
- [x] 1.4 Implement `AskBookHandler : IUseCase<AskBookRequest, AskBookResponse>` with scoped pgvector search and grounded Gemini prompt.
- [x] 1.5 Update `GenerateQuizRequest` and `GenerateQuizHandler` to accept optional `BookId` and `IsGrounded` flags.
- [x] 1.6 Register new use cases and validators in `TechDaily.Application/DependencyInjection.cs`.

## 2. Infrastructure Layer (Gemini Client & Semantic Cache)
- [x] 2.1 Implement `GeminiEmbeddingService` in `TechDaily.Infrastructure/Services` integrating Google `text-embedding-004` API.
- [x] 2.2 Wire `GeminiEmbeddingService` into DI in `TechDaily.Infrastructure/DependencyInjection.cs`.
- [x] 2.3 Upgrade `TermExplanationService` with two-tier caching: Tier 1 exact B-Tree lookup $\rightarrow$ Tier 2 pgvector HNSW cosine distance search (`<= 0.08`) $\rightarrow$ Tier 3 Gemini Flash fallback with embedding persistence.
- [x] 2.4 Hook embedding generation into `PdfIngestionWorker` and `CrawlUrlHandler` to populate `DocumentChunk.Embedding` for newly ingested slices.
- [x] 2.5 Implement a startup/on-demand curriculum embedding backfill service to vectorize all seed chunks where `Embedding IS NULL`.

## 3. API & Infrastructure Security Layer (Endpoints, Rate Limiting & Anti-Spam)
- [x] 3.1 Configure ASP.NET Core `RateLimiter` in `Program.cs` with an `AiEndpointsPolicy` (sliding window: max 10 req/min per user/IP, returning 429 Too Many Requests).
- [x] 3.2 Update `nginx/nginx.conf` with dedicated `limit_req_zone $binary_remote_addr zone=ai_limit:10m rate=10r/m;` for heavy AI/RAG routes (`/ask`, `/explain-term`, `/quiz/generate`).
- [x] 3.3 Expose `POST /api/v1/library/books/{id}/ask` in `LibraryEndpoints.cs` requiring authorization, rate limiting policy, and resolving `AskBookHandler`.
- [x] 3.4 Attach `RequireRateLimiting("AiEndpointsPolicy")` to `/api/v1/daily-focus/explain-term` and `/api/v1/quiz/generate`.
- [x] 3.5 Ensure full `CancellationToken` propagation across all vector and RAG endpoints to abort in-flight requests on client disconnect.

## 4. Frontend Layer (UI Components & Reader Integration)
- [x] 4.1 Create `frontend/components/reader/AskBookDrawer.vue` featuring slide-over panel, message history, in-flight mutex (`isAsking`), disabled buttons with loading spinners, and clickable citation pills.
- [x] 4.2 Integrate `AskBookDrawer` into `frontend/pages/read/[bookId].vue` and `frontend/pages/today.vue` with button trigger and `Shift + ?` hotkey.
- [x] 4.3 Update `FloatingExplainer.vue` to enforce 400ms selection debounce and display a discreet `⚡ Instant Cache` badge when served from semantic cache.
- [x] 4.4 Add "Grounded in Book" toggle option in `frontend/pages/quiz.vue` with button click guarding (`if (quizStore.isGenerating) return;`).
- [x] 4.5 Add bilingual localization keys in `en.json` and `vi.json` for all new drawer, rate-limit warnings, and grounded quiz UI elements.

## 5. Verification & Testing
- [x] 5.1 Unit test `GeminiEmbeddingService` and `TermExplanationService` semantic cache hit/miss behavior (`dotnet test`).
- [x] 5.2 Unit test `AskBookHandler` verifying scoped retrieval, citation mapping, and input length validation.
- [x] 5.3 Test rate limiting: send rapid requests to AI endpoints and verify `429 Too Many Requests` is returned after 10 requests.
- [x] 5.4 Local verification: Test floating term explainer with synonymous terms and verify sub-150ms cache hits without calling Gemini.
- [x] 5.5 Local verification: Open reader, ask questions via `AskBookDrawer`, verify citation links jump to the correct slice, and test client-side disabled buttons during in-flight requests.
