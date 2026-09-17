# Design: Dynamic Insights and Highlight Card Sync

## Context

TechDaily's `/insights` and `/notes` pages are core pillars of the application's learning and retention loop. Currently, their implementations suffer from interface inconsistencies, static data decoupling, and an ephemeral state bug:

1. **Filter Bar Visual Inconsistency (`/insights`):**
   - The `/insights` category filter row renders standard categories with plain text pills, but renders the saved items filter with both a Lucide `<Bookmark>` icon and an emoji prefix (`"🔖 Đã Lưu"` / `"🔖 Saved"`). This violates visual symmetry and creates unnecessary visual noise.
   - Category filters and AI generation topic inspirations are hardcoded in `frontend/pages/insights.vue` (`categories` array and `suggestedTopicPool` object). This hardcoded frontend state cannot reflect new categories, updated counts, or database curriculum changes.

2. **Ephemeral SM-2 Flashcard Association (`/notes` - F5 Reload Bug):**
   - When users click `[ ⚡ Flashcard SM-2 ]` on a reading highlight, a flashcard is persisted to `SpacedRepetitionCards` via `POST /api/v1/review/cards/from-highlight`.
   - The frontend updates a local Vue reactive set (`createdCardHighlightIds.value.add(highlightId)`), transitioning the button to a green disabled state.
   - However, `HighlightDto` and the backend `GetHighlightsHandler` do not return any indication of whether a highlight has an associated flashcard.
   - When the user refreshes the page (F5) or re-navigates to `/notes`, `createdCardHighlightIds` is re-initialized to an empty `Set()`, reverting all highlight cards back to the amber `[ ⚡ Flashcard SM-2 ]` button.

This design establishes a clean pure-text styling standard for the insights filter row, introduces a lightweight dynamic metadata endpoint (`GET /api/v1/insights/meta`), and adds relational flashcard tracking to `HighlightDto` to guarantee 100% state persistence across page lifecycles.

---

## Goals / Non-Goals

**Goals:**
- **Standardize `/insights` Filter Bar:** Deliver a uniform pure-text filter row (`[ Tất Cả Chủ Đề ]` `[ .NET & C# ]` `[ ... ]` `[ Đã Lưu ]`) by stripping component icons and emoji prefixes.
- **Dynamic Category & Topic Metadata:** Provide a CQRS query endpoint `GET /api/v1/insights/meta` that dynamically aggregates category metadata (including active insight counts) and curates topic suggestions from the 30-Day Curriculum (`Topics` table).
- **Persistent SM-2 State on Highlights:** Expose `bool HasFlashcard` on `HighlightDto` populated via an efficient batch lookup in `GetHighlightsHandler`.
- **F5-Resilient Frontend State:** Ensure `frontend/pages/notes.vue` populates `createdCardHighlightIds` on mount and renders a disabled green `<Check />` `In SM-2` button.
- **Comprehensive Lifecycle Testing:** Add unit tests in `GetHighlightsHandlerTests.cs` (backend) and `notes.spec.ts` (frontend) verifying state persistence.

**Non-Goals:**
- Modifying the underlying SM-2 algorithm, intervals, or ease factor calculations.
- Altering the card generation logic in `CreateCardFromHighlightHandler`.
- Implementing pagination or server-side filtering on `/notes` (which remains an instant client-filtered highlight hub).
- Enabling unlinking or direct deletion of flashcards from `/notes` (flashcard deletion is managed under `/review`).

---

## Decisions

### 1. Pure-Text Filter Bar Standardization (`/insights`)

To achieve a clean, distraction-free visual aesthetic:
- Remove `<Bookmark class="w-3.5 h-3.5 ..." />` from the saved filter button in `frontend/pages/insights.vue`.
- Update `frontend/i18n/locales/vi.json` and `frontend/i18n/locales/en.json`:
  ```json
  "saved_tab": "Đã Lưu"  // was "🔖 Đã Lưu"
  "saved_tab": "Saved"   // was "🔖 Saved"
  ```
- Retain the active highlight styling: when active, the saved chip uses `bg-indigo-600 text-white dark:bg-indigo-500` without requiring an icon to convey state.

```
Before: [ Tất Cả Chủ Đề ] [ Frontend & Vue ] [ .NET & C# ] [ <Bookmark> 🔖 Đã Lưu ]
After:  [ Tất Cả Chủ Đề ] [ Frontend & Vue ] [ .NET & C# ] [ Đã Lưu ]
```

### 2. Dynamic Insights Metadata Contract (`GET /api/v1/insights/meta`)

Instead of hardcoding category titles and topic suggestion pools in Vue, the backend exposes:

```csharp
// DTOs
public record InsightCategoryMetaDto(
    int Id,
    string Key,
    string LabelEn,
    string LabelVi,
    int Count
);

public record GetInsightsMetaResponse(
    List<InsightCategoryMetaDto> Categories,
    Dictionary<int, List<string>> SuggestedTopics
);
```

**Endpoint Implementation Details:**
- Route: `GET /api/v1/insights/meta` mapped in `InsightsEndpoints.cs`.
- Handled by `GetInsightsMetaHandler` (`IUseCase<GetInsightsMetaRequest, GetInsightsMetaResponse>`).
- **Category Metadata Assembly:**
  - Iterates through the canonical `Category` enum values (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`).
  - Queries `_dbContext.TechInsights.Where(i => i.IsPublished).GroupBy(i => i.Category).Select(...)` to compute published insight counts per category.
  - Maps to localized labels:
    - 0 (`FrontendWeb`): "Frontend & Vue" / "Frontend & Vue"
    - 1 (`BackendDotNet`): ".NET & C#" / ".NET & C#"
    - 2 (`DatabaseStorage`): "Postgres & DB" / "Postgres & DB"
    - 3 (`SystemDesign`): "System Design" / "Thiết Kế Hệ Thống"
- **Curated Topic Extraction:**
  - Queries active `Topics` (from the 30-Day Curriculum) ordered by `DayOrder`.
  - Groups topics by `(int)t.Category` and extracts distinct topic titles.
  - If a category has fewer than 2 topics in the database, merges with sensible defaults to guarantee rich inspiration chips in the AI generation modal.

**Frontend Store & Component Integration:**
- `useInsightsStore.ts` introduces:
  - `categoryMetadata = ref<InsightCategoryMetaDto[]>([])`
  - `suggestedTopics = ref<Record<number, string[]>>({})`
  - `fetchMetadata()`: Calls `GET /api/v1/insights/meta` on store initialization or page mount.
- `insights.vue`:
  - Replaces hardcoded `categories` with a computed array that prepends `{ id: null, label: 'insights.all_categories' }` to the dynamic category metadata.
  - Derives `currentSuggestedTopics` from `insightsStore.suggestedTopics[insightsStore.selectedCategory]` with a fallback array.

### 3. Persistent Flashcard Association (`HasFlashcard` Mapping)

**The N+1 Query Prevention Decision:**
In `GetHighlightsHandler.cs`, when fetching highlights for `request.UserId`, querying whether each highlight has a flashcard individually would cause an $N+1$ query performance trap.

Instead, we perform a single batched query:
```csharp
// 1. Fetch user highlights
var list = await query.ToListAsync(cancellationToken);

// 2. Batched fetch of all highlight IDs linked to cards for this user
var cardHighlightIds = await _dbContext.SpacedRepetitionCards
    .Where(c => c.UserId == request.UserId && c.SourceHighlightId != null)
    .Select(c => c.SourceHighlightId!.Value)
    .ToHashSetAsync(cancellationToken);

// 3. Project to DTO with O(1) in-memory check
var dtos = list.Select(h => new HighlightDto
{
    Id = h.Id,
    DocumentChunkId = h.DocumentChunkId,
    ChapterTitle = h.DocumentChunk?.ChapterTitle ?? "Reading Slice",
    BookTitle = h.DocumentChunk?.DocumentBook?.Title ?? "Core Curriculum",
    SelectedText = h.SelectedText,
    Note = h.Note,
    Tags = h.Tags,
    CreatedAt = h.CreatedAt,
    HasFlashcard = cardHighlightIds.Contains(h.Id)
}).ToList();
```

**Complexity Analysis:**
- Database Query: $1$ query on `UserHighlights` + $1$ indexed query on `SpacedRepetitionCards` filtered by `UserId`. Total queries = $2$.
- In-memory Mapping: $O(N)$ where $N$ is highlight count, backed by $O(1)$ `HashSet<Guid>.Contains` lookups.
- Space Complexity: $O(K)$ where $K$ is the number of active highlight-linked cards for the user.

### 4. Frontend State Hydration & UI Representation (`/notes`)

**Store Interface Update:**
```typescript
// frontend/stores/useNotesStore.ts
export interface Highlight {
  id: string
  documentChunkId: string
  chapterTitle: string
  bookTitle: string
  selectedText: string
  note?: string
  tags: string[]
  createdAt: string
  hasFlashcard?: boolean // New property from backend
}
```

**Component Hydration in `frontend/pages/notes.vue`:**
```typescript
function syncCreatedCardHighlightIds() {
  createdCardHighlightIds.value = new Set(
    notesStore.highlights
      .filter((h) => h.hasFlashcard)
      .map((h) => h.id)
  )
}

onMounted(async () => {
  await notesStore.fetchHighlights()
  syncCreatedCardHighlightIds()
})
```

**Button State & Visual Feedback:**
```vue
<button
  @click="handleCreateFlashcard(item.id)"
  :disabled="creatingCardHighlightId === item.id || createdCardHighlightIds.has(item.id)"
  class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 disabled:opacity-60"
  :class="createdCardHighlightIds.has(item.id)
    ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800 cursor-not-allowed'
    : 'bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 hover:bg-amber-100 dark:hover:bg-amber-900/50 border-amber-200 dark:border-amber-800/60'"
  :title="createdCardHighlightIds.has(item.id) ? $t('notes.in_sm2') : $t('notes.create_flashcard')"
>
  <Check v-if="createdCardHighlightIds.has(item.id)" class="w-3.5 h-3.5 text-emerald-500" />
  <Zap v-else class="w-3.5 h-3.5 text-amber-500" />
  <span class="hidden sm:inline">{{
    createdCardHighlightIds.has(item.id)
      ? $t('notes.in_sm2')
      : creatingCardHighlightId === item.id
        ? $t('notes.creating_card')
        : $t('notes.create_flashcard')
  }}</span>
</button>
```

**New Localization Keys:**
- `notes.in_sm2` in `frontend/i18n/locales/en.json`: `"in_sm2": "In SM-2"`
- `notes.in_sm2` in `frontend/i18n/locales/vi.json`: `"in_sm2": "Đã Trong SM-2"`

---

## Risks / Trade-offs

1. **Trade-off: Extra query in `GetHighlightsHandler` vs Eager Loading:**
   - *Consideration:* We could have added a direct navigation collection or foreign key property from `UserHighlight` to `SpacedRepetitionCard`.
   - *Decision:* The foreign key is currently on `SpacedRepetitionCard.SourceHighlightId` (pointing to `UserHighlight`). Querying `SpacedRepetitionCards` by `UserId` in a single indexed query avoids altering the database schema or creating a bidirectional EF Core navigation property that could complicate soft deletion cascades.
   - *Risk:* If a user has thousands of cards, `ToHashSetAsync` could consume memory.
   - *Mitigation:* The query filters strictly by `c.SourceHighlightId != null` and `c.UserId == request.UserId`. An engineer typically has fewer than a few hundred highlight-derived cards, making memory footprint negligible (<5 KB).

2. **Risk: Dynamic topic suggestions returning empty for newly added categories:**
   - *Mitigation:* `GetInsightsMetaHandler` includes a fallback default list of curated software engineering topics if the database query yields fewer than two active topics for a given category.

3. **Risk: Timing of metadata fetch in `insights.vue` causing layout shift:**
   - *Mitigation:* `useInsightsStore` initializes with sensible category skeleton keys (`insights.all_categories`), and updates smoothly once `fetchMetadata()` settles without jumping or flickering.
