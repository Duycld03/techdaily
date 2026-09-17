# Design: Unified Notes and Flashcard Management

## Context

TechDaily integrates reading, note-taking, and active recall retention into a cohesive platform for software engineers. While the foundational data structures (`UserHighlight`, `SpacedRepetitionCard`) exist in PostgreSQL via EF Core, their interaction models have evolved unevenly:
- In the reader (`/read/[bookId]`), five separate buttons in the selection toolbar create visual clutter and cognitive overhead. Direct flashcard generation prematurely interrupts reading immersion.
- In the notes hub (`/notes`), a redundant "Saved Insights" tab duplicates functionality from `/insights`, and highlights lack an update API or inline editing.
- In the review view (`/review`), only the due-today queue (`/deck`) is exposed; users cannot search, filter, edit, reset, or soft-delete flashcards in their personal deck library.

This design establishes a clean, unified workflow spanning focused reading, reflective note curation, and deliberate spaced repetition deck management.

---

## Goals / Non-Goals

### Goals
- Streamline the reader floating toolbar to exactly 3 essential buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`.
- Unify highlighting and note-taking into a single action: creating a highlight immediately while opening an attached reflection popover.
- Dedicate `/notes` exclusively to reading highlights, pruning the duplicate "Saved Insights" tab and adding inline reflection editing backed by `PUT /api/v1/notes/highlights/{id}`.
- Transform `/review` into a dual-mode experience featuring both the interactive "Review Session" and a comprehensive "Deck Management" view.
- Provide full deck management capabilities: paginated search and filters, card markdown editing (`PUT`), SM-2 progression reset (`POST`), and soft deletion (`DELETE`).
- Guarantee data independence: deleting a highlight must never delete or corrupt its associated flashcard (`onDelete: ReferentialAction.SetNull`, independent markdown storage).
- Maintain 100% English codebase conventions and complete multi-language i18n support in `en.json` and `vi.json` without hardcoded English in parentheses.

### Non-Goals
- Altering the core SuperMemo SM-2 algorithm formula in `SpacedRepetitionCard.ApplyReview()` (grading calculation remains intact).
- Introducing complex multi-deck partitioning or folder hierarchies (all cards belong to the user's unified spaced repetition library with `SourceType` filtering).
- Modifying the `/insights` page architecture (insight bookmarks remain exclusively managed in `/insights`).

---

## Decisions

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                SYSTEM ARCHITECTURE FLOW                                │
│                                                                                        │
│   [Reader: /read/[bookId]]                                                             │
│       │                                                                                │
│       ├─ 1. Text Selection                                                             │
│       │     └─ 3-Button Toolbar: [Explain with Gemini] [Highlight/Note] [Copy]        │
│       │                                       │                                        │
│       │                                       ▼                                        │
│       └─ 2. Instant Highlight & Popover: POST /api/v1/notes/highlights                │
│                                                                                        │
│   [Notes Hub: /notes]                                                                  │
│       │                                                                                │
│       ├─ 3. Browse & Filter: GET /api/v1/notes/highlights                             │
│       ├─ 4. Inline Edit Note: PUT /api/v1/notes/highlights/{id}                       │
│       ├─ 5. Delete Highlight: DELETE /api/v1/notes/highlights/{id} (Soft-Delete)       │
│       │                                       │                                        │
│       │                                       ▼                                        │
│       └─ 6. Deliberate Flashcard Creation: POST /api/v1/review/cards/from-highlight    │
│                                                                                        │
│   [Review & Deck: /review]                                                             │
│       │                                                                                │
│       ├── Tab 1: Review Session (Due Cards)                                            │
│       │     ├─ GET /api/v1/review/deck                                                 │
│       │     └─ POST /api/v1/review/cards/{id}/grade (SM-2 Grade 0-5)                   │
│       │                                                                                │
│       └── Tab 2: Deck Management (Card Library)                                        │
│             ├─ GET /api/v1/review/cards (Pagination, Search, Status, SourceType)       │
│             ├─ PUT /api/v1/review/cards/{id} (Edit Front/Back Markdown)                │
│             ├─ POST /api/v1/review/cards/{id}/reset (SM-2 Progression Reset)          │
│             └─ DELETE /api/v1/review/cards/{id} (Soft-Delete)                          │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### Decision 1: Reader Toolbar Simplification & Unified Annotation Flow

#### Rationale & UX Behavior
The floating selection toolbar in `frontend/pages/read/[bookId].vue` currently displays 5 buttons: `Explain with Gemini`, `Highlight`, `Flashcard`, `Add Note`, and `Copy`. We simplify this to 3 buttons:
1. `Explain with Gemini` (`Sparkles` icon): Opens `TermExplainerModal.vue`.
2. `Highlight/Note` (`Highlighter` icon): Unifies highlighting and reflection. Clicking this button:
   - Instantly persists the highlight via `POST /api/v1/notes/highlights`.
   - Smoothly displays the reflection popover pre-populated with the saved highlight ID.
   - If the user types a reflection note and/or tags and clicks `Save Note`, it calls `PUT /api/v1/notes/highlights/{id}`.
   - If the user clicks outside or dismisses the popover, the highlight remains saved with `note = null` and a success toast confirms creation.
3. `Copy` (`Copy` icon): Copies the excerpt to the clipboard.

#### Direct Flashcard Button Removal
The `⚡ Flashcard` button is completely removed from the selection toolbar. Generating flashcards while actively reading disrupts deep concentration. Instead, users capture technical excerpts and reflections while reading, and deliberately promote them to flashcards within the dedicated `/notes` hub.

---

### Decision 2: Dedicated Notes Hub Specialization & Inline Editing

#### Pruning Saved Insights Tab
In `frontend/pages/notes.vue`, the `activeTab` switcher (`insights` vs `highlights`) is removed. The page is retitled and focused entirely on reading highlights and personal annotations. Users manage insight bookmarks exclusively on the `/insights` page via the existing `[🔖 Đã lưu]` tab.

#### Highlight Editing API (`PUT /api/v1/notes/highlights/{id}`)
We introduce `UpdateHighlightHandler` in `TechDaily.Application.Features.Notes.UpdateHighlight`:
- Route: `PUT /api/v1/notes/highlights/{id:guid}`
- Contract:
  ```csharp
  public record UpdateHighlightApiRequest(string? Note, List<string>? Tags);
  public record UpdateHighlightRequest(Guid UserId, Guid HighlightId, string? Note, List<string>? Tags);
  public record UpdateHighlightResponse(HighlightDto Highlight);
  ```
- Validation:
  - `Note`: Maximum 2000 characters.
  - `Tags`: Maximum 10 tags, each tag max 50 characters.

#### Inline Note Editor in `/notes`
Each highlight card provides an inline "Edit Note" action. Clicking toggles the card into an inline editing state with a textarea for the reflection note and a tag editor. Saving dispatches `PUT /api/v1/notes/highlights/{id}`, updating the local Pinia store (`useNotesStore`) seamlessly.

---

### Decision 3: Spaced Repetition Deck Management (`/review` Dual Mode)

#### Dual-Mode Tab Switcher
`frontend/pages/review.vue` introduces a top-level tab switcher:
- **Tab 1: "Review Session" / "Ôn tập hôm nay":**
  - Displays the active 3D flip-card player (`FlashcardDeck.vue`) powered by `GET /api/v1/review/deck`.
  - Shows cards due today ($NextReviewDate \le Today$).
  - Once all due cards are graded, presents a completion banner with confetti and a button switching to Tab 2.
- **Tab 2: "Deck Management" / "Kho thẻ của tôi":**
  - Displays deck statistics: Total Cards, Learning ($Repetition=0$), Reviewing ($1 \le Repetition < 4$), and Mastered ($Repetition \ge 4$).
  - Search input with debounced keyword querying against `FrontMarkdown`, `BackMarkdown`, and topic titles.
  - Filter chips for `CardStatus` (`All`, `Learning`, `Reviewing`, `Mastered`) and `CardSourceType` (`All`, `Topic`, `Highlight`, `QuizMistake`).
  - Paginated card table / list with SM-2 metrics:
    - Status badge (`Learning` / `Reviewing` / `Mastered`).
    - Source badge (`Highlight` / `QuizMistake` / `Topic`).
    - SM-2 parameters: Interval (days), Ease Factor ($EF$), Repetition Count, Next Review Date.
    - Actions: Edit (`Pencil`), Reset Progress (`RotateCcw`), Delete (`Trash2`).

---

### Decision 4: Backend Domain Entities & Methods

#### Domain Methods on `SpacedRepetitionCard`
In `backend/src/TechDaily.Domain/Entities/SpacedRepetitionCard.cs`:
```csharp
public void UpdateContent(string frontMarkdown, string backMarkdown)
{
    if (string.IsNullOrWhiteSpace(frontMarkdown))
        throw new ArgumentException("Front markdown cannot be empty.", nameof(frontMarkdown));
    if (string.IsNullOrWhiteSpace(backMarkdown))
        throw new ArgumentException("Back markdown cannot be empty.", nameof(backMarkdown));

    FrontMarkdown = frontMarkdown.Trim();
    BackMarkdown = backMarkdown.Trim();
    MarkUpdated();
}

public void ResetProgression(DateOnly? resetDate = null)
{
    var today = resetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
    RepetitionCount = 0;
    IntervalDays = 1;
    EaseFactor = 2.50m;
    Status = CardStatus.Learning;
    NextReviewDate = today;
    MarkUpdated();
}
```

#### Domain Methods on `UserHighlight`
In `backend/src/TechDaily.Domain/Entities/UserHighlight.cs`:
```csharp
public void Update(string? note, List<string>? tags)
{
    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList() ?? new List<string>();
    MarkUpdated();
}
```

---

### Decision 5: API Contracts & CQRS Handlers

#### 1. `GET /api/v1/review/cards`
- **Purpose:** Paginated retrieval of user flashcards with filtering and statistics.
- **Request:**
  ```csharp
  public record GetReviewCardsRequest(
      Guid UserId,
      int Page = 1,
      int PageSize = 20,
      string? Search = null,
      CardStatus? Status = null,
      CardSourceType? SourceType = null);
  ```
- **Response:**
  ```csharp
  public class GetReviewCardsResponse
  {
      public List<ReviewCardDto> Cards { get; set; } = new();
      public int TotalCount { get; set; }
      public int Page { get; set; }
      public int PageSize { get; set; }
      public DeckStatisticsDto Statistics { get; set; } = new();
  }

  public class DeckStatisticsDto
  {
      public int TotalCards { get; set; }
      public int LearningCards { get; set; }
      public int ReviewingCards { get; set; }
      public int MasteredCards { get; set; }
  }
  ```
- **Handler Implementation:**
  - Queries `_dbContext.SpacedRepetitionCards.Where(c => c.UserId == userId)`.
  - Calculates statistics across all active user cards (using conditional aggregations or group counts).
  - Applies filters:
    - Search: `EF.Functions.ILike(c.FrontMarkdown, $"%{search}%") || EF.Functions.ILike(c.BackMarkdown, $"%{search}%")`
    - Status: `c.Status == request.Status.Value`
    - SourceType: `c.SourceType == request.SourceType.Value`
  - Applies pagination: `.Skip((Page - 1) * PageSize).Take(PageSize)`.

#### 2. `PUT /api/v1/review/cards/{id}`
- **Purpose:** Update front and back markdown of an existing card.
- **Contract:**
  ```csharp
  public record UpdateReviewCardApiRequest(string FrontMarkdown, string BackMarkdown);
  public record UpdateReviewCardRequest(Guid UserId, Guid CardId, string FrontMarkdown, string BackMarkdown);
  public record UpdateReviewCardResponse(ReviewCardDto Card);
  ```
- **Validation:**
  - `FrontMarkdown`: NotEmpty, MaxLength(4000).
  - `BackMarkdown`: NotEmpty, MaxLength(10000).

#### 3. `DELETE /api/v1/review/cards/{id}`
- **Purpose:** Soft-delete a flashcard.
- **Contract:**
  ```csharp
  public record DeleteReviewCardRequest(Guid UserId, Guid CardId);
  public record DeleteReviewCardResponse(bool Success);
  ```
- **Handler Implementation:**
  - Locates card by `Id == cardId && UserId == userId`.
  - If not found, returns `Error.NotFound`.
  - Calls `card.SoftDelete()`.
  - Saves changes to PostgreSQL.

#### 4. `POST /api/v1/review/cards/{id}/reset`
- **Purpose:** Reset SM-2 progression for a card.
- **Contract:**
  ```csharp
  public record ResetReviewCardProgressRequest(Guid UserId, Guid CardId);
  public record ResetReviewCardProgressResponse(ReviewCardDto Card);
  ```
- **Handler Implementation:**
  - Locates card by `Id == cardId && UserId == userId`.
  - Calls `card.ResetProgression()`.
  - Persists changes and returns updated DTO.

---

### Decision 6: Frontend Store Architecture

#### `useNotesStore.ts` Updates
```typescript
export interface Highlight {
  id: string
  documentChunkId: string
  chapterTitle: string
  bookTitle: string
  selectedText: string
  note?: string
  tags: string[]
  createdAt: string
}

// Added action:
async function updateHighlight(id: string, params: { note?: string; tags?: string[] }) {
  const api = useApiClient()
  const res = await api.put<{ highlight: Highlight }>(`/api/v1/notes/highlights/${id}`, params)
  const updated = res.highlight || (res as unknown as Highlight)
  const index = highlights.value.findIndex(h => h.id === id)
  if (index !== -1) {
    highlights.value[index] = updated
  }
  return updated
}
```

#### `useReviewStore.ts` Updates
```typescript
export interface DeckStatistics {
  totalCards: number
  learningCards: number
  reviewingCards: number
  masteredCards: number
}

// Added state:
const deckCards = ref<ReviewCard[]>([])
const deckStatistics = ref<DeckStatistics>({ totalCards: 0, learningCards: 0, reviewingCards: 0, masteredCards: 0 })
const deckTotalCount = ref(0)
const deckCurrentPage = ref(1)
const isDeckLoading = ref(false)

// Added actions:
async function fetchDeckCards(params: {
  page?: number
  pageSize?: number
  search?: string
  status?: string
  sourceType?: string
}) { ... }

async function updateCard(id: string, params: { frontMarkdown: string; backMarkdown: string }) { ... }

async function deleteCard(id: string) { ... }

async function resetCardProgress(id: string) { ... }
```

---

### Decision 7: UI Component Designs

#### 1. Reader Floating Toolbar (`/read/[bookId].vue`)
- Compact horizontal container teleported to `<body>`, positioned above the text selection.
- Exactly three buttons styled with Tailwind:
  - Gemini Explainer: `bg-brand-600 hover:bg-brand-500` with `Sparkles`.
  - Highlight/Note: `bg-amber-500/20 text-amber-300 hover:bg-amber-500 hover:text-slate-950` with `Highlighter`.
  - Copy: `text-slate-300 hover:text-white hover:bg-slate-700/60` with `Copy`.
- Clicking `Highlight/Note` creates the highlight and displays the attached popover containing:
  - Excerpt quote preview (truncated with left border).
  - Note textarea (`placeholder="Write reflection..."`).
  - Tag input field with comma/enter tokenization.
  - Action buttons: "Save Note" (`bg-brand-600`) and "Done" / "Close".

#### 2. Reading Notes Hub (`/notes.vue`)
- Header: Title, subtitle, search bar with debounce, and tag cloud filter.
- Cards render:
  - Book and chapter title badge with link to reader (`/read/[bookId]?slice=[chunk]`).
  - Selected excerpt in blockquote styling.
  - Reflection note with Markdown formatting.
  - Tag chips.
  - Action row:
    - Inline edit toggle ("Edit Note").
    - "Flashcard SM-2" generation button.
    - Delete button.
- Inline edit state:
  - Textarea with auto-sizing.
  - Tag input.
  - "Save Changes" and "Cancel" buttons.

#### 3. Review Deck Management (`/review.vue`)
- Top tab switcher:
  - `[ 🎴 Review Session / Ôn tập hôm nay ]` (with due badge counter).
  - `[ 📚 Deck Management / Kho thẻ của tôi ]` (with total cards counter).
- Tab 2 Deck Management View:
  - Statistics grid (4 cards): Total Cards, Learning, Reviewing, Mastered.
  - Search input with clear button.
  - Filter pills for Status (All, Learning, Reviewing, Mastered) and Source (All, Topic, Highlight, QuizMistake).
  - Responsive card list:
    - Front Markdown preview (rendered as clean prose/code).
    - Back Markdown preview (collapsible or split).
    - SM-2 metric pill row: Interval ($d$), Ease Factor, Repetitions, Next Review.
    - Action dropdown or icon buttons: Edit, Reset Progress, Delete.
  - Edit Modal:
    - Tabbed or split-pane editor: Edit Raw Markdown vs Live Markdown Preview.
    - Front Markdown textarea.
    - Back Markdown textarea.
    - Validation error hints.
    - "Save Changes" and "Cancel" buttons.
  - Reset Confirmation Modal:
    - Clear explanation that the card will restart from Day 1 ($Interval=1$, $Repetitions=0$).
  - Delete Confirmation Modal:
    - Warning that the card will be permanently removed from spaced repetition.

---

### Decision 8: Internationalization (i18n) Key Mappings

All user-facing strings are strictly localized in `frontend/i18n/locales/en.json` and `vi.json` without hardcoded English in parentheses:

```json
// en.json additions:
"review": {
  "tab_session": "Review Session",
  "tab_deck": "Deck Management",
  "stat_total": "Total Cards",
  "stat_learning": "Learning",
  "stat_reviewing": "Reviewing",
  "stat_mastered": "Mastered",
  "search_placeholder": "Search flashcards by keywords...",
  "filter_status_all": "All Statuses",
  "filter_source_all": "All Sources",
  "source_topic": "Curriculum Topic",
  "source_highlight": "Reading Highlight",
  "source_quiz_mistake": "Quiz Mistake",
  "col_card": "Card Content",
  "col_metrics": "SM-2 Metrics",
  "col_next_review": "Next Review",
  "col_actions": "Actions",
  "btn_edit": "Edit",
  "btn_reset": "Reset Progress",
  "btn_delete": "Delete",
  "edit_modal_title": "Edit Flashcard",
  "front_label": "Front (Prompt / Question)",
  "back_label": "Back (Answer / Explanation)",
  "tab_edit": "Edit Markdown",
  "tab_preview": "Preview",
  "save_changes": "Save Changes",
  "reset_modal_title": "Reset SM-2 Progress",
  "reset_modal_desc": "Are you sure you want to reset learning progression for this card? It will restart at interval 1 day and become due today.",
  "confirm_reset": "Reset to Learning",
  "delete_modal_title": "Delete Flashcard",
  "delete_modal_desc": "Are you sure you want to remove this flashcard from your deck? This action cannot be undone.",
  "confirm_delete": "Delete Card",
  "toast_update_success": "Flashcard updated successfully.",
  "toast_reset_success": "SM-2 progression reset to Learning.",
  "toast_delete_success": "Flashcard deleted from deck.",
  "empty_deck": "No flashcards found matching your filters."
}

// vi.json additions (clean Vietnamese without English in parentheses):
"review": {
  "tab_session": "Ôn tập hôm nay",
  "tab_deck": "Kho thẻ của tôi",
  "stat_total": "Tổng số thẻ",
  "stat_learning": "Đang học",
  "stat_reviewing": "Đang ôn tập",
  "stat_mastered": "Đã thành thạo",
  "search_placeholder": "Tìm kiếm thẻ theo từ khóa...",
  "filter_status_all": "Tất cả trạng thái",
  "filter_source_all": "Tất cả nguồn",
  "source_topic": "Chủ đề giáo trình",
  "source_highlight": "Ghi chú đọc sách",
  "source_quiz_mistake": "Câu hỏi trắc nghiệm sai",
  "col_card": "Nội dung thẻ",
  "col_metrics": "Chỉ số SM-2",
  "col_next_review": "Ngày ôn tiếp theo",
  "col_actions": "Hành động",
  "btn_edit": "Chỉnh sửa",
  "btn_reset": "Đặt lại tiến độ",
  "btn_delete": "Xóa",
  "edit_modal_title": "Chỉnh sửa Thẻ Ôn Tập",
  "front_label": "Mặt trước (Câu hỏi / Khái niệm)",
  "back_label": "Mặt sau (Đáp án / Giải thích)",
  "tab_edit": "Soạn thảo Markdown",
  "tab_preview": "Xem trước",
  "save_changes": "Lưu thay đổi",
  "reset_modal_title": "Đặt lại Tiến độ SM-2",
  "reset_modal_desc": "Bạn có chắc chắn muốn đặt lại tiến độ học cho thẻ này? Thẻ sẽ bắt đầu lại từ chu kỳ 1 ngày và cần ôn tập ngay hôm nay.",
  "confirm_reset": "Đặt lại về Đang học",
  "delete_modal_title": "Xóa Thẻ Ôn Tập",
  "delete_modal_desc": "Bạn có chắc chắn muốn xóa thẻ này khỏi kho thẻ? Hành động này không thể hoàn tác.",
  "confirm_delete": "Đồng ý xóa",
  "toast_update_success": "Cập nhật thẻ ôn tập thành công.",
  "toast_reset_success": "Đã đặt lại tiến độ SM-2 về Đang học.",
  "toast_delete_success": "Đã xóa thẻ khỏi kho thẻ.",
  "empty_deck": "Không tìm thấy thẻ ôn tập phù hợp với bộ lọc."
}
```

---

### Decision 9: Invariants, Security & Data Integrity Safeguards

1. **Highlight and Flashcard Independence Guarantee:**
   - In `backend/src/TechDaily.Infrastructure/Migrations/20260916094331_GeneralizeSpacedRepetitionCards.cs`, the foreign key `FK_SpacedRepetitionCards_UserHighlights_SourceHighlightId` has `onDelete: ReferentialAction.SetNull`.
   - When a user deletes a `UserHighlight`, EF Core and PostgreSQL set `SpacedRepetitionCards.SourceHighlightId = NULL`.
   - The card preserves its own `FrontMarkdown` and `BackMarkdown` fields, remaining fully functional and scheduled in the user's SM-2 deck.
2. **Soft Deletion Consistency:**
   - All deletions (`UserHighlight`, `SpacedRepetitionCard`) invoke `.SoftDelete()`, marking `IsDeleted = true`.
   - EF Core global query filters (`modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)`) guarantee deleted records are excluded from queries across all application layers.
3. **Tenant Security & Route Authorization:**
   - Every API endpoint requires JWT Bearer authorization (`RequireAuthorization()`).
   - All queries filter strictly by `UserId == GetUserIdFromClaims(userClaims)`.
   - Any attempt to access, edit, reset, or delete records belonging to another user results in HTTP 404 or 403.

---

## Risks / Trade-offs

| Risk / Trade-off | Evaluation | Mitigation Strategy |
|---|---|---|
| **Immediate highlight creation UX** | Clicking "Highlight/Note" creates an instant highlight record before the user finishes typing a note. | If the user dismisses the popover, the highlight remains cleanly saved as an excerpt. If they type a note and save, a lightweight `PUT` updates it immediately. This matches Kindle and Readwise interaction standards. |
| **Search query performance on markdown** | Searching `FrontMarkdown` and `BackMarkdown` across thousands of cards. | Queries are strictly bounded per user (`UserId` index). Status and source type filters narrow the candidate set prior to pagination (`Take(20)`). |
| **Resetting progression impacts daily load** | Resetting multiple cards sets `NextReviewDate = Today`, increasing today's review load. | Clear confirmation dialog warns users that reset cards immediately become due today. |
| **Mobile screen density in deck management** | Table layout with SM-2 metrics can be cramped on mobile viewports (<768px). | Responsive design: renders as a multi-column table on desktop (≥768px) and transforms into card stacked list items on mobile (<768px). |
