# Technical Design: Strict Today Auth & Fast GitBook Reader Loading

## Context

See `proposal.md` for motivation and requirements.
Currently, TechDaily has two critical performance and authorization issues:
1. `/today` is treated as a guest preview route without authentication, and client middleware trusts the presence of `tokenCookie.value` regardless of expiration status, creating redirect bounce loops between `/login` and `/today`.
2. GitBook Reader (`/read/[bookId]`) fetches the entire book with all chunk markdown bodies in a single query via `GET /api/v1/library/books/{id}`, creating 1–2MB JSON payloads and 2–3s loading latency for books with 20–70 slices.

## Goals / Non-Goals

**Goals:**
- Enforce strict JWT authentication for `/today` and `/` across both frontend route middleware and backend Minimal API endpoints.
- Eliminate infinite redirect loops by validating JWT token expiration in `auth.global.ts` via `authStore.isLoggedIn`.
- Reduce initial Reader opening latency from 2–3s down to <100ms by decoupling Table of Contents metadata from heavy markdown content.
- Provide a dedicated, cached on-demand slice content endpoint `GET /api/v1/library/books/{id}/slices/{chunkOrder}`.
- Cache loaded slices in frontend memory during a reading session for zero-latency slice toggling.

**Non-Goals:**
- Changing PDF ingestion worker or background AI slice formatting pipeline.
- Implementing offline reading or client-side IndexedDB persistence for entire books.
- Adding artificial request timeouts (as requested by user).

## Decisions

### 1. Route Guard Hardening & Today Route Protection
- **Decision:** In `frontend/middleware/auth.global.ts`, define `hasToken` strictly as `!!authStore.isLoggedIn`.
  - *Rationale:* `authStore.init()` checks JWT `exp` timestamp. If expired, `authStore.isLoggedIn` evaluates to `false` and clears expired credentials. Relying on `|| !!tokenCookie.value` causes stale cookies to pass the route guard, triggering a redirect bounce loop with `useApiClient`'s 401 response handler.
  - *Protected Routes:* Add `/today` and `/` to `isAuthRequired`. When visitors access `/` or `/today` without a valid session, navigate to `/login?redirect=${encodeURIComponent(to.fullPath)}`.
- **Backend Authorization:** Apply `.RequireAuthorization()` to `group.MapGet("/today", ...)` in `DailyFocusEndpoints.cs`.
  - *Rationale:* Aligns with `AGENTS.md` Rule 1 (Zero Fake Data / No Default User Fallbacks). Rejects unauthenticated requests with clean `401 Unauthorized` problem details.

### 2. Decoupled Table of Contents (TOC) & On-Demand Slice Loading
- **Decision:** Slim `GET /api/v1/library/books/{id}` response to return lightweight TOC metadata, and introduce `GET /api/v1/library/books/{id}/slices/{chunkOrder}` for active slice content.
  - *Alternative Considered:* Paginating slices (e.g. 10 at a time). *Rejected* because the Table of Contents requires all chapter titles to render the navigation sidebar, while the reader only displays 1 slice at a time.
  - *TOC Payload Structure:*
    ```json
    {
      "book": {
        "id": "...",
        "title": "...",
        "totalChunks": 67,
        "chunks": [
          { "id": "...", "chunkOrder": 1, "chapterTitle": "Introduction", "estimatedReadMinutes": 4, "isAiFormatted": true },
          { "id": "...", "chunkOrder": 2, "chapterTitle": "Core Architecture", "estimatedReadMinutes": 5, "isAiFormatted": true }
        ]
      }
    }
    ```
    (Omits `originalTextMarkdown`, `summaryMarkdown`, `microQuiz`, `keyTakeaways` from the TOC array).
  - *Slice Endpoint Contract:*
    `GET /api/v1/library/books/{id}/slices/{chunkOrder}` returns `ChunkDetailDto`:
    ```json
    {
      "slice": {
        "id": "...",
        "chunkOrder": 1,
        "chapterTitle": "Introduction",
        "originalTextMarkdown": "# Introduction...",
        "summaryMarkdown": "...",
        "keyTakeaways": [...],
        "microQuiz": { ... },
        "estimatedReadMinutes": 4,
        "isAiFormatted": true
      }
    }
    ```

### 3. Client-Side Slice Memory Caching in Reader
- **Decision:** Maintain a reactive in-memory map `loadedSlices = ref<Map<number, ChunkSummary>>(new Map())` in `[bookId].vue` (or `useLibraryStore`).
  - *Behavior:*
    1. On mount, fetch lightweight book TOC (<100ms) $\rightarrow$ Reader shell and TOC sidebar render immediately.
    2. Determine target slice order (from `?slice=N` query or localStorage bookmark, fallback to 1).
    3. If `loadedSlices.has(order)`, render immediately from cache.
    4. If not, fetch `GET /api/v1/library/books/{id}/slices/{order}` and store in `loadedSlices`.
    5. The existing debounced lookahead prefetcher for slice $N+1$ directly calls and populates `loadedSlices`, making sequential reading instantaneous.

## Risks / Trade-offs

- **[Risk] Multiple network requests instead of one:**
  - *Mitigation:* The initial TOC request is tiny (<5KB vs 2MB) and completes in <100ms. The active slice request is ~15KB and loads concurrently or immediately after TOC. With in-memory caching and lookahead prefetch, subsequent navigation has 0ms latency.
- **[Risk] Slices edited or re-curated mid-session:**
  - *Mitigation:* `curateSlice` updates the slice in `loadedSlices` upon completion, ensuring re-curated content is immediately reflected in the reader.
