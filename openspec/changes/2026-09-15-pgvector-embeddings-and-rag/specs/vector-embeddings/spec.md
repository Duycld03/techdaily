# Delta Specification: Vector Embeddings & pgvector Infrastructure

## ADDED Requirements

### Requirement: Cloud Embedding Service Contract
The system SHALL provide an application-layer interface `IEmbeddingService` for vectorizing text with support for single-text and batch-text operations returning 768-dimensional `Pgvector.Vector` structures.

#### Scenario: Generate embedding for a single text input
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` with valid non-empty text
- **THEN** service invokes Google Gemini `text-embedding-004` API and returns a `Vector` with exactly 768 dimensions.

#### Scenario: Generate batch embeddings for multiple chunks
- **WHEN** client invokes `IEmbeddingService.GenerateBatchEmbeddingsAsync(texts)` with a list of up to 50 text chunks
- **THEN** service sends a batched request to `text-embedding-004:batchEmbedContents` and returns ordered vectors matching the input chunk collection.

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
