# Proposal: pgvector Embeddings, Semantic Caching, In-Book RAG, and Grounded Quizzes

## Why
TechDaily's database layer already runs PostgreSQL 17 with the `pgvector` extension, equipped with `vector(768)` columns and compiled HNSW indexes (`IX_DocumentChunks_Embedding`, `IX_TermExplanationCaches_Embedding`). However, these vector capabilities remain unactivated with `Embedding` values set to `NULL`.

At the same time, three critical platform limitations and cost risks exist:
1. **Redundant AI Tooltip Overhead:** The floating term explainer (`TermExplanationService`) relies on strict string matching (`t.Term == normalizedTerm`). Minor lexical variations (e.g. *"optimistic locking"* vs *"optimistic concurrency control"*) trigger redundant Gemini 3.6 Flash completions, inducing 2–3s tooltip delays and unnecessary LLM token consumption.
2. **Passive Reading vs. Active In-Book Q&A:** When reading complex architecture manuals or 500-page uploaded PDFs, engineers cannot query the document interactively. They must manually skim hundreds of pages to find answers to specific implementation tradeoffs.
3. **Ungrounded Generic Quizzes:** The interview quiz generator at `/quiz` currently queries internet-level knowledge from Gemini without anchoring to the specific curriculum slices or custom literature the user is studying.
4. **API Token Exhaustion & Abuse Risks (Spam):** Heavy AI generation and RAG endpoints (`/explain-term`, `/ask`, `/quiz/generate`) require rigorous defense against malicious or accidental request flooding, rapid clicking, and prompt stuffing that could exhaust Gemini API quotas or strain low-spec host VPS instances.

Hosting local embedding models on small production VPS servers (1–2 vCPUs, 1–2 GB RAM) poses high Out-Of-Memory (OOM) risks. Utilizing Google's cloud-based **`text-embedding-004`** model consumes **0 MB RAM and 0% CPU** on the host server while producing native 768-dimensional embeddings that match the database schema and HNSW indexes.

## What Changes
1. **Embedding Foundation (`IEmbeddingService` & `GeminiEmbeddingService`):**
   - Provide a clean abstraction `IEmbeddingService` in the Application layer.
   - Implement `GeminiEmbeddingService` in Infrastructure utilizing Google's `text-embedding-004` (768-dimension vectors) with batch vectorization support.
2. **Semantic Cache for AI Term Explainer (Feature 3):**
   - Upgrade `TermExplanationService` to a two-tier cache:
     - Tier 1: Exact string match via B-Tree index (~1ms).
     - Tier 2: Semantic vector match via `TermExplanationCaches.Embedding` HNSW index with `CosineDistance <= 0.08` (~92% similarity).
     - Fallback: On miss, generate explanation via Gemini Flash, vectorize the term, and persist both.
3. **Curriculum & Document Ingestion Vectorization Pipeline:**
   - Automatically generate embeddings during PDF processing (`PdfIngestionWorker`) and Web article crawling (`CrawlUrlHandler`).
   - Provide a lightweight backfill utility to vectorize all 30-day curriculum slices.
4. **"Ask AI About This Book" (In-Context RAG - Feature 2):**
   - Add endpoint `POST /api/v1/library/books/{id}/ask` executing `AskBookHandler`.
   - Perform scoped vector similarity search against `DocumentChunks` belonging to the active book (`DocumentBookId == id`).
   - Construct grounded prompt for `gemini-3.5-flash-lite` with strict citation bounds.
   - Add responsive slide-over Q&A drawer (`AskBookDrawer.vue`) to the reader with clickable slice citations that jump directly to source passages.
5. **Grounded AI Quiz & Mastery Arena (Feature 4):**
   - Enhance `GenerateQuizHandler` to support a "Book / Curriculum Grounded" mode.
   - Retrieve top matching technical chunks via pgvector to ground generated scenario questions and option explanations in authoritative platform literature.
6. **Multi-Layer Anti-Spam & Rate Limiting Protection:**
   - **Dedicated AI Rate Limiting:** Enforce ASP.NET Core `RateLimiter` sliding window policy and Nginx `limit_req zone=ai_limit rate=10r/m burst=5 nodelay` on all AI-invoking endpoints (`/explain-term`, `/ask`, `/quiz/generate`), returning `429 Too Many Requests`.
   - **Client-Side Action Guarding:** In-flight mutex locks (`isAsking`, `isGenerating`), selection debouncing (400ms), and disabled button states preventing multi-click spam.
   - **Input Length & Boundary Constraints:** Enforce strict FluentValidation boundaries (e.g. questions: 3–300 chars; terms: 2–500 chars) to prevent prompt injection and token ballooning.
   - **Immediate Cancellation Propagation:** Propagate `HttpContext.RequestAborted` to abort in-flight Gemini HTTP requests if a user closes the drawer or navigates away.

## Capabilities
- **`specs/vector-embeddings/spec.md`:** Embedding service contract, Google `text-embedding-004` client, batch vectorization, and curriculum backfill.
- **`specs/reader/spec.md`:** Semantic tooltip cache, in-book RAG drawer, and multi-layer anti-spam rate limiting.
- **`specs/quiz/spec.md`:** Grounded quiz generation anchored to document chunks and curriculum topics.

## Scope & Impact
- **Backend:**
  - `TechDaily.Application`: `IEmbeddingService.cs`, `AskBookRequest.cs`, `AskBookHandler.cs`, `GenerateQuizRequest.cs` updates, FluentValidation boundaries.
  - `TechDaily.Infrastructure`: `GeminiEmbeddingService.cs`, `TermExplanationService.cs`, `PdfIngestionWorker.cs`, `CrawlUrlHandler.cs`, `CurriculumSeeder.cs`.
  - `TechDaily.Api`: `LibraryEndpoints.cs` (`/api/v1/library/books/{id}/ask`), `Program.cs` RateLimiter policies.
  - `nginx`: `nginx/nginx.conf` dedicated `ai_limit` zone for AI endpoints.
- **Frontend:**
  - `frontend/components/reader/AskBookDrawer.vue`: Slide-over chat drawer with grounded Q&A, citation pills, and anti-spam debounced submission.
  - `frontend/pages/read/[bookId].vue`: Integration button and shortcut (`Shift + ?`) to toggle Ask Book drawer.
  - `frontend/pages/quiz.vue`: Topic selector supporting grounded book / curriculum focus.
- **Database:**
  - No new migrations required; existing `Embedding vector(768)` columns and HNSW indexes on `DocumentChunks` and `TermExplanationCaches` are utilized.
