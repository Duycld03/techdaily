# Curated Catalog Operations, Ingestion Worker Recovery & Rollback Runbook

## 1. Overview
The Curated Catalog architecture separates administrator-prepared technical literature (`CatalogDocument` and `CatalogSlice`) from personal learner libraries (`DocumentBook` and `DocumentChunk`).

Ingestion processing is asynchronous, lease-backed, and generation-tracked to prevent race conditions, duplicate slices, and unrecoverable zombie processing states.

---

## 2. Ingestion Worker Recovery Procedures

### 2.1 Automatic Startup Recovery
When the ASP.NET Core API or worker process starts (`PdfIngestionWorker.ExecuteAsync`):
1. **Query Active Processing Records:** The worker scans for `CatalogDocument` records with `ProcessingStatus = ProcessingStatus.Processing`.
2. **Transition to Recoverable Failed State:**
   - `ProcessingStatus = ProcessingStatus.Failed`
   - `ProcessingStatusMessage = "Interrupted - retry available"`
   - `ProcessingErrorMessage = "Preparation was interrupted by system shutdown or worker restart. Please retry preparation."`
   - `ProcessingLeaseExpiresAt = null`
3. **Audit Log:** Logs the number of recovered documents.
4. **Administrator Workspace Reflection:** In the Administrator Catalog workspace (`/admin/catalog`), the document displays an amber/red "Interrupted" banner with an explicit **Retry Preparation** action.

### 2.2 Retry Handling & Stale Worker Protection
- **Generation Increment:** Each preparation retry increments `ProcessingGeneration`.
- **Lease Timeout:** During active extraction, workers renew `ProcessingLeaseExpiresAt = UtcNow + 10 minutes`.
- **Stale Worker Rejection:** If a slow or delayed background task attempts to write after a retry has started, the destination verifies `job.Generation == doc.ProcessingGeneration`. Any mismatch causes the older job to gracefully terminate without modifying the document.
- **Slice Idempotence:** Retries atomically delete and replace that attempt's extracted slice set in `CatalogSlices`, preventing duplicate slice orders or orphaned chunks.
- **Missing Temporary Files:** If a server restarts and the temporary PDF upload file in `/tmp` has been deleted by OS cleanup, the worker sets `ProcessingErrorMessage = "Temporary upload file is no longer available. Please upload the PDF again to retry preparation."` The administrator simply re-uploads the PDF file in the workspace.

---

## 3. Database Migration Procedures

### 3.1 Additive Schema Changes
The migration `20261014103046_AddCuratedCatalogTables` introduces:
- **Columns on `DocumentBooks`:**
  - `SourceAttribution` (`character varying(2000)`, nullable)
  - `SourceCatalogDocumentId` (`uuid`, nullable)
  - `SourceCatalogRevision` (`integer`, nullable)
- **Tables:**
  - `CatalogDocuments` (Rich catalog metadata, permission tracking, review state, visibility, and generation lease)
  - `CatalogSlices` (Chapter markdown, summaries, takeaways, estimated minutes, AI formatting flag, pgvector embedding)
- **Filtered Indexes:**
  - `IX_DocumentBooks_CreatedByUserId_SourceCatalogDocumentId`: Partial unique index ensuring at most one active adopted copy per user.
    `WHERE "CreatedByUserId" IS NOT NULL AND "SourceCatalogDocumentId" IS NOT NULL AND "IsDeleted" = false`
  - `IX_CatalogSlices_CatalogDocumentId_SliceOrder`: Unique slice ordering.
    `WHERE "IsDeleted" = false`
  - `IX_CatalogSlices_Embedding`: HNSW approximate nearest neighbor index for cosine similarity search (`vector_cosine_ops`).

### 3.2 Applying Migration to PostgreSQL
```bash
set -a; source .env; set +a
dotnet ef database update --project backend/src/DeepPace.Infrastructure --startup-project backend/src/DeepPace.Api
```

---

## 4. Rollback Procedures

### 4.1 Application & UI Rollback (Zero DB Downtime)
Because all database changes are strictly additive (new tables and nullable columns on `DocumentBooks`), the API and Frontend containers can be safely rolled back to a previous image without rolling back the database. Existing personal imports and learning data continue operating without disruption.

### 4.2 Database Rollback (If Required)
If a complete database rollback is deemed necessary during a planned maintenance window:
1. **Safety Backup:**
   ```bash
   pg_dump -U deeppace_user -d deeppace_db -Fc -f /backups/deeppace_pre_rollback.dump
   ```
2. **Revert Migration:**
   ```bash
   set -a; source .env; set +a
   dotnet ef database update 20261014083824_AddSoftDeletePartialIndexesAndSliceLookup --project backend/src/DeepPace.Infrastructure --startup-project backend/src/DeepPace.Api
   ```
3. **Data Preservation Rule:**
   Never truncate or drop personal books (`DocumentBooks`) when reverting catalog tables.
