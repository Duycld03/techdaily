# Proposal: Enforce Auth on Today Hub and Optimize Reader Initial Loading

## Why

1. **Guest Route Ambiguity & Expired Token Redirect Loops:** Currently, `/today` is unauthenticated (guest preview), while other core areas (`/roadmap`, `/quiz`, `/insights`) are protected. If an unauthenticated or expired-session user visits the app, `/today` renders with a "Sign In" button, but submitting drills fails or causes confusion. Furthermore, `frontend/middleware/auth.global.ts` evaluates `hasToken = !!authStore.isLoggedIn || !!tokenCookie.value`, which treats an expired JWT cookie as a valid session. When navigating to `/login`, the middleware redirects the user back to `/today`, causing an infinite redirect loop and infinite spinning on mobile browsers.
2. **Slow GitBook Reader Initial Load (2–3 Seconds Latency):** Currently, `GET /api/v1/library/books/{id}` fetches the entire book entity with `.Include(b => b.Chunks)` and serializes the complete `OriginalTextMarkdown`, `SummaryMarkdown`, `KeyTakeaways`, and `MicroQuiz` of **all chunks** into a single JSON payload. For technical books with 25–70 slices, this payload exceeds 1MB–2MB of text. The client must wait 2–3 seconds for the entire book's contents to transfer and parse before it can even mount Slice 1 and render the Table of Contents.

## What Changes

- **Enforce Strict Authentication on `/today` & `/`:**
  - Add `/today` and `/` to `isAuthRequired` in `frontend/middleware/auth.global.ts`.
  - Fix `hasToken` validation so that it relies strictly on `authStore.isLoggedIn` (valid, non-expired token). Expired cookies will no longer be considered valid session indicators.
  - Apply `.RequireAuthorization()` to `GET /api/v1/daily/today` in `backend/src/TechDaily.Api/Endpoints/DailyFocusEndpoints.cs`, aligning with Domain Rule 1 (Zero fake data / No default fallback users).
- **Lightweight Table of Contents (TOC) & Fast Reader Initial Load:**
  - Modify `GET /api/v1/library/books/{id}` (`GetBookByIdHandler`) to return lightweight TOC slice metadata (`id`, `chunkOrder`, `chapterTitle`, `estimatedReadMinutes`, `isAiFormatted`) without the heavy `OriginalTextMarkdown` of all slices.
  - Introduce `GET /api/v1/library/books/{id}/slices/{chunkOrder}` (`GetBookSliceHandler`) to retrieve the full markdown, takeaways, and quiz for a single slice on demand.
  - Update `useLibraryStore` and `frontend/pages/read/[bookId].vue` to fetch the lightweight TOC first (<100ms response), load the active slice content on demand, and cache visited slices locally in memory.

## Capabilities

### Modified Capabilities
- `auth`: Harden route middleware to check token validity and prevent expired-cookie redirect bounce loops.
- `today`: Require authentication on `/today` and `/`, returning 401 for unauthorized API requests and redirecting unauthenticated users to `/login`.
- `reader`: Optimize reader initial load by loading lightweight TOC metadata first and lazy-loading slice markdown on demand.
- `library`: Provide lightweight book metadata and dedicated single-slice content endpoints.

## Impact

- **API Contracts:**
  - `GET /api/v1/daily/today` now requires `Authorization: Bearer <token>`.
  - `GET /api/v1/library/books/{id}` now returns lightweight `ChunkSummaryDto` items (omitting `OriginalTextMarkdown` across non-active slices).
  - New endpoint: `GET /api/v1/library/books/{id}/slices/{chunkOrder}` for fetching single-slice content.
- **Frontend Pages & Stores:**
  - `frontend/middleware/auth.global.ts`: Protects `/today` and `/`, eliminates infinite loop on expired tokens.
  - `frontend/pages/read/[bookId].vue` & `frontend/stores/useLibraryStore.ts`: Supports lightweight initial load and lazy slice loading.
- **Dependencies:** None.
