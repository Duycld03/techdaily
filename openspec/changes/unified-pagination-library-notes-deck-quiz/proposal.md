# Proposal: Unified Pagination Strategy for Library, Notes, Deck & Quiz

## Why

As TechDaily expands its multi-thousand-page technical document ingestion, active recall flashcards, personal reading notes, and interview question pools, data density across the application has grown significantly. The platform currently suffers from fragmented, unscalable, or incomplete pagination mechanisms across four critical learning surfaces:

1. **Technical Library (`/library`): Unbounded Full-Catalog Payloads**
   - **Current State & Friction**: The endpoint `GET /api/v1/library/books` returns an unpaginated flat array of all books in the database. 
   - **Scalability Bottleneck**: As users import large technical PDF books, whitepapers, and crawled web documentation series, the catalog size grows to hundreds and eventually thousands of volumes. Loading this unconstrained dataset into the browser DOM consumes excessive memory, creates layout thrashing during client-side hydration, and inflates mobile data payloads.
   - **Architectural Rationale**: Introducing grid-aligned offset pagination (`page=1`, `pageSize=12`) solves this bottleneck. The page size of 12 is mathematically optimal because 12 is the least common multiple of standard responsive CSS grid columns: 1 column on mobile, 2 columns on tablet, 3 columns on desktop, and 4 columns on ultra-wide viewports ($12 \pmod 1 = 0, 12 \pmod 2 = 0, 12 \pmod 3 = 0, 12 \pmod 4 = 0$). This eliminates ragged trailing grid rows and guarantees visual balance across all viewports.

2. **Reading Notes & Highlights (`/notes`): Monolithic Highlight Loading & Tag Disconnection**
   - **Current State & Friction**: The endpoint `GET /api/v1/notes/highlights` returns every highlight, reflection note, and parent book/chapter metadata belonging to the authenticated user in a single monolithic query.
   - **Scalability Bottleneck**: Dedicated software engineers routinely highlight dozens of excerpts per chapter across multiple technical books. Transporting and rendering hundreds of full highlight cards simultaneously degrades DOM rendering performance, while instant tag filtering and keyword search risk falling out of sync if pagination is implemented naively without global tag frequency counts.
   - **Architectural Rationale**: Implementing stream-friendly pagination (`page=1`, `pageSize=15`) with dual presentation modes (accessible numbered pagination and an append-based "Load More Notes" stream) ensures sub-100ms response times. Crucially, the backend must return aggregated global tag frequencies across the user's entire highlight corpus alongside paginated slices, ensuring that the horizontal dynamic tag chip bar (`Tất cả (N)`, `#tag (count)`) remains accurate and actionable regardless of the active page window.

3. **Flashcard Deck Management (`/review` - Tab "Kho thẻ của tôi"): Truncated Controls & Broken URL State**
   - **Current State & Friction**: The backend endpoint `GET /api/v1/review/cards` already implements `page` and `pageSize` with `totalCount` and `DeckStatisticsDto`. However, the frontend `/review` interface only provides bare "Previous" and "Next" buttons with a static label `Page X of Y`.
   - **Scalability & UX Defect**: Users with large flashcard decks (e.g. 500+ cards across algorithms, system design, and language runtimes) cannot jump directly to a specific page or see page distribution. More critically, the current page index is held strictly in local component memory: refreshing the browser or copying the URL completely discards the user's active page, resetting them back to page 1.
   - **Architectural Rationale**: The deck management view requires full numbered pagination controls (`< 1 2 3 ... 8 >`) with active state badges, mobile ellipsis compaction, and two-way browser URL query synchronization (`?page=N&search=S&status=X&sourceType=Y`).

4. **Quiz Mistake Review Queue (`/quiz` - Tab "Review Queue / Hàng đợi ôn tập"): Unpaginated Queue UI**
   - **Current State & Friction**: While the backend endpoint `GET /api/v1/quiz/review-queue` accepts `page` and `pageSize` and calculates `totalCount`, the frontend review queue tab renders all received questions in an unpaginated linear list with no pagination navigation at all.
   - **Scalability & UX Defect**: Engineers who have accumulated substantial mistake queues (e.g. 40+ incorrect senior interview questions) experience vertical scroll fatigue. Furthermore, the single "Start Review" action forces an all-or-nothing review session rather than letting users choose between reviewing the currently viewed batch versus the entire mistake queue.
   - **Architectural Rationale**: The review queue requires clean pagination controls (`page=1`, `pageSize=10` or `pageSize=20`), page navigation indicators, URL query synchronization, and dual review session triggers ("Practice Current Batch" vs "Practice All Mistakes").

---

## What Changes

We propose an enterprise-standard, unified pagination architecture implemented across backend (.NET 10 Minimal APIs) and frontend (Nuxt 4 / Pinia / Vue 3):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 UNIFIED PAGINATION ACROSS 4 CORE SURFACES                   │
│                                                                             │
│  1. Technical Library (/library)                                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: GetBooksRequest(Category, Search, Page=1, PageSize=12)     │  │
│  │ • Envelope: { books, totalCount, page, pageSize, totalPages }          │  │
│  │ • UI: Responsive 1/2/3-col grid with BasePagination controls          │  │
│  │ • Two-way URL query sync (?page=N&category=C&search=S)                │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  2. Reading Notes & Highlights (/notes)                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: GetHighlightsRequest(UserId, Tag, Search, Page=1, Size=15) │  │
│  │ • Envelope: { highlights, totalCount, page, pageSize, totalPages,     │  │
│  │              tagCounts: [{ tag, count }] }                            │  │
│  │ • UI: Streamed "Load More" / Numbered Pagination with Global Tag Bar  │  │
│  │ • In-place note editing & SM-2 persistent badge state preserved       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  3. Flashcard Deck Management (/review)                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: Existing GetReviewCards endpoint leveraged with TotalPages │  │
│  │ • UI: Replace Prev/Next text with accessible BasePagination bar       │  │
│  │ • Two-way URL query sync (?page=N&search=S&status=X&sourceType=Y)     │  │
│  │ • Smooth scroll-to-top on page change                                 │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  4. Quiz Mistake Review Queue (/quiz)                                       │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: Existing GetQuizReviewQueue endpoint aligned               │  │
│  │ • UI: Dedicated pagination controls under mistake question cards      │  │
│  │ • Dual review CTAs: "Practice Current Page" vs "Practice All (N)"     │  │
│  │ • Two-way URL query sync (?page=N&category=C&level=L&topic=T)         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Cross-Cutting: Reusable BasePagination.vue Component                        │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Dynamic window calculation: 1 ... 4 [5] 6 ... 12                     │  │
│  │ • Full keyboard & ARIA accessibility (role="navigation", aria-current)│  │
│  │ • Bilingual localization parity (en.json & vi.json)                   │  │
│  │ • Mobile touch-friendly targets (min height >= 40px)                  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pillar 1: Technical Library Paginated Catalog (`/library`)
- Refactor `GetBooksRequest` to accept `int Page = 1` and `int PageSize = 12`.
- Refactor `GetBooksResponse` to return `{ List<BookDto> Books, int TotalCount, int Page, int PageSize, int TotalPages }`.
- Update `GetBooksHandler` to execute `CountAsync()` followed by `.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()`.
- Update `useLibraryStore` with pagination state (`currentPage`, `pageSize`, `totalCount`, `totalPages`).
- Integrate numbered pagination controls at the bottom of `/library` with active page indicator, Prev/Next buttons, and direct page buttons.
- Synchronize browser URL query parameters: updating page, category, or search pushes clean query strings (`?page=1&category=0&search=system`), and loading the page directly with `?page=2` restores page 2.

### Pillar 2: Reading Notes Streamlined Pagination (`/notes`)
- Refactor `GetHighlightsRequest` to accept `int Page = 1`, `int PageSize = 15`, `string? Tag`, and `string? Search`.
- Refactor `GetHighlightsResponse` to return `{ List<HighlightDto> Highlights, int TotalCount, int Page, int PageSize, int TotalPages, List<TagCountDto> TagCounts }`.
- The backend computes global `TagCounts` across all active highlights for the user, ensuring the horizontal tag chip bar displays complete counts even when only the current page of 15 highlights is retrieved.
- Update `useNotesStore` to support both page jumping and progressive streaming (`append = true` for "Load More Notes").
- Preserve inline reflection note editing and SM-2 flashcard creation badge persistence across paginated chunks.
- Synchronize URL query parameters (`?page=N&tag=T&search=S`).

### Pillar 3: Flashcard Deck Management Complete Pagination (`/review`)
- Retain existing backend `GetReviewCardsHandler` pagination logic while ensuring `TotalPages` calculation is consistently populated.
- Update `frontend/pages/review.vue` to replace the bare Prev/Next buttons with the full numbered pagination bar (`< 1 2 3 ... 8 >`).
- Add smart page truncation with ellipsis for large decks (e.g. `[1] 2 3 4 5 ... 20` or `1 ... 7 [8] 9 ... 20`).
- Implement two-way URL query synchronization: reading `page`, `search`, `status`, `sourceType` from `useRoute().query` on mount, and updating URL query via `useRouter().replace()` when the user paginates.

### Pillar 4: Quiz Mistake Review Queue Interactive Pagination (`/quiz`)
- Leverage existing `GetQuizReviewQueueRequest` (`Page = 1`, `PageSize = 10` or `PageSize = 20`) and `GetQuizReviewQueueResponse`.
- Update `useInterviewQuizStore` with dedicated review queue pagination state (`reviewPage`, `reviewPageSize`, `reviewTotalCount`, `reviewTotalPages`).
- Add numbered pagination controls to the Review Queue tab in `frontend/pages/quiz.vue`.
- Provide dual-mode review session launch buttons:
  - "Practice Current Page" (`startReviewSession(quizStore.reviewQueue)`): Launches the arena with only the 10 or 20 questions on the active page.
  - "Practice All Mistakes" (`startReviewSession()` with eager fetch): Launches the arena with all unmastered questions across all pages.
- Synchronize active review queue page to URL query parameters (`?tab=review&page=N`).

### Cross-Cutting Conventions
- **Reusable `BasePagination.vue` Component**: Create an accessible, responsive pagination component in `frontend/components/common/BasePagination.vue` adhering to WAI-ARIA guidelines (`aria-label="Pagination Navigation"`, `aria-current="page"`).
- **URL State Synchronization Pattern**: Standardize route query synchronization using Vue Router `replace: true` during pagination to avoid polluting browser navigation history while preserving full reload and bookmark fidelity.
- **Bilingual Parity**: Add comprehensive localization keys across `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` covering `pagination.previous`, `pagination.next`, `pagination.page_of`, `pagination.showing_items`, `notes.load_more`, `quiz.practice_current_page`, and `quiz.practice_all`.
- **Mobile Responsive Design**: Ensure all pagination touch targets satisfy accessibility minimums ($\ge 40\text{px} \times 40\text{px}$) with condensed ellipsis display on mobile viewports ($< 640\text{px}$).

---

## Capabilities

### New Capabilities
*(None. All changes modify existing capabilities.)*

### Modified Capabilities
- `library`: Adds paginated catalog browsing (`GET /api/v1/library/books` with `page` and `pageSize=12`), grid-aligned responsive presentation, numbered pagination controls, and two-way URL query synchronization.
- `notes`: Adds paginated reading highlights endpoint (`GET /api/v1/notes/highlights` with `page` and `pageSize=15`), stream-friendly "Load More" and numbered pagination, global tag frequency preservation, and URL query synchronization.
- `review`: Enhances flashcard deck library with accessible numbered pagination controls (`< 1 2 3 ... 8 >`), ellipsis compaction, and two-way URL query synchronization (`?page=N`).
- `quiz`: Enhances mistake review queue with interactive pagination controls, URL query synchronization, and dual-scope practice triggers ("Practice Current Page" vs "Practice All Mistakes").

---

## Impact

### Backend Systems & Data Layer
- **Minimal APIs & Use-Case Handlers**: Refactored `GetBooksHandler` and `GetHighlightsHandler` to accept `page` and `pageSize` and return standard pagination metadata (`totalCount`, `page`, `pageSize`, `totalPages`).
- **Database Query Performance**: EF Core queries utilize `.AsNoTracking()`, selective `.CountAsync()` before `.Skip().Take()`, and existing composite indexes on `(UserId, CreatedAt DESC)` and `(Status, CreatedAt DESC)`.
- **Global Tag Aggregation**: Efficiently queries distinct tag frequencies across the user's highlights using PostgreSQL JSONB or array operations without pulling heavy excerpt or chunk texts.

### Frontend Architecture
- **Pinia Stores**: Updated `useLibraryStore`, `useNotesStore`, `useReviewStore`, and `useInterviewQuizStore` with reactive pagination state.
- **Reusable Component**: `frontend/components/common/BasePagination.vue` establishes a standardized, reusable UI element across all 4 surfaces.
- **URL Synchronization**: Zero regression on client-side routing; users can bookmark or share direct links to specific pages in the library, notes, deck, or review queue.

### Backward Compatibility
- API endpoints provide default values (`page = 1`, `pageSize = 12` for library, `pageSize = 15` for notes) so existing consumers without query parameters continue to receive valid responses.
