# Implementation Tasks: Strict Today Auth & Fast GitBook Reader Loading

## 1. Authentication & Route Guard Hardening

- [x] 1.1 Update `frontend/middleware/auth.global.ts` to include `/today` and `/` in `isAuthRequired`.
- [x] 1.2 Harden `hasToken` in `frontend/middleware/auth.global.ts` to strictly require `!!authStore.isLoggedIn`, purging expired tokens and preventing redirect bounce loops when navigating to `/login`.
- [x] 1.3 Add `.RequireAuthorization()` to `group.MapGet("/today", ...)` in `DailyFocusEndpoints.cs` and verify unauthenticated requests return HTTP 401.

## 2. Backend Lightweight TOC & Single Slice API

- [x] 2.1 Update `GetBookByIdHandler.cs` and `BookDetailDto` to return lightweight chunk summaries (omitting heavy `OriginalTextMarkdown`, `SummaryMarkdown`, `KeyTakeaways`, and `MicroQuiz` across non-active chunks) so TOC payload is <5KB.
- [x] 2.2 Create `GetBookSliceHandler.cs`, `GetBookSliceRequest`, and `GetBookSliceResponse` in `TechDaily.Application.Features.Library.GetBookSlice`.
- [x] 2.3 Map `GET /api/v1/library/books/{id:guid}/slices/{order:int}` in `LibraryEndpoints.cs` with proper 404 handling.
- [x] 2.4 Add backend unit tests in `TechDaily.Tests` verifying `GetBookById` returns lightweight TOC and `GetBookSlice` returns full slice markdown.

## 3. Frontend Store & GitBook Reader Optimization

- [x] 3.1 Update `useLibraryStore.ts` with `fetchSlice(bookId, chunkOrder)` action to fetch individual slice details on demand.
- [x] 3.2 Update `frontend/pages/read/[bookId].vue` to render TOC immediately from lightweight book details and load the active slice on demand.
- [x] 3.3 Implement in-memory slice caching (`loadedSlices` map) in `[bookId].vue` so toggling between previously read slices is instantaneous (0ms latency, no re-fetching).
- [x] 3.4 Wire lookahead prefetching in `[bookId].vue` to pre-populate `loadedSlices` for the next sequential slice.

## 4. Verification & Testing

- [x] 4.1 Run backend automated test suite (`dotnet test`) and frontend test suite (`npm test`).
- [x] 4.2 Verify `/today` and `/` redirect unauthenticated visitors cleanly to `/login?redirect=/today` without spinning or infinite loops.
- [x] 4.3 Verify `/read/[bookId]` loads the reader header and TOC in <100ms and displays the active slice smoothly.
