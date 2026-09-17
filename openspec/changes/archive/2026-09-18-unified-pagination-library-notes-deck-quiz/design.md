# Design: Unified Enterprise Pagination Architecture

## Context

TechDaily delivers high-density technical education across four core surfaces:
1. **Technical Library (`/library`)**: Catalogs markdown books, uploaded PDFs up to 350 MB, and crawled web documentation series.
2. **Reading Notes & Highlights (`/notes`)**: Manages personal excerpts, reflections, dynamic tag chips, and SM-2 flashcard creation.
3. **Flashcard Deck Management (`/review`)**: Displays the full flashcard knowledge base with SM-2 metrics, 7-day review forecasts, and bento mastery indicators.
4. **Quiz Mistake Review Queue (`/quiz`)**: Houses questions answered incorrectly during AI interview quizzes, allowing targeted re-quizzing and promotion to flashcards.

Currently, these surfaces exhibit stark architectural inconsistencies:
- `/library` and `/notes` fetch unpaginated collections in single requests, degrading DOM performance, inflating memory usage, and multiplying network bytes as user datasets scale.
- `/review` implements server-side pagination, but the frontend only offers bare Previous/Next text buttons with zero numbered navigation and zero URL query synchronization.
- `/quiz` backend supports review queue pagination, but the frontend renders an unpaginated linear list without page controls or batch session controls.

This design establishes an enterprise-grade pagination standard across the entire stack, unifying .NET 10 Minimal APIs, EF Core 10 query optimization, Nuxt 4 / Pinia reactive stores, and an accessible, responsive Vue 3 pagination component.

---

## Goals / Non-Goals

### Goals
- **Standardized Pagination Envelope**: Implement a unified response contract (`{ items, totalCount, page, pageSize, totalPages }`) across all four surfaces.
- **Responsive Grid Alignment**: Fix library page size to $N = 12$ to ensure mathematical alignment across 1, 2, 3, and 4-column responsive grid layouts without ragged trailing cards.
- **Global Tag Frequency Preservation**: Compute global tag frequencies across the entire highlight corpus for `/notes`, ensuring dynamic tag chips do not collapse or disappear when viewing specific pages.
- **Dual-Mode Practice Triggers**: Equip the `/quiz` review queue with distinct session triggers for practicing the currently displayed batch ($N = 10$) versus all unmastered mistakes across the entire queue.
- **Two-Way URL Query Synchronization**: Bind page numbers and active filters to the browser URL query string (`?page=N`) using `useRouter().replace()`, enabling deep linking, bookmarking, and native browser history navigation without history stack pollution.
- **Reusable Accessible UI Component**: Build `frontend/components/common/BasePagination.vue` adhering to WAI-ARIA standards (`role="navigation"`, `aria-label="Pagination Navigation"`, `aria-current="page"`, minimum $40\text{px} \times 40\text{px}$ touch targets).
- **Sub-50ms Query Performance**: Utilize `.AsNoTracking()`, indexed ordering (`CreatedAt DESC`, `LastAttemptedAt DESC`, `NextReviewDate ASC`), and separated count queries to ensure lightning-fast database execution.

### Non-Goals
- **Cursor / Keyset Pagination**: Keyset pagination (e.g. `WHERE id > cursor`) is intentionally rejected for these educational catalogs because it prevents direct jumping to arbitrary page numbers (e.g. jump to page 5 or 12) and complicates total page count rendering.
- **Infinite Scrolling Without Page Numbers in Library**: Unbounded infinite scrolling in the library degrades usability by pushing footer content out of reach and preventing users from maintaining mental spatial orientation within the catalog.
- **Modifying the Core SM-2 Algorithm**: Scheduling calculations (`EaseFactor`, `IntervalDays`, `NextReviewDate`) remain untouched.

---

## Decisions

### Decision 1: Unified Paginated Response Envelope
Every paginated endpoint returns a standard JSON structure. For backwards compatibility and domain clarity, the item array retains its entity-specific property name (`books`, `highlights`, `cards`, `questions`), while pagination metadata is standardized:

```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 12,
  "totalPages": 4
}
```

*Alternatives Considered*:
- Generic `items` wrapper for all endpoints: Rejected because strongly typed domain clients in TypeScript benefit from explicit entity property names (`res.books`, `res.highlights`) without breaking existing destructured callsites.

### Decision 2: Library Page Size of 12 ($N = 12$)
The technical library grid uses responsive CSS:
- Mobile ($< 768\text{px}$): 1 column
- Tablet ($768\text{px} - 1023\text{px}$): 2 columns
- Desktop ($1024\text{px} - 1279\text{px}$): 3 columns
- Ultra-wide ($\ge 1280\text{px}$): 3 or 4 columns

Setting `pageSize = 12` ensures that $12 \pmod 1 = 0$, $12 \pmod 2 = 0$, $12 \pmod 3 = 0$, and $12 \pmod 4 = 0$. Every row is completely filled regardless of viewport size, creating a polished, balanced layout.

*Alternatives Considered*:
- `pageSize = 10` or `20`: Causes ragged trailing rows on 3-column desktop layouts ($10 = 3 \times 3 + 1$; $20 = 6 \times 3 + 2$), leaving awkward orphan cards at the bottom of the grid.

### Decision 3: Notes Dual-Mode Presentation & Global Tag Aggregation
For `/notes`, users need both structured numbered pagination and a stream-like "Load More" capability. The backend computes `TagCounts` across the user's entire highlight database using a lightweight group-by query. This decouples tag discovery from the 15-item paginated slice:

```csharp
// Global tag calculation independent of page window
var allUserTags = await _dbContext.UserHighlights
    .Where(h => h.UserId == request.UserId)
    .Select(h => h.Tags)
    .ToListAsync(cancellationToken);

var tagCounts = allUserTags
    .SelectMany(tags => tags)
    .GroupBy(tag => tag.Trim().ToLower())
    .Select(g => new TagCountDto(g.Key, g.Count()))
    .OrderByDescending(t => t.Count)
    .ThenBy(t => t.Tag)
    .ToList();
```

*Alternatives Considered*:
- Extracting tags only from the active 15 items: Rejected because the horizontal tag chip bar would dynamically shrink and reorder as the user flips pages, disorienting the user and making off-page tags unselectable.

### Decision 4: Reusable Accessible Component `BasePagination.vue`
Rather than repeating pagination DOM and button logic across four pages, all surfaces consume `frontend/components/common/BasePagination.vue`.
The component encapsulates:
- Window calculation: For $\le 7$ pages, render all pages `1 2 3 4 5 6 7`. For $> 7$ pages, render smart windowing with ellipsis: `[1] 2 3 4 5 ... 10`, `1 ... 4 [5] 6 ... 10`, or `1 ... 6 7 8 9 [10]`.
- Mobile responsiveness: Condenses window to current page and immediate neighbors on viewports $< 640\text{px}$.
- WAI-ARIA standards: Container uses `<nav role="navigation" aria-label="Pagination">`, active page has `aria-current="page"`, disabled buttons have `disabled` and `aria-disabled="true"`.

### Decision 5: Two-Way URL Query Synchronization with `replace: true`
When a user clicks page 3, the application updates route query via:
```typescript
router.replace({
  query: {
    ...route.query,
    page: newPage.toString()
  }
})
```
Using `router.replace` prevents creating 10 intermediate browser history entries if a user rapidly pages through a catalog, while preserving full bookmarking, link sharing, and reload support. Direct URL visits (e.g. paste `/library?page=2&category=1`) parse query parameters on `onMounted` and initialize store state seamlessly.

### Decision 6: Dual Review Arena Triggers for Quiz Mistake Queue
In `/quiz` review tab, users can:
1. Click "Practice Current Batch (N)": Triggers `quizStore.startReviewSession(quizStore.reviewQueue)`, loading only the currently viewed 10 questions into the Arena for a bite-sized 5-minute drill.
2. Click "Practice All Mistakes (Total N)": Eagerly fetches all unmastered questions across all pages via `GET /api/v1/quiz/review-queue?pageSize=100` and launches a comprehensive mastery session.

---

## Detailed Component Specifications

### 1. Backend Architecture (.NET 10 Minimal APIs)

#### Library Catalog (`GetBooks`)
- **Request Record**:
  ```csharp
  namespace TechDaily.Application.Features.Library.GetBooks;

  public record GetBooksRequest(
      Category? Category = null,
      string? Search = null,
      int Page = 1,
      int PageSize = 12);
  ```
- **Response Class**:
  ```csharp
  namespace TechDaily.Application.Features.Library.GetBooks;

  public class GetBooksResponse
  {
      public List<BookDto> Books { get; set; } = new();
      public int TotalCount { get; set; }
      public int Page { get; set; }
      public int PageSize { get; set; }
      public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

      public GetBooksResponse() { }

      public GetBooksResponse(List<BookDto> books, int totalCount, int page, int pageSize)
      {
          Books = books;
          TotalCount = totalCount;
          Page = page;
          PageSize = pageSize;
      }
  }
  ```
- **Handler Query Execution**:
  ```csharp
  var page = request.Page > 0 ? request.Page : 1;
  var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 12;

  var totalCount = await query.CountAsync(cancellationToken);

  var books = await query
      .OrderByDescending(b => b.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(b => new BookDto { ... })
      .ToListAsync(cancellationToken);

  return new GetBooksResponse(books, totalCount, page, pageSize);
  ```

#### Reading Notes (`GetHighlights`)
- **Request Record**:
  ```csharp
  namespace TechDaily.Application.Features.Notes.GetHighlights;

  public record GetHighlightsRequest(
      Guid UserId,
      string? Tag = null,
      string? Search = null,
      int Page = 1,
      int PageSize = 15);
  ```
- **DTOs & Response Class**:
  ```csharp
  namespace TechDaily.Application.Features.Notes.DTOs;

  public record TagCountDto(string Tag, int Count);

  public class GetHighlightsResponse
  {
      public List<HighlightDto> Highlights { get; set; } = new();
      public int TotalCount { get; set; }
      public int Page { get; set; }
      public int PageSize { get; set; }
      public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
      public List<TagCountDto> TagCounts { get; set; } = new();

      public GetHighlightsResponse() { }

      public GetHighlightsResponse(
          List<HighlightDto> highlights,
          int totalCount,
          int page,
          int pageSize,
          List<TagCountDto> tagCounts)
      {
          Highlights = highlights;
          TotalCount = totalCount;
          Page = page;
          PageSize = pageSize;
          TagCounts = tagCounts;
      }
  }
  ```

#### Flashcard Deck (`GetReviewCards`)
- Endpoint `GET /api/v1/review/cards` already executes `CountAsync()` and `Skip().Take()`.
- Ensure `GetReviewCardsResponse` exposes `TotalPages`:
  ```csharp
  public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
  ```

#### Quiz Mistake Review Queue (`GetQuizReviewQueue`)
- Endpoint `GET /api/v1/quiz/review-queue` already accepts `page` and `pageSize` (default 10 or 20) and returns `totalCount`.
- Ensure `GetQuizReviewQueueResponse` calculates `TotalPages`:
  ```csharp
  public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
  ```

---

### 2. Frontend Architecture (Nuxt 4 / Vue 3 / Pinia)

#### Reusable `BasePagination.vue` Component
- **Location**: `frontend/components/common/BasePagination.vue`
- **Props**:
  ```typescript
  interface Props {
    currentPage: number
    totalPages: number
    totalCount?: number
    pageSize?: number
    disabled?: boolean
    showItemSummary?: boolean
  }
  ```
- **Emits**:
  ```typescript
  const emit = defineEmits<{
    (e: 'update:currentPage', page: number): void
    (e: 'change', page: number): void
  }>()
  ```
- **Pagination Window Algorithm**:
  ```typescript
  const visiblePages = computed(() => {
    const current = props.currentPage
    const total = props.totalPages
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1)
    }

    const pages: (number | 'ellipsis')[] = [1]

    if (current > 3) {
      pages.push('ellipsis')
    }

    const start = Math.max(2, current - 1)
    const end = Math.min(total - 1, current + 1)

    for (let i = start; i <= end; i++) {
      pages.push(i)
    }

    if (current < total - 2) {
      pages.push('ellipsis')
    }

    pages.push(total)
    return pages
  })
  ```

#### Pinia Store Refactoring

1. **`useLibraryStore` (`frontend/stores/useLibraryStore.ts`)**:
   ```typescript
   const books = ref<Book[]>([])
   const totalCount = ref(0)
   const currentPage = ref(1)
   const pageSize = ref(12)
   const totalPages = ref(0)

   async function fetchBooks(params?: {
     category?: number
     search?: string
     page?: number
     pageSize?: number
   }) {
     // Queries /api/v1/library/books with page and pageSize
     // Sets books, totalCount, currentPage, pageSize, totalPages
   }
   ```

2. **`useNotesStore` (`frontend/stores/useNotesStore.ts`)**:
   ```typescript
   const highlights = ref<Highlight[]>([])
   const totalCount = ref(0)
   const currentPage = ref(1)
   const pageSize = ref(15)
   const totalPages = ref(0)
   const globalTagCounts = ref<Array<{ tag: string; count: number }>>([])

   async function fetchHighlights(params?: {
     tag?: string
     search?: string
     page?: number
     pageSize?: number
     append?: boolean
   }) {
     // Supports append = true for "Load More" stream
   }
   ```

3. **`useReviewStore` (`frontend/stores/useReviewStore.ts`)**:
   - Align `deckCurrentPage`, `deckPageSize`, `deckTotalCount`, `totalPages`.
   - Update `fetchDeckCards` to compute and return `totalPages`.

4. **`useInterviewQuizStore` (`frontend/stores/useInterviewQuizStore.ts`)**:
   ```typescript
   const reviewQueue = ref<QuizQuestion[]>([])
   const reviewQueueTotal = ref(0)
   const reviewQueuePage = ref(1)
   const reviewQueuePageSize = ref(10)
   const reviewQueueTotalPages = ref(0)

   async function fetchReviewQueue(params?: {
     category?: number | null
     level?: number | null
     topic?: string | null
     page?: number
     pageSize?: number
   })
   ```

---

### 3. Internationalization (i18n)

New and updated translation keys in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`:

#### `en.json`:
```json
{
  "pagination": {
    "previous": "Previous",
    "next": "Next",
    "page_info": "Page {page} of {totalPages}",
    "showing_summary": "Showing {from}-{to} of {total} items",
    "jump_to_page": "Go to page {page}"
  },
  "notes": {
    "load_more": "Load More Notes ({remaining} remaining)",
    "showing_notes_count": "Showing {count} of {total} highlights"
  },
  "quiz": {
    "practice_current_batch": "Practice Current Batch ({count})",
    "practice_all_mistakes": "Practice All Mistakes ({count})"
  }
}
```

#### `vi.json`:
```json
{
  "pagination": {
    "previous": "Trang trước",
    "next": "Trang sau",
    "page_info": "Trang {page} trên {totalPages}",
    "showing_summary": "Hiển thị {from}-{to} trên {total} mục",
    "jump_to_page": "Đến trang {page}"
  },
  "notes": {
    "load_more": "Tải thêm ghi chú (còn {remaining} mục)",
    "showing_notes_count": "Đang hiển thị {count} trên {total} ghi chú"
  },
  "quiz": {
    "practice_current_batch": "Luyện tập trang hiện tại ({count})",
    "practice_all_mistakes": "Luyện tập tất cả câu hỏi sai ({count})"
  }
}
```

---

## Risks / Trade-offs

### [Risk 1: Count Query Overhead on High-Volume Tables]
- **Risk**: Calling `.CountAsync()` on every request can degrade database throughput when tables contain millions of rows.
- **Mitigation**: All count queries execute with indexed predicates (`UserId`, `IsDeleted = false`, `Category`). TechDaily's dataset per user is bounded in the low tens of thousands, where PostgreSQL B-tree index scans resolve counts in under 2ms. No full table scans occur.

### [Risk 2: URL Query Thrashing & Back Button Traps]
- **Risk**: Pushing new history entries for every pagination click traps the user in browser history, requiring dozens of back clicks to return to the previous page.
- **Mitigation**: Route updates use `router.replace({ query: ... })` rather than `router.push()`, ensuring the browser history stack remains clean while URL bookmarkability is fully preserved.

### [Risk 3: Offset Pagination Drift (Phantom Records)]
- **Risk**: If new books or highlights are created while a user is paginating, items might shift by one position between page turns.
- **Mitigation**: Items are strictly ordered by deterministic timestamps (`CreatedAt DESC`). In an educational learning setting, occasional item drift during active authoring is entirely benign and vastly preferable to the stateful complexity of cursor-based pagination.

### [Risk 4: Mobile Touch Overlap & Screen Real Estate]
- **Risk**: Rendering 8 numbered buttons on a $375\text{px}$ iPhone screen causes line wraps and tiny touch targets that violate accessibility standards.
- **Mitigation**: `BasePagination.vue` automatically switches to compact mobile mode ($< 640\text{px}$), displaying only Previous, Current Page Indicator, and Next buttons with minimum $44\text{px} \times 44\text{px}$ touch targets.

---

## Migration Plan

1. **Backend Deployment**: Deploy the updated .NET 10 Minimal APIs and use-case handlers. Because `Page` and `PageSize` have default parameter values (`page = 1`, `pageSize = 12` / `15` / `20`), any un-updated client will continue to function without disruption.
2. **Frontend Deployment**: Ship `BasePagination.vue` along with updated Pinia stores and pages (`library.vue`, `notes.vue`, `review.vue`, `quiz.vue`). Client-side asset hashes guarantee atomic cutover.
3. **Zero Database Migrations Required**: All queries leverage existing PostgreSQL schema columns (`CreatedAt`, `UserId`, `IsDeleted`, `Status`, `Category`).
4. **Rollback Strategy**: If unforeseen issues arise in the pagination controls, reverting frontend commits restores previous linear views without database schema rollbacks or data patching.
