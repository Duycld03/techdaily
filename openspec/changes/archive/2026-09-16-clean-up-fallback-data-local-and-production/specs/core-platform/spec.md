# Core Platform Capability Delta Specification

## Purpose
Establishes database hygiene invariants, defines criteria for identifying and purging poisoned synthetic and boilerplate fallback data across relational storage, mandates zero synthetic mock persistence, defines the database maintenance runner specification (supporting transactional dry-run previews, atomic purges, and curated catalog re-seeding), and enforces batched 768-dimensional vector backfill for all unvectorized document chunks using `gemini-embedding-001`.
---

## MODIFIED Requirements

### Requirement: Curriculum Vector Backfill Pipeline
The system SHALL provide a batched backfill mechanism (`CurriculumSeeder.BackfillEmbeddingsAsync` and `DatabaseMaintenanceRunner.BackfillEmbeddingsAsync`) that iteratively scans and vectorizes all `DocumentChunks` where `Embedding IS NULL` in configurable batches (default 25 chunks per batch) until zero unvectorized chunks remain.

The backfill mechanism SHALL vectorize chunks using Google Gemini model `gemini-embedding-001` specifying `"outputDimensionality": 768`, assert that returned vectors have a length of exactly 768 floats, pace requests with an inter-batch delay to respect API rate limits, and persist changes transactionally.

#### Scenario: Backfill encounters embedding service failure
- **WHEN** `CurriculumSeeder.BackfillEmbeddingsAsync` executes during startup and `IEmbeddingService.GenerateBatchEmbeddingsAsync` returns a failure result
- **THEN** the seeder logs an error message detailing the embedding failure
- **AND** does NOT save changes to `DocumentChunks`
- **AND** leaves unvectorized chunks with `Embedding = null` in the database.

#### Scenario: Backfill processes all unvectorized chunks across multiple batches
- **WHEN** the maintenance backfill runner executes against a database with 465 unvectorized document chunks
- **THEN** the runner processes chunks in sequential batches of 25
- **AND** generates 768-dimensional vectors for each batch using `gemini-embedding-001`
- **AND** updates `DocumentChunk.Embedding` in PostgreSQL
- **AND** continues until 0 chunks remain with `Embedding IS NULL`.

#### Scenario: Backfill handles transient Google API rate limiting
- **WHEN** the embedding service encounters an HTTP 429 rate limit or transient network timeout during a batch backfill
- **THEN** the runner applies exponential backoff and retries the batch up to 3 times
- **AND** logs warning details without terminating the entire maintenance process prematurely.

---

### Requirement: AI Content Generation Handlers & Persistence
AI content generation services SHALL standardize text generation on Google Gemini model `gemini-3.5-flash-lite` and return `Result.Failure` on API errors without returning canned fallback entities wrapped in successful results. 

Application database tables (`TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`) SHALL NOT contain synthetic mock records, canned boilerplate explanations, or un-synthesized flashcard templates. Any such records identified by database hygiene maintenance runners SHALL be purged.

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

#### Scenario: Detection of legacy boilerplate flashcards
- **WHEN** a database maintenance scan evaluates `SpacedRepetitionCards`
- **AND** a card has `SourceType = 'Highlight'` with front matching `"What is the core architectural principle behind: %"` or `"Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong %"`
- **THEN** the card is flagged as tainted fallback data and deleted
- **AND** the associated `UserHighlights` record is preserved intact.

#### Scenario: Detection of legacy mock insights
- **WHEN** a database maintenance scan evaluates `TechInsights`
- **AND** an insight record has a slug ending in a randomized 6-character hex suffix (`-[0-9a-f]{6}`) and matches mock pool title signatures
- **THEN** the insight is flagged as tainted mock data and deleted
- **AND** any associated `UserInsightBookmarks` are removed via cascading foreign key deletion.

---

## ADDED Requirements
### Requirement: Database Hygiene & Tainted Data Elimination
The platform SHALL provide an operational data hygiene capability to identify, analyze, and purge synthetic, boilerplate, or corrupted fallback data across `TermExplanationCaches`, `TechInsights`, `QuizQuestions`, and `SpacedRepetitionCards`.

The data hygiene capability SHALL enforce the following invariants:
1. **Term Explanation Hygiene:** No cache entry shall contain boilerplate phrases (`"represents a core runtime or architectural mechanism"` or `"Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại"`).
2. **Tech Insights Hygiene:** No insight shall contain canned mock titles or randomized slug suffixes not defined in `tech-insights.json`.
3. **Quiz Questions Hygiene:** No quiz question shall contain deterministic mock question templates (`"When addressing \"%\", which architectural strategy is optimal?"`) or mock deep dive explanation templates.
4. **Flashcard Hygiene:** No spaced repetition card shall contain boilerplate prompt templates generated during Gemini API outages.

#### Scenario: Dry-run analysis previews affected records without data mutation
- **WHEN** an operator runs the data hygiene tooling with `--dry-run`
- **THEN** the tooling counts all records matching tainted fallback signatures across `TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`, and `DocumentChunks`
- **AND** emits a structured diagnostic count report
- **AND** rolls back the database transaction, guaranteeing zero data mutations.

#### Scenario: Transactional purge removes tainted data atomically
- **WHEN** an operator runs the data hygiene tooling with `--execute`
- **THEN** the tooling executes atomic `DELETE` statements inside a `BEGIN ... COMMIT` transaction
- **AND** removes all tainted records matching the hygiene criteria
- **AND** preserves all genuine user highlights, user profiles, and curated seed data.

---

### Requirement: Document Chunk Vector Completeness Invariant
Every document chunk in `DocumentChunks` associated with active curriculum books and imported technical publications SHALL possess a valid, non-null 768-dimensional float vector (`Embedding IS NOT NULL`) before being included in semantic vector similarity search or RAG retrieval pipelines.

#### Scenario: Verification of vector completeness post-maintenance
- **WHEN** post-maintenance integrity verification is executed
- **THEN** a query for `SELECT COUNT(*) FROM "DocumentChunks" WHERE "Embedding" IS NULL` returns exactly `0`
- **AND** an approximate nearest neighbor cosine similarity query using the `hnsw` index executes successfully without error.

---

### Requirement: Database Maintenance CLI & Tooling Contract
The platform application `TechDaily.Api` SHALL provide command-line arguments for headless operational maintenance:
- `--cleanup-data`: Activates maintenance mode.
- `--dry-run`: Analyzes and reports tainted records within an aborted transaction.
- `--execute`: Executes atomic data purge within a committed transaction.
- `--backfill-embeddings`: Iteratively backfills unvectorized document chunks using `IEmbeddingService`.
- `--batch-size=<N>`: Controls the chunk batch size for embedding requests (bounds: 5 to 50, default: 25).
- `--reseed-catalog`: Restores canonical catalog entries from `tech-insights.json` and `curriculum-30-days.json`.

#### Scenario: Headless execution in container environment
- **WHEN** the maintenance CLI is executed inside a container via `dotnet TechDaily.Api.dll --cleanup-data --execute --backfill-embeddings --reseed-catalog`
- **THEN** the application executes the purge, re-seeds curated items, completes the vector backfill, and exits with code `0`.

#### Scenario: Maintenance CLI dry-run reports non-zero tainted records
- **WHEN** the maintenance CLI is executed with `--cleanup-data --dry-run` against a database containing legacy fallback records
- **THEN** the CLI outputs the exact count of tainted records per table and exits with code `0` without altering database state.
