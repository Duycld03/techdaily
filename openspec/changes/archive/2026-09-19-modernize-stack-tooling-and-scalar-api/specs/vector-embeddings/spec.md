# Spec Delta: Vector Embeddings

## MODIFIED Requirements

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
