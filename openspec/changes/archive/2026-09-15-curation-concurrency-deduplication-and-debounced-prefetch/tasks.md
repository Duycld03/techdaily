# Tasks: Curation Concurrency Deduplication & Debounced Prefetch

## 1. Frontend In-Flight Promise Deduplication

- [x] 1.1 Add `inFlightCurations` Map to `frontend/stores/useLibraryStore.ts`.
- [x] 1.2 Update `curateSlice(bookId, order)` in `useLibraryStore.ts` to check, register, and clear in-flight promises.
- [x] 1.3 Add unit tests in `frontend/tests/stores/useLibraryStore.spec.ts` verifying concurrent calls to `curateSlice` share the same promise.

## 2. Frontend Prefetch Debouncing

- [x] 2.1 Implement `scheduleLookaheadPrefetch` with 2.5-second debounce in `frontend/pages/read/[bookId].vue`.
- [x] 2.2 Add cleanup of pending prefetch timer on component unmount and slice navigation in `read/[bookId].vue`.
- [x] 2.3 Implement matching prefetch debounce and cleanup in `frontend/pages/today.vue`.

## 3. Backend Concurrency Serialization & Double-Checked Locking

- [x] 3.1 Add keyed `SemaphoreSlim` collection (`_sliceLocks`) to `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs`.
- [x] 3.2 Implement double-checked locking inside `CurateSliceHandler.ExecuteAsync` to prevent duplicate Gemini invocations.
- [x] 3.3 Add unit/integration test in `backend/tests/TechDaily.Tests` verifying that concurrent `CurateSliceRequest` calls for the same chunk execute Gemini formatting exactly once.

## 4. Automated Verification & Regression Testing

- [x] 4.1 Run `dotnet test backend/tests/TechDaily.Tests` to verify all backend tests pass.
- [x] 4.2 Run `npm test` to verify all frontend tests pass.
- [x] 4.3 Verify in browser/Playwright that rapid slice navigation in Gitbook Reader loads cleanly without duplicate network requests or UI stalls.
