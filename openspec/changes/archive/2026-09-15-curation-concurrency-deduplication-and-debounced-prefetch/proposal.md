# Proposal: Curation Concurrency Deduplication & Debounced Prefetch

## Executive Summary
## Why

Rapid browsing across document slices triggers duplicate in-flight curation requests for the same slice between user navigation and lookahead prefetch. When multiple identical requests are dispatched to Gemini Flash Lite simultaneously, the API encounters throttling and server overload (HTTP 503 `ServiceUnavailable`), triggering backoff retries that extend slice loading times from ~7s up to 15s–65s.

## What Changes

1. **Frontend In-Flight Request Deduplication:** Reusing active curation Promises in `useLibraryStore` so identical slice requests never create redundant HTTP calls.
2. **Frontend Smart Prefetch Debouncing:** Adding a 2.5s settling timer to lookahead prefetch in `read/[bookId].vue` and `today.vue` so rapid chapter browsing does not spam prefetch requests.
3. **Backend Keyed Double-Checked Locking:** Adding a keyed `SemaphoreSlim` per `(BookId, ChunkOrder)` in `CurateSliceHandler` to serialize concurrent requests on the backend, ensuring Gemini is called at most once per slice even across multiple browser tabs.

---

## Scope of Changes

- `frontend/stores/useLibraryStore.ts`: Add `inFlightCurations` Map to deduplicate concurrent calls to `curateSlice(bookId, order)`.
- `frontend/pages/read/[bookId].vue`: Debounce `triggerLookaheadPrefetch()` by 2.5 seconds and cancel pending timers on slice change or unmount.
- `frontend/pages/today.vue`: Debounce `triggerNextDayPrefetch()` similarly.
- `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs`: Implement keyed concurrency serialization (`ConcurrentDictionary<string, SemaphoreSlim>`) with double-checked locking against `chunk.IsAiFormatted`.
- Unit tests & integration tests for both frontend and backend concurrency scenarios.

---

## User Impact

- **Zero redundant AI generation calls:** A slice is generated once and only once.
- **Faster UI responsiveness:** Clicking on a slice that is already being prefetched immediately connects to the existing promise rather than waiting for a redundant second generation.
- **Reduced 503 errors and zero quota waste:** Avoids slamming the Gemini API with burst traffic during rapid table-of-contents navigation.
