# Proposal: Dynamic Insights and Highlight Card Sync

## Why

TechDaily aims to provide senior engineers with a seamless, highly cohesive technical reading and spaced repetition retention workflow. However, usability feedback and developer inspection have identified two distinct UX friction points and an active state synchronization bug across `/insights` and `/notes`:

1. **Visual Inconsistency and Static Data Clutter in `/insights` Filter Bar:**
   - The category filter row mixes disparate visual conventions: standard category chips use clean pure text (`[ Tất Cả Chủ Đề ]`, `[ .NET & C# ]`, etc.), while the saved bookmarks chip renders an icon component (`<Bookmark>`) alongside an emoji-prefixed localization key (`"saved_tab": "🔖 Đã Lưu"` / `"🔖 Saved"`), resulting in an awkward double-bookmark indicator.
   - The categories array (`Frontend & Vue`, `.NET & C#`, `Postgres & DB`, `System Design`) and the AI topic inspiration pool (`suggestedTopicPool`) in `frontend/pages/insights.vue` are hardcoded directly into the client-side component code. This disconnects the interface from the actual `TechInsights` catalog and active `Topics` (30-Day Curriculum) in PostgreSQL, preventing newly seeded or modified curriculum domains from reflecting in the filter bar or AI generator modal.

2. **Persistent Flashcard SM-2 State Loss on `/notes` (F5 Reload Bug):**
   - On the reading highlights hub (`/notes`), engineers can convert a highlight into an active recall spaced repetition flashcard via the deliberate `[ ⚡ Flashcard SM-2 ]` button.
   - While the backend successfully creates and persists the `SpacedRepetitionCard` in PostgreSQL and the client button transitions to a green `In SM-2` disabled state, this indicator exists exclusively in temporary Vue component memory (`createdCardHighlightIds = ref<Set<string>>(new Set())`).
   - Upon page refresh (F5) or revisiting `/notes`, `fetchHighlights` retrieves `HighlightDto` objects which lack any indicator of flashcard creation (`HasFlashcard`). Consequently, the button immediately reverts to the amber `[ ⚡ Flashcard SM-2 ]` state. This creates confusion, makes engineers believe their card was lost, and encourages repeated redundant creation requests.

Resolving these issues standardizes the visual language of the insights feed to pure-text elegance, decouples topic and category suggestions from client code into database-driven metadata, and restores reliable, persistent state synchronization between reading notes and the SM-2 review deck.

---

## What Changes

We propose a targeted four-pillar synchronization and stabilization:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│               DYNAMIC INSIGHTS & HIGHLIGHT CARD SYNC                        │
│                                                                             │
│  Pillar 1: Filter Bar Pure-Text Standardization (/insights)                 │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ Pure-text chip row:                                                   │  │
│  │ [ Tất Cả Chủ Đề ]  [ .NET & C# ]  [ Postgres & DB ]  [ Đã Lưu ]       │  │
│  │ • Remove <Bookmark> icon from saved filter chip                       │  │
│  │ • Strip 🔖 emoji from saved_tab in vi.json and en.json                │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 2: Dynamic Category & AI Topic Metadata (/insights)                 │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Add GET /api/v1/insights/meta (GetInsightsMetaHandler)              │  │
│  │   - categories: [{ id, key, labelEn, labelVi, count }]                │  │
│  │   - suggestedTopics: grouped by Category from 30-Day Curriculum Topics│  │
│  │ • Remove hardcoded categories & suggestedTopicPool in insights.vue    │  │
│  │ • useInsightsStore dynamically fetches and populates chips & modal    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 3: Persistent Flashcard SM-2 State on Notes (/notes)                │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: Add bool HasFlashcard to HighlightDto                      │  │
│  │ • Backend: GetHighlightsHandler queries SpacedRepetitionCards for     │  │
│  │   user's SourceHighlightIds and sets HasFlashcard                     │  │
│  │ • Frontend: Add hasFlashcard?: boolean to Highlight in useNotesStore   │  │
│  │ • Frontend: notes.vue initializes createdCardHighlightIds from       │  │
│  │   hasFlashcard: true on mount/fetch                                   │  │
│  │ • Render disabled green <Check /> with notes.in_sm2 ("Đã Trong SM-2")  │  │
│  │ • Preserves state across browser refreshes (F5 bug fixed)             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 4: Comprehensive Lifecycle & Persistence Testing                    │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Backend: GetHighlightsHandlerTests.cs (HasFlashcard toggles true)   │  │
│  │ • Frontend: notes.spec.ts (verifies disabled In SM-2 state on reload) │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pillar 1: Filter Bar Pure-Text Standardization (`/insights`)
- Remove the `<Bookmark>` component icon from the saved filter chip in `frontend/pages/insights.vue`.
- Remove the `🔖` emoji prefix from `insights.saved_tab` in `frontend/i18n/locales/vi.json` (`"saved_tab": "Đã Lưu"`) and `frontend/i18n/locales/en.json` (`"saved_tab": "Saved"`).
- Standardize all filter chips across the insights page to 100% pure text, creating a sleek, uniform visual baseline: `[ Tất Cả Chủ Đề ]`, `[ Frontend & Vue ]`, `[ .NET & C# ]`, `[ Postgres & DB ]`, `[ Thiết Kế Hệ Thống ]`, and `[ Đã Lưu ]`.

### Pillar 2: Dynamic Category Metadata and AI Topic Suggestions (`/insights`)
- Introduce a new backend metadata query endpoint: `GET /api/v1/insights/meta`.
  - Maps to `GetInsightsMetaHandler` implementing `IUseCase<GetInsightsMetaRequest, GetInsightsMetaResponse>`.
  - Returns `categories`: A dynamic list of category definitions `{ id: number, key: string, labelEn: string, labelVi: string, count: number }` derived from published records in the `TechInsights` table.
  - Returns `suggestedTopics`: A dictionary mapping category IDs to curated topic title suggestions extracted from active `Topics` (30-Day Senior Fullstack Curriculum) in the database.
- Update `frontend/stores/useInsightsStore.ts` to manage metadata loading (`fetchMetadata()`, `categoryMetadata`, `suggestedTopics`).
- Refactor `frontend/pages/insights.vue` to eliminate the hardcoded `categories` array and the hardcoded `suggestedTopicPool` object, dynamically deriving chips and modal inspiration suggestions from the store.

### Pillar 3: Persistent Flashcard SM-2 State on Notes (`/notes` - F5 Reload Fix)
- **Backend Model & Query Update:**
  - Add `public bool HasFlashcard { get; set; }` to `HighlightDto.cs`.
  - In `GetHighlightsHandler.cs`: Query `SpacedRepetitionCards` for records where `UserId == request.UserId && SourceHighlightId != null`. Extract the set of `SourceHighlightId` GUIDs into a `HashSet<Guid>`, and project `HasFlashcard = cardHighlightIds.Contains(h.Id)` on each returned `HighlightDto`.
- **Frontend Store & UI Update:**
  - Add `hasFlashcard?: boolean` to the `Highlight` interface in `frontend/stores/useNotesStore.ts`.
  - Add localized key `notes.in_sm2` ("In SM-2" in `en.json`, "Đã Trong SM-2" in `vi.json`).
  - In `frontend/pages/notes.vue`: Populate `createdCardHighlightIds` with all highlight IDs having `hasFlashcard: true` during `fetchHighlights` / `onMounted`.
  - Render the SM-2 button in disabled state when `createdCardHighlightIds.has(item.id)` is true, displaying `<Check class="w-3.5 h-3.5 text-emerald-500" />` and `$t('notes.in_sm2')` with emerald styling (`bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800`).
  - Ensure the state persists seamlessly when reloading (F5) or navigating between routes.

### Pillar 4: Lifecycle Persistence Testing
- **Backend Test Suite:** Add `backend/tests/TechDaily.Tests/Application/GetHighlightsHandlerTests.cs` using an in-memory SQLite database to verify:
  1. A highlight without an associated spaced repetition card returns `HasFlashcard = false`.
  2. Creating a `SpacedRepetitionCard` with `SourceHighlightId = highlight.Id` causes `GetHighlightsHandler` to return `HasFlashcard = true`.
  3. User isolation is strictly enforced: cards created by other users for the same highlight ID (or same chunk) do not set `HasFlashcard = true` for the querying user.
- **Frontend Test Suite:** Update `frontend/tests/pages/notes.spec.ts` to assert that when a highlight with `hasFlashcard: true` is provided in the mock API response, the highlight card immediately renders the disabled `In SM-2` button with the check icon upon page mount without requiring user interaction.

---

## Capabilities

### Modified Capabilities
- `insights`: Standardize filter bar on pure-text styling, provide dynamic category metadata and AI topic suggestions via `GET /api/v1/insights/meta`, and eliminate hardcoded category lists and static topic inspiration pools.
- `notes`: Ensure persistent SM-2 flashcard creation state across page reloads by exposing `HasFlashcard` on `HighlightDto` mapped from `SpacedRepetitionCards`, rendering the disabled `In SM-2` check badge on mount and fetch.

---

## Impact

- **Frontend Pages & Stores:**
  - `frontend/pages/insights.vue`: Removes `<Bookmark>` icon, eliminates hardcoded categories and topic pools, binds to dynamic store metadata.
  - `frontend/stores/useInsightsStore.ts`: Adds `fetchMetadata()`, `categoryMetadata`, `suggestedTopics`, and typed response interfaces.
  - `frontend/pages/notes.vue`: Initializes `createdCardHighlightIds` from `hasFlashcard: true`, switches icon to `<Check>` and label to `notes.in_sm2`.
  - `frontend/stores/useNotesStore.ts`: Updates `Highlight` interface with `hasFlashcard?: boolean`.
  - `frontend/i18n/locales/en.json` and `vi.json`: Updates `saved_tab` to remove `🔖` emoji; adds `notes.in_sm2`.
- **Backend Application & API:**
  - `backend/src/TechDaily.Api/Endpoints/InsightsEndpoints.cs`: Registers `GET /api/v1/insights/meta`.
  - `backend/src/TechDaily.Application/Features/Insights/DTOs/InsightDtos.cs`: Adds `InsightCategoryMetaDto`, `GetInsightsMetaRequest`, and `GetInsightsMetaResponse`.
  - `backend/src/TechDaily.Application/Features/Insights/GetInsightsMeta/GetInsightsMetaHandler.cs`: Implements category counting and topic suggestion extraction.
  - `backend/src/TechDaily.Application/Features/Notes/DTOs/HighlightDto.cs`: Adds `public bool HasFlashcard { get; set; }`.
  - `backend/src/TechDaily.Application/Features/Notes/GetHighlights/GetHighlightsHandler.cs`: Populates `HasFlashcard` using an efficient `HashSet<Guid>` lookup against `SpacedRepetitionCards`.
- **Automated Tests:**
  - `backend/tests/TechDaily.Tests/Application/GetHighlightsHandlerTests.cs`: New unit test covering `HasFlashcard` lifecycle and user scoping.
  - `frontend/tests/pages/notes.spec.ts`: New Vitest scenario for mounted `hasFlashcard: true` state.
