# Technical Design: Curation Concurrency Deduplication & Debounced Prefetch

## Architecture Overview

This design coordinates the frontend client and backend application service to guarantee that heavy AI operations (Gemini curation, takeaway synthesis, scenario drill generation) are executed strictly once per slice, while pacing background lookahead requests so rapid user browsing does not overwhelm API limits.

```
[User Action] ──> checkAndCurateSlice(slice X)
                         │
                         ▼
        ┌──────────────────────────────────┐
        │ inFlightCurations.has(key)?      │
        └────────────────┬─────────────────┘
                         │
         YES ────────────┴──────────── NO
          │                             │
          ▼                             ▼
   [Await existing]            [Dispatch HTTP POST]
          │                             │
          │                             ▼
          │                     [CurateSliceHandler]
          │                             │
          │                             ▼
          │                   [Keyed SemaphoreSlim]
          │                             │
          │                     IsAiFormatted?
          │                      ├── YES ──> [Return from DB]
          │                      └── NO  ──> [Call Gemini API (once)]
          │
          ▼
   [Render Slice X] ──(after 2.5s debounce)──> triggerLookaheadPrefetch(X+1)
```

---

## 1. Frontend Deduplication (`useLibraryStore.ts`)

### State

```typescript
const inFlightCurations = new Map<string, Promise<ChunkSummary | null>>();
```

### Flow in `curateSlice(bookId: string, order: number)`

1. Compute cache key: `const key = `${bookId}:${order}``.
2. If `inFlightCurations.has(key)`, return `inFlightCurations.get(key)!`.
3. Create new promise wrapped in an async function:
   - Try: execute `api.post<{ chunk: ChunkSummary }>(...)`
   - Catch: return `null`
   - Finally: `inFlightCurations.delete(key)`
4. Store promise: `inFlightCurations.set(key, promise)`.
5. Return promise.

---

## 2. Frontend Prefetch Debouncing (`pages/read/[bookId].vue` & `pages/today.vue`)

### Timer Lifecycle

```typescript
let prefetchTimeoutId: ReturnType<typeof setTimeout> | null = null;

function scheduleLookaheadPrefetch() {
  if (prefetchTimeoutId) {
    clearTimeout(prefetchTimeoutId);
    prefetchTimeoutId = null;
  }
  prefetchTimeoutId = setTimeout(() => {
    triggerLookaheadPrefetch();
  }, 2500);
}
```

### Invalidation Triggers

- `watch(activeChunkIndex)`: calls `scheduleLookaheadPrefetch()` instead of immediate prefetch.
- `onUnmounted`: clears `prefetchTimeoutId`.

---

## 3. Backend Concurrency Serialization (`CurateSliceHandler.cs`)

### Concurrency Primitives

```csharp
private static readonly ConcurrentDictionary<string, SemaphoreSlim> _sliceLocks = new();
```

### Double-Checked Locking Flow

1. Find initial chunk by `(request.BookId, request.ChunkOrder)`. If null, return 404.
2. If `chunk.IsAiFormatted == true`, return existing DTO immediately without taking any lock.
3. Compute lock key: `var key = $"{request.BookId}:{request.ChunkOrder}"`.
4. Acquire keyed semaphore:

   ```csharp
   var semaphore = _sliceLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
   await semaphore.WaitAsync(cancellationToken);
   try
   {
       // Double-check: re-query chunk state under lock
       var freshChunk = await _dbContext.DocumentChunks
           .FirstOrDefaultAsync(c => c.DocumentBookId == request.BookId && c.ChunkOrder == request.ChunkOrder, cancellationToken);

       if (freshChunk == null) return Error.NotFound;
       if (freshChunk.IsAiFormatted)
       {
           return new CurateSliceResponse { Chunk = MapToDto(freshChunk) };
       }

       // Only execute external AI call if still uncurated
       var aiResult = await _aiFormatter.FormatSliceAsync(...);
       ...
       await _dbContext.SaveChangesAsync(cancellationToken);
       return new CurateSliceResponse { Chunk = MapToDto(freshChunk) };
   }
   finally
   {
       semaphore.Release();
   }
   ```

---

## 4. Key Invariants Preserved

- **Single Source of Truth:** `DocumentChunks.IsAiFormatted` remains the authoritative marker.
- **No Resource Leaks:** Semaphores are always released in `finally` blocks, and timers in Vue components are cleared in `onUnmounted`.
- **CancellationToken Support:** Both semaphore acquisition and HTTP calls properly accept and propagate `cancellationToken`.
