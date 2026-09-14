# Library Specification Delta: Curation Concurrency & Deduplication

## Requirements

### Requirement: In-Flight Slice Curation Deduplication

The library store SHALL maintain an active in-flight request map for slice curation calls indexed by `${bookId}:${chunkOrder}`. When `curateSlice(bookId, order)` is invoked while an identical request is already pending, the store SHALL return the existing active `Promise<ChunkSummary | null>` rather than dispatching a duplicate HTTP request to the backend.

#### Scenario: User navigates to a slice that is already undergoing background prefetch

- **GIVEN** slice 9 of a book is actively being prefetched by the reader lookahead service
- **WHEN** user clicks directly on slice 9 in the table of contents
- **THEN** `libraryStore.curateSlice` returns the in-flight prefetch promise
- **AND** zero additional HTTP POST requests are dispatched for slice 9.

#### Scenario: In-flight promise completes or fails

- **WHEN** an in-flight slice curation promise resolves or rejects
- **THEN** the store removes the `${bookId}:${chunkOrder}` key from its active map so subsequent calls can fetch fresh state if needed.

---

### Requirement: Keyed Concurrency Locking in Slice Curation API

The backend `CurateSliceHandler` SHALL serialize concurrent execution for the same `(BookId, ChunkOrder)` using a keyed lock. Upon acquiring the lock, the handler SHALL re-evaluate `chunk.IsAiFormatted` (double-checked locking). If another concurrent request has already completed curation and saved the result, the handler SHALL immediately return the formatted slice without invoking the external AI formatting service.

#### Scenario: Concurrent curation requests arrive at the backend

- **GIVEN** two concurrent requests arrive for `POST /api/v1/library/books/{bookId}/slices/{order}/curate`
- **WHEN** the first request acquires the lock and calls Gemini AI formatting
- **THEN** the second request waits on the lock
- **AND** once the first request commits the formatted slice to the database, the second request acquires the lock, detects `chunk.IsAiFormatted == true`, and returns the existing entity without calling Gemini AI.

#### Scenario: CancellationToken cancellation during lock wait

- **WHEN** a client disconnects while waiting for the slice lock
- **THEN** the handler releases any acquired resources and propagates `OperationCanceledException` cleanly without corrupting the lock dictionary.
