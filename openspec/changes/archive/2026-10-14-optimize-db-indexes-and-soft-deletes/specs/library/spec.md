# Spec Delta

## ADDED Requirements

### Requirement: Document Chunk Composite Slice Indexing
The system persistence configuration for `DocumentChunk` SHALL define a composite unique index on `(DocumentBookId, ChunkOrder)` filtered by `WHERE "IsDeleted" = false` (`IX_DocumentChunks_DocumentBookId_ChunkOrder`) to optimize sequential reading slice retrieval (`GET /api/v1/library/books/{id}/slices/{chunkOrder}`) to an $O(1)$ index seek.

#### Scenario: Reading slice lookup by book ID and chunk order
- **WHEN** an authenticated user navigates to reading slice $N$ of a book via `GetBookSlice`
- **THEN** the database query resolves the chunk using the composite index on `(DocumentBookId, ChunkOrder)` without sequential table scanning
