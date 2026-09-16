# Tasks: Clean Up Fallback and Silent Failure Data Across Local and Production Databases

## Phase 1: Maintenance Tooling & Dry-Run Scripts

- [x] 1.1 Create standalone SQL maintenance scripts in `scripts/`:
  - Create `scripts/cleanup-dry-run.sql`:
    - Wrap entire script in `BEGIN ... ROLLBACK`.
    - Query counts of tainted records in `TermExplanationCaches` matching fallback phrases.
    - Query counts of tainted records in `TechInsights` matching mock slug patterns (`-[0-9a-f]{6}$`) and mock titles.
    - Query counts of tainted records in `QuizQuestions` matching canned question templates and deep dive explanations.
    - Query counts of tainted records in `SpacedRepetitionCards` matching generic highlight prompt templates.
    - Query counts of `DocumentChunks` where `Embedding IS NULL`.
  - Create `scripts/cleanup-execute.sql`:
    - Wrap entire script in `BEGIN ... COMMIT`.
    - Execute atomic `DELETE FROM "TermExplanationCaches"` for fallback phrases.
    - Execute atomic `DELETE FROM "TechInsights"` for mock slugs/titles (cascades to `UserInsightBookmarks`).
    - Execute atomic `DELETE FROM "QuizQuestions"` for mock templates (cascades to `UserQuizProgresses`; sets null on `SpacedRepetitionCards`).
    - Execute atomic `DELETE FROM "SpacedRepetitionCards"` for boilerplate highlight cards (preserving parent `UserHighlights`).
- [x] 1.2 Implement Maintenance CLI Commands in `TechDaily.Api`:
  - Create `DatabaseMaintenanceRunner.cs` in `backend/src/TechDaily.Infrastructure/Maintenance/`:
    - Implement `AnalyzeTaintedDataAsync`: Query and log counts across all 4 content tables and unvectorized chunks.
    - Implement `PurgeTaintedDataAsync`: Transactional purge matching the SQL execute criteria.
    - Implement `BackfillEmbeddingsAsync`: Query chunks `WHERE Embedding IS NULL`, batch by 25, call `IEmbeddingService.GenerateBatchEmbeddingsAsync` with `gemini-embedding-001` (768-D), assert dimension == 768, update entities, save changes, and sleep 200ms between batches.
    - Implement `ReseedCatalogAsync`: Invoke `TechInsightsSeeder.SeedAsync` and `CurriculumSeeder.SeedAsync`.
  - Update `backend/src/TechDaily.Api/Program.cs`:
    - Check command-line arguments for `--cleanup-data`.
    - Parse flags `--dry-run`, `--execute`, `--backfill-embeddings`, `--batch-size=<N>`, `--reseed-catalog`.
    - Dispatch to `DatabaseMaintenanceRunner` before starting Kestrel or exit after completion.
- [x] 1.3 Add automated unit/integration tests for maintenance tooling:
  - Create test in `TechDaily.Tests` asserting that `AnalyzeTaintedDataAsync` correctly flags mock data signatures without modifying the database.
  - Create test asserting that `PurgeTaintedDataAsync` preserves curated seed items and user highlights.

---

## Phase 2: Local Database Purge & Backfill (`techdaily_postgres`)

- [x] 2.1 Verify local PostgreSQL container and database connectivity:
  - Run `docker ps --filter "name=techdaily_postgres"` to verify PostgreSQL 17 is running.
  - Run `pg_isready -h localhost -p 5432 -U techdaily_user -d techdaily_db`.
- [x] 2.2 Execute dry-run analysis on local database:
  - Execute `docker exec -i techdaily_postgres psql -U techdaily_user -d techdaily_db < scripts/cleanup-dry-run.sql`.
  - Record the diagnostic report and baseline counts of tainted rows.
- [x] 2.3 Execute transactional purge on local database:
  - Execute `docker exec -i techdaily_postgres psql -U techdaily_user -d techdaily_db < scripts/cleanup-execute.sql`.
  - Verify that 0 errors occurred and transaction committed.
- [x] 2.4 Execute catalog re-seeding on local database:
  - Run `dotnet run --project backend/src/TechDaily.Api -- --cleanup-data --reseed-catalog`.
  - Confirm `TechInsights` contains all 20+ curated seed insights with canonical slugs.
- [x] 2.5 Execute batched vector backfill on local database:
  - Run `dotnet run --project backend/src/TechDaily.Api -- --cleanup-data --backfill-embeddings --batch-size=25`.
  - Verify that all 465+ chunks (including curriculum Days 28, 29, 30) receive 768-D vectors.
- [x] 2.6 Verify local database integrity:
  - Run `scripts/cleanup-dry-run.sql` and verify that all tainted row counts and unvectorized chunk counts return `0`.

---

## Phase 3: Production Database Purge & Backfill (Google Cloud VPS)

- [x] 3.1 Establish secure SSH session to Google Cloud VPS:
  - Connect via SSH: `ssh duycld03@<VPS_IP>`.
  - Navigate to project directory: `cd /home/duycld03/workspace/techdaily`.
  - Verify production containers: `docker ps --filter "name=techdaily"`.
- [x] 3.2 Create mandatory pre-cleanup database snapshot:
  - Run `docker exec techdaily_postgres_prod pg_dump -Fc -U techdaily_user -d techdaily_db > /tmp/backup_pre_cleanup_$(date +%Y%m%d_%H%M%S).dump`.
  - Verify backup file exists and has non-zero size (`ls -lh /tmp/backup_pre_cleanup_*.dump`).
- [x] 3.3 Execute dry-run analysis on production database:
  - Run `docker exec -i techdaily_postgres_prod psql -U techdaily_user -d techdaily_db < scripts/cleanup-dry-run.sql`.
  - Inspect output table and verify counts of tainted records before proceeding.
- [x] 3.4 Execute transactional purge on production database:
  - Run `docker exec -i techdaily_postgres_prod psql -U techdaily_user -d techdaily_db < scripts/cleanup-execute.sql`.
  - Confirm execution succeeded without locking timeouts.
- [x] 3.5 Execute catalog re-seeding and vector backfill on production:
  - Execute maintenance runner in backend container:
    `docker exec techdaily_backend_prod dotnet TechDaily.Api.dll --cleanup-data --backfill-embeddings --reseed-catalog --batch-size=25`.
  - Monitor logs for progress and ensure all batches complete successfully.
- [x] 3.6 Verify production HNSW index coverage:
  - Verify that `DocumentChunks` has 0 rows where `Embedding IS NULL`.
  - Confirm HNSW index `ix_document_chunks_embedding` is valid and active.

---

## Phase 4: Post-Cleanup Integrity Verification & Operational Validation

- [x] 4.1 Execute post-cleanup verification query across both environments:
  - Run automated query checking for 0 tainted terms, 0 tainted insights, 0 tainted quizzes, 0 tainted cards, and 0 unvectorized chunks.
- [x] 4.2 Verify System AI Health Check endpoint:
  - Execute `curl -s http://localhost:5000/api/v1/system/ai-health` (and on production VPS).
  - Verify response is `HTTP 200 OK` with `status = "healthy"`, `embeddingModel = "healthy"`, and `dimension = 768`.
- [x] 4.3 Verify semantic search functionality (RAG):
  - Execute test query against `/api/v1/daily/explain-term` and verify genuine semantic cache hit or Gemini explanation.
  - Verify similarity search on curriculum Days 28, 29, 30 returns relevant chunks.
- [x] 4.4 Verify User Experience & Frontend:
  - In Nuxt UI, navigate to Tech Insights feed, Interview Quiz, Daily Focus, and Spaced Repetition Review deck.
  - Verify no broken cards, empty mocks, or layout anomalies exist.
- [x] 4.5 Document maintenance results and archive snapshot:
  - Record pre-cleanup and post-cleanup record counts in maintenance log.
  - Move backup dump to durable offsite/cold storage in accordance with retention policy.
