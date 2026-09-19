# vector-embeddings Specification

## Purpose
TBD - created by archiving change 2026-09-15-pgvector-embeddings-and-rag. Update Purpose after archive.

## Requirements

### Requirement: Cloud Embedding Service Contract
The system SHALL provide an application-layer interface `IEmbeddingService` for vectorizing text with support for single-text and batch-text operations returning 768-dimensional `Pgvector.Vector` structures powered by Google Gemini Developer API models (`gemini-embedding-2` or `gemini-embedding-001` under the Free Tier).

#### Scenario: Generate embedding for a single text input
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` with valid non-empty text
- **THEN** service invokes Google Gemini embedding API (`gemini-embedding-2` or `gemini-embedding-001`) with `outputDimensionality = 768`
- **AND** returns a `Vector` with exactly 768 dimensions.

#### Scenario: Generate batch embeddings for multiple chunks
- **WHEN** client invokes `IEmbeddingService.GenerateBatchEmbeddingsAsync(texts)` with a list of up to 50 text chunks
- **THEN** service sends a batched request to `:batchEmbedContents` and returns ordered vectors matching the input chunk collection.

#### Scenario: Offline mock mode remains deterministic
- **WHEN** `Gemini:ApiKey` is empty or `Gemini:UseOfflineMock` is true
- **THEN** service returns a deterministic 768-dimensional vector without making outbound HTTP calls to Google Gemini API.
---

### Requirement: Document Chunk Vectorization on Ingestion
When new documents are ingested via PDF upload or web crawling, the ingestion pipeline SHALL generate and persist vector embeddings for each document chunk (`DocumentChunk.Embedding`) using `IEmbeddingService`.

#### Scenario: PDF ingestion worker processes document slices
- **WHEN** `PdfIngestionWorker` splits an uploaded PDF into bookmark slices
- **THEN** worker generates 768-dimensional embeddings for each chunk combining `ChapterTitle`, `SummaryMarkdown`, and key takeaways before saving to PostgreSQL.

#### Scenario: Web article crawler extracts online article
- **WHEN** `CrawlUrlHandler` parses an external technical article
- **THEN** handler computes the vector embedding for the resulting chunk and stores it in `DocumentChunk.Embedding`.

---

### Requirement: Curriculum Vector Backfill Pipeline
The system SHALL provide an idempotent backfill mechanism that populates `Embedding` for all existing seed curriculum chunks where `Embedding IS NULL`.

#### Scenario: Backfill runner executes on startup or manual invocation
- **WHEN** backfill service queries `DocumentChunks` where `Embedding == null`
- **THEN** service batches chunks, calls `IEmbeddingService.GenerateBatchEmbeddingsAsync`, updates entities, and persists changes without altering existing chapter text or order.

### Requirement: Cache Hygiene for Term Explanation Service
The `TermExplanationService` SHALL guarantee database cache hygiene by strictly reserving persistence into `TermExplanationCaches` for verified, non-empty responses successfully returned by the Google Gemini API. Local fallback explanations generated on API failure or missing credentials MUST NEVER be persisted to the database. Additionally, legacy database entries containing mock or fallback placeholder text SHALL be purged.

#### Scenario: Successful Gemini LLM generation persists to cache
- **WHEN** a term explanation request results in a cache miss and Gemini returns a valid, non-empty explanation string
- **THEN** the service generates the term vector embedding via `IEmbeddingService`, stores the record in `TermExplanationCaches` with `HitCount = 1`, and returns the explanation with `isFromCache = false`.

#### Scenario: Gemini API failure or missing key bypasses database cache
- **WHEN** Gemini API returns an HTTP error, throws a network exception, or when `Gemini:ApiKey` is unconfigured
- **THEN** the service returns a localized fallback explanation from `GetFallbackExplanation(term, category, locale)` with `isFromCache = false`, and MUST NOT create or persist any record in `TermExplanationCaches`.

#### Scenario: Poisoned legacy cache entries purged from database
- **WHEN** the cache hygiene cleanup routine or database migration executes
- **THEN** all rows in `TermExplanationCaches` whose `ExplanationText` matches fallback placeholder patterns (e.g., matching `"represents a core runtime or architectural mechanism"` or `"Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại"`) are permanently deleted.

#### Scenario: Subsequent requests for previously failed terms attempt fresh generation
- **WHEN** a user requests an explanation for a term that previously triggered a fallback after Gemini connectivity is restored
- **THEN** because no poisoned cache record was persisted, the service attempts a fresh call to Gemini API rather than returning a cached fallback.
